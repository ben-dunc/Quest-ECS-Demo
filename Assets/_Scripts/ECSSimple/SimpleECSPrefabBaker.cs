using Unity.Entities;
using UnityEngine;

public class GetPrefabAuthoring : MonoBehaviour
{
    public static Entity entity;
    public GameObject gameObjectPrefab;
}

public struct EntityPrefabHolder : IComponentData
{
    public Entity entityPrefab;
}

public class SimplePrefabBaker : Baker<GetPrefabAuthoring>
{
    public override void Bake(GetPrefabAuthoring authoring)
    {
        // Register the Prefab in the Baker
        GetPrefabAuthoring.entity = GetEntity(authoring.gameObjectPrefab, TransformUsageFlags.Dynamic);
        // Add the Entity reference to a component for instantiation later
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new EntityPrefabHolder() { entityPrefab = GetPrefabAuthoring.entity });
    }
}