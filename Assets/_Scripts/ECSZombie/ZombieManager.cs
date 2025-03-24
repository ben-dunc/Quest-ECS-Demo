using UnityEngine;

public class ZombieManager : MonoBehaviour
{
    public static ZombieManager Instance;

    public int numZombies = 10;
    public float speed = 2;
    public float rotateRate = 5;
    public float rotateMaxMin = 5f;
    public float zombieRange = 50;

    [Header("OOS Spawning")]
    public bool doSpawnOOSPrefabs = false;
    public GameObject oosPrefab;
    public int numOOSZombies = 0;

    void Awake()
    {
        Instance = this;
        numOOSZombies = 0;
    }

    void Update()
    {
        while (doSpawnOOSPrefabs && numOOSZombies < numZombies)
        {
            var zombie = Instantiate(oosPrefab).GetComponent<Zombie>();
            zombie.id = ++numOOSZombies;
        }
    }
}
