using UnityEngine;

// got to 300,000 before dipping below 60 fps

public class SimpleECSManager : TestManager
{
    public static SimpleECSManager Instance;

    public int numEntities = 100;
    public float rowSize = 0.2f;
    public float columnSize = 0.5f;
    public float frequency = 0.2f;
    public float magnitude = 0.2f;
    public float zToY = 0.05f;

    void Awake()
    {
        Instance = this;
    }

    public override void SetTargetNumEntities(int num)
    {
        numEntities = num;
    }

    public override int GetNumEntities()
    {
        return SimpleECSSystem.currentNumEntities;
    }

    public override int GetTargetNumEntities()
    {
        return numEntities;
    }

}
