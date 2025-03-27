using Unity.Entities;
using UnityEngine;

public class ECSZombiePrefabHolder : MonoBehaviour
{
    public static Entity entity;
    public GameObject gameObjectPrefab;
}

public struct ECSZombiePrefabData : IComponentData
{
    public Entity entityPrefab;
}

public class ECSZombiePrefabBaker : Baker<ECSZombiePrefabHolder>
{
    public override void Bake(ECSZombiePrefabHolder authoring)
    {
        // Register the Prefab in the Baker
        ECSZombiePrefabHolder.entity = GetEntity(authoring.gameObjectPrefab, TransformUsageFlags.Dynamic);
        // Add the Entity reference to a component for instantiation later
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new ECSZombiePrefabData() { entityPrefab = ECSZombiePrefabHolder.entity });
    }
}