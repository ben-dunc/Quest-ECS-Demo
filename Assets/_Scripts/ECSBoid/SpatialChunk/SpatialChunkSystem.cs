using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst.Intrinsics;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct SpatialChunkSystem : ISystem
{
    EntityQuery query;

    public void OnCreate(ref SystemState state)
    {
        query = state.GetEntityQuery(
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<BoidData>(),
            ComponentType.ReadOnly<SpatialAgentData>()
        );
    }

    public void OnUpdate(ref SystemState state)
    {
        var job = new UpdateChunkDataJob
        {
            transformHandle = state.GetComponentTypeHandle<LocalTransform>(true),
            boidHandle = state.GetComponentTypeHandle<BoidData>(true),
            chunkDataHandle = state.GetComponentTypeHandle<SpatialChunkData>(false),
        };
        state.Dependency = job.ScheduleParallel(query, state.Dependency);
    }
}

public struct UpdateChunkDataJob : IJobChunk
{
    [ReadOnly] public ComponentTypeHandle<LocalTransform> transformHandle;
    [ReadOnly] public ComponentTypeHandle<BoidData> boidHandle;
    public ComponentTypeHandle<SpatialChunkData> chunkDataHandle;

    public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
    {
        int count = chunk.Count;

        // determine if this chunk has the SpatialChunkData
        if (chunk.HasChunkComponent(ref chunkDataHandle))
        {
            // if there are entities in this chunk, update values
            float3 heading = float3.zero;
            float3 position = float3.zero;

            if (count > 0)
            {
                var transforms = chunk.GetNativeArray(ref transformHandle);
                var boids = chunk.GetNativeArray(ref boidHandle);

                for (int i = 0; i < count; i++)
                {
                    if (!boids[i].isStatic)
                    {
                        heading += math.forward(transforms[i].Rotation);
                        position += transforms[i].Position;
                    }
                }

                heading /= count;
                position /= count;
            }
            else
            {
                heading = math.NAN;
                position = math.NAN;
            }

            // Set chunk component data
            chunk.SetChunkComponentData(ref chunkDataHandle, new SpatialChunkData
            {
                heading = heading,
                position = position
            });
        }
        else
        {
            Debug.Log("Chunk doesn't contain the chunk guy I want!");
        }
    }
}