# unity-a-star-pathfinding
2D and 3D A* pathfinding implementation for Unity.

## Key Features
- Graph size, grid cell size, and obstacle detection forgiveness threshold are customizable
- Option to recalculate graph for dynamic obstacles
- Easy visualization of nodes and paths through gizmos
- Optimized through use of binary heap and option to precalculate nodes at runtime

## Setup
1. Add Grid and Pathfinding scripts to an empty GameObject
2. Utilize the FollowPath script or create your own implementations

## Methods
### Grid
*Allows recalculation of nodes during runtime.*  
```csharp  
public void RecalculateWalkableNodes()
```

### Pathfinding2D
*Returns an array of Vector2 that represents the path of nodes in world space.*  
```csharp
public Vector2 FindPath(Vector2 startPos, Vector2 targetPos)
```

### Pathfinding3D
*Returns an array of Vector3 that represents the path of nodes in world space.*  
```csharp
public Vector3 FindPath(Vector3 startPos, Vector3 targetPos)
```

## Dependencies
* None
