using System.Collections.Generic;
using UnityEngine;

// (X,Y) in the grid correlate to (X,Z) in world space
[RequireComponent(typeof(Pathfinding2D))]
public class Grid2D : MonoBehaviour
{   
    [SerializeField] private bool _displayGridGizmos = false;
    [SerializeField] private bool _onlyDisplayUnwalkableGizmos = false;
    [SerializeField] private LayerMask _unwalkableLayerMask;
    [SerializeField] private Vector2 _gridWorldSize = new Vector2(10.0f, 10.0f);
    [SerializeField] private float _nodeRadius = 0.25f;
    [SerializeField] [Tooltip("Controls how forgiving Unwalkable Node detection is.")] [Range(0.1f, 10.0f)] private float _obstacleDetectionScale = 1.0f;
    [SerializeField] private bool _recalculateWalkableNodes = false;
    [SerializeField] private bool _precalculateNeighbors = true;

    private Node2D[,] _grid;
    private float _nodeDiameter;
    private int _gridCellCountX, _gridCellCountY;

    public int MaxSize { get { return _gridCellCountX * _gridCellCountY; } }
    public float NodeRadius { get {return _nodeRadius; } }

    private Dictionary<Node2D, List<Node2D>> _nodeNeighborDictionary;

    void Awake()
    {
        _nodeDiameter = _nodeRadius * 2;
        _gridCellCountX = Mathf.RoundToInt(_gridWorldSize.x / _nodeDiameter);
        _gridCellCountY = Mathf.RoundToInt(_gridWorldSize.y / _nodeDiameter);
        CreateGrid();
        if (_precalculateNeighbors)
        {
            PrecalculateNeighbors();
        }
    }

    void Update()
    {
        if (_recalculateWalkableNodes)
        {
            RecalculateWalkableNodes();
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(_gridWorldSize.x, 1, _gridWorldSize.y));

        if (_grid != null && _displayGridGizmos)
        {
            foreach (Node2D node in _grid)
            {   
                Gizmos.color = (node.walkable) ? Color.white : Color.red;
                if (_onlyDisplayUnwalkableGizmos)
                {
                    if (!node.walkable)
                    {
                    Gizmos.DrawWireCube(node.worldPosition, Vector3.one * (_nodeDiameter - 0.1f));
                    }
                }
                else
                {
                    Gizmos.DrawWireCube(node.worldPosition, Vector3.one * (_nodeDiameter - 0.1f));
                }
            }
        }
    }

    private void CreateGrid()
    {
        _grid = new Node2D[_gridCellCountX, _gridCellCountY];
        Vector3 worldBottomLeft = transform.position - (Vector3.right * _gridWorldSize.x / 2) - (Vector3.forward * _gridWorldSize.y / 2);

        for (int x = 0; x < _gridCellCountX; x++)
        {
            for (int y = 0; y < _gridCellCountY; y++)
            {
                // We add nodeRadius because 0,0 needs to be nodeRadius from the bottom left
                Vector3 worldPoint = worldBottomLeft + (Vector3.right * (x * _nodeDiameter + _nodeRadius)) + (Vector3.forward * (y * _nodeDiameter + _nodeRadius));
                bool walkable = !Physics.CheckSphere(worldPoint, _nodeRadius * _obstacleDetectionScale, _unwalkableLayerMask);
                _grid[x,y] = new Node2D(walkable, worldPoint, x, y);
            }
        }
    }

    public void RecalculateWalkableNodes()
    {
        Vector3 worldBottomLeft = transform.position - (Vector3.right * _gridWorldSize.x / 2) - (Vector3.forward * _gridWorldSize.y / 2);

        for (int x = 0; x < _gridCellCountX; x++)
        {
            for (int y = 0; y < _gridCellCountY; y++)
            {
                // We add nodeRadius because 0,0 needs to be nodeRadius from the bottom left
                Vector3 worldPoint = worldBottomLeft + (Vector3.right * (x * _nodeDiameter + _nodeRadius)) + (Vector3.forward * (y * _nodeDiameter + _nodeRadius));
                bool walkable = !Physics.CheckSphere(worldPoint, _nodeRadius * _obstacleDetectionScale, _unwalkableLayerMask);
                _grid[x,y].walkable = walkable;
            }
        }
    }

    public Node2D NodeFromWorldPoint(Vector3 worldPosition)
    {
        /* We find the percent of how far along the grid worldPosition is
        0 = Left and Bottom
        0.5 = Center
        1 = Right and Top
        */

        // We get the worldPosition relative to the grid (so the grid can be placed anywhere)
        worldPosition -= transform.position;
        float percentX = (worldPosition.x / _gridWorldSize.x) + 0.5f; 
        float percentY = (worldPosition.z/ _gridWorldSize.y) + 0.5f;

        // We clamp to 0,1 to prevent invalid index when worldPosition is outside of the grid
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((_gridCellCountX - 1) * percentX);
        int y = Mathf.RoundToInt((_gridCellCountY - 1) * percentY);
        return _grid[x,y];
    }

    public List<Node2D> GetNeighbors(Node2D node)
    {
        if (!_precalculateNeighbors)
        {
            return GetNeighborsRealtime(node);
        }
        else
        {
            return GetNeighborsPrecalculated(node);
        }
    }

    // Faster startup, more efficient for single requests
    public List<Node2D> GetNeighborsRealtime(Node2D node)
    {
        List<Node2D> neighbors = new List<Node2D>();
        // Checking grid positions around Central Node
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                // Skip Node argument itself (Central Node)
                if (x == 0 && y == 0)
                {
                    continue;
                }
                // Check potential neighboring Nodes and see if they are valid (Inside the Grid)
                int checkX = node.gridX + x;
                int checkY = node.gridY + y;
                if (checkX >= 0 && checkX < _gridCellCountX && checkY >= 0 && checkY < _gridCellCountY)
                {
                    neighbors.Add(_grid[checkX,checkY]);
                }
            }
        }
        return neighbors;
    }

    // Slower startup, more efficient for many requests
    public List<Node2D> GetNeighborsPrecalculated(Node2D node)
    {
        return _nodeNeighborDictionary[node];
    }

    private void PrecalculateNeighbors()
    {
        _nodeNeighborDictionary = new Dictionary<Node2D, List<Node2D>>();
        foreach (Node2D node in _grid)
        {
            for (int x = -1; x <= 1; x++)
            {   
                for (int y = -1; y <= 1; y++)
                {
                    // Skip Node argument itself (Central Node)
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }
                    // Check potential neighboring Nodes and see if they are valid (Inside the Grid)
                    int checkX = node.gridX + x;
                    int checkY = node.gridY + y;
                    if (checkX >= 0 && checkX < _gridCellCountX && checkY >= 0 && checkY < _gridCellCountY)
                    {
                        if (_nodeNeighborDictionary.ContainsKey(node))
                        {
                            _nodeNeighborDictionary[node].Add(_grid[checkX,checkY]);
                        }
                        else
                        {
                            _nodeNeighborDictionary[node] = new List<Node2D> { _grid[checkX,checkY] };
                        }
                    }
                }
            }
        }
    }
}