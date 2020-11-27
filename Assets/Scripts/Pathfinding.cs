using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Indicates which face of the cube.
/// </summary>
public enum CubeFace
{
    Front,
    Top,
    Back,
    Bottom,
    Right,
    Left
}

/// <summary>
/// Used to indicate the connection between the individual faces of the cube.
/// </summary>
public enum ConnectionDirection
{
    North,
    East,
    South,
    West,
    None
}

/// <summary>
/// Each value represents a Zone of the grid in which a point can lie. Used for pathfinding to opposing faces.
/// </summary>
public enum GridZone
{
    TopLeft,
    TopMiddle,
    TopRight,
    LeftMiddle,
    RightMiddle,
    BottomLeft,
    BottomMiddle,
    BottomRight
}

/// <summary>
/// Extending the grid by unwrapping a cube. The starting face remains in the center of the extended grid.
/// </summary>
public enum ExtendedGrid
{
    TopLeft,
    TopMiddle,
    TopRight,
    RightTop,
    RightMiddle,
    RightBot,
    BotRight,
    BotMiddle,
    BotLeft,
    LeftBot,
    LeftMiddle,
    LeftTop
}

/// <summary>
/// This Class implements the A* pathfinding algorithm for usage on a perfect Sphere. Specifically a Cube-Sphere.
/// </summary>
public class Pathfinding : MonoBehaviour
{
    public static int GridSize { get; private set; }
    public LayerMask layerMask;
    public MeshFilter cubeSphereMeshFilter;
    public DebugDraw debugDrawer;
    
    private Dictionary<CubeFace, Vector2[]> uvMapToFace;
    
    /// <summary>
    /// Start gets called at the start ;)
    /// </summary>
    private void Start()
    {
        Mesh mesh = cubeSphereMeshFilter.sharedMesh;
        uvMapToFace = new Dictionary<CubeFace, Vector2[]>()
        {
            {CubeFace.Front, mesh.uv}, {CubeFace.Top, mesh.uv2}, {CubeFace.Back, mesh.uv3},
            {CubeFace.Bottom, mesh.uv4}, {CubeFace.Left, mesh.uv5}, {CubeFace.Right, mesh.uv6}
        };
        // All tiles are made of 2 triangles and theres 6 faces on the cube
        var triangleCount = mesh.triangles.Length / 3;
        GridSize = (int) Mathf.Sqrt(triangleCount / 2 / 6);
        Debug.Log("Grid size: " + GridSize);
    }

    /// <summary>
    /// A* Pathfinding between two positions provided by two raycasts. 
    /// </summary>
    public void FindPath(RaycastHit playerRayHit, RaycastHit mouseRayHit)
    {
        Mesh cubeSphereMesh = cubeSphereMeshFilter.sharedMesh;
        Vector3[] vertices = cubeSphereMesh.vertices;
        int[] triangles = cubeSphereMesh.triangles;

        if (playerRayHit.triangleIndex == -1 || mouseRayHit.triangleIndex == -1)
            throw new Exception("Something is wrong with the mesh collider (triangleIndex = -1)");
        
        int[] startTriangleIndices =
        {
            triangles[playerRayHit.triangleIndex * 3 + 0],
            triangles[playerRayHit.triangleIndex * 3 + 1],
            triangles[playerRayHit.triangleIndex * 3 + 2],
        };
        int[] endTriangleIndices =
        {
            triangles[mouseRayHit.triangleIndex * 3 + 0],
            triangles[mouseRayHit.triangleIndex * 3 + 1],
            triangles[mouseRayHit.triangleIndex * 3 + 2],
        };
        
        GridPoint startPoint = GridPointFromTriangle(startTriangleIndices);
        GridPoint endPoint   = GridPointFromTriangle(endTriangleIndices);
        debugDrawer.DrawTriangle(endTriangleIndices, mouseRayHit.collider.transform, cubeSphereMesh);

        Debug.Log("Start: " + FaceFromVertex(startTriangleIndices[0]) + " [" + startPoint.x + "|" + startPoint.y +
                  "] " + "End: " + FaceFromVertex(endTriangleIndices[0]) + " [" + endPoint.x + "|" + endPoint.y + "] ");
        Debug.Log("Distance: " + CalculateShortestSurfaceDistance(startPoint, endPoint));         
        
        //TODO: this is where A* should start probably.
        
    }

