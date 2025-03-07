using UnityEngine;

public class SimpleECSManager : MonoBehaviour
{
    public static SimpleECSManager Instance;

    public int numEntities = 100;
    public float rowSize = 0.2f;
    public float columnSize = 0.5f;
    public float bobFrequency = 0.25f;
    public float bobMagnitude = 1f;
    public float zToY = 0.05f;

    void Awake()
    {
        Instance = this;
    }
}
