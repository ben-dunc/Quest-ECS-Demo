using UnityEngine;

public class ECSBoidManager : TestManager
{
    public static ECSBoidManager Instance = null;

    [Header("Spawning")]
    public int numBoids = 100;

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

    [Header("Debugging")]
    public bool overwritePosition = false;
    public Vector3 position;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }


    public override void SetTargetNumEntities(int num)
    {
        numBoids = num;
    }

    public override int GetNumEntities()
    {
        return ECSBoidSpawnerSystem.currentNumEntities;
    }

    public override int GetTargetNumEntities()
    {
        return numBoids;
    }

}
