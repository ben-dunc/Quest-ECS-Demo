using UnityEngine;

// [CreateAssetMenu(fileName = "BoidManager", menuName = "Scriptable Objects/BoidManager", order = 0)]
public class BoidManager : MonoBehaviour
{
    public static BoidManager Instance = null;

    [Header("Simulation")]
    [Range(0, 10)] public float simSpeed = 1;
    public float boidSpeed = 1f;
    public float boidRotateSpeed = 2f;
    public float boidRandomness = 1f;
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
    public float repellerDistance = 5;
    public float repellerStrength = 100;
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
}
