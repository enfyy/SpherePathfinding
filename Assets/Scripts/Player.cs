using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    private Pathfinding pathfinder;
    private Camera cam;
    public float positionRayDistance = 5f;

    void Start()
    {    
        cam = Camera.main;
        pathfinder = GetComponent<Pathfinding>();
    }

    private void Update()
    {
        Move();
    }

    /// <summary>
    /// Makes the player move to the clicked location.
    /// </summary>
    public void Move()
    {
        if (Input.GetMouseButtonDown(0)) //Left Click
        {
            Ray mouseRay = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(mouseRay, out RaycastHit mouseRayHit, Mathf.Infinity, pathfinder.layerMask) &&
                Physics.Raycast(transform.position, -transform.up, out RaycastHit playerRayHit, positionRayDistance, pathfinder.layerMask))
            {
                pathfinder.FindPath(playerRayHit, mouseRayHit);
            }
        }
    }
}
