//using System.Collections;
//using System.Collections.Generic;
using UnityEngine;

public class JigsawGame : MonoBehaviour
{
    //public var material: Material;
    [SerializeField] private int numHorizontalPieces = 5;
    [SerializeField] private int numVerticalPieces = 5;
    [SerializeField] private float snapTolerance = 0.3f;

    [SerializeField] private float canvasWidth = 6f;
    [SerializeField] private float canvasHeight = 6f;
    [SerializeField] private float originX = -3f;
    [SerializeField] private float originY = -3f;

    // Start is called before the first frame update
    void Start()
    {
        // Make material with texture
        var texture = Resources.Load<Texture2D>("2");
        var material = new Material(Shader.Find("Standard"));
        material.color = Color.white;
        material.SetTexture("_MainTex", texture);

        PuzzleCutter cutter = new PuzzleCutter();
        const int randomSeed = 13; // FIXME: do we need a random seed?
        var pieces = cutter.cutPieces(texture.width, texture.height, numVerticalPieces, numHorizontalPieces, randomSeed);
        int counter = 0;
        foreach (var piece in pieces) {
            // Make each piece
            var mesh = new Mesh();
            int pieceSize = piece.points.Length;
            Vector3[] vertices = new Vector3[pieceSize];
            Vector3[] normals = new Vector3[pieceSize];
            Vector2[] uv = new Vector2[pieceSize];

            for (int i = 0; i < pieceSize; ++i) {
                // Adjust scale and offset
                vertices[i] = new Vector3(
                    piece.points[i][0] * canvasWidth + originX,
                    piece.points[i][1] * canvasHeight + originY,
                    0
                );
                uv[i] = new Vector2(
                    piece.points[i][0],
                    piece.points[i][1]
                );
                normals[i] = -Vector3.forward;
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = piece.triangles;

            var pieceObj = new GameObject();
            pieceObj.name = $"piece{counter}";
            ++counter;

            // TODO: disable the line outline, or maybe based on a setting
            // Line renderer for the outline only
            LineRenderer lineRenderer = pieceObj.AddComponent<LineRenderer>();
            lineRenderer.loop = true;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.widthMultiplier = 0.02f;
            lineRenderer.positionCount = vertices.Length;
            lineRenderer.SetPositions(vertices);

            // Mesh renderer for the texture
            MeshRenderer meshRenderer = pieceObj.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
            MeshFilter meshFilter = pieceObj.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            // Make draggable
            pieceObj.AddComponent<BoxCollider2D>();
            var jigsawPiece = pieceObj.AddComponent<JigsawPiece>();
            // Link to the game
            jigsawPiece.row = piece.row;
            jigsawPiece.column = piece.column;
            jigsawPiece.targetPosition = pieceObj.transform.position;
            jigsawPiece.m_game = this;
        }

        // TODO: scatter pieces randomly
    }

    public (Vector3 position, bool isPlaced) DropPiece(JigsawPiece piece)
    {
        var distance = (piece.transform.position - piece.targetPosition).magnitude;
        if (distance < snapTolerance) {
            // TODO: apply to all other pieces in group, if dragging a group
            return (piece.targetPosition, true);
        } else {
            return (piece.transform.position, false);
        }
    }
}
