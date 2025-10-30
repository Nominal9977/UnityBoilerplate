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
    public Vector3 indexSpace;
}

public class Pathfinding : MonoBehaviour
{
    
    private List<Node> frontier = new List<Node>();
    
    private HashSet<Node> frontierSet = new HashSet<Node>();
    private HashSet<Vector3> cameFrom = new HashSet<Vector3>();
    [SerializeField] PathSmoothing smothing;
    
    private List<Node> debugPath = null;
    
    private Dictionary<Vector3, Node> world = new Dictionary<Vector3, Node>();
    
    [SerializeField] public MapGeneration mapGen;
    
    private float CellSpacing => mapGen.cellSize + mapGen.cellPadding;
    
    
    private int Heuristic(Node n1, Node n2, List<FlowUtility.InfluencePoint> influencePoints)
    {
        // Simple Manhattan distance for now - we'll add flow field back later
        Vector2Int gridPos1 = WorldToGridIndices(n1.position);
        Vector2Int gridPos2 = WorldToGridIndices(n2.position);
        
        int distance = Mathf.Abs(gridPos1.x - gridPos2.x) + Mathf.Abs(gridPos1.y - gridPos2.y);
        return distance * 10;
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
        smothing.SetPath(path);
        return path;
    }

    //Get the lowest F Cost
    private Node GetLowestFCost()
    {
        frontier.Sort((a, b) => a.f.CompareTo(b.f));
        Node lowest = frontier[0];
        frontier.RemoveAt(0);
        frontierSet.Remove(lowest); // Remove from the set too!
        return lowest;
    }

    //Get the cost of a Node
    private int GetCost(Node from, Node to)
    {
        // Simple cost for now - just distance
        float baseDistance = Vector3.Distance(from.position, to.position);
        return Mathf.RoundToInt(baseDistance * 10);
    }

    private bool IsValid(Vector3 pos)
    {
        // Convert world position to grid indices for checking
        Vector2Int gridPos = WorldToGridIndices(pos);
        
        // First check if it's within bounds
        if (gridPos.x < 0 || gridPos.x >= mapGen.mapWidth || 
            gridPos.y < 0 || gridPos.y >= mapGen.mapHeight)
        {
            return false;
        }
        
        // Check using grid coordinates
        bool isPositionBlocked = MapGeneration.IsPositionBlocked(new Vector2(gridPos.x, gridPos.y));
        
        return !isPositionBlocked;
    }

    //Get the list of neighbors
    private List<Node> GetNeighbours(Node n)
    {
        List<Node> neighbours = new List<Node>();
    
        // First, figure out which grid cell this node is in
        Vector2Int currentGrid = WorldToGridIndices(n.position);
    
        // Grid-based directions
        Vector2Int[] gridDirections = {
            Vector2Int.up,    // (0, 1)
            Vector2Int.down,  // (0, -1)
            Vector2Int.left,  // (-1, 0)
            Vector2Int.right  // (1, 0)
        };
    
        foreach (Vector2Int dir in gridDirections)
        {
            Vector2Int neighborGrid = currentGrid + dir;
        
            // Check bounds STRICTLY
            if (neighborGrid.x >= 0 && neighborGrid.x < mapGen.mapWidth &&
                neighborGrid.y >= 0 && neighborGrid.y < mapGen.mapHeight)
            {
                // Convert grid position back to world position
                Vector3 neighborWorldPos = GridToWorld(neighborGrid.x, neighborGrid.y);
                
                // ONLY get nodes that exist in our precomputed world dictionary
                if (world.ContainsKey(neighborWorldPos))
                {
                    // Check if blocked (using grid coordinates)
                    if (!MapGeneration.IsPositionBlocked(new Vector2(neighborGrid.x, neighborGrid.y)))
                    {
                        Node neighbor = world[neighborWorldPos];
                        neighbours.Add(neighbor);
                    }
                }
            }
        }
    
        return neighbours;
    }

