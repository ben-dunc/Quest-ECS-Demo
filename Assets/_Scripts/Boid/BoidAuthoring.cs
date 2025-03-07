using UnityEngine;
using Unity.Entities;

[RequireComponent(typeof(SpatialAgentAuthoring))]
public class BoidAuthoring : MonoBehaviour
{
    public bool isStatic = false;
    public bool isRepeller = false;

    private class Baker : Baker<BoidAuthoring>
    {
        public override void Bake(BoidAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new BoidData
            {
                isStatic = authoring.isStatic,
                isRepeller = authoring.isRepeller
            });
        }
    }
}