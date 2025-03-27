using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

public class SimpleOOSManager : TestManager
{
    public int numEntities = 10;
    public float rowSize = 0.2f;
    public float columnSize = 0.5f;
    public float frequency = 0.2f;
    public float magnitude = 0.2f;
    public float zToY = 0.05f;
    public GameObject simpleOOSPrefab;

    List<Transform> transforms = new();
    List<SimpleOOSDataStruct> simpleData;
    TransformAccessArray m_AccessArray;

    int currentNumEntities;
    int numEntitiesGoal;
    int currentRow;
    int currentColumn;

    void Awake()
    {
        UpdateTransformAccessArray();
        simpleData = new List<SimpleOOSDataStruct>(numEntities);
        transforms = new(numEntities);
    }

    void OnDestroy()
    {
        m_AccessArray.Dispose();
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


    // [ProPlayButton]
    void Update()
    {
        // numEntitiesGoal
        int maxNumChange = 100;
        numEntitiesGoal = numEntities;
        if (numEntitiesGoal > transforms.Count + maxNumChange)
            numEntitiesGoal = transforms.Count + maxNumChange;
        if (numEntitiesGoal < transforms.Count - maxNumChange)
            numEntitiesGoal = transforms.Count - maxNumChange;

        // create entities
        if (transforms.Count != numEntitiesGoal)
        {
            while (transforms.Count < numEntitiesGoal)
            {
                var instance = Instantiate(simpleOOSPrefab);
                simpleData.Add(new SimpleOOSDataStruct()
                {
                    id = currentNumEntities,
                    row = currentRow,
                    column = currentColumn
                });
                transforms.Add(instance.transform);

                currentColumn++;
                currentNumEntities++;

                if (currentColumn > currentRow)
                {
                    currentColumn = 0;
                    currentRow++;
                }
            }

            // destroy entities
            while (transforms.Count > numEntitiesGoal)
            {
                Destroy(transforms[transforms.Count - 1].gameObject);
                transforms.RemoveAt(transforms.Count - 1);
                simpleData.RemoveAt(transforms.Count - 1);

                currentColumn--;
                currentNumEntities--;

                if (currentColumn < 0)
                {
                    currentRow--;
                    currentColumn = currentRow;
                }
            }

            UpdateTransformAccessArray();
        }

        var simpleDataNativeList = simpleData.ToNativeList(Allocator.Persistent);

        // execute bob job
        var job = new OOSBobJob()
        {
            frequency = frequency,
            magnitude = magnitude,
            zToY = zToY,
            rowSize = rowSize,
            columnSize = columnSize,
            time = Time.time,
            simpleData = simpleDataNativeList
        };

        var bobJob = job.Schedule(m_AccessArray);
        StartCoroutine(DisposeAfterComplete(bobJob, simpleDataNativeList));
    }

    void UpdateTransformAccessArray()
    {
        m_AccessArray = new TransformAccessArray(transforms.ToArray());
    }

    IEnumerator DisposeAfterComplete(JobHandle job, NativeList<SimpleOOSDataStruct> list)
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

    public struct OOSBobJob : IJobParallelForTransform
    {
        public float frequency;
        public float magnitude;
        public float zToY;
        public float rowSize;
        public float columnSize;
        public float time;
        [ReadOnly] public NativeList<SimpleOOSDataStruct> simpleData;

        public void Execute(int index, TransformAccess transform)
        {
            Vector3 pos = transform.position;
            var simpleDatum = simpleData[index];
            pos.x = (simpleDatum.column - (simpleDatum.row / 2f)) * columnSize;
            pos.z = simpleDatum.row * rowSize;
            pos.y = (Mathf.Sin(time + pos.z) * magnitude) + (pos.z * zToY) + 5f;

            transform.position = pos;
        }
    }

    public struct SimpleOOSDataStruct
    {
        public int id;
        public int row;
        public int column;
    }

}