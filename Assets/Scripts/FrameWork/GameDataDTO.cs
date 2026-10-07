using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class GameDataDTO : PersistentSingleton<GameDataDTO>
{
    // The table names also form Addressable keys: GameData/<name>.
    private readonly string[] gameDatalist = { "CommonInstant", "Item", "Hero" };

    public ItemDTO item;
    public HeroDTO hero;
    public CommmonInstantDTO commonInstant;

    public bool IsLoaded { get; private set; }
    public float LoadProgress { get; private set; }
    public Exception LastError { get; private set; }
    public event Action Loaded;
    private Task loadTask;

    public Task GameDataLoadAll()
    {
        if (loadTask != null && !loadTask.IsCompleted)
            return loadTask;
        return loadTask = LoadAllAsync();
    }

    private async Task LoadAllAsync()
    {
        LastError = null;
        LoadProgress = 0;
        var nextCommon = default(CommmonInstantDTO);
        var nextItem = default(ItemDTO);
        var nextHero = default(HeroDTO);
        try
        {
            for (int i = 0; i < gameDatalist.Length; i++)
            {
                string name = gameDatalist[i];
                string address = "GameData/" + name;
                switch (name)
                {
                    case "CommonInstant":
                        nextCommon = await AddressableJsonLoader.LoadAsync<CommmonInstantDTO>(address, LifetimeToken);
                        break;
                    case "Item":
                        nextItem = await AddressableJsonLoader.LoadAsync<ItemDTO>(address, LifetimeToken);
                        break;
                    case "Hero":
                        nextHero = await AddressableJsonLoader.LoadAsync<HeroDTO>(address, LifetimeToken);
                        break;
                    default:
                        throw new InvalidOperationException($"No DTO registered for '{name}'.");
                }
                LoadProgress = (i + 1f) / gameDatalist.Length;
            }

            ValidateReferences(nextCommon, nextItem, nextHero);
            LifetimeToken.ThrowIfCancellationRequested();
            // Publish the complete set only after every table passes validation.
            commonInstant = nextCommon;
            item = nextItem;
            hero = nextHero;
            IsLoaded = true;
        }
        catch (Exception exception)
        {
            LastError = exception;
            throw;
        }
        Loaded?.Invoke();
    }

    public static void ValidateReferences(CommmonInstantDTO common, ItemDTO items, HeroDTO heroes)
    {
        if (common == null || items == null || heroes == null)
            throw new InvalidOperationException("CommonInstant, Item and Hero tables are required.");

        foreach (var entry in heroes.data)
        {
            if (!items.dataDict.ContainsKey(entry.base_item))
                throw new InvalidOperationException($"Hero '{entry.id}' references missing item {entry.base_item}.");
        }

        if (!heroes.dataDict.ContainsKey(common.GetData<string>("default_hero")))
            throw new InvalidOperationException("default_hero must reference an existing Hero.id.");
        if (common.GetData<int>("default_save_slot") < 1 ||
            common.GetData<int>("inventory_capacity") < 1 ||
            common.GetData<int>("starting_gold") < 0)
            throw new InvalidOperationException("Save slots/capacity must be positive; starting_gold cannot be negative.");
        foreach (string volume in new[] { "master_volume", "music_volume", "sfx_volume" })
        {
            float value = common.GetData<float>(volume);
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0 || value > 1)
                throw new InvalidOperationException($"{volume} must be between 0 and 1.");
        }
    }

    public IEnumerable<string> GetRequiredTermKeys()
    {
        if (!IsLoaded)
            throw new InvalidOperationException("Game data is not loaded.");
        foreach (var entry in item.data)
        {
            yield return entry.name_term;
            yield return entry.description_term;
        }
        foreach (var entry in hero.data)
        {
            yield return entry.name_term;
            yield return entry.description_term;
        }
    }
}
