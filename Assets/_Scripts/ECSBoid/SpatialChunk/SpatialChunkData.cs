using Unity.Entities;
using Unity.Mathematics;

public struct SpatialChunkData : IComponentData
{
    public float3 heading;
    public float3 position;
}