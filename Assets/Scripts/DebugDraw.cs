using UnityEngine;

public class DebugDraw : MonoBehaviour
{
    public GameObject pointPrefab;
    public LineRenderer lr;
    private GameObject[] trianglePoints;
    
    // Start is called before the first frame update
    void Start()
    {
        lr = GetComponent<LineRenderer>();
        lr.enabled = false;
        trianglePoints = new GameObject[3];
    }

    private void ClearTrianglePoints()
    {
        foreach (var point in trianglePoints)
            Destroy(point);
    }

    public void DrawTriangle(int[] triangleIndices, Transform hitTransform, Mesh mesh)
    {
        if (!lr.enabled)
            lr.enabled = true;

        Vector3[] positions = new Vector3[4];
        positions[0] = hitTransform.TransformPoint(mesh.vertices[triangleIndices[0]]);
        positions[1] = hitTransform.TransformPoint(mesh.vertices[triangleIndices[1]]);
        positions[2] = hitTransform.TransformPoint(mesh.vertices[triangleIndices[2]]);
        positions[3] = hitTransform.TransformPoint(mesh.vertices[triangleIndices[0]]);

        ClearTrianglePoints();
        
        trianglePoints[0] = Instantiate(pointPrefab, positions[0], Quaternion.identity);
        trianglePoints[1] = Instantiate(pointPrefab, positions[1], Quaternion.identity);
        trianglePoints[2] = Instantiate(pointPrefab, positions[2], Quaternion.identity);

        lr.SetPositions(positions);
        
    }
}
