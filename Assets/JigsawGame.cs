//using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Assets;

public class JigsawGame : MonoBehaviour
{
    [SerializeField] private int numHorizontalPieces = 5;
    [SerializeField] private int numVerticalPieces = 5;
    [SerializeField] private float snapTolerance = 0.3f;
    [SerializeField] private float longerSideUnits = 6f;
    [SerializeField] private float borderThicknessPct = 0.05f;

    [SerializeField] private bool showWireframeOutline = true;
    [SerializeField] private bool showBorder = true;

    [SerializeField] private int randomSeed = 13;

    private List<GameObject> m_pieces;
    private List<GameObject> m_dropPositions;
    private GameObject m_border;

    private int m_correctPieces = 0;
    private GameObject m_timerText;
    private System.DateTime m_startTime;
    private System.TimeSpan? m_solveTime = null;

    // Start is called before the first frame update
    void Start()
    {
        // -- picture --
        (var material, var textureWidth, var textureHeight) = MakeMaterial();
        var dropSlotMaterial = new Material(Shader.Find("Sprites/Default"));

        // -- sizes --
        // Derive board size so that the longer side is `longerSideUnits` units
        float canvasWidth = 1f;
        float canvasHeight = 1f;
        if (textureWidth > textureHeight) {
            canvasWidth = longerSideUnits;
            canvasHeight = longerSideUnits * textureHeight / textureWidth;
        } else {
            canvasHeight = longerSideUnits;
            canvasWidth = longerSideUnits * textureWidth / textureHeight;
        }
        float originX = - canvasWidth / 2f;
        float originY = - canvasHeight / 2f;

        float effectiveBorder = showBorder ? borderThicknessPct : 0f;
        float tileWidth = (1.0f - 2 * effectiveBorder) / numHorizontalPieces;
        float tileHeight = (1.0f - 2 * effectiveBorder) / numVerticalPieces;

        float horScale = tileWidth * canvasWidth;
        float verScale = tileHeight * canvasHeight;

        // -- make objects --
        PuzzleCutter cutter = new PuzzleCutter();
        var pieces = cutter.cutPieces(numVerticalPieces, numHorizontalPieces, randomSeed);
        int counter = 0;
        m_pieces = new List<GameObject>();
        m_dropPositions = new List<GameObject>();

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
                //   coordinates from 0 to 1
                vertices[i] = new Vector3(
                    piece.points[i][0],
                    piece.points[i][1],
                    0
                );
                // The line renderer doesn't apply tranformations, so we'll have to scale and translate positions ourselves
                //   coordinates from -canvasWidth, -canvasHeight to canvasWidth, canvasHeight (minus border)
                dropSlotVertices[i] = new Vector3(
                    (effectiveBorder + (piece.points[i][0] + piece.column) * tileWidth) * canvasWidth + originX,
                    (effectiveBorder + (piece.points[i][1] + piece.row) * tileHeight) * canvasHeight + originY,
                    0
                );
                // Texture coordinates
                //   coordinates from 0 to 1
                uv[i] = new Vector2(
                    effectiveBorder + piece.points[i][0] * tileWidth + piece.column * tileWidth,
                    effectiveBorder + piece.points[i][1] * tileHeight + piece.row * tileHeight
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

            // Apply scale
            pieceObj.transform.localScale = new Vector3(horScale, verScale, 1.0f);
            // put into position
            pieceObj.transform.position = new Vector3(
                originX + canvasWidth * effectiveBorder + horScale * piece.column,
                originY + canvasHeight * effectiveBorder + verScale * piece.row,
                0f
            );

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
            var dropSlot = MakeDropSlot(counter, dropSlotVertices, dropSlotMaterial);
            dropSlot.SetActive(showWireframeOutline);
            jigsawPiece.dropSlot = dropSlot;

            ++counter;
        }

        if (showBorder) {
            MakeBorder(effectiveBorder, material, canvasWidth, canvasHeight);
        }

        ScatterPieces(tileWidth, tileHeight, canvasWidth, canvasHeight);
        SetupTimer();
    }

    private (Material, int, int) MakeMaterial() {
        // Make material with texture
        var texture = Resources.Load<Texture2D>("2");
        var material = new Material(Shader.Find("Standard"));
        material.color = Color.white;
        material.SetTexture("_MainTex", texture);
        return (material, texture.width, texture.height);
    }

    private GameObject MakeDropSlot(int counter, Vector3[] dropSlotVertices, Material dropSlotMaterial) {
        var dropSlot = new GameObject();
        dropSlot.name = $"dropSlot{counter}";
        m_dropPositions.Add(dropSlot);

        // Line renderer for the outline only
        LineRenderer lineRenderer = dropSlot.AddComponent<LineRenderer>();
        lineRenderer.loop = true;
        lineRenderer.material = dropSlotMaterial;
        lineRenderer.widthMultiplier = 0.02f;
        lineRenderer.positionCount = dropSlotVertices.Length;
        lineRenderer.SetPositions(dropSlotVertices);

        return dropSlot;
    }

