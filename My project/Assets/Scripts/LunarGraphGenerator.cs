using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Assemblies;
public class LunarGraphGenerator
{
    public LunarNode[] GenerateGraph(double3[] grid, int gridDimension)
    {

        double maxTraversableSlope = 15.0;
        double slopePenaltyWeight = 0.5;

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
                        edge.horizontalDistance = CalculateHorizontalDistance(current, neighbor);
                        edge.slopeDegrees = CalculateSlopeDegrees(current, neighbor, edge.horizontalDistance);
                        edge.cost = edge.horizontalDistance * (1.0 + slopePenaltyWeight * (System.Math.Abs(edge.slopeDegrees ) / maxTraversableSlope));
                        edge.traversable = System.Math.Abs(edge.slopeDegrees) <= maxTraversableSlope;

                        current.edges.Add(edge);
                    }
                }
            }
        }

return nodes;

    }


double CalculateHorizontalDistance(LunarNode a, LunarNode b)
    {
        
        double moonRadius = 1737400.0;

        double averageElevation = (a.position.z + b.position.z) / 2.0;

        double radius = moonRadius + averageElevation;

        double lat1 = a.position.y * System.Math.PI /180.0;
        double lat2 = b.position.y * System.Math.PI / 180.0;

        double deltaLat = lat2 - lat1;

        double deltaLon = (b.position.x - a.position.x) * System.Math.PI/ 180.0;

        double averageLat = (lat1 + lat2) / 2.0;

        double northSouth = radius * deltaLat;

        double eastWest = radius * System.Math.Cos(averageLat) * deltaLon;

        double distance = System.Math.Sqrt(northSouth * northSouth + eastWest * eastWest);

        return distance;

    }


double CalculateSlopeDegrees(LunarNode a, LunarNode b, double horizontalDistance)
    {
        double elevationDifference = b.position.z - a.position.z;

        double slopeRadians = System.Math.Atan2(

            elevationDifference,
            horizontalDistance

        );

        double slopeDegrees = slopeRadians * (180.0/ System.Math.PI);

        return slopeDegrees;





    }



}
