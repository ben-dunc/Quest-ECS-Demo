using Unity.Entities;
using UnityEngine;

public class BoidPrefabHolder : MonoBehaviour
{
    public static Entity entity;
    public GameObject gameObjectPrefab;
}

public struct BoidPrefabData : IComponentData
{
    public Entity entityPrefab;
}

public class BoidPrefabBaker : Baker<BoidPrefabHolder>
{
    public override void Bake(BoidPrefabHolder authoring)
    {
        // Register the Prefab in the Baker
        BoidPrefabHolder.entity = GetEntity(authoring.gameObjectPrefab, TransformUsageFlags.Dynamic);
        // Add the Entity reference to a component for instantiation later
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new BoidPrefabData() { entityPrefab = BoidPrefabHolder.entity });
    }
}