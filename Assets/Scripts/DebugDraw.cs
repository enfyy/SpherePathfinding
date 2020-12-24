using System;
using System.Collections.Generic;
using System.Dynamic;
using UnityEngine;

public class DebugDraw : MonoBehaviour
{
    public GameObject pointPrefabRed;
    public GameObject pointPrefabBlue;
    private List<GameObject> highlightedPath;
    private List<GameObject> highlightedNeighbours;
    private GameObject selectedNode;

    private void Start()
    {
        highlightedPath = new List<GameObject>();
        highlightedNeighbours = new List<GameObject>();
    }

    public void DrawSelectedNode(Node node)
    {
        if (selectedNode != null)
            Destroy(selectedNode);

        float r = Node.nodeRadius;
        pointPrefabRed.transform.localScale = new Vector3(r,r,r);
       GameObject go = Instantiate(pointPrefabRed, node.worldPos, Quaternion.identity);
       selectedNode = go;

    }

    public void HighlightNeighbours(List<Node> neighbours)
    {
        if (highlightedNeighbours.Count > 0)
        {
            foreach (var marker in highlightedNeighbours)
                Destroy(marker);
            highlightedNeighbours.Clear();
        }
        
        float r = Node.nodeRadius;
        pointPrefabBlue.transform.localScale = new Vector3(r,r,r);
        foreach (var node in neighbours)
        {
            GameObject go = Instantiate(pointPrefabBlue, node.worldPos, Quaternion.identity);
            highlightedNeighbours.Add(go);
        }
    }

    public void HighlightPath(List<Node> path)
    {
        if (highlightedPath.Count > 0)
        {
            foreach (var marker in highlightedPath)
                Destroy(marker);
            highlightedPath.Clear();
        }
        
        float r = Node.nodeRadius;
        pointPrefabRed.transform.localScale = new Vector3(r,r,r);
        foreach (var node in path)
        {
            GameObject go = Instantiate(pointPrefabRed, node.worldPos, Quaternion.identity);
            highlightedPath.Add(go);
        }
    }

}
