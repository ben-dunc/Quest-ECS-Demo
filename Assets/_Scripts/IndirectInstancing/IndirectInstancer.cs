using UnityEngine;

public class IndirectInstancer : MonoBehaviour
{
    [SerializeField] Mesh mesh;
    [SerializeField] Material material;

    [Header("Generation")]
    [SerializeField] int count = 30;
    [SerializeField] float gridSize = 0.33333f;
    [SerializeField] float translationVariance = 0.2f;
    [SerializeField] float rotationVariance = 20f;
    [SerializeField] float scaleVariance = 0.2f;

    RenderParams rp;
    Matrix4x4[] instData;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        float size = count * gridSize;
        Gizmos.DrawWireCube(transform.position + new Vector3(size / 2, 0, size / 2), size * Vector3.one);
    }

    void Awake()
    {
        // prep array
        rp = new(material);
        instData = new Matrix4x4[count * count];
        var origin = transform.position;

        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < count; j++)
            {
                float transX = Random.Range(-translationVariance, translationVariance);
                float transZ = Random.Range(-translationVariance, translationVariance);
                float scale = 0.5f + Random.Range(-scaleVariance, scaleVariance);
                float rotY = Random.Range(-rotationVariance, rotationVariance) - (1 - scale) / 2;

                var pos = new Vector3((j * gridSize) + transX, 0.5f, (i * gridSize) + transZ);
                pos += origin;

                instData[j + (i * count)] = Matrix4x4.TRS(
                    pos,
                    Quaternion.Euler(new Vector3(0, rotY, 0)),
                    new Vector3(scale, scale, scale)
                );
            }
        }

        Debug.Log($"Instantiated {count * count} blades of grass.");
    }

    void Update()
    {
        Debug.Log("HEY YOU GOOFBALL! \"Graphics.RenderMeshInstanced\" doesn't work on the GPU. Use \"Graphics.RenderMeshIndirect\" instead!");
        // if (mesh && material)
        //     Graphics.RenderMeshIndirect(rp, mesh, 0, instData);
    }
}
