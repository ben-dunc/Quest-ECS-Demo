using Unity.Entities;
using Unity.Mathematics;

public struct ECSBoidZoneData : ISharedComponentData
{
    public int3 zone;
}