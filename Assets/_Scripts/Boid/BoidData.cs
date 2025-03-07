using Unity.Entities;

public struct BoidData : IComponentData
{
    public int id;
    public bool isStatic;
    public bool isRepeller;
}