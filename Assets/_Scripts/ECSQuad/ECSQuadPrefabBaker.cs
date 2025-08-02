using Unity.Entities;
using UnityEngine;

public class GetQuadPrefabAuthoring : MonoBehaviour
{
    public static Entity entity;
    public GameObject gameObjectPrefab;
}

public struct QuadPrefabHolder : IComponentData
{
    public Entity entityPrefab;
}

public class QuadPrefabBaker : Baker<GetQuadPrefabAuthoring>
{
    public override void Bake(GetQuadPrefabAuthoring authoring)
    {
        // Register the Prefab in the Baker
        GetQuadPrefabAuthoring.entity = GetEntity(authoring.gameObjectPrefab, TransformUsageFlags.Dynamic);
        // Add the Entity reference to a component for instantiation later
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new QuadPrefabHolder() { entityPrefab = GetQuadPrefabAuthoring.entity });
    }
}