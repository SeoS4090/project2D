using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Networking;

public static class GameDataSheetSync
{
    public const string GameSheetUrl = "https://docs.google.com/spreadsheets/d/1rYGTqYCwHdqO4_y2u1RObh84yTLCMqN06LW-Y5AzYuY/edit";
    public const string TermSheetUrl = "https://docs.google.com/spreadsheets/d/1djUPaQAYoDPs4SnhAD2SjWl25r33gyQrdJRK1KxGk14/edit";
    public const string GameDataFolder = "Assets/AddressableAssetsData/GameData";
    public const string TermFolder = "Assets/AddressableAssetsData/Term";
    public const string SnapshotPath = "Library/GameDataSheetsSnapshot.json";
    private static bool syncing;

    [MenuItem("Tools/Game Data/Sync Published Google Sheets")]
    private static async void SyncMenu()
    {
        try { await SyncAllAsync(); Debug.Log(ValidateLocal()); }
        catch (Exception exception) { Debug.LogException(exception); }
    }

    [MenuItem("Tools/Game Data/Import Connected Sheets Snapshot")]
    public static void ImportConnectedSnapshot()
    {
        if (syncing) throw new InvalidOperationException("A Google Sheets sync is already running.");
        if (!File.Exists(SnapshotPath))
            throw new FileNotFoundException("Use the connected Google Drive tools to refresh the Sheets snapshot.", SnapshotPath);
        var snapshot = JObject.Parse(File.ReadAllText(SnapshotPath));
        if ((int?)snapshot["format_version"] != 1)
            throw new InvalidDataException("Unsupported Sheets snapshot version.");
        var files = new Dictionary<string, string>();
        foreach (string table in new[] { "CommonInstant", "Item", "Hero" })
        {
            var rows = snapshot["tables"]?[table] as JArray;
            if (rows == null) throw new InvalidDataException("Snapshot is missing table " + table);
            files.Add(GameDataFolder + "/" + table + ".json", ConvertGameTable(table, RowsToCsv(rows)));
        }
        var termRows = snapshot["terms"] as JArray;
        if (termRows == null) throw new InvalidDataException("Snapshot is missing terms.");
        foreach (var pair in ConvertTerms(RowsToCsv(termRows)))
            files.Add(TermFolder + "/term_" + pair.Key + ".json", pair.Value);
        ValidateDocuments(files);
        WriteDocuments(files);
        Debug.Log("Imported connected Sheets snapshot: " + (string)snapshot["exported_at_utc"] + ". " + ValidateLocal());
    }

    private static string RowsToCsv(JArray rows)
    {
        return string.Join("\n", rows.Select(row => string.Join(",", ((JArray)row).Select(value =>
            "\"" + Convert.ToString(((JValue)value).Value, CultureInfo.InvariantCulture).Replace("\"", "\"\"") + "\""))));
    }

    public static async Task SyncAllAsync()
    {
        if (syncing) throw new InvalidOperationException("A Google Sheets sync is already running.");
        syncing = true;
        try
        {
            var files = new Dictionary<string, string>();
            foreach (string table in new[] { "CommonInstant", "Item", "Hero" })
                files.Add(GameDataFolder + "/" + table + ".json", ConvertGameTable(table, await DownloadCsvAsync(GameSheetUrl, table)));
            foreach (var pair in ConvertTerms(await DownloadCsvAsync(TermSheetUrl, "term")))
                files.Add(TermFolder + "/term_" + pair.Key + ".json", pair.Value);
            ValidateDocuments(files);
            WriteDocuments(files);
        }
        finally { syncing = false; }
    }

    public static async Task SyncGameTableAsync(string sheetUrl, string table)
    {
        if (syncing) throw new InvalidOperationException("A Google Sheets sync is already running.");
        syncing = true;
        try
        {
            string json = ConvertGameTable(table, await DownloadCsvAsync(sheetUrl, table));
            var files = ReadLocalDocuments();
            files[GameDataFolder + "/" + table + ".json"] = json;
            ValidateDocuments(files);
            WriteDocuments(files);
        }
        finally { syncing = false; }
    }

