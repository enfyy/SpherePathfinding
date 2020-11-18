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

    /// <summary>
    /// Basic constructor.
    /// </summary>
    public GridPoint(CubeFace faceGrid, int x, int y)
    {
        face = faceGrid;
        this.x = x;
        this.y = y;
    }
    
    /// <summary>
    ///  Updates the x and y coordinates as if the grid were to be rotated clockwise.
    /// </summary>
    /// <param name="rotationCount90CW"> The count of 90deg rotations </param>
    public void RotateGrid90CW(int rotationCount90CW = 1)
    {
        if (x >= Pf.GridSize || y >= Pf.GridSize)
            throw new Exception("Array will be out of bounds");

        // initialize matrix
        bool[][] matrix = new bool[Pf.GridSize][];
        for (int i = 0; i < Pf.GridSize; i++)
        {
            matrix[i] = new bool[Pf.GridSize];
            for (int j = 0; j < Pf.GridSize; j++)
                matrix[i][j] = false;
        }
        
        matrix[y][x] = true;
        Array.Reverse(matrix);

        for (int r = 0; r < rotationCount90CW; r++)
        {
            // transpose matrix
            for (int i = 0; i < Pf.GridSize; i++)
            {
                for (int j = i; j < Pf.GridSize; j++)
                {
                    bool temp = matrix[i][j];
                    matrix[i][j] = matrix[j][i];
                    matrix[j][i] = temp;
                }
            }
            // Reverse the matrix
            for (int i = 0; i < Pf.GridSize; i++)
                Array.Reverse(matrix[i]);
        }

        Array.Reverse(matrix);
        for (int i = 0; i < Pf.GridSize; i++)
        {
            for (int j = 0; j < Pf.GridSize; j++)
                if (matrix[i][j])
                {
                    x = j;
                    y = i;
                    return;
                }
        }
        throw new Exception("Something went wrong while rotating the Matrix");
    }
    
    /// <summary>
    /// Converts coordinates x and y on the grid as if the grid is mirrored horizontally (0,0 -> 3,0) (assuming Pf.GRID_SIZE = 4) 
    /// </summary>
    public void MirrorGridHorizontal()
    {
        if (x >= Pf.GridSize || y >= Pf.GridSize)
            throw new Exception("Coordinates are outside the grid.");

        // initialize matrix
        bool[][] matrix = new bool[Pf.GridSize][];
        for (int i = 0; i < Pf.GridSize; i++)
        {
            matrix[i] = new bool[Pf.GridSize];
            for (int j = 0; j < Pf.GridSize; j++)
                matrix[i][j] = false;
        }
        
        matrix[y][x] = true;
        Array.Reverse(matrix);

        for (int i = 0; i < Pf.GridSize; i++)
        {
            Array.Reverse(matrix[i]);
        }
        
        Array.Reverse(matrix);
        for (int i = 0; i < Pf.GridSize; i++)
        {
            for (int j = 0; j < Pf.GridSize; j++)
                if (matrix[i][j]) 
                {
                    x = j;
                    y = i;
                    return;
                }
        }
        throw new Exception("Mirroring horizontally went wrong.");
    }
    
    /// <summary>
    /// Converts coordinates x and y on the grid as if the grid is mirrored vertically (0,0 -> 0,3) (assuming Pf.GRID_SIZE = 4) 
    /// </summary>
    public void MirrorGridVertical()
    {
        if (x >= Pf.GridSize || y >= Pf.GridSize)
            throw new Exception("Coordinates are outside the grid.");

        // initialize matrix
        bool[][] matrix = new bool[Pf.GridSize][];
        for (int i = 0; i < Pf.GridSize; i++)
        {
            matrix[i] = new bool[Pf.GridSize];
            for (int j = 0; j < Pf.GridSize; j++)
                matrix[i][j] = false;
        }
        
        matrix[y][x] = true;
        
        Array.Reverse(matrix);
        for (int i = 0; i < Pf.GridSize; i++)
        {
            for (int j = 0; j < Pf.GridSize; j++)
                if (matrix[i][j])                 
                {
                    x = j;
                    y = i;
                    return;
                }
        }
        throw new Exception("Mirroring vertically went wrong.");
    }

    /// <summary>
    /// Used to determine where the point lies in the grid. This information is vital for calculating the distance to a point on an opposing face.
    /// Only used on start point.
    /// </summary>
    /// <returns> The GridZone in which the point in the grid lies.</returns>
    public GridZone DetermineGridZone()
    {
        if (x >= Pf.GridSize || y >= Pf.GridSize)
            throw new Exception("Coordinates are outside the grid.");
            
        
        int gridIndex = y * Pf.GridSize + x + 1;
        int bottomLeftToTopRight = y + (y * Pf.GridSize) + 1;
        int topLeftToBottomRight = Pf.GridSize + (y * Pf.GridSize) - y;

        bool topHalf = y >= Pf.GridSize / 2;
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
