using Unity.Entities;
using Unity.Mathematics;

public struct ECSBoidData : IComponentData
{
    public int id;
    public float3 Position;
    public quaternion Rotation;
}