    public static async Task SyncTermsAsync(string sheetUrl, string tab)
    {
        if (syncing) throw new InvalidOperationException("A Google Sheets sync is already running.");
        syncing = true;
        try
        {
            var files = ReadLocalDocuments();
            foreach (var pair in ConvertTerms(await DownloadCsvAsync(sheetUrl, tab)))
                files[TermFolder + "/term_" + pair.Key + ".json"] = pair.Value;
            ValidateDocuments(files);
            WriteDocuments(files);
        }
        finally { syncing = false; }
    }

    private static async Task<string> DownloadCsvAsync(string sheetUrl, string tab)
    {
        var match = Regex.Match(sheetUrl ?? "", @"^https://docs\.google\.com/spreadsheets/d/([A-Za-z0-9_-]+)");
        if (!match.Success) throw new ArgumentException("Enter a Google Sheets URL.");
        string url = "https://docs.google.com/spreadsheets/d/" + match.Groups[1].Value
            + "/gviz/tq?tqx=out:csv&sheet=" + Uri.EscapeDataString(tab);
        using (var request = UnityWebRequest.Get(url))
        {
            request.timeout = 30;
            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();
            if (request.result != UnityWebRequest.Result.Success)
                throw new IOException($"Cannot download tab '{tab}': {request.error}. The editor CSV feed requires read access without sign-in.");
            string text = request.downloadHandler.text.TrimStart('\uFEFF');
            if (text.TrimStart().StartsWith("<", StringComparison.Ordinal))
                throw new IOException($"Tab '{tab}' returned HTML instead of CSV. Check the URL and CSV read access.");
            return text;
        }
    }

