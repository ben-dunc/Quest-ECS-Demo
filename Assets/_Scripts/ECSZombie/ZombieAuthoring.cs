using UnityEngine;
using Unity.Entities;

public class ZombieAuthoring : MonoBehaviour
{
    private class Baker : Baker<ZombieAuthoring>
    {
        public override void Bake(ZombieAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new ZombieData
            {
                id = 0,
            });
        }
    }
}