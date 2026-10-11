using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    readonly List<PlayerData> players = new List<PlayerData>();
    // Только GameManager меняет состав списка, как серверный диспетчер PAWN.
    public IReadOnlyList<PlayerData> connectedPlayers { get { return players; } }
    public BaseGameMode currentGameMode { get; private set; }
    public CharacterBuilder Builder { get; private set; }
    public CharacterCustomizationUI Customization { get; private set; }
    public ThirdPersonCamera FollowCamera { get; private set; }
    public PlayerData LocalPlayer { get; private set; }
    public Vector3 SpawnPoint { get; set; } = new Vector3(0, 0.15f, -8);
    public string LastMessage { get; private set; } = "Введите /hello или /help";
    readonly List<PlayerData> updateSnapshot = new List<PlayerData>();
    int nextId;
    bool shuttingDown;
    public VehicleController StarterVehicle { get; private set; }
    readonly List<WorldPickup> pickups = new List<WorldPickup>();
    SaveGameStore saveStore;
    SaveGameData loadedSave;
    float nextAutosave, nextPickupCheck;


    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        Application.targetFrameRate = 60;
        saveStore = new SaveGameStore(Application.persistentDataPath);
        Builder = gameObject.AddComponent<CharacterBuilder>();
        if (!MobileInputManager.Instance) gameObject.AddComponent<MobileInputManager>();
        Customization = gameObject.AddComponent<CharacterCustomizationUI>();
        var cameraObject = new GameObject("GameplayCamera", typeof(Camera), typeof(AudioListener));
        cameraObject.transform.SetParent(transform);
        cameraObject.tag = "MainCamera";
        var cam = cameraObject.GetComponent<Camera>();
        cam.cullingMask = ~(1 << 31);
        cam.nearClipPlane = 0.08f;
        cam.farClipPlane = 150f;
        cam.allowHDR = false;
        cam.allowMSAA = false;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.45f, 0.72f, 0.88f);
        FollowCamera = cameraObject.AddComponent<ThirdPersonCamera>();
        gameObject.AddComponent<GameHUD>();
    }
    void Start()
    {
        if (Instance != this) return;
        loadedSave = saveStore.Load();
        if (saveStore.LastError != null) Debug.LogWarning(saveStore.LastError);
        nextAutosave = Time.unscaledTime + 15;
        Initialize(new MyGameMode());
        ConnectPlayer("Player", true);
    }
    public void Initialize(BaseGameMode mode)
    {
        if (mode == null) throw new ArgumentNullException(nameof(mode));
        if (players.Count != 0) throw new InvalidOperationException("Сначала отключите игроков.");
        if (currentGameMode != null) currentGameMode.OnGameModeExit();
        currentGameMode = mode;
        mode.OnGameModeInit();
    }
    public PlayerData ConnectPlayer(string playerName, bool local = false)
    {
        if (currentGameMode == null) throw new InvalidOperationException("Нет режима.");
        if (local && LocalPlayer != null) throw new InvalidOperationException("Локальный игрок уже есть.");
        var p = new PlayerData { playerId = nextId++, playerName = playerName, isLocal = local };
        players.Add(p);
        if (local) LocalPlayer = p;
        currentGameMode.OnPlayerConnect(p);
        return p;
    }
    bool Known(PlayerData p) { return p != null && players.Contains(p); }
    public void DisconnectPlayer(PlayerData p, int reason)
    {
        if (!Known(p)) return;
        if (p.isLocal) SaveLocal(false);
        DetachVehicle(p);
        players.Remove(p);
        currentGameMode.OnPlayerDisconnect(p, reason);
        if (p == LocalPlayer)
        {
            if (Customization) Customization.Close();
            if (FollowCamera) FollowCamera.SetTarget(null);
            if (MobileInputManager.Instance) MobileInputManager.Instance.SetGameplay(false);
            LocalPlayer = null;
        }
        if (p.characterObject) Destroy(p.characterObject);
        p.isSpawned = false;
    }
    public void CompleteCustomization(PlayerData p)
    {
        if (!Known(p)) return;
        bool editingExisting = p.customizationComplete;
        float savedHealth = p.health, savedArmor = p.armor;
        Vector3 savedPosition = p.lastFootPosition;
        p.customizationComplete = true;
        Customization.Close();
        SpawnPlayer(p);
        if (editingExisting)
        {
            p.health = savedHealth; p.armor = savedArmor;
            TeleportFoot(p, SafeFootPosition(savedPosition));
            currentGameMode.OnPlayerStatsChange(p);
        }
        SaveLocal(false);
    }
    public void SpawnPlayer(PlayerData p)
    {
        if (!Known(p) || !p.customizationComplete || !p.characterObject) return;
        DetachVehicle(p);
        var go = p.characterObject;
        var cc = go.GetComponent<CharacterController>();
        if (!cc)
        {
            cc = go.AddComponent<CharacterController>();
            cc.height = 1.85f; cc.radius = 0.3f;
            cc.center = new Vector3(0, 0.94f, 0);
            cc.stepOffset = 0.25f; cc.skinWidth = 0.03f;
        }
        cc.enabled = false;
        go.transform.SetPositionAndRotation(SpawnPoint, Quaternion.identity);
        go.SetActive(true);
        cc.enabled = true;
        p.position = p.lastFootPosition = SpawnPoint; p.health = 100; p.armor = 0; p.isSpawned = true; p.respawnAt = 0;
        var controller = go.GetComponent<PlayerController>();
        if (!controller) controller = go.AddComponent<PlayerController>();
        controller.Bind(p, FollowCamera.transform);
        controller.ResetMotion();
        controller.enabled = p.isLocal;
        if (p.isLocal)
        {
            FollowCamera.SetTarget(go.transform);
            MobileInputManager.Instance.SetGameplay(true);
        }
        currentGameMode.OnPlayerSpawn(p);
    }
    public void DamagePlayer(PlayerData p, float damage, PlayerData attacker = null, int reason = 0)
    {
        if (!Known(p) || !p.isSpawned || damage <= 0 || float.IsNaN(damage) || float.IsInfinity(damage)) return;
        float absorbed = Mathf.Min(p.armor, damage);
        p.armor -= absorbed;
        p.health = Mathf.Max(0, p.health - (damage - absorbed));
        currentGameMode.OnPlayerStatsChange(p);
        if (p.health <= 0) KillPlayer(p, attacker, reason);
    }
    public void KillPlayer(PlayerData p, PlayerData killer, int reason)
    {
        if (!Known(p) || !p.isSpawned) return;
        DetachVehicle(p);
        p.health = 0; p.isSpawned = false; p.respawnAt = Time.time + 3;
        if (p.characterObject) p.characterObject.SetActive(false);
        currentGameMode.OnPlayerDeath(p, killer, reason);
        Message("Вы погибли. Возрождение через 3 секунды; штраф до $100.");
    }
    public void SetAppearance(PlayerData p, CharacterAppearance appearance)
    {
        if (!Known(p) || appearance == null) return;
        p.appearance = appearance.Copy();
        p.appearance.hatType = Mathf.Clamp(p.appearance.hatType, 0, 2);
        p.appearance.shirtType = Mathf.Clamp(p.appearance.shirtType, 0, 1);
        Builder.ApplyAppearance(p.characterObject, p.appearance);
        // Внешность меняется только через диспетчер, callback не теряется в UI.
        currentGameMode.OnPlayerCustomizationChange(p, p.appearance.Copy());
    }
    public void SetSkin(PlayerData p, int id)
    {
        if (!Known(p) || id < 0 || id > 2) return;
        int old = p.skinId;
        p.skinId = id;
        var a = CharacterBuilder.GetDefaultAppearance();
        a.hatType = id;
        a.shirtType = id == 2 ? 1 : 0;
        if (id == 1) a.shirtColor = new Color(0.9f, 0.3f, 0.1f);
        if (id == 2) a.shirtColor = new Color(0.25f, 0.25f, 0.35f);
        SetAppearance(p, a);
        currentGameMode.OnPlayerSkinChange(p, old, id);
    }
    public void ProcessCommand(PlayerData p, string input)
    {
        if (!Known(p) || string.IsNullOrWhiteSpace(input)) return;
        string[] t = input.Trim().Split(new[] {' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
        string cmd = t[0].ToLowerInvariant();
        if (!cmd.StartsWith("/")) { Message("Команда должна начинаться с /"); return; }
        if (ProcessSystemCommand(p, t)) return;
        switch (cmd)
        {
            case "/spawn":
                if (t.Length != 1) { Message("/spawn"); return; }
                SpawnPlayer(p); break;
            case "/heal":
                if (t.Length != 1) { Message("/heal"); return; }
                if (p.isSpawned) { p.health = 100; currentGameMode.OnPlayerStatsChange(p); } break;
            case "/skin":
                int id;
                if (t.Length != 2 || !int.TryParse(t[1], out id) || id < 0 || id > 2)
                { Message("/skin [0..2]"); return; }
                SetSkin(p, id); break;
            case "/randomize":
                if (t.Length != 1) { Message("/randomize"); return; }
                SetAppearance(p, CharacterBuilder.GetRandomAppearance()); break;
            case "/color":
                byte r, g, b;
                if (t.Length != 5 || !byte.TryParse(t[2], out r) || !byte.TryParse(t[3], out g) || !byte.TryParse(t[4], out b))
                { Message("/color [skin|hair|shirt|pants|shoes|hat] [0..255] [0..255] [0..255]"); return; }
                var a = p.appearance.Copy();
                Color c = new Color32(r, g, b, 255);
                switch (t[1].ToLowerInvariant())
                {
                    case "skin": a.skinColor = c; break;
                    case "hair": a.hairColor = c; break;
                    case "shirt": a.shirtColor = c; break;
                    case "pants": a.pantsColor = c; break;
                    case "shoes": a.shoesColor = c; break;
                    case "hat": a.hatColor = c; break;
                    default: Message("Части: skin hair shirt pants shoes hat"); return;
                }
                SetAppearance(p, a); break;
            case "/help": Message("/enter /exit /repair /car /medic /armor /save /hurt 25 /spawn /heal /skin /color /randomize"); break;
            default: currentGameMode.OnPlayerCommandText(p, input.Trim()); return;
        }
        if (cmd != "/help") Message("Выполнено: " + cmd);
    }
    public void Interact(PlayerData p)
    {
        if (!Known(p) || !p.isSpawned) return;
        if (p.currentVehicle) ExitVehicle(p);
        else EnterVehicle(p);
    }
    public void Message(string message) { LastMessage = message; Debug.Log(message); }
    void LateUpdate()
    {
        if (LocalPlayer != null && MobileInputManager.Instance && MobileInputManager.Instance.ConsumeAction()) Interact(LocalPlayer);
        if (LocalPlayer != null && !LocalPlayer.isSpawned && LocalPlayer.customizationComplete && LocalPlayer.respawnAt > 0 && Time.time >= LocalPlayer.respawnAt)
            SpawnPlayer(LocalPlayer);
        if (Time.unscaledTime >= nextAutosave) { nextAutosave = Time.unscaledTime + 15; SaveLocal(false); }
        if (Time.time >= nextPickupCheck) { nextPickupCheck = Time.time + .15f; CollectPickups(); }
        // Снимок позволяет callback-у отключить игрока во время обхода.
        updateSnapshot.Clear();
        updateSnapshot.AddRange(players);
        foreach (var p in updateSnapshot)
        {
            if (!Known(p) || !p.isSpawned || !p.characterObject) continue;
            p.position = p.currentVehicle ? p.currentVehicle.transform.position : p.characterObject.transform.position;
            if (!p.currentVehicle)
            {
                var cc = p.characterObject.GetComponent<CharacterController>();
                if (cc && cc.isGrounded && SaveGameStore.PositionValid(p.position)) p.lastFootPosition = p.position;
            }
            currentGameMode.OnPlayerUpdate(p);
        }
    }
    void Shutdown()
    {
        if (shuttingDown || Instance != this) return;
        SaveLocal(false);
        shuttingDown = true;
        foreach (var p in players.ToArray()) DisconnectPlayer(p, 0);
        if (currentGameMode != null) currentGameMode.OnGameModeExit();
        currentGameMode = null;
    }
    void OnApplicationQuit() { Shutdown(); }
    void OnDestroy() { Shutdown(); if (Instance == this) Instance = null; }
    public void CreateStage2World(Transform parent)
    {
        var car = new GameObject("StarterCar"); car.transform.SetParent(parent, false);
        StarterVehicle = car.AddComponent<VehicleController>();
        StarterVehicle.Initialize(Builder, new Vector3(0, .65f, -1));
        Physics.SyncTransforms();
        if (loadedSave != null)
        {
            Vector3 pos = loadedSave.vehiclePosition; pos.y = Mathf.Max(.55f, pos.y);
            Quaternion rot = Quaternion.Euler(0, loadedSave.vehicleYaw, 0);
            if (!Physics.CheckBox(pos + Vector3.up * .65f, new Vector3(.95f, .6f, 1.9f), rot, 1, QueryTriggerInteraction.Ignore))
                StarterVehicle.ResetAt(pos, loadedSave.vehicleYaw);
            StarterVehicle.SetHealth(loadedSave.vehicleHealth);
        }
        for (int i = 0; i < 3; i++)
        {
            var go = new GameObject("Pickup_" + (PickupKind)i); go.transform.SetParent(parent, false);
            var pickup = go.AddComponent<WorldPickup>();
            pickup.Initialize(Builder, (PickupKind)i, i == 2 ? 100 : 40, new Vector3(-5, .05f, -8 + i * 6));
            pickups.Add(pickup);
        }
    }
    public bool RestoreLocalPlayer(PlayerData p)
    {
        if (loadedSave == null) return false;
        var d = loadedSave; loadedSave = null;
        p.customizationComplete = true;
        SetAppearance(p, d.appearance); p.skinId = d.skinId; p.money = d.money;
        SpawnPlayer(p);
        if (d.health > 0)
        {
            p.health = d.health; p.armor = d.armor;
            TeleportFoot(p, SafeFootPosition(d.playerPosition));
        }
        Message("Прогресс загружен. /custom — изменить внешность.");
        currentGameMode.OnPlayerStatsChange(p);
        return true;
    }
    Vector3 SafeFootPosition(Vector3 candidate)
    {
        if (!SaveGameStore.PositionValid(candidate)) return SpawnPoint;
        Physics.SyncTransforms();
        RaycastHit hit;
        if (!Physics.Raycast(candidate + Vector3.up * 2, Vector3.down, out hit, 5, 1, QueryTriggerInteraction.Ignore)) return SpawnPoint;
        candidate = hit.point + Vector3.up * .08f;
        if (Physics.CheckCapsule(candidate + Vector3.up * .32f, candidate + Vector3.up * 1.56f, .29f, 1 | (1 << VehicleController.VehicleLayer), QueryTriggerInteraction.Ignore)) return SpawnPoint;
        return candidate;
    }
    void TeleportFoot(PlayerData p, Vector3 position)
    {
        var cc = p.characterObject.GetComponent<CharacterController>();
        cc.enabled = false; p.characterObject.transform.position = position; cc.enabled = true;
        p.position = p.lastFootPosition = position;
        p.characterObject.GetComponent<PlayerController>().ResetMotion();
        if (p.isLocal) FollowCamera.SetTarget(p.characterObject.transform);
    }
    public VehicleController NearestVehicle(PlayerData p, float range)
    {
        if (p == null || !StarterVehicle) return null;
        return (p.position - StarterVehicle.transform.position).sqrMagnitude <= range * range ? StarterVehicle : null;
    }
    public void EnterVehicle(PlayerData p)
    {
        if (!Known(p) || !p.isSpawned || p.currentVehicle) return;
        var car = NearestVehicle(p, 3.2f);
        if (!car) { Message("Подойдите к машине и нажмите «Сесть» / E."); return; }
        if (car.Driver != null || car.SpeedKmh > 5) { Message("Машина занята или движется."); return; }
        if (car.Health <= 0) { Message("Машина сломана. Подойдите и выберите ремонт."); return; }
        p.lastFootPosition = p.characterObject.transform.position;
        p.currentVehicle = car; car.SetDriver(p);
        p.characterObject.SetActive(false); // Пешая физика и ввод полностью выключены.
        if (p.isLocal)
        {
            MobileInputManager.Instance.SetDriving(true);
            FollowCamera.SetTarget(car.transform, true);
        }
        currentGameMode.OnPlayerEnterVehicle(p, car);
        Message("Газ / W — вперёд; Тормоз / S — торможение и задний ход.");
    }
    void DetachVehicle(PlayerData p)
    {
        var car = p.currentVehicle;
        if (!car) { p.currentVehicle = null; return; }
        car.SetDriver(null); p.currentVehicle = null;
        if (p.isLocal && MobileInputManager.Instance) MobileInputManager.Instance.SetDriving(false);
        currentGameMode.OnPlayerExitVehicle(p, car);
    }
    public void ExitVehicle(PlayerData p)
    {
        if (!Known(p) || !p.currentVehicle) return;
        var car = p.currentVehicle;
        if (car.SpeedKmh > 3) { Message("Сначала остановите машину (менее 3 км/ч)."); return; }
        Vector3 position;
        if (!car.TryExitPosition(out position)) { Message("Двери заблокированы. Отъедьте от препятствия."); return; }
        DetachVehicle(p);
        var cc = p.characterObject.GetComponent<CharacterController>(); cc.enabled = false;
        p.characterObject.SetActive(true); TeleportFoot(p, position);
        Message("Вы вышли из машины.");
    }
    public void VehicleImpact(VehicleController car, float speed)
    {
        if (!car || speed <= 4) return;
        float damage = Mathf.Clamp((speed - 4) * 4, 0, 60);
        car.SetHealth(car.Health - damage);
        currentGameMode.OnVehicleDamage(car, damage);
        var driver = car.Driver;
        if (driver != null) DamagePlayer(driver, damage * .3f, null, 2);
        if (car.Health <= 0) Message("Машина сломана: остановитесь, выйдите или отремонтируйте за $100.");
    }
    public void RecoverVehicle(VehicleController car)
    {
        if (!car) return;
        car.ResetAt(car.HomePosition, 0);
        if (car.Driver != null && car.Driver.isLocal) FollowCamera.SetTarget(car.transform, true);
        Message("Машина возвращена на стоянку.");
    }
    public void ChangeMoney(PlayerData p, int amount)
    {
        if (!Known(p)) return;
        long result = (long)p.money + amount;
        p.money = (int)Math.Max(0L, Math.Min(1000000000L, result));
        currentGameMode.OnPlayerStatsChange(p);
    }
    bool Spend(PlayerData p, int amount)
    {
        if (p.money < amount) { Message("Недостаточно денег: нужно $" + amount); return false; }
        ChangeMoney(p, -amount); return true;
    }
    void CollectPickups()
    {
        var p = LocalPlayer;
        if (p == null || !p.isSpawned || p.currentVehicle) return;
        foreach (var pickup in pickups)
        {
            if (!pickup || !pickup.Available || (p.position - pickup.transform.position).sqrMagnitude > 1.5f * 1.5f) continue;
            if (pickup.Kind == PickupKind.Health)
            {
                if (p.health >= 100) continue;
                p.health = Mathf.Min(100, p.health + pickup.Amount);
            }
            else if (pickup.Kind == PickupKind.Armor)
            {
                if (p.armor >= 100) continue;
                p.armor = Mathf.Min(100, p.armor + pickup.Amount);
            }
            else ChangeMoney(p, pickup.Amount);
            pickup.Consume(); currentGameMode.OnPlayerStatsChange(p);
            Message(pickup.Kind == PickupKind.Money ? "+$100" : pickup.Kind == PickupKind.Health ? "Аптечка: +40 HP" : "Броня: +40");
        }
    }
    bool ProcessSystemCommand(PlayerData p, string[] t)
    {
        string cmd = t[0].ToLowerInvariant();
        if (cmd == "/save") { SaveLocal(true); return true; }
        if (cmd != "/enter" && cmd != "/exit" && cmd != "/car" && cmd != "/repair" && cmd != "/medic" && cmd != "/armor" && cmd != "/hurt" && cmd != "/custom") return false;
        if (!p.isSpawned) { Message("Дождитесь возрождения."); return true; }
        if (cmd != "/hurt" && t.Length != 1) { Message("Используйте " + cmd + " без аргументов."); return true; }
        switch (cmd)
        {
            case "/enter": EnterVehicle(p); break;
            case "/exit": ExitVehicle(p); break;
            case "/car": RecoverVehicle(StarterVehicle); break;
            case "/repair":
                var car = p.currentVehicle ? p.currentVehicle : NearestVehicle(p, 4);
                if (!car) { Message("Подойдите к машине."); break; }
                if (car.SpeedKmh > 3) { Message("Для ремонта остановитесь."); break; }
                if (car.Health >= 100) { Message("Ремонт не требуется."); break; }
                if (Spend(p, 100)) { car.SetHealth(100); Message("Машина отремонтирована за $100."); }
                break;
            case "/medic":
                if (p.health >= 100) { Message("Здоровье уже полное."); break; }
                if (Spend(p, 50)) { p.health = 100; currentGameMode.OnPlayerStatsChange(p); Message("Лечение: $50."); }
                break;
            case "/armor":
                if (p.armor >= 100) { Message("Броня уже полная."); break; }
                if (Spend(p, 100)) { p.armor = 100; currentGameMode.OnPlayerStatsChange(p); Message("Броня: $100."); }
                break;
            case "/hurt":
                int damage;
                if (t.Length != 2 || !int.TryParse(t[1], out damage) || damage < 1 || damage > 500) { Message("/hurt [1..500] — тест урона"); break; }
                DamagePlayer(p, damage, null, 3); break;
            case "/custom":
                if (p.currentVehicle) { Message("Сначала выйдите из машины."); break; }
                SaveLocal(false);
                p.isSpawned = false; p.respawnAt = 0;
                p.characterObject.SetActive(false); Customization.Open(p); break;
        }
        return true;
    }
    public bool SaveLocal(bool notify)
    {
        var p = LocalPlayer;
        if (shuttingDown || p == null || !p.customizationComplete || p.appearance == null || saveStore == null || !StarterVehicle) return false;
        // Пока открыта кастомизация существующего профиля, не сохраняем промежуточное состояние.
        if (!p.isSpawned && p.respawnAt == 0) return false;
        var car = StarterVehicle;
        Vector3 foot = p.lastFootPosition;
        if (p.currentVehicle)
        {
            Vector3 exit;
            if (p.currentVehicle.TryExitPosition(out exit)) foot = exit;
        }
        if (!SaveGameStore.PositionValid(foot)) foot = SpawnPoint;
        Vector3 carPosition = SaveGameStore.PositionValid(car.transform.position) ? car.transform.position : car.HomePosition;
        var data = new SaveGameData {
            format = "LowPolyStage2", version = 1, appearance = p.appearance.Copy(), skinId = p.skinId,
            money = p.money, health = p.health, armor = p.armor, playerPosition = foot,
            vehiclePosition = carPosition, vehicleYaw = car.transform.eulerAngles.y, vehicleHealth = car.Health
        };
        bool saved = saveStore.Save(data);
        if (saved) currentGameMode.OnPlayerSave(p);
        if (notify) Message(saved ? "Прогресс сохранён." : saveStore.LastError ?? "Запись сохранения отключена.");
        return saved;
    }
    void OnApplicationPause(bool paused) { if (paused && Instance == this) SaveLocal(false); }
    void OnApplicationFocus(bool focused) { if (!focused && Instance == this) SaveLocal(false); }
}
