using System.Linq;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/*
    NOTE FOR NEXT TIME:

    Instead of using ChunkComponent data, do the following:
    1. Query for all chunks that include the SpatialAgentData, get array
    2. Get all SpatialAgentData for each chunk
    3. Pass in archetype and spatial agent data to chunk job
    4. In chunk job, find neighboring chunks, find neighboring boids, & run boid rules
*/

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
        NativeArray<Unity.Entities.ArchetypeChunk> archChunks = boidQuery.ToArchetypeChunkArray(Allocator.TempJob);
        int numChunks = archChunks.Count();
        NativeArray<SpatialAgentData> spatialAgentData = new(archChunks.Count(), Allocator.TempJob);
        var sharedAgentComponentHandle = state.GetSharedComponentTypeHandle<SpatialAgentData>();
        for (int i = 0; i < numChunks; i++)
        {
            if (archChunks[i].NumSharedComponents() > 0)
                spatialAgentData[i] = archChunks[i].GetSharedComponent(sharedAgentComponentHandle);
            else
                Debug.Log("Chunk didn't have shared data.");
        }

        // execute job
        BoidJob job = new BoidJob
        {
            maxNumNeighborCheck = BoidManager.Instance.maxNumNeighborCheck,
            deltaTime = SystemAPI.Time.DeltaTime * BoidManager.Instance.simSpeed,
            boidSpeed = BoidManager.Instance.boidSpeed,
            boidRotateSpeed = BoidManager.Instance.boidRotateSpeed,
            boidRandomness = BoidManager.Instance.boidRandomness,
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
            spatialHashDivisions = SpatialHashManager.Instance.spatialHashDivision,
            spatialHashDivisionsPow2 = SpatialHashManager.Instance.spatialHashDivisionsPow2,
            spatialHashSize = SpatialHashManager.Instance.spatialHashSize,
            spatialHashPosition = SpatialHashManager.Instance.spatialHashPosition,
            chunkSize = SpatialHashManager.Instance.chunkSize,
            rand = this.rand,
            spatialAgentData = spatialAgentData,
            otherChunks = archChunks,
            transformHandleRW = state.GetComponentTypeHandle<LocalTransform>(false),
            boidHandle = state.GetComponentTypeHandle<BoidData>(),
            spatialAgentHandle = sharedAgentComponentHandle,
            // debug
            overwritePosition = BoidManager.Instance.overwritePosition,
            position = BoidManager.Instance.position,
        };

        state.Dependency = job.ScheduleParallel(boidQuery, state.Dependency);
    }

    public partial struct BoidJob : IJobChunk
    {
        public int maxNumNeighborCheck;
        public float deltaTime;
        public float boidSpeed;
        public float boidRotateSpeed;
        public float boidRandomness;
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
        public uint spatialHashDivisions;
        public uint spatialHashDivisionsPow2;
        public uint spatialHashSize;
        public uint chunkSize;
        public float3 spatialHashPosition;
        public Unity.Mathematics.Random rand;
        public ComponentTypeHandle<LocalTransform> transformHandleRW;
        [ReadOnly] public ComponentTypeHandle<BoidData> boidHandle;
        [ReadOnly] public SharedComponentTypeHandle<SpatialAgentData> spatialAgentHandle;
        [ReadOnly] public NativeArray<SpatialAgentData> spatialAgentData;
        [ReadOnly] public NativeArray<Unity.Entities.ArchetypeChunk> otherChunks;


        // debug
        public bool overwritePosition;
        public float3 position;

        public void Execute(in Unity.Entities.ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeArray<float3> neighborPositions = new(maxNumNeighborCheck, Allocator.Temp);
            NativeArray<Quaternion> neighborRotations = new(maxNumNeighborCheck, Allocator.Temp);
            var chunkSAD = chunk.GetSharedComponent(spatialAgentHandle);
            int numNeighbors = 0;

            // get neighboring boids
            for (int i = 0; i < otherChunks.Count(); i++)
            {
                if (numNeighbors >= maxNumNeighborCheck)
                    break;

                var diff = spatialAgentData[i].chunk - chunkSAD.chunk;
                if (math.all(-1 <= diff & diff <= 1))
                {
                    var nt = otherChunks[i].GetNativeArray(ref transformHandleRW);
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

            // iterate through neighbors
            var boids = chunk.GetNativeArray(ref boidHandle);
            var transforms = chunk.GetNativeArray(ref transformHandleRW);

            Debug.Log($"Num neighbors: {numNeighbors}");

            if (numNeighbors > 0)
            {
                for (int i = 0; i < boids.Count(); i++)
                {
                    var transform = transforms[i];
                    var heading = math.forward(transforms[i].Rotation);

                    float3 separation = float3.zero;
                    float3 alignment = float3.zero;
                    float3 cohesion = float3.zero;

                    for (int j = 0; j < numNeighbors; j++)
                    {
                        float3 diff = transform.Position - neighborPositions[j];
                        float3 nh = math.forward(neighborRotations[j]);
                        float distance = math.length(diff);

                        if (distance > 0)
                        {
                            // separation - get inverse of distance
                            if (distance < separationDistance)
                                separation += math.lerp(heading, diff, 1f - (distance / separationDistance)) * separationStrength;
                            if (distance < alignmentDistance)
                                separation += math.lerp(heading, nh, 1f - (distance / alignmentDistance)) * alignmentStrength;
                            if (distance < cohesionDistance)
                                separation += math.lerp(heading, -diff, 1f - (distance / cohesionDistance)) * cohesionStrength;
                        }
                    }

                    if (numNeighbors > 0)
                    {
                        alignment /= numNeighbors;
                        cohesion /= numNeighbors;
                    }

                    // update headings
                    float3 newHeading = heading;

                    // avoid edges of spatial hash size
                    bool3 outside = math.abs(transform.Position - spatialHashPosition) > (spatialHashSize / 2) - edgeRepellerDistance;
                    if (math.any(outside))
                        newHeading += (spatialHashPosition - transform.Position) * (float3)outside * edgeRepellerStrength;
                    else
                    {
                        newHeading += separation * separationStrength;
                        newHeading += alignment * alignmentStrength;
                        newHeading += cohesion * cohesionStrength;
                    }

                    newHeading = math.normalize(newHeading);

                    if (!math.all(newHeading == float3.zero))
                        heading = math.lerp(heading, newHeading, deltaTime * boidRotateSpeed);
                    var newLook = quaternion.LookRotation(newHeading, math.up());

                    // update position & rotation
                    transform.Position += boidSpeed * deltaTime * heading;
                    transform.Rotation = math.slerp(transform.Rotation, newLook, boidRotateSpeed * deltaTime);

                    // assign position & rotation
                    transforms[i] = transform;
                }
            }

            // garbage cleanup
            boids.Dispose();
            transforms.Dispose();
            neighborPositions.Dispose();
            neighborRotations.Dispose();
        }
    }
}


// public void Execute(in BoidData boid, ref LocalTransform transform)// ref RenderMesh renderer)
// {
//     Debug.Log("Boid!");
//     // assign id if none
// if (boid.id == 0)
// {
//     boid.id = rand.NextInt();
//     boid.heading = rand.NextFloat3(-1, 1);
//     return;
// }

// // overwrite position
// if (overwritePosition)
//     transform.Position = position;

// // assign chunk

// // if it's static, don't do anything
// if (boid.isStatic || overwritePosition)
//     return;

// // get all boids in chunk
// int numNeighbors = 0;
// int numTotalBoids = allBoids.Length;

// float3 separation = new();
// float3 alignment = new();
// float3 cohesion = new();
// float3 repellant = new();

// // Get neighbors & boid stuff
// for (int i = 0; i < numTotalBoids && numNeighbors < maxNumNeighborCheck; i++)
// {
//     BoidData b = allBoids[i];
//     LocalTransform t = allBoidTransforms[i];

//     if (b.id == boid.id || (b.isStatic && !b.isRepeller))
//         continue;
//     else if (math.all(t.Position == transform.Position))
//         transform.Position += rand.NextFloat3(-0.1f, 0.1f);

//     // check if boid is in chunk

//     if (
//         (boid.chunk.x - 1) <= b.chunk.x && b.chunk.x <= (boid.chunk.x + 1) &&
//         (boid.chunk.y - 1) <= b.chunk.y && b.chunk.y <= (boid.chunk.y + 1) &&
//         (boid.chunk.z - 1) <= b.chunk.z && b.chunk.z <= (boid.chunk.z + 1)
//     )
//     {
//         float3 diff = transform.Position - t.Position;
//         float distance = math.length(diff);

//         if (!b.isRepeller)
//         {
//             // separation - get inverse of distance
//             if (distance < separationDistance)
//                 separation += (1 - math.max(distance / separationDistance, 1f)) * diff;
//             if (distance < alignmentDistance)
//                 alignment += b.heading * alignmentStrength;
//             if (distance < cohesionDistance)
//                 cohesion -= (1 - math.max(distance / cohesionDistance, 1f)) * diff;

//             numNeighbors++;
//         }
//         else if (distance < repellerDistance)
//             repellant += (1 - math.max(distance / repellerDistance, 1f)) * diff;
//     }
// }

// if (numNeighbors > 0)
// {
//     alignment /= numNeighbors;
//     cohesion /= numNeighbors;
// }

// // update headings
// float3 newHeading = boid.heading;

// // avoid edges of spatial hash size
// bool3 outside = math.abs(transform.Position - spatialHashPosition) > (spatialHashSize / 2) - edgeRepellerDistance;
// if (math.any(outside))
//     newHeading += (spatialHashPosition - transform.Position) * (float3)outside * edgeRepellerStrength;
// else
// {
//     newHeading += separation * separationStrength;
//     newHeading += alignment * alignmentStrength;
//     newHeading += cohesion * cohesionStrength;
//     newHeading += repellant * repellerStrength;
// }

// newHeading = math.normalize(newHeading);

// if (!math.all(newHeading == float3.zero))
//     boid.heading = math.lerp(boid.heading, newHeading, deltaTime * boidRotateSpeed);

// // update position & rotation
// transform = transform.Translate(boidSpeed * deltaTime * boid.heading);

// quaternion targetDirection = quaternion.LookRotation(boid.heading, math.up());
// transform.Rotation = targetDirection;
// }