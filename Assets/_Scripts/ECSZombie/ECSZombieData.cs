using Unity.Entities;

public struct ECSZombieData : IComponentData
{
    public int id;
    public float rotateTimeRef;
    public float turningAngle;
    public float speed;
}