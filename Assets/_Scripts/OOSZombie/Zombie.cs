using UnityEngine;

public class Zombie : MonoBehaviour
{
    public float turningAngle = 10f;
    public float speed;
    public int id = 0;
    public Animator animator;

    void Start()
    {
        animator.playbackTime = Random.Range(0, 3f);
        turningAngle = (Random.Range(0, 1f) * ZombieManager.Instance.rotateMaxMin * 2) - ZombieManager.Instance.rotateMaxMin;

        speed = (Random.Range(0, 1f) * ZombieManager.Instance.speed / 2) + (ZombieManager.Instance.speed / 2);

        Vector3 pos = transform.position;
        pos.x = (Random.Range(0, 1f) * ZombieManager.Instance.zombieRange * 2) - (ZombieManager.Instance.zombieRange / 2);
        pos.z = (Random.Range(0, 1f) * ZombieManager.Instance.zombieRange * 2) - (ZombieManager.Instance.zombieRange / 2);
        pos += ZombieManager.Instance.transform.position;
        pos.y = 0;
        transform.position = pos;

        transform.Rotate(Vector3.up, 360f * Random.Range(0, 1f));
    }

    void Update()
    {
        transform.position += transform.forward * ZombieManager.Instance.speed * Time.deltaTime;

        if (transform.position.magnitude - ZombieManager.Instance.transform.position.magnitude > ZombieManager.Instance.zombieRange)
        {
            Vector3 pos = transform.position;
            pos.x = (Random.Range(0f, 1f) * ZombieManager.Instance.zombieRange) - (ZombieManager.Instance.zombieRange / 2);
            pos.z = (Random.Range(0f, 1f) * ZombieManager.Instance.zombieRange) - (ZombieManager.Instance.zombieRange / 2);
            pos += ZombieManager.Instance.transform.position;
            pos.y = 0;
            transform.position = pos;
        }

        transform.Rotate(Vector3.up, 5f * Time.deltaTime * ZombieManager.Instance.rotateRate);

        if (id > ZombieManager.Instance.numZombies)
        {
            ZombieManager.Instance.numOOSZombies--;
            Destroy(gameObject);
        }
    }
}
