using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class SpatialAgentAuthoring : MonoBehaviour
{
    private class Baker : Baker<SpatialAgentAuthoring>
    {
        public override void Bake(SpatialAgentAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddSharedComponent(entity, new SpatialAgentData());
        }
    }
}
