using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterBuilder : MonoBehaviour
{
    public Material Material { get; private set; }
    readonly List<Mesh> ownedMeshes = new List<Mesh>();
    MaterialPropertyBlock block;
    Mesh cube, cylinder;
    static readonly int ColorId = Shader.PropertyToID("_Color");
    void Awake()
    {
        // Unity создаёт native-ресурс: только в lifecycle, не в инициализаторе поля.
        block = new MaterialPropertyBlock();
        Material = Resources.Load<Material>("LowPolyDefault");
        if (!Material) throw new InvalidOperationException("Создайте Assets/Resources/LowPolyDefault.mat по README.");
        cube = PrimitiveMesh(PrimitiveType.Cube);
        cylinder = PrimitiveMesh(PrimitiveType.Cylinder);
    }
    Mesh PrimitiveMesh(PrimitiveType type)
    {
        var temp = GameObject.CreatePrimitive(type);
        temp.SetActive(false);
        var mesh = Instantiate(temp.GetComponent<MeshFilter>().sharedMesh);
        mesh.name = "LowPoly_" + type;
        var colors = new Color[mesh.vertexCount];
        for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
        mesh.colors = colors;
        ownedMeshes.Add(mesh);
        Destroy(temp);
        return mesh;
    }
    public void Paint(GameObject go, Color color)
    {
        // Один общий материал; индивидуальный цвет без Renderer.material и его копий.
        block.Clear(); block.SetColor(ColorId, color);
        go.GetComponent<MeshRenderer>().SetPropertyBlock(block);
    }
    public GameObject Part(string name, Transform parent, Vector3 position, Vector3 scale, Color color, bool round = false)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.GetComponent<MeshFilter>().sharedMesh = round ? cylinder : cube;
        go.GetComponent<MeshRenderer>().sharedMaterial = Material;
        Paint(go, color);
        return go;
    }
    Transform Pivot(string name, Transform parent, Vector3 position)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false); t.localPosition = position;
        return t;
    }
    public GameObject BuildCharacter(CharacterAppearance appearance)
    {
        var root = new GameObject("Character");
        var a = appearance ?? GetDefaultAppearance();
        Part("Torso", root.transform, new Vector3(0, 1.2f, 0), new Vector3(.5f, .6f, .27f), a.skinColor);
        Part("Shirt", root.transform, new Vector3(0, 1.2f, 0), new Vector3(.54f, .62f, .3f), a.shirtColor);
        Part("Neck", root.transform, new Vector3(0, 1.55f, 0), new Vector3(.15f, .06f, .15f), a.skinColor, true);
        Part("Head", root.transform, new Vector3(0, 1.77f, 0), new Vector3(.36f, .33f, .32f), a.skinColor);
        Part("Hair", root.transform, new Vector3(0, 1.94f, -.025f), new Vector3(.37f, .09f, .29f), a.hairColor);
        Part("HatTop", root.transform, new Vector3(0, 2.02f, 0), new Vector3(.41f, .18f, .37f), a.hatColor);
        Part("HatBrim", root.transform, new Vector3(0, 1.96f, .12f), new Vector3(.44f, .045f, .5f), a.hatColor);
        var anim = root.AddComponent<ProceduralAnimation>();
        for (int side = 0; side < 2; side++)
        {
            string s = side == 0 ? "Left" : "Right";
            float sign = side == 0 ? -1 : 1;
            Transform arm = Pivot(s + "ArmPivot", root.transform, new Vector3(sign * .36f, 1.48f, 0));
            Part("UpperArm", arm, new Vector3(0, -.18f, 0), new Vector3(.18f, .36f, .21f), a.skinColor);
            Part("Sleeve", arm, new Vector3(0, -.13f, 0), new Vector3(.2f, .26f, .23f), a.shirtColor);
            Transform elbow = Pivot("Elbow", arm, new Vector3(0, -.36f, 0));
            Part("LowerArm", elbow, new Vector3(0, -.16f, 0), new Vector3(.16f, .32f, .19f), a.skinColor);
            Part("Cuff", elbow, new Vector3(0, -.12f, 0), new Vector3(.18f, .24f, .21f), a.shirtColor);
            Transform leg = Pivot(s + "LegPivot", root.transform, new Vector3(sign * .145f, .91f, 0));
            Part("UpperLeg", leg, new Vector3(0, -.22f, 0), new Vector3(.22f, .44f, .25f), a.skinColor);
            Part("Pants", leg, new Vector3(0, -.22f, 0), new Vector3(.24f, .46f, .27f), a.pantsColor);
            Transform knee = Pivot("Knee", leg, new Vector3(0, -.44f, 0));
            Part("LowerLeg", knee, new Vector3(0, -.2f, 0), new Vector3(.2f, .4f, .22f), a.skinColor);
            Part("Shoe", knee, new Vector3(0, -.31f, .055f), new Vector3(.24f, .24f, .36f), a.shoesColor);
            if (side == 0) { anim.leftArm = arm; anim.leftLeg = leg; }
            else { anim.rightArm = arm; anim.rightLeg = leg; }
        }
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 2;
        ApplyAppearance(root, a);
        return root;
    }
    public void ApplyAppearance(GameObject character, CharacterAppearance a)
    {
        if (!character || a == null) return;
        foreach (var r in character.GetComponentsInChildren<MeshRenderer>(true))
        {
            Color c = a.skinColor;
            switch (r.name)
            {
                case "Hair": c = a.hairColor; break;
                case "Shirt": case "Sleeve": case "Cuff": c = a.shirtColor; break;
                case "Pants": c = a.pantsColor; break;
                case "Shoe": c = a.shoesColor; break;
                case "HatTop": case "HatBrim": c = a.hatColor; break;
            }
            Paint(r.gameObject, c);
            if (r.name == "Cuff") r.gameObject.SetActive(a.shirtType == 1);
            if (r.name == "Sleeve")
            {
                r.transform.localScale = new Vector3(.2f, a.shirtType == 1 ? .38f : .26f, .23f);
                r.transform.localPosition = new Vector3(0, a.shirtType == 1 ? -.18f : -.13f, 0);
            }
            if (r.name == "HatTop" || r.name == "HatBrim") r.gameObject.SetActive(a.hatType != 0);
            if (r.name == "HatBrim")
            {
                r.transform.localScale = a.hatType == 2 ? new Vector3(.62f, .045f, .56f) : new Vector3(.44f, .045f, .5f);
                r.transform.localPosition = new Vector3(0, 1.96f, a.hatType == 2 ? 0 : .12f);
            }
        }
    }
    public static CharacterAppearance GetDefaultAppearance() { return new CharacterAppearance(); }
    public static CharacterAppearance GetRandomAppearance()
    {
        var a = GetDefaultAppearance();
        a.skinColor = Color.Lerp(new Color(.3f, .16f, .09f), new Color(1, .8f, .62f), UnityEngine.Random.value);
        a.hairColor = Color.Lerp(Color.black, new Color(.8f, .55f, .15f), UnityEngine.Random.value);
        a.shirtColor = UnityEngine.Random.ColorHSV(0, 1, .5f, .9f, .6f, 1);
        a.pantsColor = UnityEngine.Random.ColorHSV(0, 1, .3f, .7f, .2f, .55f);
        a.shoesColor = UnityEngine.Random.ColorHSV(0, 1, 0, .5f, .1f, .4f);
        a.hatColor = UnityEngine.Random.ColorHSV(0, 1, .5f, .9f, .6f, 1);
        a.hatType = UnityEngine.Random.Range(0, 3); a.shirtType = UnityEngine.Random.Range(0, 2);
        return a;
    }
    void OnDestroy() { foreach (var mesh in ownedMeshes) if (mesh) Destroy(mesh); }
}
