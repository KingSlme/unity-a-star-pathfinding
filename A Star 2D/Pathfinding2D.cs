using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System;

public class Pathfinding2D : MonoBehaviour
{   
    [SerializeField] private bool _printTimeForPath = false;
    private Grid2D _grid;
    private Heap2D<Node2D> _openSet;
    private HashSet<Node2D> _closedSet;

    public float NodeRadius { get {return _grid.NodeRadius; } }

    void Awake()
    {
        _grid = GetComponent<Grid2D>();
    }

    void Start()
    {
        LazyInitializeOpenSet();
        LazyInitializeClosedSet();
    }

    private void LazyInitializeOpenSet()
    {
        _openSet = new Heap2D<Node2D>(_grid.MaxSize);
    }

    private void LazyInitializeClosedSet()
    {
        _closedSet = new HashSet<Node2D>();
    }

    public Vector3[] FindPath(Vector3 startPos, Vector3 targetPos)
    {   
        Stopwatch stopwatch = new Stopwatch();
        if (_printTimeForPath)
        {
            stopwatch.Start();
        }

        Node2D startNode = _grid.NodeFromWorldPoint(startPos);
        Node2D targetNode = _grid.NodeFromWorldPoint(targetPos);

        if (/*startNode.walkable &&*/ targetNode.walkable) // Only find path if a path is possible
        {
            // Heap<Node> _openSet = new Heap<Node>(_grid.MaxSize); // Nodes to be evaluated
            // HashSet<Node> _closedSet = new HashSet<Node>(); // Nodes already evaluated
            
            // Using lazy initialization instead of creating tons of new arrays of nodes every new calculated path
            _openSet.Clear(); // Nodes to be evaluated
            _closedSet.Clear(); // Nodes already evaluated
            _openSet.Add(startNode);

            while (_openSet.Count > 0)
            {
                // Find Node with lowest FCost, remove it from openSet and add it to closedSet
                Node2D currentNode = _openSet.RemoveFirst();
                _closedSet.Add(currentNode);
                
                // Path found
                if (currentNode == targetNode)
                {   
                    if (_printTimeForPath)
                    {
                        stopwatch.Stop();
                        print("Time for found path: " + stopwatch.ElapsedMilliseconds + " ms");
                    }
                    return RetracePath(startNode, targetNode);
                }

                foreach (Node2D neighbor in _grid.GetNeighbors(currentNode))
                {
                    // Skip Neighbor Node if not walkable or in closedSet
                    if (!neighbor.walkable || _closedSet.Contains(neighbor))
                    {
                        continue;
                    }
                    // If shorter path to Neighbor Node found or Neighbor Node not in openSet
                    int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                    if (newMovementCostToNeighbor < neighbor.gCost ||  !_openSet.Contains(neighbor))
                    {   
                        // Set FCost of Neighbor Node and set its parent to the currentNode
                        neighbor.gCost = newMovementCostToNeighbor;
                        neighbor.hCost = GetDistance(neighbor, targetNode);
                        neighbor.parent = currentNode;
                        // Add Neighbor Node to openSet if not already in it
                        if (!_openSet.Contains(neighbor))
                        {
                            _openSet.Add(neighbor);
                        }
                        else // Fix EO6
                        {
                            _openSet.UpdateItem(neighbor);
                        }
                    }
                }
            }
        }
        return new Vector3[0];
    }

    private int GetDistance(Node2D nodeA, Node2D nodeB)
    {   
        /* To calculate the distance between 2 Nodes:

        Find which axis has the lowest # of Nodes between Node A and Node B for the # of Diagonal Moves
        Subtract # of Diagonal Moves from the # of Nodes between Node A and Node B of the other axis for the # of Horizontal/Vertical Moves

        By Pythagorean Theorem:
        Horizontal/Vertical Movement = 1
        Diagonal Movement = 1.4
        To get whole numbers we multiply by 10 and get 10, 14

        In Conclusion, distance between 2 Nodes = (# of Diagonal Moves * 14) + (# of Horizontal/Vertical Moves * 10)
        */

        // Number of Nodes from Node A to Node B in respective axis
        int distX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int distY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        // X Diagonal
        if (distX < distY)
        {
            return (distX * 14) + ((distY - distX) * 10);
        }
        // Y Diagonal
        else
        {   
            return (distY * 14) + ((distX - distY) * 10);
            
        }
    }

    private Vector3[] RetracePath(Node2D startNode, Node2D endNode)
    {
        List<Node2D> path = new List<Node2D>();
        Node2D currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Add(startNode);
        Vector3[] waypoints = SimplifyPath(path);
        Array.Reverse(waypoints);
        return waypoints;
    }
    
    // Ensures we only use waypoints along that path that change path direction
    private Vector3[] SimplifyPath(List<Node2D> path)
    {   
        List<Vector3> waypoints = new List<Vector3>();
        Vector2 directionOld = Vector2.zero;

        for (int i = 1; i< path.Count; i++)
        {
            Vector2 directionNew = new Vector2(path[i-1].gridX - path[i].gridX, path[i-1].gridY - path[i].gridY);
            if(directionNew != directionOld)
            {
                waypoints.Add(path[i-1].worldPosition);
            }
            directionOld = directionNew;
        }
        return waypoints.ToArray();
    }
}