using System;
using System.Collections.Generic;
using UnityEngine;

public class Piece {
    public Vector3[] points;
    public int[] triangles;

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

        double radius = 1/6.0;
        double centerInset = 1/8.0; // must be smaller than radius
        int numStops = 12;
        double dy = Math.Sqrt(radius * radius - centerInset * centerInset);

        double startAng = Math.Atan2(dy, centerInset);
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
        /*
        var points = new Vector3[7];
        points[0] = new Vector3(0, 0, 0);
        points[1] = new Vector3(0.5f, 0, 0);
        points[2] = new Vector3(0.5f, 0.5f, 0);
        points[3] = new Vector3(0, 0.5f, 0);
        points[4] = new Vector3(0, 0.3f, 0);
        points[5] = new Vector3(0.1f, 0.25f, 0);
        points[6] = new Vector3(0, 0.2f, 0);
        var triangles = new int[5*3]
        {
            6, 5, 0,
            0, 5, 1,
            1, 5, 2,
            2, 5, 3,
            3, 5, 4,
        };
        list.Add(new Piece{points = points, triangles = triangles});
        */

        float horScale = 1.0f/columns;
        float verScale = 1.0f/rows;

        // TODO: split the whole picture into pieces
        //       edge pieces have to have flat edges
        //       inner edges should be randomly either In or Out
        //       and adjacent pieces must match

        Piece piece = makeUnitPiece(new EdgeType[4] {
            EdgeType.Out,
            EdgeType.Out,
            EdgeType.Out,
            EdgeType.Out
        });

        piece.Transform(horScale, verScale, 2*horScale, 3*verScale);
        list.Add(piece);

        Piece piece2 = makeUnitPiece(new EdgeType[4] {
            EdgeType.In,
            EdgeType.In,
            EdgeType.In,
            EdgeType.In
        });

        piece2.Transform(horScale, verScale, 4*horScale, 1*verScale);
        list.Add(piece2);

        Piece piece3 = makeUnitPiece(new EdgeType[4] {
            EdgeType.Flat,
            EdgeType.Out,
            EdgeType.In,
            EdgeType.Out
        });

        piece3.Transform(horScale, verScale, 1*horScale, 3*verScale);
        list.Add(piece3);

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

