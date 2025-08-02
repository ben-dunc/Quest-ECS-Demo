using System.Collections.Generic;
using UnityEngine;

public class OOSQuadManager : TestManager
{
    public int numEntities = 10;
    public float rowSize = 0.2f;
    public float columnSize = 0.5f;
    public float zToY = 0.05f;
    public GameObject OOSQuadPrefab;

    List<Transform> transforms = new();
    int currentNumEntities;
    int numEntitiesGoal;
    int currentRow;
    int currentColumn;

    void Awake()
    {
        transforms = new(numEntities);
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
                var instance = Instantiate(OOSQuadPrefab);
                transforms.Add(instance.transform);

                Vector3 pos = new();
                pos.x = (currentColumn - (currentRow / 2f)) * columnSize;
                pos.z = currentRow * rowSize;
                pos.y = (pos.z * pos.z / zToY) + 5f;

                instance.transform.position = pos;
                instance.transform.localScale = Vector3.one * 0.25f;

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

                currentColumn--;
                currentNumEntities--;

                if (currentColumn < 0)
                {
                    currentRow--;
                    currentColumn = currentRow;
                }
            }
        }
    }
}