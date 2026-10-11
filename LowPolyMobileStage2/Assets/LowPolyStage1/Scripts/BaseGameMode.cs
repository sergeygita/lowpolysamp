public abstract class BaseGameMode
{
    // Как в PAWN: технические системы вызывают события, правила задаёт режим.
    public virtual void OnGameModeInit() { }
    public virtual void OnGameModeExit() { }
    public virtual void OnPlayerConnect(PlayerData player) { }
    public virtual void OnPlayerDisconnect(PlayerData player, int reason) { }
    public virtual void OnPlayerSpawn(PlayerData player) { }
    public virtual void OnPlayerDeath(PlayerData player, PlayerData killer, int reason) { }
    public virtual void OnPlayerCommandText(PlayerData player, string cmdtext) { }
    public virtual void OnPlayerUpdate(PlayerData player) { }
    public virtual void OnPlayerSkinChange(PlayerData player, int oldSkin, int newSkin) { }
    public virtual void OnPlayerCustomizationChange(PlayerData player, CharacterAppearance appearance) { }
    public virtual void OnPlayerEnterVehicle(PlayerData player, VehicleController vehicle) { }
    public virtual void OnPlayerExitVehicle(PlayerData player, VehicleController vehicle) { }
    public virtual void OnPlayerStatsChange(PlayerData player) { }
    public virtual void OnVehicleDamage(VehicleController vehicle, float damage) { }
    public virtual void OnPlayerSave(PlayerData player) { }
}
