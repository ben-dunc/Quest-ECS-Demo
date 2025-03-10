using Unity.Entities;
using UnityEngine;

public partial struct SimpleECSData : IComponentData
{
    public int id;
    public int row;
    public int column;
}
