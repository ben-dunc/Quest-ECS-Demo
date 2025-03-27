using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ECSZombieSpawnerSystem : ISystem
{
    public static int currentNumEntities;
    EntityQuery destroyQuery;
    Random rand;

    public void OnCreate(ref SystemState state)
    {
        currentNumEntities = 0;

        destroyQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<ECSZombieData>()
        );
        rand = new Random(4241);

        state.RequireForUpdate(state.GetEntityQuery(ComponentType.ReadOnly<ECSZombiePrefabData>()));
    }

    public void OnUpdate(ref SystemState state)
    {
        int maxNumChange = 100;
        int numEntitiesGoal = ECSZombieManager.Instance.numZombies;
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
            foreach (var prefabHolder in SystemAPI.Query<RefRO<ECSZombiePrefabData>>())
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
                    float rotateTimeRef = rand.NextFloat() * ECSZombieManager.Instance.rotateRate;
                    float turningAngle = (rand.NextFloat() * ECSZombieManager.Instance.rotateMaxMin * 2) - ECSZombieManager.Instance.rotateMaxMin;

                    ecb.SetComponent(instance, new ECSZombieData()
                    {
                        id = currentNumEntities,
                        rotateTimeRef = rotateTimeRef,
                        turningAngle = turningAngle,
                        speed = (rand.NextFloat() * ECSZombieManager.Instance.speed / 2) + (ECSZombieManager.Instance.speed / 2)
                    });

                    var trans = new LocalTransform();
                    trans.Position.x = (rand.NextFloat() * ECSZombieManager.Instance.zombieRange * 2) - (ECSZombieManager.Instance.zombieRange / 2);
                    trans.Position.z = (rand.NextFloat() * ECSZombieManager.Instance.zombieRange * 2) - (ECSZombieManager.Instance.zombieRange / 2);
                    trans.Position += (float3)ECSZombieManager.Instance.transform.position;
                    trans.Position.y = 0;
                    trans.Rotation = quaternion.identity;
                    trans.Scale = 1f;

                    trans = trans.RotateY(360f * rand.NextFloat());

                    ecb.SetComponent(instance, trans);

                    currentNumEntities++;
                }
            }

            if (currentNumEntities > numEntitiesGoal)
            {
                var destroyJob = new ECSDestroyZombieJob()
                {
                    numEntitiesGoal = numEntitiesGoal,
                    ecb = ecb.AsParallelWriter(),
                    entityHandle = state.GetEntityTypeHandle(),
                    ZombieHandle = state.GetComponentTypeHandle<ECSZombieData>()
                };

                state.Dependency = destroyJob.Schedule(destroyQuery, state.Dependency);
                currentNumEntities = numEntitiesGoal;
            }
        }
    }

    public partial struct ECSDestroyZombieJob : IJobChunk
    {
        public int numEntitiesGoal;
        [ReadOnly] public EntityTypeHandle entityHandle;
        [ReadOnly] public ComponentTypeHandle<ECSZombieData> ZombieHandle;
        public EntityCommandBuffer.ParallelWriter ecb;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(entityHandle);  // Get the entities in the chunk
            var Zombies = chunk.GetNativeArray(ref ZombieHandle);  // Get the entities in the chunk

            for (int i = 0; i < chunk.Count; i++)
            {
                if (Zombies[i].id > numEntitiesGoal)
                    ecb.DestroyEntity(unfilteredChunkIndex, entities[i]);
            }
        }
    }

}
