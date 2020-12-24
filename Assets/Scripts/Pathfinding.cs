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
    
    public static int gridSize { get; private set; }
    public static int gridMax => gridSize - 1;
    public LayerMask layerMask;
    public GameObject cubeSphere;
    public DebugDraw debugDrawer;
    
    private Dictionary<CubeFace, Vector2[]> uvMapToFace;
    private Mesh cubeSphereMesh;
    
    /// <summary>
    /// Start gets called at the start ;)
    /// </summary>
    private void Start()
    {
        cubeSphereMesh = cubeSphere.GetComponent<MeshFilter>().mesh;
        InitializeUvMapToFaceDictionary(cubeSphereMesh);
        
        // All tiles are made of 2 triangles and theres 6 faces on the cube
        var triangleCount = cubeSphereMesh.triangles.Length / 3;
        // ReSharper disable once PossibleLossOfFraction
        gridSize = (int) Mathf.Sqrt(triangleCount / 2 / 6);
        
        InitializeNodeGrids(cubeSphereMesh.GetTriangles(0));
    }

    /// <summary>
    /// A* Pathfinding between two positions provided by two raycasts. 
    /// </summary>
    public void FindPath(RaycastHit playerRayHit, RaycastHit mouseRayHit)
    {
        int[] triangles = cubeSphereMesh.triangles;

        if (playerRayHit.triangleIndex == -1 || mouseRayHit.triangleIndex == -1)
            throw new Exception("Something is wrong with the mesh collider (triangleIndex = -1)");
        
        int[] startTriangleIndices =
        {
            triangles[playerRayHit.triangleIndex * 3 + 0],
            triangles[playerRayHit.triangleIndex * 3 + 1],
            triangles[playerRayHit.triangleIndex * 3 + 2]
        };
        int[] endTriangleIndices =
        {
            triangles[mouseRayHit.triangleIndex * 3 + 0],
            triangles[mouseRayHit.triangleIndex * 3 + 1],
            triangles[mouseRayHit.triangleIndex * 3 + 2]
        };

        Node end = NodeFromTriangle(endTriangleIndices);
        //debugDrawer.DrawSelectedNode(end);
        Debug.Log("| X: " + end.x + "| Y: " + end.y + " | Face: " + end.face);
        //debugDrawer.HighlightNeighbours(end.Neighbours());
        AStar(NodeFromTriangle(startTriangleIndices), NodeFromTriangle(endTriangleIndices));
    }

    /// <summary>
    /// Implementation of the A* algorithm.
    /// </summary>
    private void AStar(Node startNode, Node endNode)
    {
        List<Node> open = new List<Node>();
        HashSet<Node> closed = new HashSet<Node>();
        List<Node> path = new List<Node>();
        open.Add(startNode);

        while (open.Count > 0 )
        {
            Node current = open[0];
            for (int i = 1; i < open.Count; i++) // find node in open with lowest fCost
            {
                if (open[i].fCost < current.fCost || open[i].fCost == current.fCost && open[i].hCost < current.hCost)
                    current = open[i];
            }

            open.Remove(current);
            closed.Add(current); // add to closed list and remove from open list

            if (current == endNode) // did we find the end ?
            {
                Debug.Log("Path found");
                debugDrawer.HighlightPath(RetracePath(startNode, endNode));
                return;
            }

            foreach (var neighbour in current.Neighbours())
            {
                if (!neighbour.walkable || closed.Contains(neighbour))
                    continue;
                
                
                int newCost = current.gCost + CalculateShortestSurfaceDistance(current.ToGridPoint(), neighbour.ToGridPoint());
                if (newCost < neighbour.gCost || !open.Contains(neighbour))
                {
                    neighbour.gCost = newCost;
                    neighbour.hCost = CalculateShortestSurfaceDistance(neighbour.ToGridPoint(), endNode.ToGridPoint());
                    neighbour.parent = current;
                    
                    if (!open.Contains(neighbour))
                        open.Add(neighbour);
                }
            }
        }
    }
    
    /// <summary>
    ///  Retraces the path of traversed nodes from the start node to the end node.
    /// </summary>
    private List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node current = endNode;

        while (current != startNode)
        {
            path.Add(current);
            current = current.parent;
        }

        path.Reverse();
        return path;
    }

    /// <summary>
    /// Initializes the Dictionary that connects uv maps to CubeFaces
    /// </summary>
    /// <param name="mesh">The mesh that contains the uv maps.</param>
    private void InitializeUvMapToFaceDictionary(Mesh mesh)
    {
        uvMapToFace = new Dictionary<CubeFace, Vector2[]>()
        {
            {CubeFace.Front, mesh.uv}, {CubeFace.Top, mesh.uv2}, {CubeFace.Back, mesh.uv3},
            {CubeFace.Bottom, mesh.uv4}, {CubeFace.Left, mesh.uv5}, {CubeFace.Right, mesh.uv6}
        };
    }

    /// <summary>
    /// Initializes all the Nodes in the dictionary (Node.nodes).
    /// </summary>
    private void InitializeNodeGrids(int[] tri)
    {
        if (gridSize == 0) throw new Exception("can't initialize node grid before gridsize is initialized.");
        Node.nodes = new Dictionary<Tuple<CubeFace, int, int>, Node>();
        Vector3[] vertices = cubeSphereMesh.vertices;
        
        // Create the node objects
        foreach (CubeFace face in (CubeFace[]) Enum.GetValues(typeof(CubeFace)))
        {
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    var key = new Tuple<CubeFace, int, int>(face, x, y);
                    Node.nodes[key] = new Node(face, x, y);
                }
            }
        }

        // set node triangles
        for (int i = 0; i < tri.Length; i += 3)
        {
            // indices
            int i1 = tri[i + 0];
            int i2 = tri[i + 1];
            int i3 = tri[i + 2];
            Node node = NodeFromTriangle(new [] {i1,i2,i3});
            if (node.worldPos != Vector3.zero) continue;
            
            // points in world space
            Vector3[] points =
            {
                cubeSphere.transform.TransformPoint(vertices[i1]),
                cubeSphere.transform.TransformPoint(vertices[i2]),
                cubeSphere.transform.TransformPoint(vertices[i3])
            };
            node.AddTriangle(points);
        }
        // set world positions
        foreach (var pair in Node.nodes)
            pair.Value.CalculateWorldPosition();
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

        // CASE 2: end point is on a face that is connected to the start point.
        if (IsConnectedFace(start.face, end.face))
            return ConnectedFaceDistance(start, end);
        
        // CASE 3: start and end point are on opposing faces.
        return OpposingFaceDistance(start, end);
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
    /// Finds the shortest surface distance in the case of the points being on connected Faces.
    /// This method is separate from CalculateShortestSurfaceDistance only for readability purposes.
    /// </summary>
    private static int ConnectedFaceDistance(GridPoint start, GridPoint end)
    {
        ConnectionDirection dir1 = GetConnectionDirection(start.face, end.face);
        ConnectionDirection dir2 = GetConnectionDirection(start.face, end.face);
            
        if (dir1 == dir2) // rotate 180
        {
            end.RotateGrid90CW();
            end.RotateGrid90CW();
        } 
        else if (!IsOppositeDirection(dir1, dir2))
        { // rotate 90, but in which direction ???
            if (dir1 == ConnectionDirection.North && dir2 == ConnectionDirection.East  ||
                dir1 == ConnectionDirection.East  && dir2 == ConnectionDirection.South ||
                dir1 == ConnectionDirection.South && dir2 == ConnectionDirection.West  ||
                dir1 == ConnectionDirection.West  && dir2 == ConnectionDirection.North)
            {
                end.RotateGrid90CW();
            }
            else
            {
                end.RotateGrid90CCW();
            }
        }
        switch (dir1)
        {
            case ConnectionDirection.North:
                end.y += gridSize;
                break;
                
            case ConnectionDirection.East:
                end.x += gridSize;
                break;
                
            case ConnectionDirection.South:
                start.y += gridSize;
                break;
                
            case ConnectionDirection.West:
                start.x += gridSize;
                break;
                
            case ConnectionDirection.None:
                throw new Exception("Calculating shortest surface distance should've been detected as Case 3 (opposite faces) but somehow got Case 2");
                    
            default:
                throw new Exception("Uh-oh");
        }

        
        
        return CalculateShortestDistance(start, end);
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
                start.x += gridSize;
                end.y += (gridSize * 2);
                break;
            
            case ExtendedGrid.TopMiddle:
                end.MirrorGridVertical();
                end.y += (gridSize * 2);
                break;
            
            case ExtendedGrid.TopRight:
                end.MirrorGridVertical();
                end.RotateGrid90CW();
                end.x += gridSize;
                end.y += (gridSize * 2);
                break;
            
            
            case ExtendedGrid.RightTop:
                end.MirrorGridVertical();
                end.RotateGrid90CW();
                end.x += (gridSize * 2);
                end.y += gridSize;
                break;
            
            case ExtendedGrid.RightMiddle:
                end.MirrorGridHorizontal();
                end.x += (gridSize * 2);
                break;
            
            case ExtendedGrid.RightBot:
                end.MirrorGridHorizontal();
                end.RotateGrid90CW();
                start.y += gridSize;
                end.x += gridSize * 2;
                break;
            
            
            case ExtendedGrid.BotRight:
                end.MirrorGridHorizontal();
                end.RotateGrid90CW();
                start.y += gridSize * 2;
                end.x += gridSize;
                break;
            
            case ExtendedGrid.BotMiddle:
                end.MirrorGridVertical();
                start.y += (gridSize * 2);
                break;
            
            case ExtendedGrid.BotLeft:
                end.MirrorGridVertical();
                end.RotateGrid90CW();
                start.x += gridSize;
                start.y += gridSize * 2;
                break;
            
            
            case ExtendedGrid.LeftBot:
                end.MirrorGridVertical();
                end.RotateGrid90CW();
                start.y += gridSize;
                start.x += (gridSize * 2);
                break;
            
            case ExtendedGrid.LeftMiddle:
                end.MirrorGridHorizontal();
                start.x += gridSize * 2;
                break;
            
            case ExtendedGrid.LeftTop:
                end.MirrorGridHorizontal();
                end.RotateGrid90CW();
                start.x += gridSize * 2;
                end.y += gridSize;
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
        if (
            startFace == CubeFace.Front  && endFace == CubeFace.Top    ||
            startFace == CubeFace.Top    && endFace == CubeFace.Back   ||
            startFace == CubeFace.Back   && endFace == CubeFace.Bottom ||
            startFace == CubeFace.Bottom && endFace == CubeFace.Front  ||
            startFace == CubeFace.Right  && endFace == CubeFace.Top    ||
            startFace == CubeFace.Left   && endFace == CubeFace.Top      
            )
            return ConnectionDirection.North;
        
        if (
            startFace == CubeFace.Front  && endFace == CubeFace.Right  ||
            startFace == CubeFace.Right  && endFace == CubeFace.Back   ||
            startFace == CubeFace.Back   && endFace == CubeFace.Left   ||
            startFace == CubeFace.Left   && endFace == CubeFace.Front  ||
            startFace == CubeFace.Top    && endFace == CubeFace.Right  ||
            startFace == CubeFace.Bottom && endFace == CubeFace.Right    
            )
            return ConnectionDirection.East;
        
        if (
            startFace == CubeFace.Front  && endFace == CubeFace.Bottom ||
            startFace == CubeFace.Bottom && endFace == CubeFace.Back   ||
            startFace == CubeFace.Back   && endFace == CubeFace.Top    ||
            startFace == CubeFace.Top    && endFace == CubeFace.Front  ||
            startFace == CubeFace.Right  && endFace == CubeFace.Bottom ||
            startFace == CubeFace.Left   && endFace == CubeFace.Bottom   
            )
            return ConnectionDirection.South;
        
        if (
            startFace == CubeFace.Front  && endFace == CubeFace.Left   ||
            startFace == CubeFace.Left   && endFace == CubeFace.Back   ||
            startFace == CubeFace.Back   && endFace == CubeFace.Right  ||
            startFace == CubeFace.Right  && endFace == CubeFace.Front  ||
            startFace == CubeFace.Top    && endFace == CubeFace.Left   ||
            startFace == CubeFace.Bottom && endFace == CubeFace.Left     
            )
            return ConnectionDirection.West;

        return ConnectionDirection.None; // Faces aren't connected.
    }

    /// <summary>
    /// Returns the face that is connected to the provided face in the provided direction.
    /// </summary>
    public static CubeFace GetConnectedFace(CubeFace face, ConnectionDirection direction)
    {
        if (
            face == CubeFace.Front && direction == ConnectionDirection.North ||
            face == CubeFace.Left  && direction == ConnectionDirection.North ||
            face == CubeFace.Right && direction == ConnectionDirection.North ||
            face == CubeFace.Back  && direction == ConnectionDirection.North 
            )
            return CubeFace.Top;
        
        if (
            face == CubeFace.Front && direction == ConnectionDirection.South ||
            face == CubeFace.Left  && direction == ConnectionDirection.South ||
            face == CubeFace.Right && direction == ConnectionDirection.South ||
            face == CubeFace.Back  && direction == ConnectionDirection.South 
            )
            return CubeFace.Bottom;

        if (
            face == CubeFace.Right  && direction == ConnectionDirection.West  ||
            face == CubeFace.Left   && direction == ConnectionDirection.East  ||
            face == CubeFace.Top    && direction == ConnectionDirection.South ||
            face == CubeFace.Bottom && direction == ConnectionDirection.North 
            )
            return CubeFace.Front;
        
        if (
            face == CubeFace.Front  && direction == ConnectionDirection.East ||
            face == CubeFace.Back   && direction == ConnectionDirection.West ||
            face == CubeFace.Top    && direction == ConnectionDirection.East ||
            face == CubeFace.Bottom && direction == ConnectionDirection.East 
            )
            return CubeFace.Right;

        if (
            face == CubeFace.Front  && direction == ConnectionDirection.West ||
            face == CubeFace.Back   && direction == ConnectionDirection.East ||
            face == CubeFace.Top    && direction == ConnectionDirection.West ||
            face == CubeFace.Bottom && direction == ConnectionDirection.West 
            )
            return CubeFace.Left;

        if (
            face == CubeFace.Left   && direction == ConnectionDirection.West  ||
            face == CubeFace.Right  && direction == ConnectionDirection.East  ||
            face == CubeFace.Top    && direction == ConnectionDirection.North ||
            face == CubeFace.Bottom && direction == ConnectionDirection.South 
            )
            return CubeFace.Back;
        
        throw new Exception("Faces are not connected");
    }
    
    /// <summary>
    /// Checks all UV maps to find out on which face the vertices lie on.
    /// </summary>
    private CubeFace FaceFromVertices(int[] vertexIndices)
    {
        foreach (var mapFacePair in uvMapToFace)
        {
            var point1 = mapFacePair.Value[vertexIndices[0]];
            var point2 = mapFacePair.Value[vertexIndices[1]];
            var point3 = mapFacePair.Value[vertexIndices[2]];
            
            if (point1.x >= 0 && point1.y >= 0 && point2.x >= 0 && point2.y >= 0 && point3.x >= 0 && point3.y >= 0)
                return mapFacePair.Key;
        }
        throw new Exception("Vertex is not on any of the UV maps.");
    }

    /// <summary>
    /// Converts the position of a triangle (3 vertices) into a Node.
    /// </summary>
    private Node NodeFromTriangle(int[] vertexIndices)
    {
        float tileSize = (float) 1 / gridSize;
        CubeFace face = FaceFromVertices(vertexIndices);
        List<float> uvXValues = new List<float>();
        List<float> uvYValues = new List<float>();

        foreach (var vertexIndex in vertexIndices)
        {
            uvXValues.Add(uvMapToFace[face][vertexIndex].x);
            uvYValues.Add(uvMapToFace[face][vertexIndex].y);
        }

        //TODO: floating point number headaches when gridSize isn't as even
        int x = Mathf.RoundToInt(uvXValues.Min() / tileSize); 
        int y = Mathf.RoundToInt(uvYValues.Min() / tileSize);
        
        return Node.GetNode(face, x, y);
    }

    /// <summary>
    ///  Returns true if the directions provided are opposite directions
    /// </summary>
    public static bool IsOppositeDirection(ConnectionDirection dir1, ConnectionDirection dir2)
    {
        return dir1 == ConnectionDirection.North && dir2 == ConnectionDirection.South ||
               dir1 == ConnectionDirection.South && dir2 == ConnectionDirection.North ||
               dir1 == ConnectionDirection.East  && dir2 == ConnectionDirection.West  ||
               dir1 == ConnectionDirection.West  && dir2 == ConnectionDirection.East;
    }

}