    public static string ConvertGameTable(string table, string csv)
    {
        var rows = ParseCsv(csv);
        if (rows.Count < 2) throw new InvalidDataException(table + " has no data rows.");
        var headers = ReadHeaders(rows[0]);
        var data = new JArray();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        Type rowType = table == "Item" ? typeof(ItemDTO.ItemBaseDTO)
            : table == "Hero" ? typeof(HeroDTO.HeroBaseDTO) : null;
        if (rowType == null && table != "CommonInstant")
            throw new InvalidDataException("Register a DTO schema before importing table '" + table + "'.");

        for (int i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            string key = Cell(row, headers, table == "CommonInstant" ? "key" : "id");
            if (string.IsNullOrWhiteSpace(key)) throw new InvalidDataException($"{table} row {i + 1} has an empty key.");
            if (!seen.Add(key)) throw new InvalidDataException($"{table} has duplicate key '{key}'.");
            var item = new JObject();
            if (table == "CommonInstant")
            {
                item["key"] = key;
                string type = Cell(row, headers, "type").Trim().ToLowerInvariant();
                string value = Cell(row, headers, "value");
                switch (type)
                {
                    case "int": item["value"] = int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture); break;
                    case "float":
                        double number = double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
                        if (double.IsNaN(number) || double.IsInfinity(number)) throw new InvalidDataException("Non-finite value: " + key);
                        item["value"] = number; break;
                    case "bool": item["value"] = bool.Parse(value); break;
                    case "string": item["value"] = value; break;
                    case "list":
                        try
                        {
                            // Preserve JSON element types for GetData<List<T>>() and GetData<T[]>().
                            item["value"] = JArray.Parse(value);
                        }
                        catch (JsonException exception)
                        {
                            throw new InvalidDataException(
                                $"CommonInstant '{key}' (row {i + 1}) requires a JSON array for type 'list'.",
                                exception);
                        }
                        break;
                    default: throw new InvalidDataException($"Unknown CommonInstant type '{type}' for '{key}'.");
                }
            }
            else
            {
                foreach (var field in rowType.GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    string value = Cell(row, headers, field.Name);
                    item[field.Name] = field.FieldType == typeof(int)
                        ? new JValue(int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture))
                        : new JValue(value);
                }
            }
            data.Add(item);
        }
        return new JObject { ["data"] = data }.ToString(Formatting.Indented);
    }

    public static Dictionary<string, string> ConvertTerms(string csv)
    {
        var rows = ParseCsv(csv);
        if (rows.Count < 2) throw new InvalidDataException("Term sheet has no data.");
        var headers = ReadHeaders(rows[0]);
        if (!headers.ContainsKey("key")) throw new InvalidDataException("Term sheet requires a key column.");
        var result = new Dictionary<string, string>();
        var dictionaries = new Dictionary<string, Dictionary<string, string>>();
        foreach (string header in headers.Keys.Where(key => key != "key"))
        {
            string code = Term.NormalizeLanguage(header);
            if (dictionaries.ContainsKey(code)) throw new InvalidDataException("Duplicate language alias: " + code);
            var terms = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 1; i < rows.Count; i++)
            {
                string key = Cell(rows[i], headers, "key");
                string value = Cell(rows[i], headers, header);
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                    throw new InvalidDataException($"Term row {i + 1}, language '{header}' is empty.");
                if (terms.ContainsKey(key)) throw new InvalidDataException("Duplicate term key: " + key);
                terms.Add(key, value);
            }
            dictionaries.Add(code, terms);
            result.Add(code, JsonConvert.SerializeObject(terms, Formatting.Indented));
        }
        if (dictionaries.Count == 0) throw new InvalidDataException("Term sheet has no language columns.");
        var reference = dictionaries.Values.First();
        foreach (var terms in dictionaries.Values) Term.ValidateTranslations(reference, terms);
        return result;
    }

    private static Dictionary<string, int> ReadHeaders(List<string> row)
    {
        var headers = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < row.Count; i++)
        {
            string header = row[i].Trim();
            if (string.IsNullOrEmpty(header)) continue;
            if (headers.ContainsKey(header)) throw new InvalidDataException("Duplicate header: " + header);
            headers.Add(header, i);
        }
        return headers;
    }

    private static string Cell(List<string> row, Dictionary<string, int> headers, string key)
    {
        if (!headers.TryGetValue(key, out int index))
            throw new InvalidDataException("Missing column: " + key);
        return index < row.Count ? row[index] : string.Empty;
    }

    public static List<List<string>> ParseCsv(string csv)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        bool quoted = false;
        string input = csv.TrimStart('\uFEFF');
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < input.Length && input[i + 1] == '"') { field.Append('"'); i++; }
                else if (c == '"') quoted = false;
                else field.Append(c);
            }
            else if (c == '"' && field.Length == 0) quoted = true;
            else if (c == ',') { row.Add(field.ToString()); field.Clear(); }
            else if (c == '\r' || c == '\n')
            {
                if (c == '\r' && i + 1 < input.Length && input[i + 1] == '\n') i++;
                row.Add(field.ToString()); field.Clear();
                if (row.Any(value => !string.IsNullOrWhiteSpace(value))) rows.Add(row);
                row = new List<string>();
            }
            else field.Append(c);
        }
        if (quoted) throw new InvalidDataException("CSV contains an unclosed quoted field.");
        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            if (row.Any(value => !string.IsNullOrWhiteSpace(value))) rows.Add(row);
        }
        return rows;
    }

    private static Dictionary<string, string> ReadLocalDocuments()
    {
        var files = new Dictionary<string, string>();
        foreach (string folder in new[] { GameDataFolder, TermFolder })
            if (Directory.Exists(folder))
                foreach (string path in Directory.GetFiles(folder, "*.json"))
                    files[path.Replace('\\', '/')] = File.ReadAllText(path);
        return files;
    }

    private static void WriteDocuments(Dictionary<string, string> files)
    {
        // Validate every document before replacing any of the last known good files.
        ValidateDocuments(files);
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var pair in files)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(pair.Key));
                File.WriteAllText(pair.Key, pair.Value, new UTF8Encoding(false));
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.Refresh();
        RegisterAddressables();
    }

    public static string ValidateDocuments(Dictionary<string, string> files)
    {
        T Read<T>(string path) => JsonConvert.DeserializeObject<T>(files[path]);
        var common = Read<CommmonInstantDTO>(GameDataFolder + "/CommonInstant.json");
        var item = Read<ItemDTO>(GameDataFolder + "/Item.json");
        var hero = Read<HeroDTO>(GameDataFolder + "/Hero.json");
        if (common == null || item == null || hero == null) throw new InvalidDataException("A data table is null.");
        common.MakeDictionary(); item.MakeDictionary(); hero.MakeDictionary();
        GameDataDTO.ValidateReferences(common, item, hero);
        string fallback = Term.NormalizeLanguage(common.GetData<string>("fallback_language"));
        string primary = Term.NormalizeLanguage(common.GetData<string>("default_language"));
        var reference = Read<Dictionary<string, string>>(TermFolder + "/term_" + fallback + ".json");
        if (!files.ContainsKey(TermFolder + "/term_" + primary + ".json"))
            throw new InvalidDataException("Default language JSON is missing.");
        var required = item.data.SelectMany(row => new[] { row.name_term, row.description_term })
            .Concat(hero.data.SelectMany(row => new[] { row.name_term, row.description_term }));
        foreach (var pair in files.Where(pair => pair.Key.StartsWith(TermFolder + "/", StringComparison.Ordinal)))
        {
            var terms = JsonConvert.DeserializeObject<Dictionary<string, string>>(pair.Value);
            if (terms == null || terms.Count == 0 || terms.Any(entry => string.IsNullOrWhiteSpace(entry.Key) || string.IsNullOrWhiteSpace(entry.Value)))
                throw new InvalidDataException(pair.Key + " has empty terms.");
            foreach (string key in required)
                if (!terms.ContainsKey(key)) throw new InvalidDataException(pair.Key + " is missing term " + key);
            Term.ValidateTranslations(reference, terms);
        }
        return $"Validated: {common.data.Count} constants, {item.data.Count} items, {hero.data.Count} heroes, {reference.Count} terms per language.";
    }

    [MenuItem("Tools/Game Data/Register Addressable JSON")]
    public static void RegisterAddressables()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) throw new InvalidOperationException("Addressables settings are missing.");
        foreach (var folder in new[] { GameDataFolder, TermFolder })
        {
            string groupName = folder == GameDataFolder ? "GameData" : "Term";
            var group = settings.FindGroup(groupName) ?? settings.CreateGroup(groupName, false, false, true, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            foreach (string rawPath in Directory.GetFiles(folder, "*.json"))
            {
                string path = rawPath.Replace('\\', '/');
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid)) throw new InvalidOperationException("Import this asset before registering: " + path);
                string address = groupName + "/" + Path.GetFileNameWithoutExtension(path);
                var conflict = settings.groups.Where(g => g != null).SelectMany(g => g.entries)
                    .FirstOrDefault(e => e.address == address && e.guid != guid);
                if (conflict != null) throw new InvalidOperationException("Duplicate Addressable key: " + address);
                var entry = settings.CreateOrMoveEntry(guid, group);
                entry.SetAddress(address);
                entry.SetLabel(groupName, true, true);
            }
        }
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Game Data/Validate Game Data")]
    private static void ValidateMenu() => Debug.Log(ValidateLocal());

    public static string ValidateLocal()
    {
        var files = ReadLocalDocuments();
        string result = ValidateDocuments(files);
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) throw new InvalidOperationException("Addressables settings are missing.");
        foreach (string path in files.Keys)
        {
            string group = path.StartsWith(GameDataFolder + "/", StringComparison.Ordinal) ? "GameData" : "Term";
            string address = group + "/" + Path.GetFileNameWithoutExtension(path);
            var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
            if (entry == null || entry.address != address || entry.parentGroup.Name != group)
                throw new InvalidOperationException("Addressable registration does not match: " + path);
        }
        return result;
    }
}

public sealed class GameDataBuildValidator : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report)
    {
        try { GameDataSheetSync.ValidateLocal(); }
        catch (Exception exception) { throw new BuildFailedException("Game data validation failed: " + exception.Message); }
    }
}
