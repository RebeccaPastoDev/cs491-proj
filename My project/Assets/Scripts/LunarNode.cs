using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;
public class LunarNode
{
    public int row;
    public int col;

    public double3 position;

    public List<LunarEdge> edges = new List<LunarEdge>();
}
