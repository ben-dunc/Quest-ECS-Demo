using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public struct BallData : IComponentData
{
    public float speed;
    public float3 direction;
}
