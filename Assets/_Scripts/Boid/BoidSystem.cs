using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct BoidSystem : ISystem
{
    EntityQuery boidQuery;
    Random rand;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BoidData>();

        rand = new Random(88);
        boidQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(BoidData), typeof(LocalTransform) }
        });
    }

    public void OnUpdate(ref SystemState state)
    {
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

            // debug
            overwritePosition = BoidManager.Instance.overwritePosition,
            position = BoidManager.Instance.position,
        };

        NativeArray<BoidData> boidArray = boidQuery.ToComponentDataArray<BoidData>(Allocator.TempJob);
        NativeArray<LocalTransform> localTransformArray = boidQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
        job.allBoids = boidArray;
        job.allBoidTransforms = localTransformArray;

        job.ScheduleParallel();

        boidArray.Dispose(state.Dependency);
        localTransformArray.Dispose(state.Dependency);
    }

    public partial struct BoidJob : IJobEntity
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
        public Random rand;
        [ReadOnly] public NativeArray<BoidData> allBoids;
        [ReadOnly] public NativeArray<LocalTransform> allBoidTransforms;

        // debug
        public bool overwritePosition;
        public float3 position;

        public void Execute(ref BoidData boid, ref LocalTransform transform)// ref RenderMesh renderer)
        {
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
        }
    }
}