    /// <summary>
    /// Calculates the shortest distance between two points on a grid.
    /// </summary>
    /// <returns> The distance between the two points</returns>
    private static int CalculateShortestDistance(GridPoint start, GridPoint end)
    {
        var xDiff = Mathf.Abs(start.x - end.x);
        var yDiff = Mathf.Abs(start.y - end.y);

        if (xDiff < yDiff)
        {
            return xDiff * 14 + (yDiff - xDiff) * 10;
        }
        return yDiff * 14 + (xDiff - yDiff) * 10;
    }

    /// <summary>
    /// Calculates the shortest surface distance between two points on a 3D cube.
    /// </summary>
    /// <returns> The distance between the two points</returns>
    private static int CalculateShortestSurfaceDistance(GridPoint start, GridPoint end)
    {
        // CASE 1: start and end point are on the same face.
        if (start.face == end.face)
            return CalculateShortestDistance(start, end);
        Debug.Log("Not Case 1");

        // CASE 2: end point is on a face that is connected to the start point.
        if (IsConnectedFace(start.face, end.face))
        {
            ConnectionDirection dir = GetConnectionDirection(start.face, end.face);
            switch (dir)
            {
                case ConnectionDirection.North:
                    end.y += GridSize;
                    break;
                
                case ConnectionDirection.East:
                    end.x += GridSize;
                    break;
                
                case ConnectionDirection.South:
                    start.y += GridSize;
                    break;
                
                case ConnectionDirection.West:
                    start.x += GridSize;
                    break;
                
                case ConnectionDirection.None:
                    throw new Exception("Calculating shortest surface distance should've been detected as Case 3 (opposite faces) but somehow got Case 2");
                    
                default:
                    throw new Exception("Uh-oh");
            }
            return CalculateShortestDistance(start, end);
        }
        Debug.Log("Not Case 2");
        // CASE 3: start and end point are on opposing faces.
        return OpposingFaceDistance(start, end);
    }
    