    private Node GetOrCreateNode(Vector3 pos)
    {
        // ONLY return nodes that already exist in the world dictionary
        // Don't create new ones on the fly
        if (world.ContainsKey(pos))
        {
            return world[pos];
        }
        else
        {
            Debug.LogWarning($"Attempted to get node at {pos} which doesn't exist in world!");
            return null;
        }
    }


    public IEnumerator A_Star_Coroutine(Node start, Node goal, List<FlowUtility.InfluencePoint> influencePoints, System.Action<List<Node>> onComplete)
    {
        // Clear previous search state
        frontier.Clear();
        frontierSet.Clear();
        cameFrom.Clear();
        
        // Reset all nodes in the world
        foreach(var node in world.Values)
        {
            node.g = int.MaxValue;
            node.h = 0;
            node.f = int.MaxValue;
            node.parent = null;
        }
        
        frontier.Add(start);
        frontierSet.Add(start);
        
        start.g = 0;
        start.h = Heuristic(start, goal, influencePoints);
        start.f = start.g + start.h;
        start.parent = null;

        int iterations = 0;
        int maxIterations = 10000; // Safety limit
        
        Debug.Log($"A* Search started from {start.position} to {goal.position}");
        Debug.Log($"Start grid: {WorldToGridIndices(start.position)}, Goal grid: {WorldToGridIndices(goal.position)}");

        while(frontier.Count > 0 && iterations < maxIterations)
        {
            Node current = GetLowestFCost();
            iterations++;
            
            // Debug every 100 iterations
            if (iterations % 100 == 0)
            {
                Vector2Int currentGrid = WorldToGridIndices(current.position);
                Debug.Log($"Iteration {iterations}: Current grid ({currentGrid.x},{currentGrid.y}), f={current.f}, frontier size={frontier.Count}");
            }

            // Check if we reached the goal
            if (current == goal) // Direct node comparison since we're using precomputed nodes
            {
                Debug.Log($"Goal found after {iterations} iterations!");
                List<Node> path = ReconstructPath(current);
                onComplete?.Invoke(path);
                debugPath = path; // Store for gizmos
                yield break;
            }
            
            cameFrom.Add(current.position);

            List<Node> neighbors = GetNeighbours(current);
            
            if (iterations <= 5)
            {
                Vector2Int currentGrid = WorldToGridIndices(current.position);
                Debug.Log($"Iteration {iterations}: Exploring grid ({currentGrid.x},{currentGrid.y}), {neighbors.Count} neighbors");
            }

            foreach (Node neighbor in neighbors)
            {
                //Check if visited already
                if (cameFrom.Contains(neighbor.position))
                    continue;
                
                int tentativeG = current.g + GetCost(current, neighbor);
                
                // If neighbor is not in frontier, add it
                if (!frontierSet.Contains(neighbor))
                {
                    neighbor.g = tentativeG;
                    neighbor.h = Heuristic(neighbor, goal, influencePoints);
                    neighbor.f = neighbor.g + neighbor.h;
                    neighbor.parent = current;
                    frontier.Add(neighbor);
                    frontierSet.Add(neighbor);
                    
                    if (iterations <= 5)
                    {
                        Vector2Int nGrid = WorldToGridIndices(neighbor.position);
                        Debug.Log($"  Added neighbor grid ({nGrid.x},{nGrid.y}) with f={neighbor.f} (g={neighbor.g}, h={neighbor.h})");
                    }
                }
                // If we found a better path to this neighbor, update it
                else if (tentativeG < neighbor.g)
                {
                    neighbor.g = tentativeG;
                    neighbor.f = neighbor.g + neighbor.h;
                    neighbor.parent = current;
                    
                    if (iterations <= 5)
                    {
                        Vector2Int nGrid = WorldToGridIndices(neighbor.position);
                        Debug.Log($"  Updated neighbor grid ({nGrid.x},{nGrid.y}) with better g={neighbor.g}");
                    }
                }
            }

            if (iterations % 10 == 0)
                yield return null;
        }

        Debug.Log($"No path found after {iterations} iterations. Frontier empty: {frontier.Count == 0}, Max iterations: {iterations >= maxIterations}");
        
        // Debug: Show some explored positions
        Debug.Log($"Explored {cameFrom.Count} nodes out of {world.Count} total");
        
        onComplete?.Invoke(null);
    }
    
