using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public class Node //Node class that each node will have
{
    public int g;
    public int h; //Estimated goal
    public int f; //Total estimated cost
    public Node parent; //For reconstruction
    public Vector3 position;
}

public class Pathfinding : MonoBehaviour
{
    
    private List<Node> frontier = new List<Node>();
    private HashSet<Node> cameFrom = new HashSet<Node>();
    
    private Dictionary<Vector3, Node> world = new Dictionary<Vector3, Node>();
    
    [SerializeField] public MapGeneration mapGen;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(WaitForFlowFieldAndInitialize());
    }
    
    
    private int Heuristic(Node n1, Node n2, List<FlowUtility.InfluencePoint> influencePoints)
    {
        Vector2 pos2D = new Vector2(n1.position.x, n1.position.z);
        Vector2 goal2D = new Vector2(n2.position.x, n2.position.z);
        Vector2 directionToGoal = (goal2D - pos2D).normalized;

        // Get array dimensions
        int width = MapGeneration.flowField.GetLength(0);
        int height = MapGeneration.flowField.GetLength(1);
    
        // Clamp positions to valid range
        int x = Mathf.Clamp((int)pos2D.x, 0, width - 1);
        int y = Mathf.Clamp((int)pos2D.y, 0, height - 1);
    
        // Get flow vector at this point
        var flowVector = MapGeneration.flowField[x, y];

        // Calculate flow alignment
        float flowAlignment = Vector2.Dot(directionToGoal, flowVector);

        float distance = Vector2.Distance(pos2D, goal2D);

        // Apply flow bonus/penalty to heuristic
        float heuristic = distance * (1f - (flowAlignment * 0.3f));

        return (int)heuristic;
    }

    private List<Node> ReconstructPath(Node n1)
    {
        List<Node> path = new List<Node>();
        Node current = n1;
    
        while (current != null)
        {
            path.Add(current);
            current = current.parent;
        }
    
        path.Reverse();
        return path;
    }

    //Get the lowest F Cost
    private Node GetLowestFCost()
    {
        frontier.Sort((a, b) => a.f.CompareTo(b.f));
        Node lowest = frontier[0];
        frontier.RemoveAt(0);
        return lowest;
    }

    //Get the cost of a Node
    private int GetCost(Node n1, Node n2)
    {
        return (int)Vector3.Distance(n1.position, n2.position);
    }

    private bool IsValid(Vector3 pos)
    {
        bool isPositionBlocked = MapGeneration.IsPositionBlocked(pos);
        
        // If the position isnt blocked return true
        if (!isPositionBlocked)
        {
            return true;
        }
        return false;
    }

    //Get the list of neighbors
    private List<Node> GetNeighbours(Node n)
    {
        List<Node> neighbours = new List<Node>();
    
        // Example for a grid-based system:
        Vector3[] directions = {
            Vector3.forward, Vector3.back, 
            Vector3.left, Vector3.right
        };
    
        foreach (Vector3 dir in directions)
        {
            Vector3 neighborPos = n.position + dir;
            // Check if this position is valid and create/get the node
            if (IsValid(neighborPos))
            {
                Node neighbor = GetOrCreateNode(neighborPos);
                neighbours.Add(neighbor);
            }
        }
    
        return neighbours;
    }

    private Node GetOrCreateNode(Vector3 pos)
    {
        if (world.ContainsKey(pos))
        {
            return world[pos];
        }
        else
        {
            Node newNode = new Node();
            newNode.position = pos;
            world[pos] = newNode;
            return newNode;
        }
    }


    public IEnumerator A_Star_Coroutine(Node start, Node goal, List<FlowUtility.InfluencePoint> influencePoints, System.Action<List<Node>> onComplete)
    {
        frontier.Add(start);
        cameFrom.Clear();

        start.g = 0;
        start.h = Heuristic(start, goal, influencePoints);
        start.f = start.g + start.h;
        start.parent = null;

        int iterations = 0;
        Debug.Log("A* Search started");

        while(frontier.Count > 0)
        {
            Node current = GetLowestFCost();
            iterations++;

            if (iterations % 100 == 0)
            {
                Debug.Log($"A* Iteration {iterations}, Frontier size: {frontier.Count}");
            }

            if (current.position == goal.position)
            {
                Debug.Log($"Goal found after {iterations} iterations!");
                List<Node> path = ReconstructPath(current);
                onComplete?.Invoke(path);
                yield break;
            }
            
            cameFrom.Add(current);

            foreach (Node neighbor in GetNeighbours(current))
            {
                //Check if visited already
                if (cameFrom.Contains(neighbor))
                    continue;
    
                int tentativeG = current.g + GetCost(current, neighbor);

                // If neighbor is not in frontier, add it
                if (!frontier.Contains(neighbor))
                {
                    neighbor.g = tentativeG;
                    neighbor.h = Heuristic(neighbor, goal, influencePoints);
                    neighbor.f = neighbor.g + neighbor.h;
                    neighbor.parent = current;
                    frontier.Add(neighbor);
                }
                // If we found a better path to this neighbor, update it
                else if (tentativeG < neighbor.g)
                {
                    neighbor.g = tentativeG;
                    neighbor.f = neighbor.g + neighbor.h;
                    neighbor.parent = current;
                }
            }

            yield return null;
        }

        Debug.Log($"No path found after {iterations} iterations. Frontier empty.");
        onComplete?.Invoke(null);
    }
    
    public void StartPathfinding(Node start, Node goal, List<FlowUtility.InfluencePoint> influencePoints, System.Action<List<Node>> onComplete)
    {
        StartCoroutine(A_Star_Coroutine(start, goal, influencePoints, onComplete));
    }
    
    //Set the world grid
    private void SetWorldGrid()
    {
        var flowField = MapGeneration.flowField;
    
        Debug.Log($"Loading {flowField.Length} positions from flowField");
        for (int x = 0; x < mapGen.mapWidth; x++)
        {
            for (int y = 0; y < mapGen.mapHeight; y++)
            {
                Vector3 pos = new Vector3(x, 0, y);
                var node = GetOrCreateNode(pos);
            }
        }
        
        Debug.Log($"Loaded {world.Count} nodes into world");
    }
    
    private IEnumerator WaitForFlowFieldAndInitialize()
    {
        // Wait until flowField is populated
        while (MapGeneration.flowField == null || MapGeneration.flowField.Length == 0)
        {
            Debug.Log("Waiting for MapGeneration.flowField to be populated...");
            yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds
        }
    
        Debug.Log("FlowField is ready!");
        SetWorldGrid();
    
        // Now initialize pathfinding
        if (world.Count < 2)
        {
            Debug.LogError("Not enough nodes in world to pathfind");
            yield break;
        }
        
        Node startNode = GetOrCreateNode(mapGen.startPoint);
        Node goalNode = GetOrCreateNode(mapGen.targetPoint);
        

        Debug.Log($"Starting pathfind from {startNode.position} to {goalNode.position}");

        StartPathfinding(startNode, goalNode, mapGen.influencePoints, (path) => {            
            if (path != null)
            {
                Debug.Log("===== PATH FOUND =====");
                Debug.Log("Total nodes in path: " + path.Count);
                Debug.Log("===== PATH NODES =====");
        
                for (int i = 0; i < path.Count; i++)
                {
                    Debug.Log($"[{i}]: {path[i].position}");
                }
        
                Debug.Log("===== END PATH =====");
            }
            else
            {
                Debug.Log("No path found");
            }
        });
    }
    
}