    /// <summary>
    /// Finds the shortest surface distance in the case of the points being on opposing faces.
    /// This method is separate from CalculateShortestSurfaceDistance only for readability purposes.
    /// </summary>
    private static int OpposingFaceDistance(GridPoint start, GridPoint end)
    {
        List<int> distances = new List<int>();
        
        GridZone gz = start.DetermineGridZone();
        ExtendedGrid[] grids;
        switch (gz)
        { 
            case GridZone.TopLeft:
                grids = new[]
                {
                                           ExtendedGrid.TopMiddle,   ExtendedGrid.TopRight,
                                           ExtendedGrid.RightMiddle,
                                           ExtendedGrid.BotMiddle,   
                    ExtendedGrid.LeftBot,  ExtendedGrid.LeftMiddle
                };
                distances.AddRange(grids.Select(grid => CalculateDistanceOnExtendedGrid(grid, start, end)));
                break;
            case GridZone.TopMiddle:
                grids = new[]
                {
                    ExtendedGrid.TopLeft,  ExtendedGrid.TopMiddle,   ExtendedGrid.TopRight,
                                           ExtendedGrid.RightMiddle, ExtendedGrid.RightBot,
                                           ExtendedGrid.BotMiddle,
                    ExtendedGrid.LeftBot,  ExtendedGrid.LeftMiddle
                };
                distances.AddRange(grids.Select(grid => CalculateDistanceOnExtendedGrid(grid, start, end)));
                break;
            case GridZone.TopRight:
                grids = new[]
                {
                    ExtendedGrid.TopLeft,  ExtendedGrid.TopMiddle,
                                           ExtendedGrid.RightMiddle, ExtendedGrid.RightBot,
                                           ExtendedGrid.BotMiddle,   
                                           ExtendedGrid.LeftMiddle
                };
                distances.AddRange(grids.Select(grid => CalculateDistanceOnExtendedGrid(grid, start, end)));
                break;
            
            case GridZone.BottomLeft:
                grids = new[]
                {
                                           ExtendedGrid.TopMiddle,   
                                           ExtendedGrid.RightMiddle,
                    ExtendedGrid.BotRight, ExtendedGrid.BotMiddle,
                                           ExtendedGrid.LeftMiddle,  ExtendedGrid.LeftTop
                };
                distances.AddRange(grids.Select(grid => CalculateDistanceOnExtendedGrid(grid, start, end)));
                break;
            case GridZone.BottomMiddle:
                grids = new[]
                {
                                           ExtendedGrid.TopMiddle,
                    ExtendedGrid.RightTop, ExtendedGrid.RightMiddle,
                    ExtendedGrid.BotRight, ExtendedGrid.BotMiddle,   ExtendedGrid.BotLeft,
                                           ExtendedGrid.LeftMiddle,  ExtendedGrid.LeftTop
                };
                distances.AddRange(grids.Select(grid => CalculateDistanceOnExtendedGrid(grid, start, end)));
                break;
            case GridZone.BottomRight:
                grids = new[]
                {
                                           ExtendedGrid.TopMiddle,
                    ExtendedGrid.RightTop, ExtendedGrid.RightMiddle,
                                           ExtendedGrid.BotMiddle,   ExtendedGrid.BotLeft,
                                           ExtendedGrid.LeftMiddle
                };
                distances.AddRange(grids.Select(grid => CalculateDistanceOnExtendedGrid(grid, start, end)));
                break;
            
            case GridZone.LeftMiddle:
                grids = new[]
                {
                                           ExtendedGrid.TopMiddle,   ExtendedGrid.TopRight,
                                           ExtendedGrid.RightMiddle,
                    ExtendedGrid.BotRight, ExtendedGrid.BotMiddle,
                    ExtendedGrid.LeftBot,  ExtendedGrid.LeftMiddle,  ExtendedGrid.LeftTop
                };
                distances.AddRange(grids.Select(grid => CalculateDistanceOnExtendedGrid(grid, start, end)));
                break;
            case GridZone.RightMiddle:
                grids = new[]
                {
                    ExtendedGrid.TopLeft,  ExtendedGrid.TopMiddle,
                    ExtendedGrid.RightTop, ExtendedGrid.RightMiddle, ExtendedGrid.RightBot,
                                           ExtendedGrid.BotMiddle,   ExtendedGrid.BotLeft,
                                           ExtendedGrid.LeftMiddle
                };
                distances.AddRange(grids.Select(grid => CalculateDistanceOnExtendedGrid(grid, start, end)));
                break;
            
            default:
                throw new Exception("uh oh...");
        }
        
        return distances.Min();
    }

