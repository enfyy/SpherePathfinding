using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Pf = Pathfinding;
using Vector3 = UnityEngine.Vector3;

public class Node
{
    public static Dictionary<Tuple<CubeFace, int, int>, Node> nodes { get; set; }
    public static float nodeRadius;

    public int x { get; }
    public int y { get; }
    public CubeFace face { get; }
    public Vector3[] triangle1 { get; set; }
    public Vector3[] triangle2 { get; set; }
    public Vector3 worldPos { get; set; }
    public bool walkable { get; set; }
    public int gCost { get; set; }
    public int hCost { get; set; }
    public int fCost => gCost + hCost;
    public Node parent;
    

    public Node(CubeFace face, int x, int y)
    {
        if (x > Pf.gridMax || y > Pf.gridMax || y < 0 || x < 0)
            throw new Exception("Node coordinates outside of grid."); 
        this.face = face;
        this.x = x;
        this.y = y;
        walkable = true;
        worldPos = Vector3.zero;
    }

    /// <summary>
    /// Returns a new gridPoint with the same coordinates as the node.
    /// </summary>
    public GridPoint ToGridPoint()
    {
        return new GridPoint(face, x, y);
    }

    /// <summary>
    /// Sets triangle property.
    /// </summary>
    public void AddTriangle(Vector3[] triangle)
    {
        if (triangle1 == null)
            triangle1 = triangle;
        else if (triangle2 == null)
            triangle2 = triangle;
        else
            throw new Exception("both triangles of this node are already set");
    }

    /// <summary>
    /// Calculates the worldPosition (center point in world space) of this node. 
    /// </summary>
    public void CalculateWorldPosition()
    {
        if (triangle1 == null || triangle2 == null) throw new Exception("worldPosition cannot be calculated because the triangles of the node are not set " + x + " | "+ y );
        var points = triangle1.Intersect(triangle2).ToArray();
        worldPos = Vector3.Lerp(points[0], points[1], 0.5f);

        if (x == 0 && y == 0 && face == CubeFace.Front) // this is probably bad practice :)
            nodeRadius = Vector3.Distance(points[0], worldPos);

    }

    /// <summary>
    /// Looks up the node in the node grid dictionary
    /// </summary>
    public static Node GetNode(CubeFace face, int x, int y)
    {
        if (x > Pf.gridMax || y > Pf.gridMax || y < 0 || x < 0)
            throw new Exception("Node coordinates outside of grid."); 
        if (nodes == null)
            throw new Exception("Node grid has not been initialized yet.");
        return nodes[new Tuple<CubeFace, int, int>(face, x, y)];
    }
    
