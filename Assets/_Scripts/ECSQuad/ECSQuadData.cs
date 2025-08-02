using Unity.Entities;
using UnityEngine;

public partial struct ECSQuadData : IComponentData
{
    public int id;
    public int row;
    public int column;
}
