using UnityEngine;

public class MyGameMode : BaseGameMode
{
    GameObject world;
    public override void OnGameModeInit()
    {
        Debug.Log("GameMode started");
        world = new GameObject("TestWorld");
        world.transform.SetParent(GameManager.Instance.transform);
        world.AddComponent<TestSceneGenerator>(); // Awake строит мир синхронно.
        GameManager.Instance.CreateStage2World(world.transform);
    }
    public override void OnGameModeExit() { if (world) Object.Destroy(world); }
    public override void OnPlayerConnect(PlayerData player)
    {
        var gm = GameManager.Instance;
        player.appearance = CharacterBuilder.GetRandomAppearance();
        player.characterObject = gm.Builder.BuildCharacter(player.appearance);
        player.characterObject.name = "Player_" + player.playerId;
        player.characterObject.transform.SetParent(gm.transform);
        player.characterObject.SetActive(false);
        // Локальный игрок сначала подтверждает внешность; остальные спавнятся сразу.
        if (player.isLocal && gm.RestoreLocalPlayer(player)) return;
        player.money = 500;
        if (player.isLocal) gm.Customization.Open(player);
        else { player.customizationComplete = true; gm.SpawnPlayer(player); }
    }
    public override void OnPlayerCommandText(PlayerData player, string cmdtext)
    {
        if (cmdtext.Trim().ToLowerInvariant() == "/hello")
            GameManager.Instance.Message("Привет, " + player.playerName + "!");
        else GameManager.Instance.Message("Неизвестная команда: " + cmdtext);
    }
    public override void OnPlayerDeath(PlayerData player, PlayerData killer, int reason)
    {
        GameManager.Instance.ChangeMoney(player, -Mathf.Min(100, player.money));
    }
    public override void OnPlayerUpdate(PlayerData player)
    {
        // Игровое правило вынесено в режим; сам контроллер о нём не знает.
        if (player.position.y < -15f) GameManager.Instance.KillPlayer(player, null, 0);
    }
}
