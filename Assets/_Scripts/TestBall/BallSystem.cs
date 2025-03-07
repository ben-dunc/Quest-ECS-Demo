using Unity.Entities;
using Unity.Transforms;

public partial struct BallSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BallData>();
    }

    public void OnUpdate(ref SystemState state)
    {
        BallJob job = new BallJob
        {
            deltaTime = SystemAPI.Time.DeltaTime
        };

        job.ScheduleParallel();
    }

    public partial struct BallJob : IJobEntity
    {
        public float deltaTime;

        public void Execute(ref BallData ball, ref LocalTransform transform)
        {
            transform = transform.Translate(ball.speed * deltaTime * ball.direction);
        }
    }
}
