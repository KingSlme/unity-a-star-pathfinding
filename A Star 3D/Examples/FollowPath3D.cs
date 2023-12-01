using UnityEngine;

// Simple example on how to implement A* for Transform movement
public class FollowPath3D : MonoBehaviour
{   
    [SerializeField] bool _displayPathGizmos = false;
    [SerializeField] private Pathfinding3D _pathfinding;
    [SerializeField] private Transform _target;
    [SerializeField] private float _speed = 5f;
    private Vector3[] _path;

    private bool _hasReachedNextNode =  true;
    private Vector3 _lastTargetPosition;

    void Update()
    {   
            FollowPath();
    }

    private void FollowPath()
    {   
        if (_hasReachedNextNode || _path.Length == 0) // Ensures next node fully reached to prevent recalculating a path that possibly slightly clips obstacles
        {   
            _path = _pathfinding.FindPath(transform.position, _target.position);
            _hasReachedNextNode = false;
            _lastTargetPosition = _target.position;
        }
        else if (_lastTargetPosition != _target.position) // Ensures path recalculation if target moves (Prevents following a now irrelevant node when a new path is needed)
        {
            _path = _pathfinding.FindPath(transform.position, _target.position);
            _hasReachedNextNode = false;
            _lastTargetPosition = _target.position;
        }

        if (_path.Length > 0)
        {   
            Vector3 newPosition = Vector3.MoveTowards(transform.position, _path[0], _speed * Time.deltaTime);
            transform.position = newPosition;
            if (transform.position == _path[0])
            {
                _hasReachedNextNode = true;
            }
        }
    }

    public void OnDrawGizmos()
    {   
        if (_displayPathGizmos)
        {
            if (_path != null && _path.Length > 0)
            {
                for ( int i = 0; i < _path.Length; i++)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(_path[i], _pathfinding.NodeRadius);
                    if (i == 0)
                    {
                        Gizmos.DrawLine(transform.position, _path[i]);
                    }
                    else
                    {
                        Gizmos.DrawLine(_path[i-1], _path[i]);
                    }
                }
            }
        }
    }
}