using Unity.Entities;
using Unity.Mathematics;

public struct SpatialAgentData : ISharedComponentData
{
    public int3 chunk;
}