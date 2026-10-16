using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Vlg.Core.Jobs;

/// <summary>
/// Fixed background threads for data-parallel loops (hordes). <see cref="Run"/> splits [0, count) into
/// chunks taken by the workers and the calling thread, and returns when all are done.
/// Allocation-free per run. Not reentrant: one Run at a time, from one thread.
/// </summary>
public sealed class WorkerPool : IDisposable
{
    /// <summary>Chunks per participating thread, for load balance when some ranges are denser.</summary>
    const int ChunksPerThread = 4;

    readonly Thread[] _threads;
    readonly SemaphoreSlim _start = new(0);
    readonly ManualResetEventSlim _done = new(false);
    readonly int _minParallelCount;
    readonly int _minChunkSize;

    IRangeJob? _job;
    int _count;
    int _chunkSize;
    int _chunkCount;
    int _nextChunk;
    int _remaining;
    volatile bool _stopping;
    Exception? _error;

    /// <summary>Background threads; 0 = everything runs on the caller.</summary>
    public int WorkerCount => _threads.Length;

    /// <param name="workers">Background threads. Negative = processors - 2 (room for engine threads), at least 0.</param>
    /// <param name="minParallelCount">Below this count Run executes inline: threading costs more than it saves.</param>
    public WorkerPool(int workers, int minParallelCount = 2048, int minChunkSize = 256)
    {
        if (workers < 0) workers = Math.Max(0, Environment.ProcessorCount - 2);
        _minParallelCount = Math.Max(1, minParallelCount);
        _minChunkSize = Math.Max(1, minChunkSize);
        _threads = new Thread[workers];
        for (int i = 0; i < workers; i++)
        {
            _threads[i] = new Thread(WorkerLoop) { IsBackground = true, Name = $"vlg-worker-{i}" };
            _threads[i].Start();
        }
    }

    public void Run(IRangeJob job, int count)
    {
        if (count <= 0) return;
        if (_threads.Length == 0 || count < _minParallelCount)
        {
            job.Execute(0, count);
            return;
        }

        int participants = _threads.Length + 1;
        _chunkSize = Math.Max(_minChunkSize, (count + participants * ChunksPerThread - 1) / (participants * ChunksPerThread));
        _chunkCount = (count + _chunkSize - 1) / _chunkSize;
        _count = count;
        _job = job;
        _error = null;
        _nextChunk = -1;
        _remaining = _threads.Length;
        _done.Reset();
        // Writes above are published by the semaphore release (full fence).
        _start.Release(_threads.Length);

        try
        {
            Work();
        }
        catch (Exception e)
        {
            Interlocked.CompareExchange(ref _error, e, null);
        }
        // Always wait: workers must not still run this job when Run returns or throws.
        _done.Wait();
        _job = null;
        if (_error != null) ExceptionDispatchInfo.Capture(_error).Throw();
    }

    void WorkerLoop()
    {
        while (true)
        {
            _start.Wait();
            if (_stopping) return;
            try
            {
                Work();
            }
            catch (Exception e)
            {
                Interlocked.CompareExchange(ref _error, e, null);
            }
            // Each permit consumed = one decrement, so done fires after every woken worker finished.
            if (Interlocked.Decrement(ref _remaining) == 0) _done.Set();
        }
    }

    void Work()
    {
        var job = _job!;
        while (true)
        {
            int chunk = Interlocked.Increment(ref _nextChunk);
            if (chunk >= _chunkCount) return;
            int start = chunk * _chunkSize;
            job.Execute(start, Math.Min(start + _chunkSize, _count));
        }
    }

    public void Dispose()
    {
        if (_stopping) return;
        _stopping = true;
        if (_threads.Length > 0) _start.Release(_threads.Length);
        foreach (var t in _threads) t.Join();
        _start.Dispose();
        _done.Dispose();
    }
}
