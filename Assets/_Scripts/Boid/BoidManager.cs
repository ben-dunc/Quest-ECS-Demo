using UnityEngine;

[CreateAssetMenu(fileName = "BoidManager", menuName = "Scriptable Objects/BoidManager", order = 0)]
public class BoidManager : ScriptableObject
{
    public static BoidManager Instance = null;

    [Header("Simulation")]
    public float simSpeed = 1;
    public float boidSpeed = 1f;
    public float boidRotateSpeed = 2f;
    public float boidRandomness = 1f;
    public int maxNumNeighborCheck = 20;

    [Header("Separation")]
    public float separationDistance;
    public float separationStrength;

    [Header("Alignment")]
    public float alignmentDistance;
    public float alignmentStrength;

    [Header("Cohesion")]
    public float cohesionDistance;
    public float cohesionStrength;

    [Header("Repeller")]
    public float repellerDistance;
    public float repellerStrength;
    public float edgeRepellerDistance;
    public float edgeRepellerStrength;

    [Header("Debugging")]
    public bool overwritePosition = false;
    public Vector3 position;

    void OnEnable()
    {
        if (Instance == null)
            Instance = this;
    }
}
