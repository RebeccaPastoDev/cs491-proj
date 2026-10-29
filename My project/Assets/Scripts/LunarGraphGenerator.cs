using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Assemblies;
public class LunarGraphGenerator
{
    public LunarNode[] GenerateGraph(double3[] grid, int gridDimension)
    {
        LunarNode[] nodes = new LunarNode[grid.Length];

        for (int row = 0; row < gridDimension; row++) 
        {
            for (int col = 0; col < gridDimension; col++)
            {
                int index = row * gridDimension + col;

                LunarNode node = new LunarNode();

                node.row = row;

                node.col = col;

                node.position = grid[index];

                nodes[index] = node;
            }
        }

        for (int row = 0 ; row < gridDimension ; row++)
        {
            for (int col = 0; col < gridDimension ; col++)
            {

                int index = row * gridDimension + col;

                LunarNode current = nodes[index];


                for (int rowOffset = -1; rowOffset <= 1 ; rowOffset++)
                {

                    for (int colOffset = -1; colOffset <=1; colOffset++)
                    {
                        if(rowOffset == 0 && colOffset == 0)
                        {
                            continue;
                        }

                        int neighborRow = row + rowOffset;
                        int neighborCol = col + colOffset;

                        if (neighborRow < 0 || neighborRow >= gridDimension || neighborCol < 0 || neighborCol >= gridDimension)
                        {
                            continue;
                        }

                        int neighborIndex = neighborRow * gridDimension + neighborCol;

                        LunarNode neighbor = nodes[neighborIndex];

                        LunarEdge edge = new LunarEdge();

                        edge.destination = neighbor;
                        edge.cost = 0.0;
                        edge.traversable = true;

                        current.edges.Add(edge);
                    }
                }
            }
        }

return nodes;

    }
}