    private void MakeBorder(float thickness, Material material, float canvasWidth, float canvasHeight) {
        m_border = new GameObject();
        m_border.name = "border";

        /*
        0 --- 1 -------- 2 --- 3
        | \   |   \      | \   |
        |   \ |       \  |   \ |
        4 --- 5 -------- 6 --- 7
        |\    |          |\    |
        | \   |          | \   |
        |  \  |          |  \  |
        |   \ |          |   \ |
        8 --- 9 --------10 ---11
        | \   |   \      | \   |
        |   \ |       \  |   \ |
       12 ---13 --------14 ---15
        */

        // Vertices should be in clockwise order for triangles to be front facing
        int[] triangles = {
            0, 1, 5,    0, 5, 4,
            1, 2, 6,    1, 6, 5,
            2, 3, 7,    2, 7, 6,
            4, 5, 9,    4, 9, 8,
            6, 7, 11,   6, 11, 10,
            8, 9, 13,   8, 13, 12,
            9, 10, 14,  9, 14, 13,
            10, 11, 15, 10, 15, 14,
        };

        const int borderPoints = 16;
        Vector3[] vertices = new Vector3[borderPoints];
        Vector3[] normals = new Vector3[borderPoints];
        Vector2[] uv = new Vector2[borderPoints];
        float[] stops = { 0f, thickness, 1f - thickness, 1f };
        for (var row = 0; row < 4; ++row) {
            float texCoordY = 1f - stops[row];
            float screenCoordY = (texCoordY - 0.5f) * canvasHeight;
            for (var col = 0; col < 4; ++col) {
                float texCoordX = stops[col];
                float screenCoordX = (texCoordX - 0.5f) * canvasWidth;
                int arrayIndex = row * 4 + col;
                vertices[arrayIndex] = new Vector3(screenCoordX, screenCoordY, 0.125f);
                normals[arrayIndex] = -Vector3.forward;
                uv[arrayIndex] = new Vector2(texCoordX, texCoordY);
            }
        }

        var mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.triangles = triangles;

        var renderer = m_border.AddComponent<MeshRenderer>();
        renderer.material = material;
        var filter = m_border.AddComponent<MeshFilter>();
        filter.mesh = mesh;
    }

    private void ScatterPieces(float tileWidth, float tileHeight, float canvasWidth, float canvasHeight) {
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
            right = (pieceCount - left) / 3;
            above = (pieceCount - left - right) / 2;
            below = pieceCount - left - right - above;
        }

        //Debug.Log($"left {left}, right {right}, above {above}, below {below}");
        float leftSpacing = (left > 1) ? canvasHeight / (left - 1) : 0;
        float rightSpacing = (right > 1) ? canvasHeight / (right - 1) : 0;
        float aboveSpacing = (above > 1) ? canvasWidth / (above - 1) : 0;
        float belowSpacing = (below > 1) ? canvasWidth / (below - 1) : 0;

        for (var i = 0; i < left; ++i) {
            float jitter1 = Random.Range(-0.3f, 0.3f);
            float jitter2 = Random.Range(-0.3f, 0.3f);
            positions.Add(new Vector2(
                - canvasWidth / 2f - horExtra / 2f + jitter1,
                (i - left/2) * leftSpacing + jitter2));
        }

        for (var i = 0; i < right; ++i) {
            float jitter1 = Random.Range(-0.3f, 0.3f);
            float jitter2 = Random.Range(-0.3f, 0.3f);
            positions.Add(new Vector2(
                canvasWidth / 2f + horExtra / 2f + jitter1,
                (i - right/2) * rightSpacing + jitter2));
        }

        for (var i = 0; i < above; ++i) {
            float jitter1 = Random.Range(-0.3f, 0.3f);
            float jitter2 = Random.Range(-0.3f, 0.3f);
            positions.Add(new Vector2(
                (i - above/2) * aboveSpacing + jitter1,
                canvasHeight / 2f + verExtra / 2f + jitter2));
        }

        for (var i = 0; i < below; ++i) {
            float jitter1 = Random.Range(-0.3f, 0.3f);
            float jitter2 = Random.Range(-0.3f, 0.3f);
            positions.Add(new Vector2(
                (i - below/2) * belowSpacing + jitter1,
                - canvasHeight / 2f - verExtra / 2f + jitter2));
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

    private void SetupTimer()
    {
        m_timerText = GameObject.Find("Timer");
        m_startTime = System.DateTime.Now;
    }

    public void Update()
    {
        if (m_solveTime != null) {
            return;
        }
        var text = m_timerText.GetComponent<TMPro.TMP_Text>();
        var now = System.DateTime.Now;
        if (text) {
            var timeDiff = now - m_startTime;
            if (timeDiff.TotalSeconds >= 60) {
                text.text = $"Time: {(int)timeDiff.TotalSeconds / 60}m {(int)timeDiff.TotalSeconds % 60}s";
            } else {
                text.text = $"Time: {(int)timeDiff.TotalSeconds % 60}s";
            }
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
            m_correctPieces++;
            return (piece.targetPosition, true);
        } else {
            return (piece.transform.position, false);
        }
    }

    public void CheckWin()
    {
        if (m_correctPieces != m_pieces.Count) {
            return;
        }
        // Win
        Debug.Log("Completed!");
        var win = GameObject.Find("Win");
        if (win) {
            win.WithChild("Stars").SetActive(true);
        }
        m_solveTime = System.DateTime.Now - m_startTime;
    }
}
