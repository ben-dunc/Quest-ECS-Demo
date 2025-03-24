using Unity.Entities;
using UnityEngine;

public class ZombiePrefabHolder : MonoBehaviour
{
    public static Entity entity;
    public GameObject gameObjectPrefab;
}

public struct ZombiePrefabData : IComponentData
{
    public Entity entityPrefab;
}

public class ZombiePrefabBaker : Baker<ZombiePrefabHolder>
{
    public override void Bake(ZombiePrefabHolder authoring)
    {
        // Register the Prefab in the Baker
        ZombiePrefabHolder.entity = GetEntity(authoring.gameObjectPrefab, TransformUsageFlags.Dynamic);
        // Add the Entity reference to a component for instantiation later
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new ZombiePrefabData() { entityPrefab = ZombiePrefabHolder.entity });
    }
}