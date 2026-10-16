namespace Vlg.Core.Bench;

/// <summary>Sampled by the engine side at step end. MB.</summary>
public readonly record struct MemorySample(float VideoMb, float WorkingSetMb, float PrivateMb, float GcHeapMb);
