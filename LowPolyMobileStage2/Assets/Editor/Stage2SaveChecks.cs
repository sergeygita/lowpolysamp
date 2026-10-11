#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Проверки реального SaveGameStore. Пишут только в уникальную временную папку,
// не затрагивают профиль игрока. Запуск: LowPoly > Run Save Checks.
public static class Stage2SaveChecks
{
    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Save check failed: " + message);
    }
    static SaveGameData Sample()
    {
        return new SaveGameData {
            format = "LowPolyStage2", version = 1, appearance = CharacterBuilder.GetDefaultAppearance(),
            money = 500, health = 100, armor = 40, playerPosition = new Vector3(0, .15f, -8),
            vehiclePosition = new Vector3(0, .65f, -1), vehicleHealth = 100
        };
    }
    [MenuItem("LowPoly/Run Save Checks")]
    public static void Run()
    {
        string directory = Path.Combine(Path.GetTempPath(), "LowPolySaveChecks-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        int passed = 0;
        try
        {
            var store = new SaveGameStore(directory);
            Check(store.Load() == null && store.CanWrite, "missing file must allow a new game"); passed++;
            var sample = Sample();
            Check(store.Save(sample), "first write");
            var loaded = new SaveGameStore(directory).Load();
            Check(loaded != null && loaded.money == 500 && loaded.armor == 40 && loaded.appearance.shirtColor == sample.appearance.shirtColor, "round trip"); passed++;
            sample.health = 70; Check(store.Save(sample), "second write");
            Check(File.Exists(store.FilePath + ".bak"), "backup must exist");
            File.WriteAllText(store.FilePath, "{broken");
            var recovery = new SaveGameStore(directory); loaded = recovery.Load();
            Check(loaded != null && loaded.health == 100, "recover previous valid data from backup"); passed++;
            loaded.health = 55; Check(recovery.Save(loaded), "write after recovery");
            Check(Directory.GetFiles(directory, "*.invalid-*").Length == 1, "preserve corrupt original");
            Check(new SaveGameStore(directory).Load().health == 55, "recovered progress saved"); passed++;
            string before = File.ReadAllText(store.FilePath);
            loaded.money = -1; Check(!recovery.Save(loaded), "reject negative money");
            Check(File.ReadAllText(store.FilePath) == before, "invalid data cannot overwrite save"); passed++;
            sample = Sample(); sample.version = 2;
            string future = JsonUtility.ToJson(sample); File.WriteAllText(store.FilePath, future);
            var oldClient = new SaveGameStore(directory);
            Check(oldClient.Load() == null && !oldClient.CanWrite && !oldClient.Save(Sample()), "future version write protection");
            Check(File.ReadAllText(store.FilePath) == future, "future save remains intact"); passed++;
            Debug.Log("Stage 2 save checks: " + passed + "/6 passed. Runtime driving still requires Play Mode.");
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
#endif
