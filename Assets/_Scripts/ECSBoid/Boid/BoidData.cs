using Unity.Entities;
using Unity.Mathematics;

public struct BoidData : IComponentData
{
    public int id;
    public bool isRepeller;
    public float3 Position;
    public quaternion Rotation;
}