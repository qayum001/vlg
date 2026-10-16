using System;
using System.Numerics;
using System.Threading;
using Vlg.Core.Jobs;
using Vlg.Core.Sim;
using Xunit;

namespace Vlg.Core.Tests;

public class WorkerPoolTests
{
    /// <summary>Counts visits per index; a race or a missed/double chunk shows up as a count != 1.</summary>
    sealed class CountJob : IRangeJob
    {
        public int[] Hits = Array.Empty<int>();
        public int Calls;

        public void Execute(int start, int end)
        {
            Interlocked.Increment(ref Calls);
            for (int i = start; i < end; i++) Interlocked.Increment(ref Hits[i]);
        }
    }

    sealed class ThrowJob : IRangeJob
    {
        public void Execute(int start, int end)
        {
            if (start > 0) throw new InvalidOperationException("boom");
        }
    }

    [Theory]
    [InlineData(4, 0)]
    [InlineData(4, 1)]
    [InlineData(4, 100)]
    [InlineData(4, 4096)]
    [InlineData(4, 100_003)]
    [InlineData(0, 100_003)]
    public void Every_index_runs_exactly_once(int workers, int count)
    {
        using var pool = new WorkerPool(workers, minParallelCount: 1000, minChunkSize: 64);
        var job = new CountJob { Hits = new int[count] };
        pool.Run(job, count);

        Assert.All(job.Hits, h => Assert.Equal(1, h));
    }

    [Fact]
    public void Small_counts_run_inline_as_one_call()
    {
        using var pool = new WorkerPool(4, minParallelCount: 1000);
        var job = new CountJob { Hits = new int[999] };
        pool.Run(job, 999);

        Assert.Equal(1, job.Calls);
    }

    [Fact]
    public void Large_counts_are_split_across_chunks()
    {
        using var pool = new WorkerPool(3, minParallelCount: 10, minChunkSize: 10);
        var job = new CountJob { Hits = new int[10_000] };
        pool.Run(job, 10_000);

        Assert.True(job.Calls > 4, $"calls {job.Calls}");
    }

    [Fact]
    public void Many_back_to_back_runs_do_not_deadlock_or_mix()
    {
        using var pool = new WorkerPool(6, minParallelCount: 1, minChunkSize: 1);
        var job = new CountJob { Hits = new int[257] };
        for (int r = 0; r < 2000; r++) pool.Run(job, 257);

        Assert.All(job.Hits, h => Assert.Equal(2000, h));
    }

    [Fact]
    public void Worker_exception_reaches_the_caller_and_pool_stays_usable()
    {
        using var pool = new WorkerPool(4, minParallelCount: 1, minChunkSize: 10);
        Assert.Throws<InvalidOperationException>(() => pool.Run(new ThrowJob(), 1000));

        var job = new CountJob { Hits = new int[1000] };
        pool.Run(job, 1000);
        Assert.All(job.Hits, h => Assert.Equal(1, h));
    }

    [Fact]
    public void Run_does_not_allocate_per_call_on_the_calling_thread()
    {
        const int runs = 2000;
        using var pool = new WorkerPool(4, minParallelCount: 1, minChunkSize: 16);
        var job = new CountJob { Hits = new int[4096] };
        for (int r = 0; r < 50; r++) pool.Run(job, 4096);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int r = 0; r < runs; r++) pool.Run(job, 4096);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // ManualResetEventSlim lazily creates a 24 B lock object the first time Wait blocks (once).
        // Any per-call allocation would be >= runs * 24 B.
        Assert.True(allocated < 256, $"allocated {allocated} B over {runs} runs");
    }

    [Fact]
    public void Negative_worker_count_means_processors_minus_two()
    {
        using var pool = new WorkerPool(-1);
        Assert.Equal(Math.Max(0, Environment.ProcessorCount - 2), pool.WorkerCount);
    }

    [Fact]
    public void Threaded_horde_matches_single_thread_exactly()
    {
        var t = TestConfig.Sim;
        using var pool = new WorkerPool(4, minParallelCount: 64, minChunkSize: 32);
        var single = new HordeSim(t, 7) { SpawnMode = SpawnMode.Area };
        var threaded = new HordeSim(t, 7, pool) { SpawnMode = SpawnMode.Area };
        var aim = new Vector2(0, -1);
        for (int f = 0; f < 120; f++)
        {
            single.Step(1f / 60f, Vector2.Zero, aim, 3000, 1500);
            threaded.Step(1f / 60f, Vector2.Zero, aim, 3000, 1500);
        }

        Assert.Equal(single.KilledTotal, threaded.KilledTotal);
        Assert.Equal(single.Enemies.Count, threaded.Enemies.Count);
        for (int i = 0; i < single.Enemies.Count; i++)
        {
            Assert.Equal(single.Enemies.Position[i], threaded.Enemies.Position[i]);
            Assert.Equal(single.Enemies.Heading[i], threaded.Enemies.Heading[i]);
        }
    }
}
