using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-100)]
public class MobileInputManager : MonoBehaviour
{
    public static MobileInputManager Instance { get; private set; }
    public RectTransform SafeRoot { get; private set; }
    public Vector2 Move { get { return CanInput ? joystick : Vector2.zero; } }
    public Vector2 Look { get; private set; }
    public bool Sprint { get { return CanInput && (sprintPointer != int.MinValue || Input.GetKey(KeyCode.LeftShift)); } }
    public bool GameplayEnabled { get; private set; }
    public bool Driving { get; private set; }
    public float DriveThrottle { get { return !CanInput ? 0 : Mathf.Clamp(Move.y + (sprintPointer != int.MinValue ? 1 : 0) - (reversePointer != int.MinValue ? 1 : 0), -1, 1); } }
    public bool Handbrake { get { return CanInput && (handbrakePointer != int.MinValue || Input.GetKey(KeyCode.Space)); } }
    int reversePointer = int.MinValue, handbrakePointer = int.MinValue;
    Text jumpLabel, sprintLabel, actionLabel;
    GameObject handbrakeButton;
    public bool CanInput { get { return GameplayEnabled && (!commandField || !commandField.isFocused); } }
    public Font Font { get; private set; }
    public Sprite RoundSprite { get; private set; }
    public Sprite BoxSprite { get; private set; }
    readonly List<Sprite> sprites = new List<Sprite>();
    readonly List<RaycastResult> rayResults = new List<RaycastResult>();
    RectTransform joystickRect, knob;
    GameObject hud, canvasObject, ownedEventSystem;
    InputField commandField;
    Text status;
    Vector2 joystick;
    int joystickPointer = int.MinValue, sprintPointer = int.MinValue, lookFinger = -1;
    bool jump, action;
    Rect safeArea;
    int width, height;
    Vector2 mouseLast;
    bool mouseLook;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(this); return; }
        Instance = this;
        Input.multiTouchEnabled = true;
        Input.simulateMouseWithTouches = false;
        Font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        RoundSprite = Own(SpriteGenerator.GenerateCircle(96, Color.white, new Color(.65f, .8f, 1), 3));
        BoxSprite = Own(SpriteGenerator.GenerateRoundedRect(128, 64, 8, Color.white, new Color(.5f, .7f, .85f)));
        canvasObject = new GameObject("MobileCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.GetComponent<Canvas>().sortingOrder = 10;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
        SafeRoot = Rect("SafeArea", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        if (!FindObjectOfType<EventSystem>())
        {
            ownedEventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            ownedEventSystem.transform.SetParent(transform);
        }
        hud = Rect("HUD", SafeRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        joystickRect = Rect("Joystick", hud.transform, Vector2.zero, Vector2.zero, new Vector2(145, 145), new Vector2(190, 190));
        var bg = joystickRect.gameObject.AddComponent<Image>(); bg.sprite = RoundSprite; bg.color = new Color(.15f, .25f, .35f, .55f);
        knob = Rect("Knob", joystickRect, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(78, 78));
        var ki = knob.gameObject.AddComponent<Image>(); ki.sprite = RoundSprite; ki.color = new Color(.4f, .8f, 1, .85f); ki.raycastTarget = false;
        On(joystickRect.gameObject, EventTriggerType.PointerDown, e => {
            if (!CanInput || joystickPointer != int.MinValue) return;
            joystickPointer = e.pointerId; DragJoystick(e);
        });
        On(joystickRect.gameObject, EventTriggerType.Drag, DragJoystick);
        On(joystickRect.gameObject, EventTriggerType.PointerUp, e => {
            if (joystickPointer == e.pointerId) { joystickPointer = int.MinValue; joystick = Vector2.zero; knob.anchoredPosition = Vector2.zero; }
        });
        Button jumpButton = Button(hud.transform, "Jump", new Vector2(1, 0), new Vector2(-85, 140), new Vector2(110, 110), null, true);
        jumpLabel = jumpButton.GetComponentInChildren<Text>();
        On(jumpButton.gameObject, EventTriggerType.PointerDown, e => {
            if (!CanInput) return;
            if (Driving) { if (reversePointer == int.MinValue) reversePointer = e.pointerId; }
            else jump = true;
        });
        On(jumpButton.gameObject, EventTriggerType.PointerUp, e => { if (reversePointer == e.pointerId) reversePointer = int.MinValue; });
        Button sprintButton = Button(hud.transform, "Sprint", new Vector2(1, 0), new Vector2(-210, 95), new Vector2(105, 105), null, true);
        sprintLabel = sprintButton.GetComponentInChildren<Text>();
        On(sprintButton.gameObject, EventTriggerType.PointerDown, e => { if (CanInput && sprintPointer == int.MinValue) sprintPointer = e.pointerId; });
        On(sprintButton.gameObject, EventTriggerType.PointerUp, e => { if (sprintPointer == e.pointerId) sprintPointer = int.MinValue; });
        var actionButton = Button(hud.transform, "Сесть", new Vector2(1, 0), new Vector2(-85, 270), new Vector2(110, 75), () => { if (CanInput) action = true; });
        actionLabel = actionButton.GetComponentInChildren<Text>();
        handbrakeButton = Button(hud.transform, "Ручник", new Vector2(1, 0), new Vector2(-220, 235), new Vector2(115, 65), null).gameObject;
        On(handbrakeButton, EventTriggerType.PointerDown, e => { if (CanInput && handbrakePointer == int.MinValue) handbrakePointer = e.pointerId; });
        On(handbrakeButton, EventTriggerType.PointerUp, e => { if (handbrakePointer == e.pointerId) handbrakePointer = int.MinValue; });
        SetDriving(false);
        CreateCommandBar();
        UpdateSafeArea();
        SetGameplay(false);
    }
    public Sprite Own(Sprite sprite) { sprites.Add(sprite); return sprite; }
    public static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
    {
        var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(parent, false); r.anchorMin = min; r.anchorMax = max;
        r.anchoredPosition = pos; r.sizeDelta = size;
        return r;
    }
    public Text Label(Transform parent, string text, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize = 22)
    {
        var r = Rect("Label", parent, anchor, anchor, pos, size);
        var label = r.gameObject.AddComponent<Text>(); label.font = Font; label.text = text;
        label.color = Color.white; label.fontSize = fontSize; label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        return label;
    }
    public Button Button(Transform parent, string title, Vector2 anchor, Vector2 pos, Vector2 size, UnityAction click, bool round = false)
    {
        var r = Rect(title, parent, anchor, anchor, pos, size);
        var img = r.gameObject.AddComponent<Image>(); img.sprite = round ? RoundSprite : BoxSprite;
        img.color = new Color(.12f, .26f, .36f, .94f);
        var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = img;
        var navigation = b.navigation; navigation.mode = Navigation.Mode.None; b.navigation = navigation;
        if (click != null) b.onClick.AddListener(click);
        Label(r, title, new Vector2(.5f, .5f), Vector2.zero, size - new Vector2(8, 8), 20);
        return b;
    }
    static void On(GameObject go, EventTriggerType type, System.Action<PointerEventData> handler)
    {
        var trigger = go.GetComponent<EventTrigger>(); if (!trigger) trigger = go.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(data => handler((PointerEventData)data));
        trigger.triggers.Add(entry);
    }
    void DragJoystick(PointerEventData e)
    {
        if (e.pointerId != joystickPointer || !CanInput) return;
        Vector2 p;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickRect, e.position, e.pressEventCamera, out p);
        joystick = Vector2.ClampMagnitude(p / 65f, 1); knob.anchoredPosition = joystick * 65f;
    }
    void CreateCommandBar()
    {
        var r = Rect("Command", hud.transform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-55, -40), new Vector2(500, 48));
        var image = r.gameObject.AddComponent<Image>(); image.sprite = BoxSprite; image.color = new Color(.08f, .13f, .18f, .9f);
        commandField = r.gameObject.AddComponent<InputField>(); commandField.targetGraphic = image; commandField.characterLimit = 128;
        Text value = Label(r, "", new Vector2(.5f, .5f), Vector2.zero, new Vector2(470, 42), 20);
        value.alignment = TextAnchor.MiddleLeft; value.supportRichText = false;
        commandField.textComponent = value;
        Text placeholder = Label(r, "/help", new Vector2(.5f, .5f), Vector2.zero, new Vector2(470, 42), 20);
        placeholder.color = Color.gray; placeholder.alignment = TextAnchor.MiddleLeft; commandField.placeholder = placeholder;
        Button(hud.transform, "Send", new Vector2(.5f, 1), new Vector2(255, -40), new Vector2(100, 48), SubmitCommand);
        status = Label(hud.transform, "", new Vector2(.5f, 1), new Vector2(0, -115), new Vector2(660, 58), 18);
        status.supportRichText = false;
    }
    void SubmitCommand()
    {
        var gm = GameManager.Instance;
        if (gm && gm.LocalPlayer != null) gm.ProcessCommand(gm.LocalPlayer, commandField.text);
        commandField.text = ""; commandField.DeactivateInputField();
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
    }
    public void SetGameplay(bool value)
    {
        GameplayEnabled = value; ResetInput(); if (hud) hud.SetActive(value);
    }
    public void SetDriving(bool value)
    {
        Driving = value; ResetInput();
        if (jumpLabel) jumpLabel.text = value ? "Тормоз / R" : "Jump";
        if (sprintLabel) sprintLabel.text = value ? "Газ" : "Sprint";
        if (actionLabel) actionLabel.text = value ? "Выйти" : "Сесть";
        if (handbrakeButton) handbrakeButton.SetActive(value);
    }
    void ResetInput()
    {
        joystick = Look = Vector2.zero; jump = action = mouseLook = false;
        joystickPointer = sprintPointer = reversePointer = handbrakePointer = int.MinValue; lookFinger = -1;
        if (knob) knob.anchoredPosition = Vector2.zero;
    }
    public bool ConsumeJump() { bool value = CanInput && jump; jump = false; return value; }
    public bool ConsumeAction() { bool value = CanInput && action; action = false; return value; }
    bool OverUI(Vector2 position)
    {
        if (!EventSystem.current) return false;
        var data = new PointerEventData(EventSystem.current) { position = position };
        rayResults.Clear(); EventSystem.current.RaycastAll(data, rayResults);
        return rayResults.Count != 0;
    }
    void UpdateSafeArea()
    {
        safeArea = Screen.safeArea; width = Screen.width; height = Screen.height;
        if (width == 0 || height == 0) return;
        SafeRoot.anchorMin = new Vector2(safeArea.xMin / width, safeArea.yMin / height);
        SafeRoot.anchorMax = new Vector2(safeArea.xMax / width, safeArea.yMax / height);
    }
    void Update()
    {
        if (Screen.safeArea != safeArea || width != Screen.width || height != Screen.height) UpdateSafeArea();
        if (status && GameManager.Instance) status.text = GameManager.Instance.LastMessage;
        Look = Vector2.zero;
        if (!CanInput) { ResetInput(); return; }
        if (joystickPointer == int.MinValue)
        {
            float x = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
            float y = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
            joystick = Vector2.ClampMagnitude(new Vector2(x, y), 1);
        }
        jump |= !Driving && Input.GetKeyDown(KeyCode.Space); action |= Input.GetKeyDown(KeyCode.E);
        bool foundLook = false;
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Began && lookFinger < 0 && touch.position.x > Screen.width * .5f && !OverUI(touch.position))
                lookFinger = touch.fingerId;
            if (touch.fingerId != lookFinger) continue;
            foundLook = true;
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) lookFinger = -1;
            else Look = touch.deltaPosition * (720f / Mathf.Max(1, Screen.height));
        }
        if (!foundLook) lookFinger = -1;
        if (Input.touchCount == 0)
        {
            if (Input.GetMouseButtonDown(1) && !OverUI(Input.mousePosition)) { mouseLook = true; mouseLast = Input.mousePosition; }
            if (!Input.GetMouseButton(1)) mouseLook = false;
            if (mouseLook) { Vector2 now = Input.mousePosition; Look = (now - mouseLast) * (720f / Mathf.Max(1, Screen.height)); mouseLast = now; }
        }
    }
    void OnApplicationFocus(bool focused) { if (!focused) ResetInput(); }
    void OnApplicationPause(bool paused) { if (paused) ResetInput(); }
    void OnDestroy()
    {
        if (Instance != this) return;
        foreach (var s in sprites) if (s) { Destroy(s.texture); Destroy(s); }
        if (canvasObject) Destroy(canvasObject);
        if (ownedEventSystem) Destroy(ownedEventSystem);
        Instance = null;
    }
}
