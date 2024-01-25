using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[ExecuteInEditMode]
public class TestHintLineInEditMode : HMonoBehaviour
{
    [InlineButton("Run", "Run")]
    [SerializeField] private float moveSpeed = 3f;
    [ReadOnly]
    [SerializeField] private Vector3 currentDestination;
    [ReadOnly]
    [SerializeField] private bool isMoving;
    [ReadOnly]
    [SerializeField] private List<Vector3> path = new();
    // Update is called once per frame
    void Update()
    {
        if (isMoving && path.Count > 0)
        {
            // Move to destination
            transform.position = Vector3.MoveTowards(transform.position, currentDestination, moveSpeed * Time.deltaTime);
            // if reach to destination, set new destination
            if (Vector3.Distance(Tf.position, currentDestination) < 0.01f)
            {
                if (_pathDestinationIndex < path.Count - 1)
                {
                    _pathDestinationIndex++;
                    currentDestination = path[_pathDestinationIndex];
                }
                else
                {
                    isMoving = false;
                }
            }
        } 
    }

    private void Run()
    {
        if (path.Count == 0) return;
        isMoving = true;
        Tf.position = path[0];
        _pathDestinationIndex = 1;
        currentDestination = path[_pathDestinationIndex];
    }
    
    private int _pathDestinationIndex;
    public void TestHintMoving(List<Vector3> pathIn)
    {
        if (pathIn.Count == 0) return;
        path = pathIn;
        isMoving = true;
        Tf.position = path[0];
        _pathDestinationIndex = 1;
        currentDestination = path[_pathDestinationIndex];
    }
}
