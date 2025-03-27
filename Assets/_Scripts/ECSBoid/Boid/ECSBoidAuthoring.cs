using UnityEngine;
using Unity.Entities;

[RequireComponent(typeof(SpatialAgentAuthoring))]
public class ECSBoidAuthoring : MonoBehaviour
{
    private class Baker : Baker<ECSBoidAuthoring>
    {
        public override void Bake(ECSBoidAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new ECSBoidData
            {
                id = 0
            });
        }
    }
}