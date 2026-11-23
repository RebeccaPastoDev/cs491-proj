using UnityEngine; 
using Unity.Mathematics; //Double3FunctionForStoringCoords
using CesiumForUnity;
using System.Threading.Tasks;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using System.Collections.Generic;
using System.Data; //TerrainSamplingFunctions


public class LunarTerrainSampler : MonoBehaviour
{

    public Cesium3DTileset moonTiles;


double convertMet2DegStep(double moonRadius, double vSize, double elevation)
    {
        double latDegreesPerMove = (vSize*180/((moonRadius+ elevation)*System.Math.PI));

        return latDegreesPerMove;
    }

double convertLat2LongStep(double latitude, double latDegreesPerMove)
    {
        double longDegreesPerMove = latDegreesPerMove/(System.Math.Cos((latitude * (System.Math.PI/180))));

        return longDegreesPerMove;
    }

   [ContextMenu("Sample Moon Terrain")] //allows triggering from inspector
   async void SampleMoonTerrain() //runs once, async allows await function
    {
        
double latitude = -88.76053;

double longitude = -107.5095;

double3 location = new double3(longitude, latitude, 0.0); //long, lat, height

if (moonTiles == null)
        {
            Debug.LogError("No MoonTiles Found");
            return;
        }


double3 position = await AcquireTerrainData(location);


int gridDimension = 101;

double3[] grid = await GenerateGrid(position, gridDimension);

LunarGraphGenerator graphGenerator = new LunarGraphGenerator();

LunarNode[] nodes = graphGenerator.GenerateGraph(grid, gridDimension);

Debug.Log("Total Nodes: " + nodes.Length);
Debug.Log("Corner node edges: " + nodes[0].edges.Count);
Debug.Log("Center node edges: " + nodes[5100].edges.Count);

LunarNode center = nodes[5100];

foreach (LunarEdge edge in center.edges)
{
    Debug.Log(
        "Neighbor Row: " + edge.destination.row +
        " | Neighbor Col: " + edge.destination.col +
        " | Horizontal Distance: " + edge.horizontalDistance + " meters" +
        " | Slope: " + edge.slopeDegrees + " degrees" +
        " | Edge Cost: " + edge.cost
    );
}

int traversableEdges = 0;
int blockedEdges = 0;


foreach (LunarNode node in nodes)
{
    foreach (LunarEdge edge in node.edges)
    {
        if (edge.traversable)
        {
            traversableEdges++;
        }
        else
        {
            blockedEdges++;
        }
    }
}

Debug.Log("Traversable edges: " + traversableEdges);
Debug.Log("Blocked edges: " + blockedEdges);

double minTraversableEdgeCost = double.PositiveInfinity;

foreach(LunarNode node in nodes)
        {
            foreach (LunarEdge edge in node.edges)
            {
                if(edge.traversable && edge.cost < minTraversableEdgeCost)
                {
                    minTraversableEdgeCost = edge.cost;
                }
            }
        }

Debug.Log("Minimum Traversable Edge Cost: " + minTraversableEdgeCost);

LunarPathfinder pathfinder = new LunarPathfinder();

List<LunarNode> path = pathfinder.FindPath(nodes[5100], nodes[7650], minTraversableEdgeCost);

LunarPathVisualizer visualizer = GetComponent<LunarPathVisualizer>();

if (visualizer != null)
        {
            
            visualizer.DrawPath(path);
        }
        else
        {
            Debug.LogError("LunarPathVisualizer not found!");
        }

Debug.Log("A* Path Node Count : " + path.Count);

foreach (LunarNode node in path)
        {
            Debug.Log("Path Node Row: " + node.row + " | Path Node Col: " + node.col);
        }


double totalPathCost = 0.0;

for (int i = 0; i < path.Count -1 ; i++)
        {
            
            LunarNode current = path[i];
            LunarNode next = path[i + 1];

            LunarEdge edge = current.edges.Find(e => e.destination == next);

            if (edge == null || !edge.traversable)
            {
                
                Debug.LogError("Invalid path connection!");
                break;

            }

            totalPathCost += edge.cost;

        }
        
        Debug.Log("A* Total Path Cost : " + totalPathCost);


        // Existing A* path cost calculation
for (int i = 0; i < path.Count - 1; i++)
{
    // Your existing code
}

Debug.Log("A* Total Path Cost: " + totalPathCost);

// ADD THE NEW DIJKSTRA TEST HERE
List<LunarNode> dijkstraPath = pathfinder.FindPath(
    nodes[5100], nodes[7650], 0.0);

double dijkstraCost = 0.0;

for (int i = 0; i < dijkstraPath.Count - 1; i++)
{
    LunarNode current = dijkstraPath[i];
    LunarNode next = dijkstraPath[i + 1];

    LunarEdge edge = current.edges.Find(e => e.destination == next);

    if (edge == null || !edge.traversable)
    {
        Debug.LogError("Invalid Dijkstra path connection!");
        break;
    }

    dijkstraCost += edge.cost;
}

Debug.Log("Dijkstra Path Node Count: " + dijkstraPath.Count);
Debug.Log("Dijkstra Total Path Cost: " + dijkstraCost);


    }




async Task<double3> AcquireTerrainData(double3 location) //task = async void
    {

        CesiumSampleHeightResult result = await moonTiles.SampleHeightMostDetailed(location);

        if (result.sampleSuccess[0])
        {
            double3 position = result.longitudeLatitudeHeightPositions[0];

            Debug.Log("Longitude : " + position.x);
            Debug.Log("Latitude : " + position.y);
            Debug.Log("Elevation: " + position.z);

            return position;
        }

        else
        {
            Debug.LogWarning("Sample Failed");

            foreach(string warning in result.warnings)
            {
                Debug.LogWarning(warning);
            }

            throw new System.Exception("Terrain Data Sample Failed");
        }

    }

async Task<double3[]> GenerateGrid(double3 position, int gridDimension)
    {
        double vSize = 5.0;
        double moonRadius = 1737400.0;
        
        


        double latDegreesPerMove = convertMet2DegStep(moonRadius, vSize, position.z);

        double longDegreesPerMove = convertLat2LongStep(position.y, latDegreesPerMove);

        double3[] grid = new double3[gridDimension * gridDimension];

    
        for (int row = 0; row < gridDimension; row++)
        {
            for (int col = 0; col < gridDimension; col++)
            {
                int index = row*gridDimension + col;

                double rowOffset = row - ((gridDimension-1) / 2.0);
                double colOffset = col - ((gridDimension-1) / 2.0);

                double lat= position.y - (rowOffset *latDegreesPerMove);
                double longi = position.x -(colOffset*longDegreesPerMove);

                grid[index] = new double3(longi, lat, 0.0);
            }
        }    
   
          
int batchSize = 256;

for (int start = 0; start < grid.Length; start += batchSize)
{
    int count = System.Math.Min(batchSize, grid.Length - start);

    double3[] positions = new double3[count];

    System.Array.Copy(grid, start, positions, 0, count);

    CesiumSampleHeightResult result =
        await moonTiles.SampleHeightMostDetailed(positions);

    for (int i = 0; i < count; i++)
    {
        if (!result.sampleSuccess[i])
        {
            throw new System.Exception(
                "Failed to sample vertex " + (start + i)
            );
        }

        grid[start + i] =
            result.longitudeLatitudeHeightPositions[i];
    }
    Debug.Log("Vertices sampled: " + (start + count) + "/" + grid.Length);
}



        return grid;
    }

  



}
