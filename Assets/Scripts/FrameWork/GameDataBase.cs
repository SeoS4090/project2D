using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public interface IDTO
{
    void MakeDictionary();
}

[Serializable]
public class CommmonInstantDTO : IDTO
{
    [Serializable]
    public class CommmonInstantBaseDTO
    {
        public string key;
        public object value;
    }

    public List<CommmonInstantBaseDTO> data = new List<CommmonInstantBaseDTO>();
    [JsonIgnore] public Dictionary<string, CommmonInstantBaseDTO> dataDict;

    public void MakeDictionary()
    {
        dataDict = GameDataIndex.Create(data, row => row.key, "CommonInstant");
    }

    public T GetData<T>(string key)
    {
        if (dataDict == null)
            MakeDictionary();
        if (!dataDict.TryGetValue(key, out var entry))
            throw new KeyNotFoundException($"CommonInstant key '{key}' was not found.");
        try
        {
            JToken token = entry.value as JToken ?? (entry.value == null ? JValue.CreateNull() : JToken.FromObject(entry.value));
            if (token.Type == JTokenType.Null)
            {
                if (typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
                    throw new InvalidCastException("Null cannot be converted to a non-nullable value.");
                return default;
            }
            return entry.value is T value ? value : token.ToObject<T>();
        }
        catch (Exception exception) when (exception is JsonException || exception is FormatException ||
                                          exception is InvalidCastException || exception is OverflowException ||
                                          exception is ArgumentException)
        {
            throw new InvalidOperationException($"CommonInstant '{key}' cannot be converted to {typeof(T).Name}.", exception);
        }
    }
}

[Serializable]
public class HeroDTO : IDTO
{
    [Serializable]
    public class HeroBaseDTO
    {
        public string id;
        public string name_term;
        public string description_term;
        public int base_item;
        public int base_speed;
        public int base_str;
        public int base_dex;
        public int base_int;
        public int base_wis;
        public int base_luk;
        public int base_con;
        [JsonIgnore] public int HP => Mathf.RoundToInt(base_str * 0.5f + base_con * 1.2f);
        [JsonIgnore] public int MP => Mathf.RoundToInt(base_int * 0.5f + base_wis);
    }

    public List<HeroBaseDTO> data = new List<HeroBaseDTO>();
    [JsonIgnore] public Dictionary<string, HeroBaseDTO> dataDict;

    public void MakeDictionary()
    {
        var index = GameDataIndex.Create(data, row => row.id, "Hero");
        foreach (var row in data)
        {
            GameDataIndex.RequireTerms(row.name_term, row.description_term, row.id);
            if (row.base_item <= 0 || row.base_speed <= 0 ||
                row.base_str < 0 || row.base_dex < 0 || row.base_int < 0 ||
                row.base_wis < 0 || row.base_luk < 0 || row.base_con < 0)
                throw new InvalidOperationException($"Hero '{row.id}' has invalid stats.");
        }
        dataDict = index;
    }

    public HeroBaseDTO GetData(string id)
    {
        if (dataDict == null) MakeDictionary();
        return dataDict.TryGetValue(id, out var row) ? row : throw new KeyNotFoundException($"Hero '{id}' was not found.");
    }
}

public enum ItemCategory { Weapon = 1, Consumable = 2, Armor = 3, Material = 4 }
public enum ItemType { Sword = 1, Bow = 2, Staff = 3, HealthPotion = 10, ManaPotion = 11, Armor = 20, Material = 30 }

[Serializable]
public class ItemDTO : IDTO
{
    [Serializable]
    public class ItemBaseDTO
    {
        public int id;
        public string name_term;
        public string description_term;
        public int category;
        public int type;
        public int price;
        public int max_stack;
        // param_1: weapon attack, potion recovery, armor defense; param_2/3 reserved.
        public int param_1;
        public int param_2;
        public int param_3;
    }

    public List<ItemBaseDTO> data = new List<ItemBaseDTO>();
    [JsonIgnore] public Dictionary<int, ItemBaseDTO> dataDict;

    public void MakeDictionary()
    {
        var index = GameDataIndex.Create(data, row => row.id, "Item");
        foreach (var row in data)
        {
            GameDataIndex.RequireTerms(row.name_term, row.description_term, row.id.ToString());
            int expectedCategory = row.type <= 3 ? 1 : row.type <= 11 ? 2 : row.type == 20 ? 3 : 4;
            if (row.id <= 0 || row.price < 0 || row.max_stack < 1 || row.param_1 < 0 ||
                !Enum.IsDefined(typeof(ItemCategory), row.category) ||
                !Enum.IsDefined(typeof(ItemType), row.type) || row.category != expectedCategory)
                throw new InvalidOperationException($"Item '{row.id}' has invalid category, type or values.");
        }
        dataDict = index;
    }

    public ItemBaseDTO GetData(int id)
    {
        if (dataDict == null) MakeDictionary();
        return dataDict.TryGetValue(id, out var row) ? row : throw new KeyNotFoundException($"Item '{id}' was not found.");
    }
}

internal static class GameDataIndex
{
    public static Dictionary<TKey, TRow> Create<TKey, TRow>(List<TRow> rows, Func<TRow, TKey> keySelector, string table)
        where TRow : class
    {
        if (rows == null || rows.Count == 0)
            throw new InvalidOperationException($"{table}.data must contain at least one row.");
        var result = new Dictionary<TKey, TRow>();
        foreach (var row in rows)
        {
            if (row == null) throw new InvalidOperationException($"{table} contains a null row.");
            TKey key = keySelector(row);
            if (key == null || (key is string text && string.IsNullOrWhiteSpace(text)))
                throw new InvalidOperationException($"{table} contains an empty key.");
            if (result.ContainsKey(key))
                throw new InvalidOperationException($"{table} contains duplicate key '{key}'.");
            result.Add(key, row);
        }
        return result;
    }

    public static void RequireTerms(string name, string description, string id)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description))
            throw new InvalidOperationException($"Data row '{id}' requires name_term and description_term.");
    }
}
