using System.Linq;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.Profiling;

public partial struct ECSBoidSystem : ISystem
{
    EntityQuery boidQuery;
    Unity.Mathematics.Random rand;
    SharedComponentTypeHandle<ECSBoidZoneData> spatialAgentDataHandle;
    ComponentTypeHandle<LocalTransform> transformHandleRW;
    ComponentTypeHandle<ECSBoidData> boidHandleRO;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ECSBoidData>();

        rand = new Unity.Mathematics.Random(88);
        boidQuery = state.GetEntityQuery(
            ComponentType.ReadWrite<LocalTransform>(),
            ComponentType.ReadOnly<ECSBoidZoneData>(),
            ComponentType.ReadOnly<ECSBoidData>()
        );

        transformHandleRW = state.GetComponentTypeHandle<LocalTransform>(false);
        boidHandleRO = state.GetComponentTypeHandle<ECSBoidData>(true);
        spatialAgentDataHandle = state.GetSharedComponentTypeHandle<ECSBoidZoneData>();
    }


    public void OnUpdate(ref SystemState state)
    {
        // handle updates
        spatialAgentDataHandle.Update(ref state);
        transformHandleRW.Update(ref state);
        boidHandleRO.Update(ref state);

        // chunks
        NativeArray<ArchetypeChunk> archChunks = boidQuery.ToArchetypeChunkArray(Allocator.TempJob);
        int numChunks = archChunks.Count();

        // data
        NativeArray<ECSBoidZoneData> spatialAgentArray = new(numChunks, Allocator.TempJob);

        for (int i = 0; i < numChunks; i++)
        {
            if (archChunks[i] != ArchetypeChunk.Null && !archChunks[i].Invalid() && archChunks[i].Archetype.Valid && archChunks[i].Capacity > 0)
            {
                // assign agent data
                if (archChunks[i].NumSharedComponents() > 0)
                    spatialAgentArray[i] = archChunks[i].GetSharedComponent(spatialAgentDataHandle);
                else
                    spatialAgentArray[i] = new() { zone = new(-999, -999, -999) };
            }
        }

        // execute job
        BoidJob job = new()
        {
            maxNumNeighborCheck = ECSBoidManager.Instance.maxNumNeighborCheck,
            deltaTime = SystemAPI.Time.DeltaTime * ECSBoidManager.Instance.simSpeed,
            boidSpeed = ECSBoidManager.Instance.boidSpeed,
            separationDistance = ECSBoidManager.Instance.separationDistance,
            separationStrength = ECSBoidManager.Instance.separationStrength,
            alignmentDistance = ECSBoidManager.Instance.alignmentDistance,
            alignmentStrength = ECSBoidManager.Instance.alignmentStrength,
            cohesionDistance = ECSBoidManager.Instance.cohesionDistance,
            cohesionStrength = ECSBoidManager.Instance.cohesionStrength,
            edgeRepellerDistance = ECSBoidManager.Instance.edgeRepellerDistance,
            edgeRepellerStrength = ECSBoidManager.Instance.edgeRepellerStrength,
            spatialHashSize = SpatialHashManager.Instance.spatialHashSize,
            spatialHashPosition = SpatialHashManager.Instance.spatialHashPosition,

            // handles
            transformHandleRW = transformHandleRW,
            boidHandleRO = boidHandleRO,
            spatialAgentHandle = spatialAgentDataHandle,

            // data
            spatialAgentArray = spatialAgentArray,
        };

        state.Dependency = job.ScheduleParallel(boidQuery, state.Dependency);

        ECSUpdatePositionBoid updatePos = new();
        state.Dependency = updatePos.ScheduleParallel(state.Dependency);
    }

    public partial struct BoidJob : IJobChunk
    {
        public int maxNumNeighborCheck;
        public float deltaTime;
        public float boidSpeed;
        public float separationDistance;
        public float separationStrength;
        public float alignmentDistance;
        public float alignmentStrength;
        public float cohesionDistance;
        public float cohesionStrength;
        public float edgeRepellerDistance;
        public float edgeRepellerStrength;
        public uint spatialHashSize;
        public float3 spatialHashPosition;

        // handles
        public ComponentTypeHandle<LocalTransform> transformHandleRW;
        [ReadOnly] public ComponentTypeHandle<ECSBoidData> boidHandleRO;
        [ReadOnly] public SharedComponentTypeHandle<ECSBoidZoneData> spatialAgentHandle;

        // data
        [ReadOnly] public NativeArray<ECSBoidZoneData> spatialAgentArray;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeArray<float3> neighborPositions = new(maxNumNeighborCheck, Allocator.Temp);
            NativeArray<quaternion> neighborRotations = new(maxNumNeighborCheck, Allocator.Temp);

            // get boids in this chunk & iterate through neighbors
            ExecuteBoidAlgorithm(in chunk);

            // garbage cleanup
            neighborPositions.Dispose();
            neighborRotations.Dispose();
        }

        void ExecuteBoidAlgorithm(in ArchetypeChunk chunk)
        {
            Profiler.BeginSample("ExecuteBoidAlgorithm");
            var transforms = chunk.GetNativeArray(ref transformHandleRW);
            int numBoids = transforms.Count();
            int numNeighbors = math.min(numBoids, maxNumNeighborCheck);

            for (int i = 0; i < numBoids; i++)
            {
                var transform = transforms[i];
                var heading = math.forward(transform.Rotation);

                float3 separation = float3.zero;
                float3 alignment = float3.zero;
                float3 cohesion = float3.zero;

                for (int j = 0; j < numNeighbors; j++)
                {
                    if (math.all(transform.Position != transforms[j].Position))
                    {
                        float3 diff = transform.Position - transforms[j].Position;
                        float3 diffNorm = math.normalize(diff);
                        float3 nh = math.forward(transforms[j].Rotation);
                        float distance = math.length(diff);
                        separation += math.select(
                            float3.zero,
                            diffNorm * (1f - (distance / separationDistance)),
                            distance < separationDistance
                        );
                        alignment += math.select(
                            float3.zero,
                            nh * (1f - (distance / alignmentDistance)),
                            distance < alignmentDistance
                        );
                        cohesion += math.select(
                            float3.zero,
                            -diffNorm * (1f - (distance / cohesionDistance)),
                            distance < cohesionDistance
                        );
                    }
                }

                alignment /= numNeighbors;
                cohesion /= numNeighbors;

                // avoid edges of spatial hash size
                bool3 outside = math.abs(transform.Position - spatialHashPosition) > (spatialHashSize / 2) - edgeRepellerDistance;
                heading += (spatialHashPosition - transform.Position) * (float3)outside * edgeRepellerStrength;

                // add everything up
                heading += separation * separationStrength;
                heading += alignment * alignmentStrength;
                heading += cohesion * cohesionStrength;

                heading = math.normalize(heading);

                if (heading.x == math.NAN)
                    UnityEngine.Debug.LogError("NAN!");

                // update position & rotation
                transform.Position += boidSpeed * deltaTime * heading;
                transform.Rotation = quaternion.LookRotation(heading, math.up());

                // assign position & rotation
                transforms[i] = transform;
            }
            Profiler.EndSample();
        }
    }

    public partial struct ECSUpdatePositionBoid : IJobEntity
    {
        public void Execute(ref ECSBoidData boid, in LocalTransform localTransform)
        {
            boid.Position = localTransform.Position;
            boid.Rotation = localTransform.Rotation;
        }
    }
}
