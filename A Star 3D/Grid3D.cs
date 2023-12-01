using System.Collections.Generic;
using UnityEngine;

// (X,Y,Z) in the grid correlate to (X,Y,Z) in world space. Note how Z in world space no longer represents Y in the grid
[RequireComponent(typeof(Pathfinding3D))]
public class Grid3D : MonoBehaviour
{   
    [SerializeField] private bool _displayGridGizmos = false;
    [SerializeField] private bool _displayOnlyUnwalkableGizmos = false;
    [SerializeField] private LayerMask _unwalkableLayerMask;
    [SerializeField] private Vector3 _gridWorldSize = new Vector3(10.0f, 10.0f, 10.0f);
    [SerializeField] private float _nodeRadius = 0.25f;
    [SerializeField] [Tooltip("Controls how forgiving Unwalkable Node detection is.")] [Range(0.1f, 10.0f)] private float _obstacleDetectionScale = 1.0f;
    [SerializeField] private bool _recalculateWalkableNodes = false;
    [SerializeField] private bool _precalculateNeighbors = true;

    private Node3D[,,] _grid;
    private float _nodeDiameter;
    private int _gridCellCountX, _gridCellCountY, _gridCellCountZ;

    public int MaxSize { get { return _gridCellCountX * _gridCellCountY * _gridCellCountZ; } }
    public float NodeRadius { get {return _nodeRadius; } }

    private Dictionary<Node3D, List<Node3D>> _nodeNeighborDictionary;

    void Awake()
    {
        _nodeDiameter = _nodeRadius * 2;
        _gridCellCountX = Mathf.RoundToInt(_gridWorldSize.x / _nodeDiameter);
        _gridCellCountY = Mathf.RoundToInt(_gridWorldSize.y / _nodeDiameter);
        _gridCellCountZ = Mathf.RoundToInt(_gridWorldSize.z / _nodeDiameter);
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
        Gizmos.DrawWireCube(transform.position, new Vector3(_gridWorldSize.x, _gridWorldSize.y, _gridWorldSize.z));
        
        if (_grid != null && _displayGridGizmos)
        {   
            foreach (Node3D node in _grid)
            {   
                Gizmos.color = (node.walkable) ? Color.white : Color.red;
                if (_displayOnlyUnwalkableGizmos)
                {
                    if (!node.walkable)
                    {
                        Gizmos.DrawWireCube(node.worldPosition, Vector3.one * (_nodeDiameter));
                    }
                }
                else
                {
                    Gizmos.DrawWireCube(node.worldPosition, Vector3.one * (_nodeDiameter));
                }
            }
        }
    }

    private void CreateGrid() 
    {
        _grid = new Node3D[_gridCellCountX, _gridCellCountY, _gridCellCountZ];
        Vector3 worldLeftDownBottom = transform.position - (Vector3.right * _gridWorldSize.x / 2) - (Vector3.up * _gridWorldSize.y / 2) - (Vector3.forward * _gridWorldSize.z / 2);

        for (int x = 0; x < _gridCellCountX; x++)
        {
            for (int y = 0; y < _gridCellCountY; y++)
            {
                for (int z = 0; z < _gridCellCountZ; z++)
                {
                    // We add nodeRadius because 0,0,0 needs to be nodeRadius from the left down bottom
                    Vector3 worldPoint = worldLeftDownBottom + (Vector3.right * (x * _nodeDiameter + _nodeRadius)) + (Vector3.up * (y * _nodeDiameter + _nodeRadius)) + (Vector3.forward * (z * _nodeDiameter + _nodeRadius));
                    bool walkable = !Physics.CheckSphere(worldPoint, _nodeRadius * _obstacleDetectionScale, _unwalkableLayerMask);
                    _grid[x,y,z] = new Node3D(walkable, worldPoint, x, y, z);
                }
            }
        }
    }

