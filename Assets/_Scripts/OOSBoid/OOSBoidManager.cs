using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public class OOSBoidManager : TestManager
{
    [Header("Spawning")]
    public int numEntities = 10;
    public int currentNumEntities = 0;
    public GameObject boidPrefab;

    [Header("Simulation")]
    [Range(0, 10)] public float simSpeed = 1;
    public float boidSpeed = 1f;
    public int maxNumNeighborCheck = 20;

    [Header("Separation")]
    public float separationDistance = 2;
    public float separationStrength = 1f;

    [Header("Alignment")]
    public float alignmentDistance = 8;
    public float alignmentStrength = 1f;

    [Header("Cohesion")]
    public float cohesionDistance = 4;
    public float cohesionStrength = 1f;

    [Header("Repeller")]
    public float edgeRepellerDistance = 10;
    public float edgeRepellerStrength = 10;

    [Header("Spatial Map")]
    public float chunkSize = 5f;
    public float size = 50f;
    public

    List<OOSBoidData> boids = new();
    List<Transform> transforms = new();
    TransformAccessArray m_transforms;
    int numEntitiesGoal;

    void Awake()
    {
        UpdateTransformAccessArray();
    }

    void OnDestroy()
    {
        m_transforms.Dispose();
    }

    void UpdateTransformAccessArray()
    {
        m_transforms = new TransformAccessArray(transforms.ToArray());
    }

    void Update()
    {
        // numEntitiesGoal
        int maxNumChange = 100;
        numEntitiesGoal = Mathf.Clamp(numEntities, numEntities - maxNumChange, numEntities + maxNumChange);

        // create entities
        if (transforms.Count != numEntitiesGoal)
        {
            while (transforms.Count < numEntitiesGoal)
            {
                currentNumEntities++;
                var instance = Instantiate(boidPrefab);

                // position
                Vector3 pos = instance.transform.position;
                pos.x = UnityEngine.Random.Range(-2, 2);
                pos.y = UnityEngine.Random.Range(-2, 2);
                pos.z = UnityEngine.Random.Range(-2, 2);
                pos += transform.position;
                instance.transform.position = pos;

                // rotation
                instance.transform.Rotate(Vector3.up, UnityEngine.Random.Range(0f, 360f));
                instance.transform.Rotate(Vector3.right, UnityEngine.Random.Range(0f, 360f));
                instance.transform.Rotate(Vector3.forward, UnityEngine.Random.Range(0f, 360f));
                transforms.Add(instance.transform);
                boids.Add(new OOSBoidData());
            }

            // destroy entities
            while (transforms.Count > numEntitiesGoal)
            {
                currentNumEntities--;
                Destroy(transforms[transforms.Count - 1].gameObject);
                transforms.RemoveAt(transforms.Count - 1);
                boids.RemoveAt(transforms.Count - 1);
            }

            UpdateTransformAccessArray();
        }

        var boidsNA = boids.ToNativeArray(Allocator.TempJob);

        // execute bob job
        var job = new OOSBoidJob()
        {
            boidSpeed = boidSpeed,
            maxNumNeighborCheck = maxNumNeighborCheck,
            separationDistance = separationDistance,
            separationStrength = separationStrength,
            alignmentDistance = alignmentDistance,
            alignmentStrength = alignmentStrength,
            cohesionDistance = cohesionDistance,
            cohesionStrength = cohesionStrength,
            edgeRepellerDistance = edgeRepellerDistance,
            edgeRepellerStrength = edgeRepellerStrength,
            boidManagerPosition = transform.position,
            deltaTime = Time.deltaTime,
            boidArea = size,
            boids = boidsNA,
        };

        var jobHandle = job.Schedule(m_transforms);

        var updateJob = new OOSUpdateBoidJob()
        {
            chunkSize = chunkSize,
            boids = boidsNA,
        };

        var updateJobHandle = updateJob.ScheduleReadOnlyByRef(m_transforms, 5, jobHandle);

        StartCoroutine(DisposeAfterComplete(new[] { updateJobHandle, jobHandle }, boidsNA));
    }

    IEnumerator DisposeAfterComplete(JobHandle[] jobs, NativeArray<OOSBoidData> boidsNA)
    {
        int waitNum = 4;
        while (waitNum > 0)
        {
            waitNum--;
            yield return new WaitForEndOfFrame();
        }
        foreach (var j in jobs)
            j.Complete();

        for (int i = 0; i < boidsNA.Count(); i++)
            boids[i] = boidsNA[i];
        boidsNA.Dispose();
    }

    public override void SetTargetNumEntities(int num)
    {
        numEntities = num;
    }

    public override int GetNumEntities()
    {
        return currentNumEntities;
    }

    public override int GetTargetNumEntities()
    {
        return numEntities;
    }

    public struct OOSBoidJob : IJobParallelForTransform
    {
        public float boidSpeed;
        public int maxNumNeighborCheck;
        public float separationDistance;
        public float separationStrength;
        public float alignmentDistance;
        public float alignmentStrength;
        public float cohesionDistance;
        public float cohesionStrength;
        public float edgeRepellerDistance;
        public float edgeRepellerStrength;
        public float boidArea;
        public Vector3 boidManagerPosition;
        public float deltaTime;
        [ReadOnly] public NativeArray<OOSBoidData> boids;

        public void Execute(int index, TransformAccess transform)
        {
            int numBoids = boids.Length;
            int numNeighbors = 0;

            var heading = math.forward(transform.rotation);
            var boid = boids[index];

            float3 separation = float3.zero;
            float3 alignment = float3.zero;
            float3 cohesion = float3.zero;

            for (int i = 0; i < numBoids && numNeighbors < maxNumNeighborCheck; i++)
            {
                var nb = boids[i];
                var zoneDiff = boid.zone - nb.zone;
                if (
                    i != index && (
                        -1 <= zoneDiff.x && zoneDiff.x <= 1 ||
                        -1 <= zoneDiff.y && zoneDiff.y <= 1 ||
                        -1 <= zoneDiff.z && zoneDiff.z <= 1
                    ) &&
                    math.all(boid.position != nb.position)
                )
                {
                    numNeighbors++;
                    float3 diff = transform.position - (Vector3)nb.position;
                    float3 diffNorm = math.normalize(diff);
                    float3 nh = math.forward(nb.rotation);
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

            if (numNeighbors > 0)
            {
                alignment /= numNeighbors;
                cohesion /= numNeighbors;

                // add everything up
                heading += separation * separationStrength;
                heading += alignment * alignmentStrength;
                heading += cohesion * cohesionStrength;
            }

            // avoid edges of spatial hash size
            bool3 outside = math.abs(transform.position - boidManagerPosition) > (boidArea / 2) - edgeRepellerDistance;
            heading += (boidManagerPosition - transform.position) * (float3)outside * edgeRepellerStrength;

            heading = math.normalize(heading);

            if (heading.x == math.NAN)
                UnityEngine.Debug.LogError("NAN!");

            // update position & rotation
            transform.position += (Vector3)(boidSpeed * deltaTime * heading);
            transform.rotation = quaternion.LookRotation(heading, math.up());
        }
    }

    public struct OOSUpdateBoidJob : IJobParallelForTransform
    {
        public float chunkSize;
        public NativeArray<OOSBoidData> boids;

        public void Execute(int index, TransformAccess transform)
        {
            var b = boids[index];
            b.position = (float3)transform.position;
            b.rotation = (quaternion)transform.rotation;
            b.zone = new int3(
                (int)(b.position.x / chunkSize),
                (int)(b.position.y / chunkSize),
                (int)(b.position.z / chunkSize)
            );
            boids[index] = b;
        }
    }

    public struct OOSBoidData
    {
        public int3 zone;
        public float3 position;
        public quaternion rotation;
    }
}
