using Unity.Entities;
using Unity.Mathematics;

public struct ArchetypeChunk : IComponentData
{
    public float3 heading;
    public float3 position;
}