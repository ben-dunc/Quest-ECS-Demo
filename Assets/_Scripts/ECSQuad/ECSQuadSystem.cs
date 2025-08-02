using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ECSQuadSystem : ISystem
{
    public static int currentNumEntities;
    static int numEntitiesGoal;
    static int currentRow;
    static int currentColumn;
    EntityQuery destroyQuery;

    public void OnCreate(ref SystemState state)
    {
        currentNumEntities = 0;
        currentRow = 0;
        currentColumn = 0;

        destroyQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<ECSQuadData>()
        );

        state.RequireForUpdate(state.GetEntityQuery(ComponentType.ReadOnly<QuadPrefabHolder>()));
    }

    public void OnUpdate(ref SystemState state)
    {
        int maxNumChange = 100;
        numEntitiesGoal = ECSQuadManager.Instance.numEntities;
        if (numEntitiesGoal > currentNumEntities + maxNumChange)
            numEntitiesGoal = currentNumEntities + maxNumChange;
        if (numEntitiesGoal < currentNumEntities - maxNumChange)
            numEntitiesGoal = currentNumEntities - maxNumChange;

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged); // Deferred execution

        // create or destroy entities
        if (currentNumEntities != ECSQuadManager.Instance.numEntities)
        {
            Entity entityPrefab = Entity.Null;

            // Get prefab
            foreach (var prefabHolder in SystemAPI.Query<RefRO<QuadPrefabHolder>>())
            {
                if (prefabHolder.ValueRO.entityPrefab != null)
                {
                    entityPrefab = prefabHolder.ValueRO.entityPrefab;
                    break;
                }
            }

            if (entityPrefab != Entity.Null)
            {
                while (currentNumEntities < numEntitiesGoal)
                {
                    var instance = ecb.Instantiate(entityPrefab);
                    ecb.SetComponent(instance, new ECSQuadData()
                    {
                        id = currentNumEntities,
                        row = currentRow,
                        column = currentColumn
                    });

                    float3 pos = new();
                    pos.x = (currentColumn - (currentRow / 2f)) * ECSQuadManager.Instance.columnSize;
                    pos.z = currentRow * ECSQuadManager.Instance.rowSize;
                    pos.y = (pos.z * pos.z / ECSQuadManager.Instance.zToY) + 5f;

                    ecb.SetComponent(instance, new LocalTransform
                    {
                        Rotation = quaternion.identity,
                        Scale = 0.25f,
                        Position = pos
                    });

                    currentColumn++;
                    currentNumEntities++;

                    if (currentColumn > currentRow)
                    {
                        currentColumn = 0;
                        currentRow++;
                    }
                }
            }

            if (currentNumEntities > numEntitiesGoal)
            {
                var destroyJob = new DestroyQuadJob()
                {
                    numEntitiesGoal = numEntitiesGoal,
                    ecb = ecb.AsParallelWriter(),
                    entityHandle = state.GetEntityTypeHandle(),
                    QuadHandle = state.GetComponentTypeHandle<ECSQuadData>()
                };

                state.Dependency = destroyJob.Schedule(destroyQuery, state.Dependency);
                int difference = currentNumEntities - numEntitiesGoal;

                for (int i = 0; i < difference; i++)
                {
                    currentColumn--;
                    if (currentColumn < 0)
                    {
                        currentRow--;
                        currentColumn = currentRow;
                    }
                }

                currentNumEntities = numEntitiesGoal;
            }
        }
    }

    [BurstCompile]
    public partial struct DestroyQuadJob : IJobChunk
    {
        public int numEntitiesGoal;
        [ReadOnly] public EntityTypeHandle entityHandle;
        [ReadOnly] public ComponentTypeHandle<ECSQuadData> QuadHandle;
        public EntityCommandBuffer.ParallelWriter ecb;

        public void Execute(in Unity.Entities.ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(entityHandle);  // Get the entities in the chunk
            var Quads = chunk.GetNativeArray(ref QuadHandle);  // Get the entities in the chunk

            for (int i = 0; i < chunk.Count; i++)
            {
                if (Quads[i].id >= numEntitiesGoal)
                    ecb.DestroyEntity(unfilteredChunkIndex, entities[i]);
            }
        }
    }
}
