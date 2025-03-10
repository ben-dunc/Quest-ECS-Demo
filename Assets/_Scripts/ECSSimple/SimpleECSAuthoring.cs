using Unity.Entities;
using UnityEngine;

public class SimpleECSAuthoring : MonoBehaviour
{
    class Baker : Baker<SimpleECSAuthoring>
    {
        public override void Bake(SimpleECSAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new SimpleECSData { });
        }
    }
}
