using Unity.Entities;
using UnityEngine;

public class ECSBoidPrefabHolder : MonoBehaviour
{
    public static Entity entity;
    public GameObject gameObjectPrefab;
}

public struct ECSBoidPrefabData : IComponentData
{
    public Entity entityPrefab;
}

public class ECSBoidPrefabBaker : Baker<ECSBoidPrefabHolder>
{
    public override void Bake(ECSBoidPrefabHolder authoring)
    {
        // Register the Prefab in the Baker
        ECSBoidPrefabHolder.entity = GetEntity(authoring.gameObjectPrefab, TransformUsageFlags.Dynamic);
        // Add the Entity reference to a component for instantiation later
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new ECSBoidPrefabData() { entityPrefab = ECSBoidPrefabHolder.entity });
    }
}