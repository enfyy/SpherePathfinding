using System;
using Pf = Pathfinding;

/// <summary>
/// Objects of this class represent a point (x,y) on a grid of Pf.GRID_SIZE. Each grid represents one face of a cube.
/// </summary>
public class GridPoint
{
    public int x { get; set; }
    public int y { get; set; }
    public CubeFace face { get; }

    public GridPoint(CubeFace face, int x, int y)
    {
        this.face = face;
        this.x = x;
        this.y = y;
    }

    /// <summary>
    ///  Updates the x and y coordinates as if the grid were to be rotated clockwise.
    /// </summary>
    public void RotateGrid90CW()
    {
        var tmp = x;
        x = y;
        y = -tmp + Pf.gridMax;
    }
    
    /// <summary>
    ///  Updates the x and y coordinates as if the grid were to be rotated counter-clockwise.
    /// </summary>
    public void RotateGrid90CCW()
    {
        var tmp = y;
        y = x;
        x = -tmp + Pf.gridMax;
    }

    /// <summary>
    /// Converts coordinates x and y on the grid as if the grid is mirrored horizontally (0,0 -> 3,0) (assuming Pf.GRID_SIZE = 4) 
    /// </summary>
    public void MirrorGridHorizontal()
    {
        y = -y + Pf.gridMax;
    }

    /// <summary>
    /// Converts coordinates x and y on the grid as if the grid is mirrored vertically (0,0 -> 0,3) (assuming Pf.GRID_SIZE = 4) 
    /// </summary>
    public void MirrorGridVertical()
    {
        x = -x + Pf.gridMax;
    }

    /// <summary>
    /// Used to determine where the point lies in the grid. This information is vital for calculating the distance to a point on an opposing face.
    /// Only used on start point.
    /// </summary>
    /// <returns> The GridZone in which the point in the grid lies.</returns>
    public GridZone DetermineGridZone()
    {
        if (x > Pf.gridMax || y > Pf.gridMax || y < 0 || x < 0)
            throw new Exception("Coordinates are outside the grid.");


        int gridIndex = y * Pf.gridSize + x + 1;
        int bottomLeftToTopRight = y + (y * Pf.gridSize) + 1;
        int topLeftToBottomRight = Pf.gridSize + (y * Pf.gridSize) - y;

        bool topHalf = y >= Pf.gridSize / 2;
        bool bottomHalf = !topHalf;


        if (gridIndex < topLeftToBottomRight && topHalf || gridIndex < bottomLeftToTopRight && bottomHalf)
            return GridZone.LeftMiddle;

        if (gridIndex > bottomLeftToTopRight && topHalf || gridIndex > topLeftToBottomRight && bottomHalf)
            return GridZone.RightMiddle;

        if (gridIndex > topLeftToBottomRight && gridIndex < bottomLeftToTopRight && topHalf)
            return GridZone.TopMiddle;

        if (gridIndex > bottomLeftToTopRight && gridIndex < topLeftToBottomRight && bottomHalf)
            return GridZone.BottomMiddle;

        if (gridIndex == topLeftToBottomRight && topHalf)
            return GridZone.TopLeft;

        if (gridIndex == bottomLeftToTopRight && topHalf)
            return GridZone.TopRight;

        if (gridIndex == topLeftToBottomRight && bottomHalf)
            return GridZone.BottomRight;

        if (gridIndex == bottomLeftToTopRight && bottomHalf)
            return GridZone.BottomLeft;

        throw new Exception("Something went wrong while determining the grid zone.");
    }
}
