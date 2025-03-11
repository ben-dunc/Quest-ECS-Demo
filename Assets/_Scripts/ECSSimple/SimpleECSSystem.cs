using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct SimpleECSSystem : ISystem
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
            ComponentType.ReadOnly<SimpleECSData>()
        );

        state.RequireForUpdate(state.GetEntityQuery(ComponentType.ReadOnly<EntityPrefabHolder>()));
    }

    public void OnUpdate(ref SystemState state)
    {
        int maxNumChange = 100;
        numEntitiesGoal = SimpleECSManager.Instance.numEntities;
        if (numEntitiesGoal > currentNumEntities + maxNumChange)
            numEntitiesGoal = currentNumEntities + maxNumChange;
        if (numEntitiesGoal < currentNumEntities - maxNumChange)
            numEntitiesGoal = currentNumEntities - maxNumChange;

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged); // Deferred execution

        // create or destroy entities
        if (currentNumEntities != SimpleECSManager.Instance.numEntities)
        {
            Entity entityPrefab = Entity.Null;

            // Get prefab
            foreach (var prefabHolder in SystemAPI.Query<RefRO<EntityPrefabHolder>>())
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
                    ecb.SetComponent(instance, new SimpleECSData()
                    {
                        id = currentNumEntities,
                        row = currentRow,
                        column = currentColumn
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
                var destroyJob = new DestroySimpleJob()
                {
                    numEntitiesGoal = numEntitiesGoal,
                    ecb = ecb.AsParallelWriter(),
                    entityHandle = state.GetEntityTypeHandle(),
                    simpleHandle = state.GetComponentTypeHandle<SimpleECSData>()
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

        // update simple entities' position
        var job = new BobJob
        {
            frequency = SimpleECSManager.Instance.frequency,
            magnitude = SimpleECSManager.Instance.magnitude,
            zToY = SimpleECSManager.Instance.zToY,
            columnSize = SimpleECSManager.Instance.columnSize,
            rowSize = SimpleECSManager.Instance.rowSize,
            time = Time.time,
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);
    }

    public partial struct BobJob : IJobEntity
    {
        public float frequency;
        public float magnitude;
        public float zToY;
        public float rowSize;
        public float columnSize;
        public float time;

        public void Execute(ref LocalTransform transform, in SimpleECSData simpleECSData)
        {
            float3 pos = transform.Position;
            pos.x = (simpleECSData.column - (simpleECSData.row / 2f)) * columnSize;
            pos.z = simpleECSData.row * rowSize;
            pos.y = (math.sin(time + pos.z) * magnitude) + (pos.z * zToY) + 5f;

            transform.Position = pos;
        }
    }

    public partial struct DestroySimpleJob : IJobChunk
    {
        public int numEntitiesGoal;
        [ReadOnly] public EntityTypeHandle entityHandle;
        [ReadOnly] public ComponentTypeHandle<SimpleECSData> simpleHandle;
        public EntityCommandBuffer.ParallelWriter ecb;

        public void Execute(in Unity.Entities.ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(entityHandle);  // Get the entities in the chunk
            var simples = chunk.GetNativeArray(ref simpleHandle);  // Get the entities in the chunk

            for (int i = 0; i < chunk.Count; i++)
            {
                if (simples[i].id > numEntitiesGoal)
                    ecb.DestroyEntity(unfilteredChunkIndex, entities[i]);
            }
        }
    }
}