    public void StartPathfinding(Node start, Node goal, List<FlowUtility.InfluencePoint> influencePoints, System.Action<List<Node>> onComplete)
    {
        StartCoroutine(A_Star_Coroutine(start, goal, influencePoints, onComplete));
    }
    
    
    private Vector3 GridToWorld(int gx, int gy)
    {
        // Use MapGeneration's method to ensure consistency
        return mapGen.GridToWorldPosition(gx, gy) + mapGen.transform.position;
    }
    
    private Vector2Int WorldToGridIndices(Vector3 worldPos)
    {
        // Remove the transform offset to get local position
        Vector3 localPos = worldPos - mapGen.transform.position;
    
        // Calculate grid indices
        int gx = Mathf.RoundToInt(localPos.x / CellSpacing);
        int gy = Mathf.RoundToInt(localPos.z / CellSpacing);
    
        // Clamp to grid bounds
        gx = Mathf.Clamp(gx, 0, mapGen.mapWidth - 1);
        gy = Mathf.Clamp(gy, 0, mapGen.mapHeight - 1);
    
        return new Vector2Int(gx, gy);
    }
    
    //Set the world grid - precompute ALL valid nodes
    private void SetWorldGrid()
    {
        world.Clear(); // Clear existing nodes
    
        for (int x = 0; x < mapGen.mapWidth; x++)
        {
            for (int y = 0; y < mapGen.mapHeight; y++)
            {
                Vector3 worldPos = GridToWorld(x, y);
                Node node = new Node();
                node.position = worldPos;
                node.g = int.MaxValue;
                node.h = 0;
                node.f = int.MaxValue;
                node.parent = null;
                node.indexSpace = new Vector3(x, 0, y);
                world[worldPos] = node;
            }
        }
        
        Debug.Log($"World grid created with {world.Count} nodes for {mapGen.mapWidth}x{mapGen.mapHeight} grid");
    }
    
    private IEnumerator WaitForFlowFieldAndInitialize()
    {
        // Wait until flowField is populated
        while (MapGeneration.flowField == null || MapGeneration.flowField.Length == 0)
        {
            Debug.Log("Waiting for MapGeneration.flowField to be populated...");
            yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds
        }
    
        Debug.Log($"FlowField is ready! Size: {MapGeneration.flowField.GetLength(0)}x{MapGeneration.flowField.GetLength(1)}");
        SetWorldGrid();
    
        // Now initialize pathfinding
        if (world.Count < 2)
        {
            Debug.LogError("Not enough nodes in world to pathfind");
            yield break;
        }
        
        // Debug the start and goal positions
        Debug.Log($"Start point (grid): {mapGen.startPoint}");
        Debug.Log($"Target point (grid): {mapGen.targetPoint}");
        
        // Get the exact nodes from our precomputed world
        Vector3 startWorldPos = GridToWorld((int)mapGen.startPoint.x, (int)mapGen.startPoint.y);
        Vector3 goalWorldPos = GridToWorld((int)mapGen.targetPoint.x, (int)mapGen.targetPoint.y);
        
        if (!world.ContainsKey(startWorldPos))
        {
            Debug.LogError($"Start position {startWorldPos} not found in world!");
            yield break;
        }
        
        if (!world.ContainsKey(goalWorldPos))
        {
            Debug.LogError($"Goal position {goalWorldPos} not found in world!");
            yield break;
        }
        
        Node startNode = world[startWorldPos];
        Node goalNode = world[goalWorldPos];
        
        Debug.Log($"Start node (world): {startNode.position}");
        Debug.Log($"Goal node (world): {goalNode.position}");
        
        // Add this debugging check right before StartPathfinding
        Debug.Log($"Is start blocked? {MapGeneration.IsPositionBlocked(new Vector2(mapGen.startPoint.x, mapGen.startPoint.y))}");
        Debug.Log($"Is goal blocked? {MapGeneration.IsPositionBlocked(new Vector2(mapGen.targetPoint.x, mapGen.targetPoint.y))}");

        // Also check the neighbors of the goal
        Vector2Int goalGrid = new Vector2Int((int)mapGen.targetPoint.x, (int)mapGen.targetPoint.y);
        Debug.Log($"Goal neighbors:");
        Debug.Log($"  Left ({goalGrid.x-1},{goalGrid.y}): {MapGeneration.IsPositionBlocked(new Vector2(goalGrid.x-1, goalGrid.y))}");
        Debug.Log($"  Right ({goalGrid.x+1},{goalGrid.y}): {MapGeneration.IsPositionBlocked(new Vector2(goalGrid.x+1, goalGrid.y))}");
        Debug.Log($"  Down ({goalGrid.x},{goalGrid.y-1}): {MapGeneration.IsPositionBlocked(new Vector2(goalGrid.x, goalGrid.y-1))}");
        Debug.Log($"  Up ({goalGrid.x},{goalGrid.y+1}): {MapGeneration.IsPositionBlocked(new Vector2(goalGrid.x, goalGrid.y+1))}");

        StartPathfinding(startNode, goalNode, mapGen.influencePoints, (path) => {            
            if (path != null)
            {
                debugPath = path;
                Debug.Log("===== PATH FOUND =====");
                Debug.Log("Total nodes in path: " + path.Count);
                Debug.Log("===== PATH NODES =====");
        
                for (int i = 0; i < path.Count; i++)
                {
                    Debug.Log($"[{i}]: {path[i].position} (grid: {WorldToGridIndices(path[i].position)})");
                }
        
                Debug.Log("===== END PATH =====");
            }
            else
            {
                Debug.Log("No path found - check if there are obstacles blocking the path");
            }
        });
    }
    
