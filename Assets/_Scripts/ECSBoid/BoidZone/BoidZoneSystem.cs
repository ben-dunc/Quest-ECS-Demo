using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct SpatialAgentSystem : ISystem
{
    EntityQuery sadQuery;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ECSBoidZoneData>();
        sadQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<ECSBoidZoneData>()
        );
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged); // Deferred execution

        // determine which agents need to be updated
        var job = new SetAgentZoneJob
        {
            chunkSize = SpatialHashManager.Instance.chunkSize,
            ecb = ecb.AsParallelWriter()
        };
        state.Dependency = job.ScheduleParallel(state.Dependency);
    }

    public partial struct SetAgentZoneJob : IJobEntity
    {
        public uint chunkSize;
        public EntityCommandBuffer.ParallelWriter ecb;

        public void Execute(Entity entity, in LocalTransform transform, in ECSBoidZoneData sad)
        {
            // update chunk
            int3 newZone = new int3(
                (int)(transform.Position.x / chunkSize),
                (int)(transform.Position.y / chunkSize),
                (int)(transform.Position.z / chunkSize)
            );

            if (math.any(newZone != sad.zone))
                ecb.SetSharedComponent(0, entity, new ECSBoidZoneData { zone = newZone });
        }
    }
}
