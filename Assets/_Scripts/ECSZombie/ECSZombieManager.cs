
public class ECSZombieManager : TestManager
{
    public static ECSZombieManager Instance;

    public int numZombies = 10;
    public float speed = 2;
    public float rotateRate = 5;
    public float rotateMaxMin = 5f;
    public float zombieRange = 50;

    public override int GetNumEntities()
    {
        return ECSZombieSpawnerSystem.currentNumEntities;
    }

    public override int GetTargetNumEntities()
    {
        return numZombies;
    }

    public override void SetTargetNumEntities(int num)
    {
        numZombies = num;
    }

    void Awake()
    {
        Instance = this;
    }
}
