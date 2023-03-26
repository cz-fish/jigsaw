using System;
using System.Collections.Generic;
using UnityEngine;

public class Piece {
    public Vector3[] points;
    public int[] triangles;
    public int row;
    public int column;

    public void Transform(float scaleX, float scaleY, float translateX, float translateY)
    {
        for (int i = 0; i < points.Length; ++i) {
            points[i] = new Vector3(
                points[i].x * scaleX + translateX,
                points[i].y * scaleY + translateY,
                0
            );
        }
    }
}

public class PuzzleCutter 
{
    private enum EdgeType {
        Flat,
        In,
        Out,
    }

    private enum EdgeOrder {
        Left,
        Bottom,
        Right,
        Top,
    }

    // coordinates of a unit bump (Out) on each of the 4 edges
    private List<Vector2>[] m_bumpCoords;

    public PuzzleCutter() {
        m_bumpCoords = new List<Vector2>[4] {
            new List<Vector2>(),
            new List<Vector2>(),
            new List<Vector2>(),
            new List<Vector2>(),
        };

        // Radius of the circular bump
        double radius = 1/6.0;
        // Offset of the center of the bump from the piece edge. Must be smaller than radius
        double centerInset = 1/9.0;
        // Number of points on the bump
        int numStops = 12;

        // Calculate the angle of the first point of the bump - this point is on the edge of the unit square
        double dy = Math.Sqrt(radius * radius - centerInset * centerInset);
        double startAng = Math.Atan2(dy, centerInset);
        // By adjusting the angle slightly, we curve the piece a little, so that the edges are not straight
        startAng -= 0.25;
        // Remaining angle to be divided into numStops equal stops
        double remAng = 2 * (Math.PI - startAng);
        double stopAng = remAng / numStops;

        for (int i = 0; i <= numStops; ++i) {
            {
                // EdgeOrder.Left
                double x = -centerInset + radius * Math.Cos(startAng + i * stopAng);
                double y = 0.5 + radius * Math.Sin(startAng + i * stopAng);
                m_bumpCoords[(int)EdgeOrder.Left].Add(new Vector2((float)x, (float)y));
            }
            {
                // EdgeOrder.Bottom
                double x = 0.5 - radius * Math.Sin(startAng + i * stopAng);
                double y = -centerInset + radius * Math.Cos(startAng + i * stopAng);
                m_bumpCoords[(int)EdgeOrder.Bottom].Add(new Vector2((float)x, (float)y));
            }
            {
                // EdgeOrder.Right
                double x = centerInset - radius * Math.Cos(startAng + i * stopAng);
                double y = 0.5 - radius * Math.Sin(startAng + i * stopAng);
                m_bumpCoords[(int)EdgeOrder.Right].Add(new Vector2((float)x, (float)y));
            }
            {
                // EdgeOrder.Top
                double x = 0.5 + radius * Math.Sin(startAng + i * stopAng);
                double y = centerInset - radius * Math.Cos(startAng + i * stopAng);
                m_bumpCoords[(int)EdgeOrder.Top].Add(new Vector2((float)x, (float)y));
            }
        }
    }

    public List<Piece> cutPieces(int width, int height, int rows, int columns, int randomSeed)
    {
        var list = new List<Piece>();

        var horizontalBumps = new List<List<bool>>();
        var verticalBumps = new List<List<bool>>();
        var rand = new System.Random(randomSeed);

        float horScale = 1.0f/columns;
        float verScale = 1.0f/rows;

        // randomly choose horizontal bumps of all pieces
        for (var row = 0; row < rows; ++row) {
            horizontalBumps.Add(new List<bool>());
            for (var column = 0; column < columns - 1; ++column) {
                horizontalBumps[row].Add(rand.Next() > int.MaxValue / 2);
            }
        }

        // randomly choose vertical bumps of all pieces
        for (var column = 0; column < columns; ++column) {
            verticalBumps.Add(new List<bool>());
            for (var row = 0; row < rows - 1; ++row) {
                verticalBumps[column].Add(rand.Next() > int.MaxValue / 2);
            }
        }

        // create all the pieces
        for (var row = 0; row < rows; ++row) {
            for (var column = 0; column < columns; ++column) {
                // edges are in EdgeOrder: left bottom right top
                var edges = new EdgeType[4] { EdgeType.Flat, EdgeType.Flat, EdgeType.Flat, EdgeType.Flat };
                if (column > 0) {
                    edges[0] = horizontalBumps[row][column-1] ? EdgeType.In : EdgeType.Out;
                }
                if (column < columns - 1) {
                    edges[2] = horizontalBumps[row][column] ? EdgeType.Out : EdgeType.In;
                }
                if (row > 0) {
                    edges[1] = verticalBumps[column][row-1] ? EdgeType.In : EdgeType.Out;
                }
                if (row < rows - 1) {
                    edges[3] = verticalBumps[column][row] ? EdgeType.Out : EdgeType.In;
                }

                var piece = makeUnitPiece(edges);
                piece.row = row;
                piece.column = column;
                piece.Transform(horScale, verScale, column * horScale, row * verScale);
                list.Add(piece);
            }
        }

        return list;
    }

    // edges are in EdgeOrder: left bottom right top
    private Piece makeUnitPiece(EdgeType[] edges)
    {
        if (edges.Length != 4) {
            throw new ArgumentException("Not 4 edges");
        }
        var corners = new Vector3[4] {
            new Vector3(0, 1),
            new Vector3(0, 0),
            new Vector3(1, 0),
            new Vector3(1, 1),
        };
        List<Vector3> points = new List<Vector3>();
        for (int e = 0; e < edges.Length; ++e) {
            var edgePoints = new List<Vector2>();
            points.Add(corners[e]);
            if (edges[e] != EdgeType.Flat) {
                float addX = 0;
                float addY = 0;
                float flipX = 1;
                float flipY = 1;
                if (e == (int)EdgeOrder.Right) {
                    addX = 1;
                }
                if (e == (int)EdgeOrder.Top) {
                    addY = 1;
                }
                if (edges[e] == EdgeType.In) {
                    if (e == (int)EdgeOrder.Left || e == (int)EdgeOrder.Right) {
                        flipX = -1;
                    } else {
                        flipY = -1;
                    }
                }

                for (int i = 0; i < m_bumpCoords[e].Count; ++i) {
                    var point = m_bumpCoords[e][i];
                    points.Add(new Vector3(
                        point.x * flipX + addX,
                        point.y * flipY + addY,
                        0
                    ));
                }
            }
            // else - flat edge - no extra points
        }
        
        var triangulator = new Triangulator(points);

        return new Piece() {
            points = points.ToArray(),
            triangles = triangulator.Triangulate()
        };
    }

}

