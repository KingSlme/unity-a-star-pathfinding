using UnityEngine;

public class Node3D : IHeapItem3D<Node3D>
{
    public bool walkable;
    public Vector3 worldPosition;
    public int gridX, gridY, gridZ;

    public int gCost; // Distance from starting Node
    public int hCost; // Distance from end Node
    public Node3D parent;
    private int _heapIndex;

    public int FCost
    { 
        get { return gCost + hCost; } 
    }

    public Node3D(bool walkable, Vector3 worldPosition, int gridX, int gridY, int gridZ)
    {
        this.walkable = walkable;
        this.worldPosition = worldPosition;
        this.gridX = gridX;
        this.gridY = gridY;
        this.gridZ = gridZ;
    }

    public int HeapIndex
    {
        get
        {
            return _heapIndex;
        }
        set
        {
            _heapIndex = value;
        }
    }

    public int CompareTo(Node3D nodeToCompare)
    {
        int compare = FCost.CompareTo(nodeToCompare.FCost);
        if (compare == 0)
        {
            compare = hCost.CompareTo(nodeToCompare.hCost);
        }
        return -compare;
    }
}