using System.Collections.Generic;
using UnityEngine;

public class TestSceneGenerator : MonoBehaviour
{
    CharacterBuilder builder;
    readonly List<Mesh> meshes = new List<Mesh>();
    void Awake()
    {
        builder = GameManager.Instance.Builder;
        var savedState = Random.state;
        Random.InitState(12345);
        try { Generate(); }
        finally { Random.state = savedState; }
    }
    void Generate()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground_100x100"; ground.transform.SetParent(transform, false);
        ground.transform.localScale = new Vector3(10, 1, 10);
        Mesh mesh = Instantiate(ground.GetComponent<MeshFilter>().sharedMesh);
        var colors = new Color[mesh.vertexCount]; var vertices = mesh.vertices;
        for (int i = 0; i < colors.Length; i++)
            colors[i] = Color.Lerp(new Color(.12f, .32f, .12f), new Color(.45f, .7f, .24f), Mathf.InverseLerp(-5, 5, vertices[i].z));
        mesh.colors = colors; mesh.name = "GradientGround"; meshes.Add(mesh);
        ground.GetComponent<MeshFilter>().sharedMesh = mesh;
        ground.GetComponent<MeshCollider>().sharedMesh = mesh;
        ground.GetComponent<MeshRenderer>().sharedMaterial = builder.Material;
        Box("Road", new Vector3(0, .02f, 10), new Vector3(7, .04f, 75), new Color(.22f, .24f, .28f));
        for (int i = 0; i < 18; i++)
        {
            float side = i % 2 == 0 ? -1 : 1;
            Vector3 pos = new Vector3(side * Random.Range(27f, 45f), 0, Random.Range(-43f, 43f));
            var tree = new GameObject("Tree_" + i); tree.transform.SetParent(transform, false);
            tree.transform.localPosition = pos; tree.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            float scale = Random.Range(.8f, 1.4f); tree.transform.localScale = Vector3.one * scale;
            var trunk = builder.Part("Trunk", tree.transform, new Vector3(0, 1, 0), new Vector3(.45f, 1, .45f), new Color(.4f, .23f, .12f), true);
            trunk.AddComponent<BoxCollider>();
            Mesh crownMesh = Cone(7); meshes.Add(crownMesh);
            Custom("Crown", tree.transform, crownMesh, new Vector3(0, 1.3f, 0), new Vector3(1.6f, 3.3f, 1.6f), new Color(.15f, Random.Range(.4f, .8f), .16f), false);
        }
        Color[] buildingColors = { new Color(1, .75f, .2f), new Color(.2f, .7f, .9f), new Color(.95f, .4f, .6f) };
        for (int i = 0; i < 7; i++)
        {
            float h = Random.Range(3.5f, 7f), w = Random.Range(5f, 8f), depth = Random.Range(5f, 8f);
            Vector3 position = new Vector3(i % 2 == 0 ? -15 : 15, h * .5f, -30 + (i / 2) * 18);
            Box("Building_" + i, position, new Vector3(w, h, depth), buildingColors[i % 3]);
            if (i % 2 == 0)
            {
                Mesh roof = Cone(4, Mathf.PI * .25f); meshes.Add(roof);
                Custom("Roof", transform, roof, position + Vector3.up * (h * .5f), new Vector3(w * .8f, 2, depth * .8f), new Color(.55f, .19f, .16f), true);
            }
        }
        GameManager.Instance.SpawnPoint = new Vector3(0, .15f, -8);
        var marker = builder.Part("SpawnMarker", transform, new Vector3(0, .055f, -8), new Vector3(1.6f, .015f, 1.6f), new Color(1, .35f, .12f), true);
        // Маркер без коллайдера: не подбрасывает CharacterController.
        foreach (var t in GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
    }
    GameObject Box(string name, Vector3 pos, Vector3 size, Color color)
    {
        var go = builder.Part(name, transform, pos, size, color); go.AddComponent<BoxCollider>(); return go;
    }
    void Custom(string name, Transform parent, Mesh mesh, Vector3 pos, Vector3 size, Color color, bool collider)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localScale = size;
        go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = builder.Material;
        builder.Paint(go, color);
        if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
    }
    Mesh Cone(int sides, float offset = 0)
    {
        // Раздельные вершины граней; цвет грани подчёркивает LowPoly даже в Unlit.
        var vertices = new List<Vector3>(); var triangles = new List<int>(); var colors = new List<Color>();
        for (int i = 0; i < sides; i++)
        {
            float a = offset + i * Mathf.PI * 2 / sides, b = offset + (i + 1) * Mathf.PI * 2 / sides;
            Vector3 p = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)), q = new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b));
            vertices.Add(p); vertices.Add(Vector3.up); vertices.Add(q);
            vertices.Add(Vector3.zero); vertices.Add(p); vertices.Add(q);
            float shade = .72f + .28f * (i % 3) / 2f;
            for (int k = 0; k < 6; k++) { triangles.Add(i * 6 + k); colors.Add(new Color(shade, shade, shade)); }
        }
        var mesh = new Mesh { name = "LowPolyCone_" + sides };
        mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.SetColors(colors);
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }
    void OnDestroy() { foreach (var mesh in meshes) if (mesh) Destroy(mesh); }
}
