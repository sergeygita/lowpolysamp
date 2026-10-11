using UnityEngine;

// Аналог набора переменных игрока в PAWN. Unity-объект не является сетевым ID.
public class PlayerData
{
    public int playerId;
    public string playerName;
    public GameObject characterObject;
    public CharacterAppearance appearance;
    public Vector3 position;
    public float health = 100f, armor;
    public int money;
    public bool isSpawned;
    public int skinId; // В этапе 1: пресеты 0..2, не каталог моделей GTA.
    public bool isLocal;
    public bool customizationComplete;
    public VehicleController currentVehicle;
    public Vector3 lastFootPosition;
    public float respawnAt;
}
