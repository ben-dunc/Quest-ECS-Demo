using System.Diagnostics;
using System.Linq;
using System.Security;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Profiling;

public partial struct BoidSystem : ISystem
{
    EntityQuery boidQuery;
    Unity.Mathematics.Random rand;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BoidData>();

        rand = new Unity.Mathematics.Random(88);
        boidQuery = state.GetEntityQuery(
            ComponentType.ReadWrite<LocalTransform>(),
            ComponentType.ReadOnly<SpatialAgentData>(),
            ComponentType.ReadOnly<BoidData>()
        );
    }

    public void OnUpdate(ref SystemState state)
    {
        // get all chunks
        NativeArray<ArchetypeChunk> archChunks = boidQuery.ToArchetypeChunkArray(Allocator.TempJob);
        int numChunks = archChunks.Count();
        NativeArray<SpatialAgentData> spatialAgentData = new(numChunks, Allocator.TempJob);
        var sharedAgentComponentHandle = state.GetSharedComponentTypeHandle<SpatialAgentData>();
        for (int i = 0; i < numChunks; i++)
        {
            if (archChunks[i] != ArchetypeChunk.Null && !archChunks[i].Invalid() && archChunks[i].Archetype.Valid && archChunks[i].Capacity > 0)
            {
                if (archChunks[i].NumSharedComponents() > 0)
                    spatialAgentData[i] = archChunks[i].GetSharedComponent(sharedAgentComponentHandle);
                else
                    UnityEngine.Debug.Log("Chunk didn't have shared data.");
            }
        }

        // execute job
        BoidJob job = new()
        {
            maxNumNeighborCheck = BoidManager.Instance.maxNumNeighborCheck,
            deltaTime = SystemAPI.Time.DeltaTime * BoidManager.Instance.simSpeed,
            boidSpeed = BoidManager.Instance.boidSpeed,
            boidRotateSpeed = BoidManager.Instance.boidRotateSpeed,
            separationDistance = BoidManager.Instance.separationDistance,
            separationStrength = BoidManager.Instance.separationStrength,
            alignmentDistance = BoidManager.Instance.alignmentDistance,
            alignmentStrength = BoidManager.Instance.alignmentStrength,
            cohesionDistance = BoidManager.Instance.cohesionDistance,
            cohesionStrength = BoidManager.Instance.cohesionStrength,
            repellerDistance = BoidManager.Instance.repellerDistance,
            repellerStrength = BoidManager.Instance.repellerStrength,
            edgeRepellerDistance = BoidManager.Instance.edgeRepellerDistance,
            edgeRepellerStrength = BoidManager.Instance.edgeRepellerStrength,
            spatialHashSize = SpatialHashManager.Instance.spatialHashSize,
            spatialHashPosition = SpatialHashManager.Instance.spatialHashPosition,
            spatialAgentData = spatialAgentData,
            otherChunks = archChunks,
            transformHandleRW = state.GetComponentTypeHandle<LocalTransform>(false),
            boidHandleRO = state.GetComponentTypeHandle<BoidData>(true),
            spatialAgentHandle = sharedAgentComponentHandle,
        };

        state.Dependency = job.ScheduleParallel(boidQuery, state.Dependency);

        UpdatePositionBoid updatePos = new();
        state.Dependency = updatePos.ScheduleParallel(state.Dependency);
    }

    public partial struct BoidJob : IJobChunk
    {
        public int maxNumNeighborCheck;
        public float deltaTime;
        public float boidSpeed;
        public float boidRotateSpeed;
        public float separationDistance;
        public float separationStrength;
        public float alignmentDistance;
        public float alignmentStrength;
        public float cohesionDistance;
        public float cohesionStrength;
        public float repellerDistance;
        public float repellerStrength;
        public float edgeRepellerDistance;
        public float edgeRepellerStrength;
        public uint spatialHashSize;
        public float3 spatialHashPosition;
        public ComponentTypeHandle<LocalTransform> transformHandleRW;
        [ReadOnly] public ComponentTypeHandle<BoidData> boidHandleRO;
        [ReadOnly] public SharedComponentTypeHandle<SpatialAgentData> spatialAgentHandle;
        [ReadOnly] public NativeArray<SpatialAgentData> spatialAgentData;
        [ReadOnly] public NativeArray<ArchetypeChunk> otherChunks;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeArray<float3> neighborPositions = new(maxNumNeighborCheck, Allocator.Temp);
            NativeArray<quaternion> neighborRotations = new(maxNumNeighborCheck, Allocator.Temp);
            var chunkSAD = chunk.GetSharedComponent(spatialAgentHandle);

            // get neighboring boids
            int numNeighbors = GetNeighboringBoids(ref neighborPositions, ref neighborRotations, chunkSAD);

            // get boids in this chunk & iterate through neighbors
            if (numNeighbors > 0)
                ExecuteBoidAlgorithm(in chunk, numNeighbors, in neighborPositions, in neighborRotations);

            // garbage cleanup
            neighborPositions.Dispose();
            neighborRotations.Dispose();
        }

        int GetNeighboringBoids(ref NativeArray<float3> neighborPositions, ref NativeArray<quaternion> neighborRotations, SpatialAgentData sad)
        {
            Profiler.BeginSample("GetNeighboringBoids");
            int numNeighbors = 0;
            for (int i = 0; i < otherChunks.Count(); i++)
            {
                if (numNeighbors >= maxNumNeighborCheck)
                    break;

                var diff = spatialAgentData[i].chunk - sad.chunk;
                if (
                    -1 <= diff.x && diff.x <= 1 &&
                    -1 <= diff.y && diff.y <= 1 &&
                    -1 <= diff.z && diff.z <= 1
                )
                {
                    var nt = otherChunks[i].GetNativeArray(ref boidHandleRO);
                    int numNeighborsInChunk = nt.Count();

                    for (int j = 0; j < numNeighborsInChunk; j++)
                    {
                        if (numNeighbors < maxNumNeighborCheck)
                        {
                            neighborPositions[numNeighbors] = nt[j].Position;
                            neighborRotations[numNeighbors] = nt[j].Rotation;
                            numNeighbors++;
                        }
                        else
                            break;
                    }
                }
            }
            Profiler.EndSample();
            return numNeighbors;
        }

        void ExecuteBoidAlgorithm(in ArchetypeChunk chunk, int numNeighbors, in NativeArray<float3> neighborPositions, in NativeArray<quaternion> neighborRotations)
        {
            if (numNeighbors == 0)
                return;

            Profiler.BeginSample("ExecuteBoidAlgorithm");
            var boids = chunk.GetNativeArray(ref boidHandleRO);
            var transforms = chunk.GetNativeArray(ref transformHandleRW);

            for (int i = 0; i < boids.Count(); i++)
            {
                var transform = transforms[i];
                var heading = math.forward(transform.Rotation);

                float3 separation = float3.zero;
                float3 alignment = float3.zero;
                float3 cohesion = float3.zero;

                for (int j = 0; j < numNeighbors; j++)
                {
                    if (math.all(transform.Position != neighborPositions[j]))
                    {
                        float3 diff = transform.Position - neighborPositions[j];
                        float3 diffNorm = math.normalize(diff);
                        float3 nh = math.forward(neighborRotations[j]);
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

    public partial struct UpdatePositionBoid : IJobEntity
    {
        public void Execute(ref BoidData boid, in LocalTransform localTransform)
        {
            boid.Position = localTransform.Position;
            boid.Rotation = localTransform.Rotation;
        }
    }
}
