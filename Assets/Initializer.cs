using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Initializer : MonoBehaviour
{
    //public var material: Material;

    // Start is called before the first frame update
    void Start()
    {
        var texture = Resources.Load<Texture2D>("2");
        var material = new Material(Shader.Find("Standard"));
        material.color = Color.white;
        material.SetTexture("_MainTex", texture);

        var mesh = new Mesh();
        const float width = 6;
        const float height = 6;
        Vector3 origin = new Vector3(-3, -3, 0);
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(0, 0, 0) + origin,
            new Vector3(width, 0, 0) + origin,
            new Vector3(0, height, 0) + origin,
            new Vector3(width, height, 0) + origin
        };
        mesh.vertices = vertices;

        int[] tris = new int[6]
        {
            // lower left triangle
            0, 2, 1,
            // upper right triangle
            2, 3, 1
        };
        mesh.triangles = tris;

        Vector3[] normals = new Vector3[4]
        {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward
        };
        mesh.normals = normals;

        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        mesh.uv = uv;

        var piece = new GameObject();
        piece.name = "piece";
        MeshRenderer meshRenderer = piece.AddComponent<MeshRenderer>();
        meshRenderer.material = material;
        MeshFilter meshFilter = piece.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