    /// <summary>
    /// Returns the neighbour that is not on the same grid.
    /// </summary>
    private Node EdgeNeighbour(ConnectionDirection direction, int xCoordinate, int yCoordinate)
    {
        //TODO: optimization
        int neighbourX = xCoordinate;
        int neighbourY = yCoordinate;

        if (face == CubeFace.Front  && direction == ConnectionDirection.North ||
            face == CubeFace.Bottom && direction == ConnectionDirection.North )
            neighbourY = 0;

        if (face == CubeFace.Front && direction == ConnectionDirection.East ||
            face == CubeFace.Right && direction == ConnectionDirection.East ||
            face == CubeFace.Back  && direction == ConnectionDirection.East ||
            face == CubeFace.Left  && direction == ConnectionDirection.East )
            neighbourX = 0;

        if (face == CubeFace.Front && direction == ConnectionDirection.West ||
            face == CubeFace.Right && direction == ConnectionDirection.West ||
            face == CubeFace.Back  && direction == ConnectionDirection.West ||
            face == CubeFace.Left  && direction == ConnectionDirection.West )
            neighbourX = Pf.gridMax;

        if (face == CubeFace.Front && direction == ConnectionDirection.South ||
            face == CubeFace.Top   && direction == ConnectionDirection.South )
            neighbourY = Pf.gridMax;

        if (face == CubeFace.Top && direction == ConnectionDirection.East )
        {
            neighbourX = yCoordinate;
            neighbourY = Pf.gridMax;
        }

        if (face == CubeFace.Top  && direction == ConnectionDirection.North ||
            face == CubeFace.Back && direction == ConnectionDirection.North )
        {
            neighbourX = Pf.gridMax - xCoordinate;
            neighbourY = Pf.gridMax;
        }

        if (face == CubeFace.Right && direction == ConnectionDirection.North)
        {
            neighbourX = Pf.gridMax;
            neighbourY = xCoordinate;
        }

        if (face == CubeFace.Left && direction == ConnectionDirection.South)
        {
            neighbourX = 0;
            neighbourY = xCoordinate;
        }

        if (face == CubeFace.Back   && direction == ConnectionDirection.South ||
            face == CubeFace.Bottom && direction == ConnectionDirection.South )
        {
            neighbourX = Pf.gridMax - xCoordinate;
            neighbourY = 0;
        }

        if (face == CubeFace.Bottom && direction == ConnectionDirection.East)
        {
            neighbourX = Pf.gridMax - yCoordinate;
            neighbourY = 0;
        }

        if (face == CubeFace.Bottom && direction == ConnectionDirection.West)
        {
            neighbourX = yCoordinate;
            neighbourY = 0;
        }

        if (face == CubeFace.Top && direction == ConnectionDirection.West)
        {
            neighbourX = Pf.gridMax - yCoordinate;
            neighbourY = Pf.gridMax;
        }

        if (face == CubeFace.Left && direction == ConnectionDirection.North)
        {
            neighbourX = 0;
            neighbourY = Pf.gridMax - xCoordinate;
        }

        if (face == CubeFace.Right && direction == ConnectionDirection.South)
        {
            neighbourX = Pf.gridMax;
            neighbourY = Pf.gridMax - xCoordinate;
        }

        return GetNode(Pf.GetConnectedFace(face, direction), neighbourX, neighbourY);
    }

