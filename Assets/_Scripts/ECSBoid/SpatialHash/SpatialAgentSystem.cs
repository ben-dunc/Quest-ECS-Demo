using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct SpatialAgentSystem : ISystem
{
    EntityQuery sadQuery;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SpatialAgentData>();
        sadQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<SpatialAgentData>()
        );
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged); // Deferred execution

        // determine which agents need to be updated
        var job = new DetermineAgentJob
        {
            chunkSize = SpatialHashManager.Instance.chunkSize,
            ecb = ecb.AsParallelWriter()
        };
        state.Dependency = job.ScheduleParallel(state.Dependency);
        // state.Dependency.Complete(); // Ensure job finishes before modifying data
    }

    public partial struct DetermineAgentJob : IJobEntity
    {
        public uint chunkSize;
        public EntityCommandBuffer.ParallelWriter ecb;

        public void Execute(Entity entity, in LocalTransform transform, in SpatialAgentData sad)
        {
            // update chunk
            int3 newChunk = new int3(
                (int)(transform.Position.x / chunkSize),
                (int)(transform.Position.y / chunkSize),
                (int)(transform.Position.z / chunkSize)
            );

            if (math.any(newChunk != sad.chunk))
                ecb.SetSharedComponent(0, entity, new SpatialAgentData { chunk = newChunk });
        }
    }
}
