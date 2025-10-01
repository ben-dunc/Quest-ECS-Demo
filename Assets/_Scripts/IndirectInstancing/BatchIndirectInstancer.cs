using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEngine;

public class BatchIndirectInstancer : MonoBehaviour
{
    [SerializeField] Mesh mesh;
    [SerializeField] Material material;

    [Header("Generation")]
    [SerializeField] int sideCount = 30;
    [SerializeField, ReadOnly] int estimatedCount = 0;
    [SerializeField] float gridSize = 0.33333f;
    [SerializeField] float translationVariance = 0.2f;
    [SerializeField] float rotationVariance = 20f;

    [Header("Scale Noise")]
    [SerializeField] float perlinScale = 0.1f;
    [SerializeField] float scaleVariance = 0.2f;

    RenderParams rp;

    // command buffer
    GraphicsBuffer commandBuf;
    GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;
    const int MAX_COMMAND_INSTANCE_COUNT = 1024;
    int commandCount = 1;
    int count = 1;

    // TRS buffer
    GraphicsBuffer trsBuffer;
    Matrix4x4[] trsMatrices;


    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;

        var origin = transform.position;
        float size = sideCount * gridSize;
        var bounds = new Bounds(origin + size / 2 * Vector3.one, size * Vector3.one);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }

    void OnValidate()
    {
        estimatedCount = sideCount * sideCount;
    }

    void Start()
    {
        // command buffer
        count = sideCount * sideCount;
        commandCount = Mathf.CeilToInt(count / MAX_COMMAND_INSTANCE_COUNT);
        commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[commandCount];
        for (int i = 0; i < commandCount; i++)
        {
            commandData[i].indexCountPerInstance = mesh.GetIndexCount(0);
            commandData[i].instanceCount = (uint)Mathf.Max(count - i * MAX_COMMAND_INSTANCE_COUNT, MAX_COMMAND_INSTANCE_COUNT);
        }
        commandBuf.SetData(commandData);

        // prep rp
        rp = new(material);
        var origin = transform.position;
        float batchSize = sideCount * gridSize;
        rp.worldBounds = new Bounds(origin + batchSize / 2 * Vector3.one, batchSize * Vector3.one);
        rp.matProps = new MaterialPropertyBlock();

        // trs buffer
        trsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, sideCount * sideCount, sizeof(float) * 16); // 4x4 matrix
        trsMatrices = new Matrix4x4[sideCount * sideCount];

        for (int i = 0; i < sideCount; i++)
        {
            for (int j = 0; j < sideCount; j++)
            {
                // position
                float transX = Random.Range(-translationVariance, translationVariance);
                float transZ = Random.Range(-translationVariance, translationVariance);
                var pos = new Vector3((j * gridSize) + transX, 0.5f, (i * gridSize) + transZ);
                pos += origin;

                // scale
                float scale = 1f + Mathf.PerlinNoise(pos.x, pos.z) * scaleVariance;

                // rotation
                float rotY = Random.Range(-rotationVariance, rotationVariance);// - (1 - scale) / 2;


                trsMatrices[j + (i * sideCount)] = Matrix4x4.TRS(
                    pos,
                    Quaternion.Euler(new Vector3(0, rotY, 0)),
                    new Vector3(scale, scale, scale)
                );
            }
        }

        trsBuffer.SetData(trsMatrices);
        rp.matProps.SetBuffer("_InstanceTransforms", trsBuffer);

        Debug.Log($"Prepped {sideCount * sideCount} blades of grass.");
    }

    void Update()
    {
        if (mesh && material && commandBuf != null && commandBuf.IsValid())
            Graphics.RenderMeshIndirect(rp, mesh, commandBuf, commandCount);
    }

    void OnDestroy()
    {
        commandBuf?.Release();
        commandBuf = null;
        trsBuffer?.Release();
        trsBuffer = null;
    }
}
