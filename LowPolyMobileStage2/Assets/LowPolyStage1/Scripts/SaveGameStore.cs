using System;
using System.IO;
using System.Text;
using UnityEngine;

[Serializable]
public class SaveGameData
{
    public string format;
    public int version;
    public CharacterAppearance appearance;
    public int skinId, money;
    public float health, armor;
    public Vector3 playerPosition;
    public Vector3 vehiclePosition;
    public float vehicleYaw, vehicleHealth;
}

// DTO не содержит GameObject. Один локальный профиль; резервная копия переживает
// прерванную запись. Формат будущей версии не перезаписываем старым клиентом.
public class SaveGameStore
{
    public string FilePath { get; private set; }
    public bool CanWrite { get; private set; } = true;
    public string LastError { get; private set; }
    bool primaryWasValid;
    public SaveGameStore(string directory) { FilePath = Path.Combine(directory, "stage2-save.json"); }
    public SaveGameData Load()
    {
        LastError = null;
        foreach (string path in new[] { FilePath, FilePath + ".bak" })
        {
            if (!File.Exists(path)) continue;
            try
            {
                if (new FileInfo(path).Length > 65536) throw new InvalidDataException("Слишком большой файл.");
                var data = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(path));
                if (data != null && data.format == "LowPolyStage2" && data.version > 1)
                {
                    CanWrite = false; LastError = "Сохранение из более новой версии: запись отключена."; return null;
                }
                if (!IsValid(data)) throw new InvalidDataException("Неверная структура сохранения.");
                primaryWasValid = path == FilePath;
                if (!primaryWasValid) LastError = "Загружена резервная копия сохранения.";
                return data;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { LastError = "Не удалось прочитать сохранение: " + e.Message; }
        }
        return null;
    }
    static bool Finite(float x) { return !float.IsNaN(x) && !float.IsInfinity(x); }
    static bool Range(float x, float min, float max) { return Finite(x) && x >= min && x <= max; }
    static bool ColorValid(Color c) { return Range(c.r, 0, 1) && Range(c.g, 0, 1) && Range(c.b, 0, 1) && Range(c.a, 0, 1); }
    public static bool PositionValid(Vector3 p) { return Range(p.x, -48, 48) && Range(p.z, -48, 48) && Range(p.y, -1, 30); }
    public static bool IsValid(SaveGameData d)
    {
        if (d == null || d.format != "LowPolyStage2" || d.version != 1 || d.appearance == null) return false;
        var a = d.appearance;
        return d.money >= 0 && d.money <= 1000000000 && d.skinId >= 0 && d.skinId <= 2 &&
            Range(d.health, 0, 100) && Range(d.armor, 0, 100) && PositionValid(d.playerPosition) &&
            PositionValid(d.vehiclePosition) && Range(d.vehicleYaw, -360, 360) && Range(d.vehicleHealth, 0, 100) &&
            a.hatType >= 0 && a.hatType <= 2 && a.shirtType >= 0 && a.shirtType <= 1 &&
            ColorValid(a.skinColor) && ColorValid(a.hairColor) && ColorValid(a.shirtColor) &&
            ColorValid(a.pantsColor) && ColorValid(a.shoesColor) && ColorValid(a.hatColor);
    }
    public bool Save(SaveGameData data)
    {
        if (!CanWrite) return false;
        if (!IsValid(data)) { LastError = "Сохранение отклонено: недопустимые данные."; return false; }
        string temp = FilePath + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data, true));
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            if (File.Exists(FilePath))
            {
                // Повреждённый оригинал сохраняем отдельно, не затирая исправный .bak.
                if (!primaryWasValid)
                {
                    File.Copy(FilePath, FilePath + ".invalid-" + Guid.NewGuid().ToString("N"));
                    File.Delete(FilePath); File.Move(temp, FilePath);
                }
                else
                {
                    try { File.Replace(temp, FilePath, FilePath + ".bak"); }
                    catch (PlatformNotSupportedException) { ReplaceWithBackup(temp); }
                    catch (NotSupportedException) { ReplaceWithBackup(temp); }
                    catch (IOException)
                    {
                        if (!File.Exists(temp) || !File.Exists(FilePath)) throw;
                        ReplaceWithBackup(temp);
                    }
                }
            }
            else File.Move(temp, FilePath);
            primaryWasValid = true; LastError = null; return true;
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        { LastError = "Не удалось сохранить: " + e.Message; return false; }
    }
    void ReplaceWithBackup(string temp)
    {
        File.Copy(FilePath, FilePath + ".bak", true);
        File.Delete(FilePath);
        File.Move(temp, FilePath);
    }
}
