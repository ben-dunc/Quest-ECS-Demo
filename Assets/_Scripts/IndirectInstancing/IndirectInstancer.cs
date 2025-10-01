using Unity.XR.CoreUtils;
using UnityEngine;

public class IndirectInstancer : MonoBehaviour
{
    [SerializeField] Mesh mesh;
    [SerializeField] Material material;

    [Header("Generation")]
    [SerializeField] int count = 30;
    [SerializeField, ReadOnly] int estimatedCount = 30;
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
    const int commandCount = 1;

    // TRS buffer
    GraphicsBuffer trsBuffer;
    Matrix4x4[] trsMatrices;


    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        var origin = transform.position;
        float size = count * gridSize;
        var bounds = new Bounds(origin + size / 2 * Vector3.one, size * Vector3.one);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }

    void OnValidate()
    {
        estimatedCount = count * count;
    }

    void Start()
    {
        // prep array
        rp = new(material);
        var origin = transform.position;
        float size = count * gridSize;
        rp.worldBounds = new Bounds(origin + size / 2 * Vector3.one, size * Vector3.one);
        rp.matProps = new MaterialPropertyBlock();

        // command buffer
        commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[commandCount];
        commandData[0].indexCountPerInstance = mesh.GetIndexCount(0);
        commandData[0].instanceCount = (uint)(count * count);
        commandBuf.SetData(commandData);

        // trs buffer
        trsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count * count, sizeof(float) * 16); // 4x4 matrix
        trsMatrices = new Matrix4x4[count * count];

        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < count; j++)
            {
                // position
                float transX = Random.Range(-translationVariance, translationVariance);
                float transZ = Random.Range(-translationVariance, translationVariance);
                var pos = new Vector3((j * gridSize) + transX, 0.5f, (i * gridSize) + transZ);
                pos += origin;

                // scale
                float scale = 0.5f + Mathf.PerlinNoise(pos.x, pos.z) * scaleVariance;

                // rotation
                float rotY = Random.Range(-rotationVariance, rotationVariance);// - (1 - scale) / 2;


                trsMatrices[j + (i * count)] = Matrix4x4.TRS(
                    pos,
                    Quaternion.Euler(new Vector3(0, rotY, 0)),
                    new Vector3(scale, scale, scale)
                );
            }
        }
        trsBuffer.SetData(trsMatrices);
        rp.matProps.SetBuffer("_InstanceTransforms", trsBuffer);

        Debug.Log($"Prepped {count * count} blades of grass.");
    }

    void Update()
    {
        if (mesh && material && commandBuf != null && commandBuf.IsValid())
            Graphics.RenderMeshIndirect(rp, mesh, commandBuf, 1);
    }

    void OnDestroy()
    {
        commandBuf?.Release();
        commandBuf = null;
        trsBuffer?.Release();
        trsBuffer = null;
    }
}
