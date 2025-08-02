using Unity.Entities;
using UnityEngine;

public class ECSQuadAuthoring : MonoBehaviour
{
    class Baker : Baker<ECSQuadAuthoring>
    {
        public override void Bake(ECSQuadAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new ECSQuadData { });
        }
    }
}
