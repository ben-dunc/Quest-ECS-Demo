using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public class OOSZombieManager : TestManager
{

    public int numEntities = 10;
    public int currentNumEntities = 0;
    public float speed = 2;
    public float rotateRate = 5;
    public float rotateMaxMin = 5f;
    public float zombieRange = 50;

    public GameObject zombiePrefab;

    List<Transform> transforms = new();
    List<ZombieOOSData> zombieArray;
    TransformAccessArray m_AccessArray;

    int numEntitiesGoal;

    void Awake()
    {
        UpdateTransformAccessArray();
        zombieArray = new List<ZombieOOSData>(numEntities);
        transforms = new(numEntities);
    }

    void OnDestroy()
    {
        m_AccessArray.Dispose();
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
                var instance = Instantiate(zombiePrefab);

                // position
                Vector3 pos = instance.transform.position;
                pos.x = UnityEngine.Random.Range(-zombieRange, zombieRange);
                pos.z = UnityEngine.Random.Range(-zombieRange, zombieRange);
                pos += transform.position;
                pos.y = 0;
                instance.transform.position = pos;

                // rotation
                instance.transform.Rotate(Vector3.up, UnityEngine.Random.Range(0f, 360f));

                transforms.Add(instance.transform);
                var s = UnityEngine.Random.Range(speed / 2, speed);
                zombieArray.Add(new ZombieOOSData()
                {
                    speed = s
                });

                if (instance.TryGetComponent<Animator>(out var animator))
                    animator.playbackTime = s * 0.7f;
            }

            // destroy entities
            while (transforms.Count > numEntitiesGoal)
            {
                currentNumEntities--;
                Destroy(transforms[transforms.Count - 1].gameObject);
                transforms.RemoveAt(transforms.Count - 1);
                zombieArray.RemoveAt(transforms.Count - 1);
            }

            UpdateTransformAccessArray();
        }

        var zombieDataNativeList = zombieArray.ToNativeList(Allocator.TempJob);

        // execute bob job
        var job = new OOSZombieJob()
        {
            speed = speed,
            rotateRate = rotateRate,
            rotateMaxMin = rotateMaxMin,
            zombieRange = zombieRange,
            zombieManagerPosition = transform.position,
            zombieData = zombieDataNativeList,
            deltaTime = Time.deltaTime,
        };

        var zombieJob = job.Schedule(m_AccessArray);
        StartCoroutine(DisposeAfterComplete(zombieJob, zombieDataNativeList));
    }

    void UpdateTransformAccessArray()
    {
        m_AccessArray = new TransformAccessArray(transforms.ToArray());
    }

    IEnumerator DisposeAfterComplete(JobHandle job, NativeList<ZombieOOSData> list)
    {
        int waitNum = 4;
        while (waitNum > 0)
        {
            waitNum--;
            yield return new WaitForEndOfFrame();
        }
        job.Complete();
        list.Dispose();
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


    public struct OOSZombieJob : IJobParallelForTransform
    {
        public float speed;
        public float rotateRate;
        public float rotateMaxMin;
        public float zombieRange;
        public Vector3 zombieManagerPosition;
        public float deltaTime;
        [ReadOnly] public NativeList<ZombieOOSData> zombieData;

        public void Execute(int index, TransformAccess transform)
        {
            // move
            transform.rotation *= Quaternion.AngleAxis(deltaTime * rotateRate, Vector3.up);
            transform.position += (Vector3)math.forward(transform.rotation) * zombieData[index].speed * deltaTime;

            // reset position if out of bounds
            if (transform.position.magnitude - math.length(zombieManagerPosition) > zombieRange)
            {
                Vector3 pos = transform.position;
                pos.x = 0;
                pos.z = 0;
                pos += zombieManagerPosition;
                pos.y = 0;
                transform.position = pos;
            }

        }
    }

    public struct ZombieOOSData
    {
        public float speed;
    }
}
