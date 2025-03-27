using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ECSBoidSpawnerSystem : ISystem
{
    public static int currentNumEntities;
    EntityQuery destroyQuery;
    Unity.Mathematics.Random rand;

    public void OnCreate(ref SystemState state)
    {
        currentNumEntities = 0;

        destroyQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<ECSBoidData>()
        );
        rand = new Unity.Mathematics.Random(41);

        state.RequireForUpdate(state.GetEntityQuery(ComponentType.ReadOnly<ECSBoidPrefabData>()));
    }

    public void OnUpdate(ref SystemState state)
    {
        int maxNumChange = 100;
        int numEntitiesGoal = ECSBoidManager.Instance.numBoids;
        if (numEntitiesGoal > currentNumEntities + maxNumChange)
            numEntitiesGoal = currentNumEntities + maxNumChange;
        if (numEntitiesGoal < currentNumEntities - maxNumChange)
            numEntitiesGoal = currentNumEntities - maxNumChange;

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged); // Deferred execution

        // create or destroy entities
        if (currentNumEntities != numEntitiesGoal)
        {
            Entity entityPrefab = Entity.Null;

            // Get prefab
            foreach (var prefabHolder in SystemAPI.Query<RefRO<ECSBoidPrefabData>>())
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
                    if (instance != Entity.Null)
                    {
                        // boid data
                        ecb.SetComponent(instance, new ECSBoidData()
                        {
                            id = currentNumEntities
                        });

                        // transform
                        var trans = new LocalTransform
                        {
                            Rotation = quaternion.identity,
                            Scale = 1f,
                            Position = ECSBoidManager.Instance.transform.position
                        };
                        trans = trans.RotateX(rand.NextFloat(-15f, 15f));
                        trans = trans.RotateY(rand.NextFloat(-180f, 180));
                        trans = trans.RotateZ(rand.NextFloat(-15f, 15f));
                        trans.Position += new float3(rand.NextFloat3(-5f, 5f));

                        ecb.SetComponent(instance, trans);

                        // increment num entities
                        currentNumEntities++;
                    }
                }
            }

            if (currentNumEntities > numEntitiesGoal)
            {
                var destroyJob = new ECSDestroyBoidJob()
                {
                    numEntitiesGoal = numEntitiesGoal,
                    ecb = ecb.AsParallelWriter(),
                    entityHandle = state.GetEntityTypeHandle(),
                    boidHandle = state.GetComponentTypeHandle<ECSBoidData>()
                };

                state.Dependency = destroyJob.Schedule(destroyQuery, state.Dependency);
                currentNumEntities = numEntitiesGoal;
            }
        }
    }

    public partial struct ECSDestroyBoidJob : IJobChunk
    {
        public int numEntitiesGoal;
        [ReadOnly] public EntityTypeHandle entityHandle;
        [ReadOnly] public ComponentTypeHandle<ECSBoidData> boidHandle;
        public EntityCommandBuffer.ParallelWriter ecb;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(entityHandle);  // Get the entities in the chunk
            var boids = chunk.GetNativeArray(ref boidHandle);  // Get the entities in the chunk

            for (int i = 0; i < chunk.Count; i++)
            {
                if (boids[i].id > numEntitiesGoal)
                    ecb.DestroyEntity(unfilteredChunkIndex, entities[i]);
            }
        }
    }

}
