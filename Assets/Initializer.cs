//using System.Collections;
//using System.Collections.Generic;
using UnityEngine;

public class Initializer : MonoBehaviour
{
    //public var material: Material;

    // Start is called before the first frame update
    void Start()
    {
        // Make material with texture
        var texture = Resources.Load<Texture2D>("2");
        var material = new Material(Shader.Find("Standard"));
        material.color = Color.white;
        material.SetTexture("_MainTex", texture);

        const float width = 6;
        const float height = 6;
        Vector3 origin = new Vector3(-3, -3, 0);

        PuzzleCutter cutter = new PuzzleCutter();
        var pieces = cutter.cutPieces(texture.width, texture.height, 5, 5, 13);
        int counter = 0;
        print($"Pieces: {pieces.Count}");
        foreach (var piece in pieces) {
            // Make each piece
            var mesh = new Mesh();
            int pieceSize = piece.points.Length;
            print($"Piece size: {pieceSize}");
            Vector3[] vertices = new Vector3[pieceSize];
            Vector3[] normals = new Vector3[pieceSize];
            Vector2[] uv = new Vector2[pieceSize];

            for (int i = 0; i < pieceSize; ++i) {
                // Adjust scale and offset
                vertices[i] = new Vector3(
                    piece.points[i][0] * width + origin[0],
                    piece.points[i][1] * height + origin[1],
                    0
                );
                uv[i] = new Vector2(
                    piece.points[i][0],
                    piece.points[i][1]
                );
                normals[i] = -Vector3.forward;
            }

            // FIXME: when the PuzzleCutter returns correct triangulation,
            //        use the mesh renderer instead of the line renderer
            
            /*
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = piece.triangles;
            */

            var pieceObj = new GameObject();
            pieceObj.name = $"piece{counter}";
            ++counter;

            print($"Vertices size: {vertices.Length}");

            LineRenderer lineRenderer = pieceObj.AddComponent<LineRenderer>();
            lineRenderer.loop = true;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.widthMultiplier = 0.05f;
            lineRenderer.positionCount = vertices.Length;
            lineRenderer.SetPositions(vertices);

            /*
            MeshRenderer meshRenderer = pieceObj.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
            MeshFilter meshFilter = pieceObj.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            */
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
