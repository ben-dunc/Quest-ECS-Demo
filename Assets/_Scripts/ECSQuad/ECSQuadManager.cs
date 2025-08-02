using UnityEngine;

// got to 300,000 before dipping below 60 fps

public class ECSQuadManager : TestManager
{
    public static ECSQuadManager Instance;

    public int numEntities = 100;
    public float rowSize = 0.2f;
    public float columnSize = 0.5f;
    public float zToY = 0.05f;

    void Awake()
    {
        Instance = this;
        ECSQuadSystem.currentNumEntities = 0;
    }

    public override void SetTargetNumEntities(int num)
    {
        numEntities = num;
    }

    public override int GetNumEntities()
    {
        return ECSQuadSystem.currentNumEntities;
    }

    public override int GetTargetNumEntities()
    {
        return numEntities;
    }

}