    public void RecalculateWalkableNodes() 
    {
        Vector3 worldLeftDownBottom = transform.position - (Vector3.right * _gridWorldSize.x / 2) - (Vector3.up * _gridWorldSize.y / 2) - (Vector3.forward * _gridWorldSize.z / 2);

        for (int x = 0; x < _gridCellCountX; x++)
        {
            for (int y = 0; y < _gridCellCountY; y++)
            {
                for (int z = 0; z < _gridCellCountZ; z++)
                {
                    // We add nodeRadius because 0,0 needs to be nodeRadius from the left down bottom
                    Vector3 worldPoint = worldLeftDownBottom + (Vector3.right * (x * _nodeDiameter + _nodeRadius)) + (Vector3.up * (y * _nodeDiameter + _nodeRadius)) + (Vector3.forward * (z * _nodeDiameter + _nodeRadius));
                    bool walkable = !Physics.CheckSphere(worldPoint, _nodeRadius * _obstacleDetectionScale, _unwalkableLayerMask);
                    _grid[x,y,z].walkable = walkable;
                }
            }
        }
    }

    public Node3D NodeFromWorldPoint(Vector3 worldPosition)
    {
        /* We find the percent of how far along the grid worldPosition is
        0 = Left, Down, and Bottom
        0.5 = Center
        1 = Right, Up, and Top
        */

        // We get the worldPosition relative to the grid (so the grid can be placed anywhere)
        worldPosition -= transform.position;
        float percentX = (worldPosition.x / _gridWorldSize.x) + 0.5f; 
        float percentY = (worldPosition.y/ _gridWorldSize.y) + 0.5f;
        float percentZ = (worldPosition.z / _gridWorldSize.z) + 0.5f;

        // We clamp to 0,1 to prevent invalid index when worldPosition is outside of the grid
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);
        percentZ = Mathf.Clamp01(percentZ);

        int x = Mathf.RoundToInt((_gridCellCountX - 1) * percentX);
        int y = Mathf.RoundToInt((_gridCellCountY - 1) * percentY);
        int z = Mathf.RoundToInt((_gridCellCountZ - 1) * percentZ);
        return _grid[x,y,z];
    }

    public List<Node3D> GetNeighbors(Node3D node)
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
    public List<Node3D> GetNeighborsRealtime(Node3D node)
    {
        List<Node3D> neighbors = new List<Node3D>();
        // Checking grid positions around Central Node
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    // Skip Node argument itself (Central Node)
                    if (x == 0 && y == 0 && z == 0)
                    {
                        continue;
                    }
                    // Check potential neighboring Nodes and see if they are valid (Inside the Grid)
                    int checkX = node.gridX + x;
                    int checkY = node.gridY + y;
                    int checkZ = node.gridZ + z;
                    if (checkX >= 0 && checkX < _gridCellCountX && checkY >= 0 && checkY < _gridCellCountY && checkZ >= 0 && checkZ < _gridCellCountZ)
                    {
                        neighbors.Add(_grid[checkX,checkY,checkZ]);
                    }
                }
            }
        }
        return neighbors;
    }

    // Slower startup, more efficient for many requests
    public List<Node3D> GetNeighborsPrecalculated(Node3D node)
    {
        return _nodeNeighborDictionary[node];
    }

    private void PrecalculateNeighbors()
    {
        _nodeNeighborDictionary = new Dictionary<Node3D, List<Node3D>>();
        foreach (Node3D node in _grid)
        {
            for (int x = -1; x <= 1; x++)
            {   
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        // Skip Node argument itself (Central Node)
                        if (x == 0 && y == 0 && z == 0)
                        {
                            continue;
                        }
                        // Check potential neighboring Nodes and see if they are valid (Inside the Grid)
                        int checkX = node.gridX + x;
                        int checkY = node.gridY + y;
                        int checkZ = node.gridZ + z;
                        if (checkX >= 0 && checkX < _gridCellCountX && checkY >= 0 && checkY < _gridCellCountY && checkZ >= 0 && checkZ < _gridCellCountZ)
                        {
                            if (_nodeNeighborDictionary.ContainsKey(node))
                            {
                                _nodeNeighborDictionary[node].Add(_grid[checkX,checkY,checkZ]);
                            }
                            else
                            {
                                _nodeNeighborDictionary[node] = new List<Node3D> { _grid[checkX,checkY,checkZ] };
                            }
                        }
                    }
                }
            }
        }
    }
}