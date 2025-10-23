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
    
    
    // Update is called once per frame
    void Update()
    {
        
    }

    private int Huristic(Node n1, Node n2)
    {
        return (int)Vector3.Distance(n1.position, n2.position);
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
        //Loop through all nodes and check to see if that pos is in the frontier
        foreach (Node node in frontier)
        {
            if (node.position == pos)
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


    public List<Node> A_Star(Node start, Node goal)
    {
        //Initialize the Node properties
        frontier.Add(start);
        cameFrom.Clear();
        
        //Bootstrap Case
        start.g = 0;
        start.h = Huristic(start, goal);
        start.f = start.g + start.h;
        start.parent = null;
        
        //Now go through and pathfind
        while(frontier.Count > 0)
        {
            Node current = GetLowestFCost();

            if (current == goal)
                return ReconstructPath(current);

            //Iterate through neighs
            foreach (Node neighbor in GetNeighbours(current))
            {
                
                //Check if the node is already in the frontier
                if (frontier.Contains(neighbor))
                    continue;
                
                int tentativeG = current.g + GetCost(current, neighbor);

                if (tentativeG < goal.g || !cameFrom.Contains(neighbor))
                {
                    neighbor.g = tentativeG;
                    neighbor.h = Huristic(neighbor, goal);
                    neighbor.f = neighbor.g + neighbor.h;
                    neighbor.parent = current;

                    if (!frontier.Contains(neighbor))
                    {
                        frontier.Add(neighbor);
                    }
                }
            }
        }
        
        
        
        return null;
    }
    
}
