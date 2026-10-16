namespace Vlg.Core.Jobs;

/// <summary>
/// Work over an index range [start, end). Called concurrently on disjoint ranges, so it may only write
/// data owned by its own indices. Implement as a cached class instance: no allocation per run.
/// </summary>
public interface IRangeJob
{
    void Execute(int start, int end);
}
