using Unity.Entities;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class SimpleECSSpawner : MonoBehaviour
{
    public GameObject simpleEntityPrefab;

    void Start()
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        // entityManager.Instantiate(simpleEntityPrefab);
    }
}
