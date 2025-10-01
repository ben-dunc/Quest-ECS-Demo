using System.Collections.Generic;
using com.cyborgAssets.inspectorButtonPro;
using Meta.XR.MRUtilityKit;
using UnityEditor;
using UnityEngine;

public class MeshCombiner : MonoBehaviour
{
    [ProButton]
    void RandomizeChildPositions(Vector3 minOffset, Vector3 maxOffset)
    {
        var children = GetComponentsInChildren<MeshRenderer>();

        foreach (var child in children)
        {
            if (child.gameObject != this.gameObject)
            {

                var position = child.transform.position;
                position += new Vector3(
                    UnityEngine.Random.Range(minOffset.x, maxOffset.x),
                    UnityEngine.Random.Range(minOffset.y, maxOffset.y),
                    UnityEngine.Random.Range(minOffset.z, maxOffset.z)
                );
                child.transform.position = position;
            }
        }
    }

    [ProButton]
    void RandomizeChildRotations(Vector3 minOffsetEuler, Vector3 maxOffsetEuler)
    {
        var children = GetComponentsInChildren<MeshRenderer>();

        foreach (var child in children)
        {
            if (child.gameObject != this.gameObject)
            {
                var rotation = child.transform.rotation.eulerAngles;
                rotation += new Vector3(
                    UnityEngine.Random.Range(minOffsetEuler.x, maxOffsetEuler.x),
                    UnityEngine.Random.Range(minOffsetEuler.y, maxOffsetEuler.y),
                    UnityEngine.Random.Range(minOffsetEuler.z, maxOffsetEuler.z)
                );
                child.transform.rotation = Quaternion.Euler(rotation);
            }
        }
    }

    [ProButton]
    void RandomizeChildScale(Vector3 minChange, Vector3 maxChange)
    {
        var children = GetComponentsInChildren<MeshRenderer>();

        foreach (var child in children)
        {
            if (child.gameObject != this.gameObject)
            {

                var scale = child.transform.localScale;
                scale += new Vector3(
                    UnityEngine.Random.Range(minChange.x, maxChange.x),
                    UnityEngine.Random.Range(minChange.y, maxChange.y),
                    UnityEngine.Random.Range(minChange.z, maxChange.z)
                );
                child.transform.localScale = scale;
            }
        }
    }

    [ProButton]
    void AssignMeshToAllMeshFilters(Mesh mesh)
    {
        var children = GetComponentsInChildren<MeshFilter>();

        foreach (var child in children)
        {
            if (child.gameObject != this.gameObject)
                child.sharedMesh = mesh;
        }
    }

    [ProButton]
    void CombineChildMeshes()
    {
        // initialize
        var childrenRenderers = GetComponentsInChildren<Renderer>();
        List<Vector3> vertices = new();
        List<Vector3> normals = new();
        List<Vector2> uvs = new();
        List<int> triangles = new();
        var material = childrenRenderers[0].sharedMaterial;

        int triangleIndex = 0;
        Transform t = transform;

        // iterate through childen
        foreach (var cr in childrenRenderers)
        {
            // if it has a mesh filter
            if (cr.TryGetComponent<MeshFilter>(out var cf) && cf.gameObject != this.gameObject)
            {
                Transform ct = cf.transform;
                var childMesh = cf.sharedMesh;
                foreach (Vector3 v in childMesh.vertices)
                    vertices.Add(t.InverseTransformPoint(ct.TransformPoint(v)));
                foreach (Vector3 n in childMesh.normals)
                    normals.Add(t.InverseTransformDirection(ct.TransformDirection(n)));
                uvs.AddRange(childMesh.uv);

                foreach (int tri in childMesh.triangles)
                    triangles.Add(tri + triangleIndex);

                triangleIndex += childMesh.vertexCount;
                cr.enabled = false;
            }
        }

        // assign
        Mesh mesh = new();
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.name = name;

        // light map
        if (!Unwrapping.GenerateSecondaryUVSet(mesh))
            Debug.LogError("Unable to generate secondary (lightmap) UVs");

        // assign mesh
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr == null)
            mr = gameObject.AddComponent<MeshRenderer>();
        mr.sharedMaterial = material;

        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null)
            mf = gameObject.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
    }
}
