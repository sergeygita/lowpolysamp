using UnityEngine;
using UnityEngine.UI;

// Только представление. Все кнопки вызывают публичные команды GameManager.
public class GameHUD : MonoBehaviour
{
    GameObject root;
    Text hpText, armorText, moneyText, vehicleText, hintText;
    Image hpBar, armorBar;
    float nextRefresh;
    void Start()
    {
        var ui = MobileInputManager.Instance;
        root = MobileInputManager.Rect("GameStats", ui.SafeRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        hpText = ui.Label(root.transform, "", new Vector2(0, 1), new Vector2(120, -24), new Vector2(210, 28), 19);
        hpBar = Bar(ui, new Vector2(120, -47), new Color(.9f, .22f, .22f));
        armorText = ui.Label(root.transform, "", new Vector2(0, 1), new Vector2(120, -70), new Vector2(210, 28), 19);
        armorBar = Bar(ui, new Vector2(120, -93), new Color(.2f, .65f, 1));
        moneyText = ui.Label(root.transform, "", new Vector2(1, 1), new Vector2(-125, -30), new Vector2(220, 40), 25);
        vehicleText = ui.Label(root.transform, "", new Vector2(1, 1), new Vector2(-145, -100), new Vector2(270, 65), 19);
        hintText = ui.Label(root.transform, "", new Vector2(.5f, 0), new Vector2(0, 48), new Vector2(650, 60), 20);
        ui.Button(root.transform, "Лечение $50", new Vector2(0, 1), new Vector2(115, -140), new Vector2(200, 42), () => Command("/medic"));
        ui.Button(root.transform, "Броня $100", new Vector2(0, 1), new Vector2(115, -190), new Vector2(200, 42), () => Command("/armor"));
        ui.Button(root.transform, "Ремонт $100", new Vector2(0, 1), new Vector2(115, -240), new Vector2(200, 42), () => Command("/repair"));
        ui.Button(root.transform, "Сохранить", new Vector2(1, 1), new Vector2(-115, -165), new Vector2(190, 42), () => Command("/save"));
    }
    Image Bar(MobileInputManager ui, Vector2 position, Color color)
    {
        var rect = MobileInputManager.Rect("Bar", root.transform, new Vector2(0, 1), new Vector2(0, 1), position, new Vector2(200, 10));
        var back = rect.gameObject.AddComponent<Image>(); back.sprite = ui.BoxSprite; back.color = new Color(.08f, .1f, .13f); back.raycastTarget = false;
        var fillRect = MobileInputManager.Rect("Fill", rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var fill = fillRect.gameObject.AddComponent<Image>(); fill.sprite = ui.BoxSprite; fill.color = color; fill.raycastTarget = false;
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0;
        return fill;
    }
    void Command(string command) { var gm = GameManager.Instance; gm.ProcessCommand(gm.LocalPlayer, command); }
    void Update()
    {
        if (!root || Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + .1f;
        var gm = GameManager.Instance; var p = gm ? gm.LocalPlayer : null;
        bool visible = p != null && p.customizationComplete && MobileInputManager.Instance.GameplayEnabled;
        root.SetActive(visible);
        if (!visible) return;
        hpText.text = "Здоровье " + Mathf.CeilToInt(p.health) + " / 100"; hpBar.fillAmount = p.health / 100;
        armorText.text = "Броня " + Mathf.CeilToInt(p.armor) + " / 100"; armorBar.fillAmount = p.armor / 100;
        moneyText.text = "$ " + p.money;
        var vehicle = p.currentVehicle;
        vehicleText.text = vehicle ? Mathf.RoundToInt(vehicle.SpeedKmh) + " км/ч\nМашина: " + Mathf.CeilToInt(vehicle.Health) + "%" : "ПЕШКОМ";
        if (!p.isSpawned) hintText.text = "Возрождение через " + Mathf.Max(0, Mathf.CeilToInt(p.respawnAt - Time.time)) + "…";
        else if (vehicle) hintText.text = "E / Выйти — после остановки";
        else if (gm.NearestVehicle(p, 3.2f)) hintText.text = "E / Сесть — за руль";
        else hintText.text = "Красный куб: здоровье · Синий: броня · Жёлтый: $";
    }
    void OnDestroy() { if (root) Destroy(root); }
}
