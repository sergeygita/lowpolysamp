using UnityEngine;
using UnityEngine.UI;

public class CharacterCustomizationUI : MonoBehaviour
{
    PlayerData player;
    GameObject panel, preview, previewCameraObject;
    RenderTexture renderTexture;
    string selected = "skin";
    Text selectedLabel;
    readonly Color[] palette = {
        new Color(1, .8f, .62f), new Color(.52f, .3f, .17f), new Color(.13f, .1f, .12f),
        new Color(.95f, .95f, .88f), new Color(.9f, .2f, .15f), new Color(.1f, .65f, .95f),
        new Color(.2f, .75f, .3f), new Color(1, .7f, .12f)
    };
    public void Open(PlayerData data)
    {
        Close(); player = data; selected = "skin";
        var ui = MobileInputManager.Instance;
        ui.SetGameplay(false);
        panel = MobileInputManager.Rect("Customization", ui.SafeRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        var background = panel.AddComponent<Image>();
        background.sprite = ui.BoxSprite; background.color = new Color(.045f, .075f, .11f, 1);
        ui.Label(panel.transform, "СОЗДАЙТЕ ПЕРСОНАЖА", new Vector2(.5f, 1), new Vector2(0, -65), new Vector2(800, 60), 32);
        selectedLabel = ui.Label(panel.transform, "Цвет: Голова", new Vector2(.5f, 0), new Vector2(0, 175), new Vector2(600, 40), 22);
        string[] titles = {"Голова", "Волосы", "Торс", "Ноги", "Обувь", "Шляпа"};
        string[] keys = {"skin", "hair", "shirt", "pants", "shoes", "hat"};
        for (int i = 0; i < titles.Length; i++)
        {
            int index = i;
            ui.Button(panel.transform, titles[i], new Vector2(.5f, .5f), new Vector2(-400, 155 - i * 58), new Vector2(175, 48), () => {
                selected = keys[index]; selectedLabel.text = "Цвет: " + titles[index];
            });
        }
        for (int i = 0; i < palette.Length; i++)
        {
            Color color = palette[i];
            var b = ui.Button(panel.transform, "", new Vector2(.5f, 0), new Vector2((i - 3.5f) * 65, 120), new Vector2(52, 52), () => SetColor(color));
            b.GetComponent<Image>().color = color;
        }
        ui.Button(panel.transform, "Тип шляпы", new Vector2(.5f, .5f), new Vector2(400, 105), new Vector2(190, 52), () => {
            var a = player.appearance.Copy(); a.hatType = (a.hatType + 1) % 3; Apply(a);
        });
        ui.Button(panel.transform, "Тип одежды", new Vector2(.5f, .5f), new Vector2(400, 40), new Vector2(190, 52), () => {
            var a = player.appearance.Copy(); a.shirtType = 1 - a.shirtType; Apply(a);
        });
        ui.Button(panel.transform, "Рандом", new Vector2(.5f, .5f), new Vector2(400, -40), new Vector2(190, 52), () => Apply(CharacterBuilder.GetRandomAppearance()));
        ui.Button(panel.transform, "Готово", new Vector2(.5f, 0), new Vector2(0, 48), new Vector2(220, 52), () => GameManager.Instance.CompleteCustomization(player));
        preview = GameManager.Instance.Builder.BuildCharacter(player.appearance);
        preview.transform.position = new Vector3(0, 1000, 0);
        foreach (var t in preview.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
        previewCameraObject = new GameObject("PreviewCamera", typeof(Camera));
        var camera = previewCameraObject.GetComponent<Camera>();
        camera.cullingMask = 1 << 31;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.08f, .14f, .2f);
        camera.allowHDR = false; camera.allowMSAA = false;
        camera.orthographic = true; camera.orthographicSize = 1.2f;
        camera.nearClipPlane = .1f; camera.farClipPlane = 10;
        camera.transform.position = new Vector3(0, 1001.1f, 4);
        camera.transform.LookAt(new Vector3(0, 1001.1f, 0));
        renderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
        renderTexture.name = "CharacterPreview"; renderTexture.Create(); camera.targetTexture = renderTexture;
        var rect = MobileInputManager.Rect("Preview", panel.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 45), new Vector2(340, 340));
        var raw = rect.gameObject.AddComponent<RawImage>(); raw.texture = renderTexture; raw.raycastTarget = false;
    }
    void SetColor(Color color)
    {
        var a = player.appearance.Copy();
        switch (selected)
        {
            case "skin": a.skinColor = color; break;
            case "hair": a.hairColor = color; break;
            case "shirt": a.shirtColor = color; break;
            case "pants": a.pantsColor = color; break;
            case "shoes": a.shoesColor = color; break;
            case "hat": a.hatColor = color; break;
        }
        Apply(a);
    }
    void Apply(CharacterAppearance a)
    {
        // UI не обходит PAWN callback: сначала GameManager, затем копия для превью.
        GameManager.Instance.SetAppearance(player, a);
        GameManager.Instance.Builder.ApplyAppearance(preview, player.appearance);
    }
    public void Close()
    {
        if (panel) { panel.SetActive(false); Destroy(panel); }
        if (preview) Destroy(preview);
        if (previewCameraObject)
        {
            previewCameraObject.GetComponent<Camera>().targetTexture = null;
            previewCameraObject.SetActive(false); Destroy(previewCameraObject);
        }
        if (renderTexture) { renderTexture.Release(); Destroy(renderTexture); }
        panel = preview = previewCameraObject = null; renderTexture = null; player = null;
    }
    void OnDestroy() { Close(); }
}
