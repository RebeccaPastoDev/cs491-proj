using UnityEngine;
using System.Collections.Generic;
public class LunarPathfinder
{
    public List <LunarNode> FindPath (LunarNode start, LunarNode goal, double minTraversableEdgeCost)
    {
        List<LunarNode> openSet = new List<LunarNode>();
        openSet.Add(start);

        HashSet<LunarNode> closedSet = new HashSet<LunarNode>();

        Dictionary<LunarNode, double> gScore = new Dictionary<LunarNode, double>();
        gScore[start] = 0.0;

        Dictionary<LunarNode, double> fScore = new Dictionary<LunarNode, double>();
        fScore[start] = gScore[start] + CalculateHeuristic(start, goal, minTraversableEdgeCost);

        Dictionary<LunarNode, LunarNode> cameFrom = new Dictionary<LunarNode, LunarNode>();

        while (openSet.Count > 0)
        {
            LunarNode current = openSet[0];

            foreach (LunarNode node in openSet)
            {
                if(fScore[node] < fScore[current])
                {
                    current = node;
                }
            }


                if (current == goal)
            {
                return ReconstructPath(cameFrom, current);
            }

                openSet.Remove(current);
                closedSet.Add(current);


            foreach(LunarEdge edge in current.edges)
            {
                if (!edge.traversable)
                {
                    continue;
                }

                LunarNode neighbor = edge.destination;

                if (closedSet.Contains(neighbor))
                {
                    continue;
                }

                double tentativeGScore = gScore[current] + edge.cost; 

                if(!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    gScore[neighbor] = tentativeGScore;

                    cameFrom[neighbor] = current;

                    fScore[neighbor] = gScore[neighbor] + CalculateHeuristic(neighbor, goal, minTraversableEdgeCost);

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }

        }

        return new List<LunarNode>();
    }


    
private double CalculateHeuristic(
    LunarNode current, LunarNode goal, double minTraversableEdgeCost)
{
    int rowDifference = System.Math.Abs(goal.row - current.row);
    int colDifference = System.Math.Abs(goal.col - current.col);

    int minimumMoves = System.Math.Max(rowDifference, colDifference);

    return minimumMoves * minTraversableEdgeCost;
}


    private List<LunarNode> ReconstructPath(
        Dictionary<LunarNode, LunarNode> cameFrom, LunarNode current)
    {
        
        List<LunarNode> path = new List<LunarNode>();

        path.Add(current);

        while(cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);

        }
            path.Reverse();
            return path;
    }
    


}
