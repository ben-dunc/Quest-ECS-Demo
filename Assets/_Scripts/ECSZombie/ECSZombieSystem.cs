using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct ECSZombieSystem : ISystem
{
    Random rand;

    public void OnCreate(ref SystemState state)
    {
        rand = new Random(4814);
        state.RequireForUpdate<ECSZombieData>();
    }

    public void OnUpdate(ref SystemState state)
    {
        ZombieJob job = new()
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            rotateRate = ECSZombieManager.Instance.rotateRate,
            zombieRange = ECSZombieManager.Instance.zombieRange,
            zombieManagerPosition = (float3)ECSZombieManager.Instance.transform.position,
            rand = rand
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);
    }

    public partial struct ZombieJob : IJobEntity
    {
        public float deltaTime;
        public float rotateRate;
        public float zombieRange;
        public float3 zombieManagerPosition;
        public Random rand;

        public void Execute(ref LocalTransform transform, ref ECSZombieData zombieData)
        {
            transform.Position += math.forward(transform.Rotation) * zombieData.speed * deltaTime;

            if (math.length(transform.Position - zombieManagerPosition) > zombieRange)
            {
                transform.Position.x = (rand.NextFloat() * ECSZombieManager.Instance.zombieRange) - (ECSZombieManager.Instance.zombieRange / 2);
                transform.Position.z = (rand.NextFloat() * ECSZombieManager.Instance.zombieRange) - (ECSZombieManager.Instance.zombieRange / 2);
                transform.Position += zombieManagerPosition;
                transform.Position.y = 0;
            }

            transform = transform.RotateY(zombieData.turningAngle * deltaTime * rotateRate);
        }
    }
}
