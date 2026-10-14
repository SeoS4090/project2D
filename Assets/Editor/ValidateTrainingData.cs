using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using ProjectAr.GameData;
using UnityEngine;

public static class ValidateTrainingData
{
    public static string Main()
    {
        var passed = new List<string>();
        Action<bool,string> check = (ok,label) => { if (!ok) throw new Exception(label); passed.Add(label); };
        string json = File.ReadAllText("Assets/Resources/GameData/Training/TrainingData.json");
        string manifest = File.ReadAllText("Assets/Resources/GameData/Training/manifest.json");
        var catalog = TrainingDataCatalog.Install(json, manifest);
        var profile = catalog.Get(TrainingLoadoutIds.TrainingGreatsword);
        check(profile.Steps.Count == 3 && profile.Steps[0].range_units == 2.3f && profile.Charge.mana_cost == 20f, "Generated profiles load; scene range and mana preserved");
        Action<string,Action<JObject>,bool> reject = (label, mutate, rehash) =>
        {
            var changed = JObject.Parse(json); mutate(changed);string bad = changed.ToString();
            var meta = JObject.Parse(manifest);
            if (rehash)
            {
                using (var sha = SHA256.Create()) meta["outputs"]["TrainingData.json"] = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(bad))).Replace("-", "").ToLowerInvariant();
            }
            bool rejected = false;
            try { TrainingDataCatalog.Install(bad, meta.ToString()); } catch (Exception) { rejected = true; }
            check(rejected && ReferenceEquals(catalog,TrainingDataCatalog.Current),label + "; previous catalog retained");
        };
        reject("Hash mismatch", d => d["schema_version"] = "changed", false);
        reject("Unknown schema", d => d["schema_version"] = "data-999", true);
        reject("Missing required numeric value", d => ((JObject)d["tables"]["ChargeProfile"][0]).Remove("mana_cost"), true);
        reject("Broken FK", d => d["tables"]["TrainingLoadout"][0]["combo_id"] = 999, true);
        reject("Duplicate key", d => ((JArray)d["tables"]["ChargeProfile"]).Add(d["tables"]["ChargeProfile"][0].DeepClone()), true);
        reject("Unsupported row state", d => d["tables"]["ChargeProfile"][0]["row_state"] = 1, true);
        reject("Step gap", d => d["tables"]["AttackStep"][1]["step_index"] = 4, true);
        reject("Invalid charge bounds", d => d["tables"]["ChargeProfile"][0]["damage_min"] = 1000, true);
        reject("String ID cannot be coerced", d => d["tables"]["TrainingLoadout"][0]["id"] = "1", true);
        reject("Fractional ID cannot be rounded", d => d["tables"]["TrainingLoadout"][0]["id"] = 1.2, true);
        reject("String enum cannot be coerced", d => d["tables"]["TrainingLoadout"][0]["row_state"] = "Test", true);
        var output = new JObject { ["passed"] = JArray.FromObject(passed), ["failure"] = null }.ToString();
        Directory.CreateDirectory("Library/GameDataMigration");File.WriteAllText("Library/GameDataMigration/RuntimeValidation.json",output);
        return output;
    }
}
