using Unity.Entities;

public struct ZombieData : IComponentData
{
    public int id;
    public float rotateTimeRef;
    public float turningAngle;
    public float speed;
}