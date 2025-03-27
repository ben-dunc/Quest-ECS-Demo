
using UnityEngine;

public abstract class TestManager : MonoBehaviour
{
    public abstract void SetTargetNumEntities(int num);
    public abstract int GetNumEntities();
    public abstract int GetTargetNumEntities();
}