    /// <summary>
    /// Returns a list of all the neighbouring Nodes.
    /// </summary>
    public List<Node> Neighbours()
    {
        List<Node> neighbours = new List<Node>();

        if (y != Pf.gridMax && y != 0 && x != Pf.gridMax && x != 0) // NO EDGE NEIGHBOURS
        {
            
            neighbours.Add(GetNode(face,   x,     y + 1));
            neighbours.Add(GetNode(face,   x,     y - 1));
            neighbours.Add(GetNode(face, x + 1, y + 1));
            neighbours.Add(GetNode(face, x + 1,   y    ));
            neighbours.Add(GetNode(face, x + 1, y - 1));
            neighbours.Add(GetNode(face, x - 1, y + 1));
            neighbours.Add(GetNode(face, x - 1, y - 1));
            neighbours.Add(GetNode(face, x - 1,   y    ));
        }
        else if (x == 0) // LEFT EDGE
        {
            neighbours.Add(GetNode(face, x + 1, y));

            if (y == Pf.gridMax) // TOP LEFT CORNER
            {
                neighbours.Add(GetNode(face, x, y - 1));
                neighbours.Add(GetNode(face, x + 1, y - 1));
                // NORTH EDGE NEIGHBOURS 
                neighbours.Add(EdgeNeighbour(ConnectionDirection.North, x, y));
                neighbours.Add(EdgeNeighbour(ConnectionDirection.North, x + 1, y));
                // WEST EDGE NEIGHBOURS
                neighbours.Add(EdgeNeighbour(ConnectionDirection.West, x, y));
                neighbours.Add(EdgeNeighbour(ConnectionDirection.West, x, y - 1));
            }
            else if (y == 0) // BOTTOM LEFT CORNER
            {
                neighbours.Add(GetNode(face, x, y + 1));
                neighbours.Add(GetNode(face, x + 1, y + 1));
                // SOUTH EDGE NEIGHBOURS 
                neighbours.Add(EdgeNeighbour(ConnectionDirection.South, x, y));
                neighbours.Add(EdgeNeighbour(ConnectionDirection.South, x + 1, y));
                // WEST EDGE NEIGHBOURS
                neighbours.Add(EdgeNeighbour(ConnectionDirection.West, x, y));
                neighbours.Add(EdgeNeighbour(ConnectionDirection.West, x, y + 1));
            }
            else
            {
                neighbours.Add(GetNode(face, x + 1, y + 1));
                neighbours.Add(GetNode(face, x + 1, y - 1));
                neighbours.Add(GetNode(face, x, y + 1));
                neighbours.Add(GetNode(face, x, y - 1));

                // WEST EDGE NEIGHBOURS
                neighbours.Add(EdgeNeighbour(ConnectionDirection.West, x, y));
                neighbours.Add(EdgeNeighbour(ConnectionDirection.West, x, y + 1));
                neighbours.Add(EdgeNeighbour(ConnectionDirection.West, x, y - 1));
            }
        }
        else if (x == Pf.gridMax) // RIGHT EDGE
        {
            neighbours.Add(GetNode(face, x - 1, y));

            if (y == Pf.gridMax) // TOP RIGHT CORNER
            {
                neighbours.Add(GetNode(face, x, y - 1));
                neighbours.Add(GetNode(face, x - 1, y - 1));
                // NORTH EDGE NEIGHBOURS 
                neighbours.Add(EdgeNeighbour(ConnectionDirection.North, x, y));
                neighbours.Add(EdgeNeighbour(ConnectionDirection.North, x - 1, y));
                // EAST EDGE NEIGHBOURS
                neighbours.Add(EdgeNeighbour(ConnectionDirection.East, x, y));
                neighbours.Add(EdgeNeighbour(ConnectionDirection.East, x, y - 1));
            }
            else if (y == 0) // BOTTOM RIGHT CORNER
            {
                neighbours.Add(GetNode(face, x - 1, y + 1));
                neighbours.Add(GetNode(face, x, y + 1));
                // EAST EDGE NEIGHBOURS 
                neighbours.Add(EdgeNeighbour(ConnectionDirection.East, x, y));
                neighbours.Add(EdgeNeighbour(ConnectionDirection.East, x, y + 1));
                // SOUTH EDGE NEIGHBOURS
                neighbours.Add(EdgeNeighbour(ConnectionDirection.South, x, y));
                neighbours.Add(EdgeNeighbour(ConnectionDirection.South, x - 1, y));
            }
            else
            {
                neighbours.Add(GetNode(face, x - 1, y + 1));
                neighbours.Add(GetNode(face, x - 1, y - 1));
                neighbours.Add(GetNode(face, x, y + 1));
                neighbours.Add(GetNode(face, x, y - 1));

                // EAST EDGE NEIGHBOURS
                neighbours.Add(EdgeNeighbour(ConnectionDirection.East, x, y));
                neighbours.Add(EdgeNeighbour(ConnectionDirection.East, x, y + 1));
                neighbours.Add(EdgeNeighbour(ConnectionDirection.East, x, y - 1));
            }
        }
        else if (y == 0) // BOTTOM EDGE
        {
            neighbours.Add(GetNode(face, x - 1, y));
            neighbours.Add(GetNode(face, x + 1, y));
            
            neighbours.Add(GetNode(face, x - 1, y + 1));
            neighbours.Add(GetNode(face, x + 0, y + 1));
            neighbours.Add(GetNode(face, x + 1, y + 1));


            neighbours.Add(EdgeNeighbour(ConnectionDirection.South, x + 0, y));
            neighbours.Add(EdgeNeighbour(ConnectionDirection.South, x - 1, y));
            neighbours.Add(EdgeNeighbour(ConnectionDirection.South, x + 1, y));
        } 
        else if (y == Pf.gridMax) // TOP EDGE
        {
            neighbours.Add(GetNode(face, x - 1, y));
            neighbours.Add(GetNode(face, x + 1, y));
            
            neighbours.Add(GetNode(face, x - 1, y - 1));
            neighbours.Add(GetNode(face, x + 0, y - 1));
            neighbours.Add(GetNode(face, x + 1, y - 1));

            neighbours.Add(EdgeNeighbour(ConnectionDirection.North, x + 0, y));
            neighbours.Add(EdgeNeighbour(ConnectionDirection.North, x - 1, y));
            neighbours.Add(EdgeNeighbour(ConnectionDirection.North, x + 1, y));
        } 
        return neighbours;
    }
}
