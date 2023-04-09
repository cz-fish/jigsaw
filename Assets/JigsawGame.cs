//using System.Collections;
using System.Collections.Generic;
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

    [SerializeField] private bool showWireframeOutline = true;

    [SerializeField] private int randomSeed = 13;

    private List<GameObject> m_pieces;
    private List<GameObject> m_dropPositions;

    // Start is called before the first frame update
    void Start()
    {
        // Make material with texture
        var texture = Resources.Load<Texture2D>("2");
        var material = new Material(Shader.Find("Standard"));
        material.color = Color.white;
        material.SetTexture("_MainTex", texture);

        PuzzleCutter cutter = new PuzzleCutter();
        var pieces = cutter.cutPieces(texture.width, texture.height, numVerticalPieces, numHorizontalPieces, randomSeed);
        int counter = 0;
        m_pieces = new List<GameObject>();
        m_dropPositions = new List<GameObject>();

        float tileWidth = 1.0f / numHorizontalPieces;
        float tileHeight = 1.0f / numVerticalPieces;

        float horScale = tileWidth * canvasWidth;
        float verScale = tileHeight * canvasHeight;

        foreach (var piece in pieces) {
            // Make each piece
            var mesh = new Mesh();
            int pieceSize = piece.points.Length;
            Vector3[] vertices = new Vector3[pieceSize];
            Vector3[] dropSlotVertices = new Vector3[pieceSize];
            Vector3[] normals = new Vector3[pieceSize];
            Vector2[] uv = new Vector2[pieceSize];

            for (int i = 0; i < pieceSize; ++i) {
                // Keep unit coordinates; will be scaled and translated by the transform.
                vertices[i] = new Vector3(
                    piece.points[i][0],
                    piece.points[i][1],
                    0
                );
                // The line renderer doesn't apply tranformations, so we'll have to scale and translate positions ourselves
                dropSlotVertices[i] = new Vector3(
                    (piece.points[i][0] + piece.column) * tileWidth * canvasWidth + originX,
                    (piece.points[i][1] + piece.row) * tileHeight * canvasHeight + originY,
                    0
                );
                // Texture coordinates must be scaled
                uv[i] = new Vector2(
                    piece.points[i][0] * tileWidth + piece.column * tileWidth,
                    piece.points[i][1] * tileHeight + piece.row * tileHeight
                );
                normals[i] = -Vector3.forward;
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = piece.triangles;

            var pieceObj = new GameObject();
            pieceObj.name = $"piece{counter}";
            m_pieces.Add(pieceObj);

            // Apply scale and put into position.
            pieceObj.transform.localScale = new Vector3(horScale, verScale, 1.0f);
            pieceObj.transform.position = new Vector3(originX + horScale * piece.column, originY + verScale * piece.row, 0f);

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

            // Drop slot
            var dropSlot = new GameObject();
            dropSlot.name = $"dropSlot{counter}";
            m_dropPositions.Add(dropSlot);

            // Line renderer for the outline only
            LineRenderer lineRenderer = dropSlot.AddComponent<LineRenderer>();
            lineRenderer.loop = true;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.widthMultiplier = 0.02f;
            lineRenderer.positionCount = dropSlotVertices.Length;
            lineRenderer.SetPositions(dropSlotVertices);

            dropSlot.SetActive(showWireframeOutline);
            jigsawPiece.dropSlot = dropSlot;

            ++counter;
        }

        ScatterPieces(tileWidth, tileHeight);
    }

    private void ScatterPieces(float tileWidth, float tileHeight) {
        var bounds = OrthographicBounds(Camera.main);
        var horExtra = bounds.extents.x - canvasWidth / 2;
        var verExtra = bounds.extents.y - canvasHeight / 2;
        var pieceCount = m_pieces.Count;
        var positions = new List<Vector2>();

        //Debug.Log($"Scattering Pieces. horExtra {horExtra}, verExtra {verExtra}, pieceCount {pieceCount}");

        // Generate list of positions in the empty space around the main board
        int left = 0;
        int right = 0;
        int above = 0;
        int below = 0;
        if (horExtra > 1 && verExtra <= 1) {
            // space left and right
            left = pieceCount / 2;
            right = pieceCount - left;
        } else if (verExtra > 1 && horExtra <= 1) {
            // space above and below
            above = pieceCount / 2;
            below = pieceCount - above;
        } else {
            // space on all 4 sides, or on neither side
            left = pieceCount / 4;
            right = pieceCount / 4;
            above = pieceCount / 4;
            below = pieceCount - left - right - above;
        }

        //Debug.Log($"left {left}, right {right}, above {above}, below {below}");

        for (var i = 0; i < left; ++i) {
            float jitter1 = Random.Range(-0.3f, 0.3f);
            float jitter2 = Random.Range(-0.3f, 0.3f);
            positions.Add(new Vector2(
                - canvasWidth / 2f - horExtra  / 2f + jitter1,
                - canvasHeight / 2f + (float)i / (left - 1) * canvasHeight + jitter2));
        }

        for (var i = 0; i < right; ++i) {
            float jitter1 = Random.Range(-0.3f, 0.3f);
            float jitter2 = Random.Range(-0.3f, 0.3f);
            positions.Add(new Vector2(
                canvasWidth / 2f + horExtra  / 2f + jitter1,
                - canvasHeight / 2f + (float)i / (right - 1) * canvasHeight + jitter2));
        }

        for (var i = 0; i < above; ++i) {
            float jitter1 = Random.Range(-0.3f, 0.3f);
            float jitter2 = Random.Range(-0.3f, 0.3f);
            positions.Add(new Vector2(
                - canvasWidth / 2f + (float)i / (above - 1) * canvasWidth + jitter1,
                canvasHeight / 2f + verExtra  / 2f + jitter2));
        }

        for (var i = 0; i < below; ++i) {
            float jitter1 = Random.Range(-0.3f, 0.3f);
            float jitter2 = Random.Range(-0.3f, 0.3f);
            positions.Add(new Vector2(
                - canvasWidth / 2f + (float)i / (below - 1) * canvasWidth + jitter1,
                - canvasHeight / 2f - verExtra  / 2f + jitter2));
        }

        // Randomly shuffle the positions
        // https://stackoverflow.com/a/1262619
        var rand = new System.Random(randomSeed);
        for (int n = positions.Count - 1; n > 0; --n) {
            int k = rand.Next(n + 1);
            var temp = positions[k];
            positions[k] = positions[n];
            positions[n] = temp;
        }

        // Assign randomize positions to pieces
        Debug.Assert(positions.Count == m_pieces.Count, "Logic error: not all pieces are shuffled!");

        for (int i = 0; i < m_pieces.Count; ++i) {
            //Debug.Log($"Scattered position {positions[i]}");
            m_pieces[i].transform.position = new Vector3(positions[i].x - tileWidth / 2f * canvasWidth, positions[i].y - tileHeight / 2f * canvasHeight, 0);
        }
    }

    public static Bounds OrthographicBounds(Camera camera)
    {
        float screenAspect = (float)Screen.width / (float)Screen.height;
        float cameraHeight = camera.orthographicSize * 2;
        Bounds bounds = new Bounds(
            camera.transform.position,
            new Vector3(cameraHeight * screenAspect, cameraHeight, 0));
        return bounds;
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
