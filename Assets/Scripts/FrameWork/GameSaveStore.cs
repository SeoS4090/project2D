using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

[Serializable]
public sealed class PlayerSaveData
{
    public int schema_version = 1;
    public string saved_at_utc;
    public string player_name;
    public string hero_id;
    public int level = 1;
    public int gold;
    public List<InventorySaveEntry> inventory = new List<InventorySaveEntry>();
}

[Serializable]
public sealed class InventorySaveEntry
{
    public int item_id;
    public int count;
}

public sealed class GameSaveStore
{
    private readonly string directory;
    private readonly int slotCount;
    private readonly SemaphoreSlim fileLock = new SemaphoreSlim(1, 1);
    public bool LastLoadUsedBackup { get; private set; }

    public GameSaveStore(string directory, int slotCount)
    {
        if (slotCount < 1) throw new ArgumentOutOfRangeException(nameof(slotCount));
        this.directory = Path.GetFullPath(directory);
        this.slotCount = slotCount;
    }

    public static PlayerSaveData CreateNewGame(GameDataDTO gameData, string playerName)
    {
        if (gameData == null || !gameData.IsLoaded)
            throw new InvalidOperationException("Load game data before creating a new game.");
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be empty.", nameof(playerName));
        string heroId = gameData.commonInstant.GetData<string>("default_hero");
        var hero = gameData.hero.GetData(heroId);
        return new PlayerSaveData
        {
            player_name = playerName.Trim(),
            hero_id = heroId,
            gold = gameData.commonInstant.GetData<int>("starting_gold"),
            inventory = new List<InventorySaveEntry> { new InventorySaveEntry { item_id = hero.base_item, count = 1 } }
        };
    }

    public async Task SaveAsync(int slot, PlayerSaveData data, CancellationToken cancellationToken = default)
    {
        Validate(data);
        string path = GetPath(slot);
        // Snapshot on the calling thread so subsequent gameplay edits do not change the saved payload.
        data.saved_at_utc = DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        await fileLock.WaitAsync(cancellationToken);
        try
        {
            await Task.Run(() =>
            {
                Directory.CreateDirectory(directory);
                string temporary = path + ".tmp";
                try
                {
                    File.WriteAllText(temporary, json, new System.Text.UTF8Encoding(false));
                    if (File.Exists(path))
                        File.Replace(temporary, path, path + ".bak");
                    else
                        File.Move(temporary, path);
                }
                finally
                {
                    if (File.Exists(temporary)) File.Delete(temporary);
                }
            }, cancellationToken);
        }
        finally { fileLock.Release(); }
    }

    public async Task<PlayerSaveData> LoadAsync(int slot, CancellationToken cancellationToken = default)
    {
        string path = GetPath(slot);
        await fileLock.WaitAsync(cancellationToken);
        try
        {
            LastLoadUsedBackup = false;
            string json = await ReadAsync(path, cancellationToken);
            try { return Parse(json); }
            catch (Exception exception) when (exception is JsonException || exception is InvalidDataException)
            {
                if (!File.Exists(path + ".bak")) throw;
                PlayerSaveData backup = Parse(await ReadAsync(path + ".bak", cancellationToken));
                LastLoadUsedBackup = true;
                return backup;
            }
        }
        finally { fileLock.Release(); }
    }

    public bool Exists(int slot) => File.Exists(GetPath(slot));

    private string GetPath(int slot)
    {
        if (slot < 1 || slot > slotCount)
            throw new ArgumentOutOfRangeException(nameof(slot), $"Slot must be between 1 and {slotCount}.");
        return Path.Combine(directory, "slot_" + slot + ".json");
    }

    private static Task<string> ReadAsync(string path, CancellationToken token) =>
        Task.Run(() => File.ReadAllText(path), token);

    private static PlayerSaveData Parse(string json)
    {
        var result = JsonConvert.DeserializeObject<PlayerSaveData>(json);
        Validate(result);
        return result;
    }

    private static void Validate(PlayerSaveData data)
    {
        if (data == null || data.schema_version != 1 || string.IsNullOrWhiteSpace(data.player_name) ||
            string.IsNullOrWhiteSpace(data.hero_id) || data.level < 1 || data.gold < 0 || data.inventory == null)
            throw new InvalidDataException("Save data is invalid or uses an unsupported schema version.");
        foreach (var item in data.inventory)
            if (item == null || item.item_id <= 0 || item.count < 1)
                throw new InvalidDataException("Save inventory contains an invalid entry.");
    }
}