    /// <summary>
    /// Calculates one of the surface distances (not the necessarily the shortest one) for 2 points on opposing cube faces.
    /// </summary>
    private static int CalculateDistanceOnExtendedGrid(ExtendedGrid extendedGrid, GridPoint startPoint, GridPoint endPoint)
    {
        GridPoint start = new GridPoint(startPoint.face, startPoint.x, startPoint.y);
        GridPoint end = new GridPoint(endPoint.face, endPoint.x, endPoint.y);
        
        switch (extendedGrid)
        {
            case ExtendedGrid.TopLeft:
                end.MirrorGridHorizontal();
                end.RotateGrid90CW();
                start.x += GridSize;
                end.y += (GridSize * 2);
                break;
            
            case ExtendedGrid.TopMiddle:
                end.MirrorGridVertical();
                end.y += (GridSize * 2);
                break;
            
            case ExtendedGrid.TopRight:
                end.MirrorGridVertical();
                end.RotateGrid90CW();
                end.x += GridSize;
                end.y += (GridSize * 2);
                break;
            
            
            case ExtendedGrid.RightTop:
                end.MirrorGridVertical();
                end.RotateGrid90CW();
                end.x += (GridSize * 2);
                end.y += GridSize;
                break;
            
            case ExtendedGrid.RightMiddle:
                end.MirrorGridHorizontal();
                end.x += (GridSize * 2);
                break;
            
            case ExtendedGrid.RightBot:
                end.MirrorGridHorizontal();
                end.RotateGrid90CW();
                start.y += GridSize;
                end.x += GridSize * 2;
                break;
            
            
            case ExtendedGrid.BotRight:
                end.MirrorGridHorizontal();
                end.RotateGrid90CW();
                start.y += GridSize * 2;
                end.x += GridSize;
                break;
            
            case ExtendedGrid.BotMiddle:
                end.MirrorGridVertical();
                start.y += (GridSize * 2);
                break;
            
            case ExtendedGrid.BotLeft:
                end.MirrorGridVertical();
                end.RotateGrid90CW();
                start.x += GridSize;
                start.y += GridSize * 2;
                break;
            
            
            case ExtendedGrid.LeftBot:
                end.MirrorGridVertical();
                end.RotateGrid90CW();
                start.y += GridSize;
                start.x += (GridSize * 2);
                break;
            
            case ExtendedGrid.LeftMiddle:
                end.MirrorGridHorizontal();
                start.x += GridSize * 2;
                break;
            
            case ExtendedGrid.LeftTop:
                end.MirrorGridHorizontal();
                end.RotateGrid90CW();
                start.x += GridSize * 2;
                end.y += GridSize;
                break;
            
            default:
                throw new Exception("huh?");
        }

        return CalculateShortestDistance(start, end);
    }

    /// <summary>
    /// Returns true if face from parameter is directly connected to the face. (true unless its an opposing face)
    /// </summary>
    private static bool IsConnectedFace(CubeFace startFace, CubeFace endFace)
    {
        switch (startFace)
        {
            case CubeFace.Front:
                return endFace != CubeFace.Back;

            case CubeFace.Top:
                return endFace != CubeFace.Bottom;

            case CubeFace.Back:
                return endFace != CubeFace.Front;

            case CubeFace.Bottom:
                return endFace != CubeFace.Top;

            case CubeFace.Right:
                return endFace != CubeFace.Left;

            case CubeFace.Left:
                return endFace != CubeFace.Right;
            
            default: return false;
        }
    }

    /// <summary>
    /// Returns in which direction the first face (start) is connected to the second face (end), if at all.
    /// </summary>
    private static ConnectionDirection GetConnectionDirection(CubeFace startFace, CubeFace endFace)
    {
        if (startFace == CubeFace.Front  && endFace == CubeFace.Top    ||
            startFace == CubeFace.Top    && endFace == CubeFace.Back   ||
            startFace == CubeFace.Back   && endFace == CubeFace.Bottom ||
            startFace == CubeFace.Bottom && endFace == CubeFace.Front  ||
            startFace == CubeFace.Right  && endFace == CubeFace.Top    ||
            startFace == CubeFace.Left   && endFace == CubeFace.Top      )
            return ConnectionDirection.North;
        
        if (startFace == CubeFace.Front  && endFace == CubeFace.Right  ||
            startFace == CubeFace.Right  && endFace == CubeFace.Back   ||
            startFace == CubeFace.Back   && endFace == CubeFace.Left   ||
            startFace == CubeFace.Left   && endFace == CubeFace.Front  ||
            startFace == CubeFace.Top    && endFace == CubeFace.Right  ||
            startFace == CubeFace.Bottom && endFace == CubeFace.Right    )
            return ConnectionDirection.East;
        
        if (startFace == CubeFace.Front  && endFace == CubeFace.Bottom ||
            startFace == CubeFace.Bottom && endFace == CubeFace.Back   ||
            startFace == CubeFace.Back   && endFace == CubeFace.Top    ||
            startFace == CubeFace.Top    && endFace == CubeFace.Front  ||
            startFace == CubeFace.Right  && endFace == CubeFace.Bottom ||
            startFace == CubeFace.Left   && endFace == CubeFace.Bottom   )
            return ConnectionDirection.South;
        
        if (startFace == CubeFace.Front  && endFace == CubeFace.Left   ||
            startFace == CubeFace.Left   && endFace == CubeFace.Back   ||
            startFace == CubeFace.Back   && endFace == CubeFace.Right  ||
            startFace == CubeFace.Right  && endFace == CubeFace.Front  ||
            startFace == CubeFace.Top    && endFace == CubeFace.Left   ||
            startFace == CubeFace.Bottom && endFace == CubeFace.Left     )
            return ConnectionDirection.West;

        return ConnectionDirection.None; // Faces aren't connected.
    }

