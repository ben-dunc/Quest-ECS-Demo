using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct SpatialAgentSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SpatialAgentData>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged); // Deferred execution

        NativeList<Entity> affectedEntities = new NativeList<Entity>(1000, Allocator.TempJob);
        NativeList<int3> newChunks = new NativeList<int3>(1000, Allocator.TempJob);

        // determine which boids need to move
        var job = new DetermineAgentChunkJob
        {
            chunkSize = SpatialHashManager.Instance.chunkSize,
            affectedEntities = affectedEntities.AsParallelWriter(),
            newChunks = newChunks.AsParallelWriter()
        };
        state.Dependency = job.ScheduleParallel(state.Dependency);
        state.Dependency.Complete(); // Ensure job finishes before modifying data

        // Schedule the structural changes via the EntityCommandBuffer (deferred execution)
        var applyJob = new ApplyAgentChunkJob
        {
            affectedEntities = affectedEntities,
            newChunks = newChunks,
            ecb = ecb.AsParallelWriter()
        };
        state.Dependency = applyJob.Schedule(state.Dependency);

        affectedEntities.Dispose(state.Dependency);
        newChunks.Dispose(state.Dependency);

        // state.Dependency.Complete();

        // assign chunk data to any chunks that don't have it
        var query = state.GetEntityQuery(
            ComponentType.ReadOnly<SpatialAgentData>(),
            ComponentType.ChunkComponentExclude<SpatialChunkData>()
        );
        var numChunks = query.CalculateChunkCount();
        if (numChunks > 0)
            Debug.Log($"Chunks assigned: {query.CalculateChunkCount()}");
        state.EntityManager.AddChunkComponentData(query, new SpatialChunkData());
    }

    [BurstCompile]
    public partial struct DetermineAgentChunkJob : IJobEntity
    {
        public uint chunkSize;
        public NativeList<Entity>.ParallelWriter affectedEntities;
        public NativeList<int3>.ParallelWriter newChunks;

        public void Execute(Entity entity, in LocalTransform transform)
        {
            // update chunk
            int3 newChunk = new int3(
                (int)(transform.Position.x / chunkSize),
                (int)(transform.Position.y / chunkSize),
                (int)(transform.Position.z / chunkSize)
            );

            affectedEntities.AddNoResize(entity);
            newChunks.AddNoResize(newChunk);
        }
    }

    [BurstCompile]
    public struct ApplyAgentChunkJob : IJob
    {
        public NativeList<Entity> affectedEntities;
        public NativeList<int3> newChunks;
        public EntityCommandBuffer.ParallelWriter ecb;

        public void Execute()
        {
            for (int i = 0; i < affectedEntities.Length; i++)
                ecb.SetSharedComponent(i, affectedEntities[i], new SpatialAgentData { chunk = newChunks[i] });
        }
    }
}
