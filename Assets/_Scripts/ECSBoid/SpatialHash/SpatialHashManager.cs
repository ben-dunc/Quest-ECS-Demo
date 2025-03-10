using Unity.XR.CoreUtils;
using UnityEngine;

public class SpatialHashManager : MonoBehaviour
{
    public static SpatialHashManager Instance = null;

    [Header("Spatial Hash")]
    public uint spatialHashSize = 50;
    public uint spatialHashDivision = 10;
    public Vector3 spatialHashPositionOffset = new Vector3(0, 25, 0);

    [Header("Read Only")]
    [ReadOnly] public uint spatialHashDivisionsPow2 = 0;
    [ReadOnly] public Vector3 spatialHashPosition;
    [ReadOnly] public uint chunkSize = 0;

    void OnDrawGizmos()
    {
        // chunk 0
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + spatialHashPositionOffset - (Vector3.one * (spatialHashSize / 2)) + (Vector3.one * ((float)(spatialHashSize / spatialHashDivision) / 2)), Vector3.one * (spatialHashSize / spatialHashDivision));

        // spatial map bounds
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(transform.position + spatialHashPositionOffset, Vector3.one * spatialHashSize);
    }

    void OnValidate()
    {
        float div1 = spatialHashSize / spatialHashDivision;

        if (Mathf.Floor(div1) != div1)
        {
            Debug.LogError("zoneDivisions must be an integer multiple of zoneSize");
            spatialHashSize = 50;
            spatialHashDivision = 10;
        }
        else
        {
            chunkSize = spatialHashSize / spatialHashDivision;
            spatialHashDivisionsPow2 = (uint)Mathf.Pow(spatialHashDivision, 2);
        }
        spatialHashPosition = transform.position + spatialHashPositionOffset;
    }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }
        spatialHashPosition = transform.position + spatialHashPositionOffset;

        Instance = this;
    }
}