    /// <summary>
    /// Returns the face that is connected to the provided face in the provided direction.
    /// </summary>
    public static CubeFace GetConnectedFace(CubeFace face, ConnectionDirection direction)
    {
        switch (direction)
        {
            case ConnectionDirection.North:
                if (face == CubeFace.Front || face == CubeFace.Back || face == CubeFace.Left || face == CubeFace.Right)
                    return CubeFace.Top;
                if (face == CubeFace.Top || face == CubeFace.Bottom)
                    return CubeFace.Back;
                break;
            
            case ConnectionDirection.South:
                if (face == CubeFace.Front || face == CubeFace.Back || face == CubeFace.Left || face == CubeFace.Right)
                    return CubeFace.Bottom;
                if (face == CubeFace.Top || face == CubeFace.Bottom)
                    return CubeFace.Front;
                break;
            
            case ConnectionDirection.East:
                if (face == CubeFace.Front || face == CubeFace.Top)
                    return CubeFace.Left;
                if (face == CubeFace.Left)
                    return CubeFace.Back;
                if (face == CubeFace.Back || face == CubeFace.Bottom)
                    return CubeFace.Right;
                if (face == CubeFace.Right)
                    return CubeFace.Front;
                break;
            
            case ConnectionDirection.West:
                if (face == CubeFace.Front || face == CubeFace.Top)
                    return CubeFace.Right;
                if (face == CubeFace.Right)
                    return CubeFace.Back;
                if (face == CubeFace.Back || face == CubeFace.Bottom)
                    return CubeFace.Left;
                if (face == CubeFace.Left)
                    return CubeFace.Front;
                break;
            
            default:
                throw new Exception("Faces are not connected");
        }
        throw new Exception("Faces are not connected");
    }
    
    /// <summary>
    /// Checks all UV maps to find out on which face the vertex lies on.
    /// </summary>
    private CubeFace FaceFromVertex(int vertexIndex)
    {
        foreach (var mapFacePair in uvMapToFace)
        {
            var point = mapFacePair.Value[vertexIndex];
            if (point.x >= 0 && point.y >= 0)
                return mapFacePair.Key;
        }
        throw new Exception("Vertex is not on any of the UV maps.");
    }

    /// <summary>
    /// Converts the position of a triangle (3 vertices) into a position on a grid.
    /// </summary>
    private GridPoint GridPointFromTriangle(int[] vertexIndices)
    {
        float tileSize = (float) 1 / GridSize;
        CubeFace face = FaceFromVertex(vertexIndices[0]); // they should all be on the same face, surely nothing can go wrong
        List<float> uvXValues = new List<float>();
        List<float> uvYValues = new List<float>();

        foreach (var vertexIndex in vertexIndices)
        {
            uvXValues.Add(uvMapToFace[face][vertexIndex].x);
            uvYValues.Add(uvMapToFace[face][vertexIndex].y);
        }

        int x = Mathf.RoundToInt(uvXValues.Min() / tileSize); //TODO: UV + floating point numbers = im too tired for this.
        int y = Mathf.RoundToInt(uvYValues.Min() / tileSize);
        
        return new GridPoint(face, x, y);
    }

}