    private void OnDrawGizmos()
    {
        // Draw explored nodes in gray
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        foreach (Vector3 pos in cameFrom)
        {
            Gizmos.DrawCube(pos, Vector3.one * 0.8f);
        }

        // Draw path in bright green (drawn after so it's on top)
        if (debugPath != null && debugPath.Count > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < debugPath.Count - 1; i++)
            {
                Gizmos.DrawLine(debugPath[i].position + Vector3.up * 0.5f, 
                    debugPath[i + 1].position + Vector3.up * 0.5f);
                Gizmos.DrawCube(debugPath[i].position, Vector3.one * 0.5f);
            }
            // Draw the last node
            Gizmos.DrawCube(debugPath[debugPath.Count - 1].position, Vector3.one * 0.5f);
        }
        
        // Draw blocked cells in red
        if (mapGen != null && Application.isPlaying)
        {
            Gizmos.color = Color.red;
            for (int x = 0; x < mapGen.mapWidth; x++)
            {
                for (int y = 0; y < mapGen.mapHeight; y++)
                {
                    if (MapGeneration.IsPositionBlocked(new Vector2(x, y)))
                    {
                        Vector3 worldPos = GridToWorld(x, y);
                        Gizmos.DrawWireCube(worldPos, Vector3.one * 0.9f);
                    }
                }
            }
            
            // Draw start in blue
            if (mapGen.startPoint != null)
            {
                Gizmos.color = Color.blue;
                Vector3 startWorld = GridToWorld((int)mapGen.startPoint.x, (int)mapGen.startPoint.y);
                Gizmos.DrawWireSphere(startWorld, 0.5f);
            }
            
            // Draw goal in yellow
            if (mapGen.targetPoint != null)
            {
                Gizmos.color = Color.yellow;
                Vector3 goalWorld = GridToWorld((int)mapGen.targetPoint.x, (int)mapGen.targetPoint.y);
                Gizmos.DrawWireSphere(goalWorld, 0.5f);
            }
        }
    }
    
    //Start the A* search
    public void StartPathfinding()
    {
        StartCoroutine(WaitForFlowFieldAndInitialize());
    }
}