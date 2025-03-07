using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct SimpleECSSystem : ISystem
{
    public uint numEntities;

    public void OnCreate(ref SystemState state)
    {
        numEntities = 0;
        state.RequireForUpdate<SimpleECSData>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var job = new BobJob
        {
            frequency = 0.25f,
            zToY = 0.05f,
            time = Time.time
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        if (Input.GetKey(KeyCode.Space))
        {
            int numPrefabHolders = 0;
            // Get all Entities that have the component with the Entity reference
            foreach (var prefab in SystemAPI.Query<RefRO<EntityPrefabHolder>>())
            {
                numPrefabHolders++;
                // Instantiate the prefab Entity
                var instance = ecb.Instantiate(prefab.ValueRO.entityPrefab);
                ecb.SetComponent(instance, new LocalTransform
                {
                    Position = new float3(0, 5, 10)
                });
            }
            Debug.Log($"numPrefabHolders: {numPrefabHolders}");

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    public partial struct BobJob : IJobEntity
    {
        public float frequency;
        public float zToY;
        public float time;

        public void Execute(ref LocalTransform transform, in SimpleECSData simpleECSData)
        {
            // get z coordinate & calculate y coordinate
            float3 pos = transform.Position;
            pos.y = math.sin(time + pos.z) + (pos.z * zToY) + 5f;

            // apply to y transform
            transform.Position = pos;
        }
    }
}
