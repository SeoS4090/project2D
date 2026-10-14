using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ProjectAr.GameData
{
    /// <summary>Immutable resolved training definitions. A failed load never replaces a valid catalog.</summary>
    public sealed class TrainingLoadoutSnapshot
    {
        public MovementProfileRow Movement { get; }
        public ManaProfileRow Mana { get; }
        public ComboProfileRow Combo { get; }
        public ChargeProfileRow Charge { get; }
        public IReadOnlyList<AttackStepRow> Steps { get; }
        internal TrainingLoadoutSnapshot(MovementProfileRow movement, ManaProfileRow mana,
            ComboProfileRow combo, ChargeProfileRow charge, AttackStepRow[] steps)
        { Movement = movement; Mana = mana; Combo = combo; Charge = charge; Steps = Array.AsReadOnly(steps); }
    }

    public sealed class TrainingDataCatalog
    {
        public const string SchemaVersion = "data-1.0";
        private readonly Dictionary<int, TrainingLoadoutSnapshot> loadouts;
        private static TrainingDataCatalog current;
        public static TrainingDataCatalog Current => current ?? LoadResources();
        private TrainingDataCatalog(Dictionary<int, TrainingLoadoutSnapshot> values) { loadouts = values; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() { current = null; }

        public TrainingLoadoutSnapshot Get(TrainingLoadoutId id)
        {
            if (!loadouts.TryGetValue(id.Value, out var value))
                throw new InvalidOperationException("Unknown TrainingLoadout ID: " + id.Value);
            return value;
        }

        private static TrainingDataCatalog LoadResources()
        {
            var json = Resources.Load<TextAsset>("GameData/Training/TrainingData");
            var manifest = Resources.Load<TextAsset>("GameData/Training/manifest");
            if (json == null || manifest == null) throw new InvalidOperationException("Generated training data is missing. Run Tools/GameData/data_pipeline.py.");
            return Install(json.text, manifest.text);
        }

        /// <summary>Call only at a safe scene boundary. Controllers retain their original snapshot.</summary>
        public static TrainingDataCatalog Install(string json, string manifest)
        {
            var candidate = Parse(json, manifest);
            current = candidate;
            return candidate;
        }

        public static TrainingDataCatalog Parse(string json, string manifest)
        {
            var metadata = JObject.Parse(manifest);
            if ((string)metadata["schema_version"] != SchemaVersion)
                throw new InvalidOperationException("Unsupported training manifest schema.");
            using (var sha = SHA256.Create())
            {
                var hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(json))).Replace("-", "").ToLowerInvariant();
                if (hash != (string)metadata["outputs"]?["TrainingData.json"])
                    throw new InvalidOperationException("Training data hash does not match its manifest.");
            }
            var doc = JsonConvert.DeserializeObject<TrainingDataDocument>(json,
                new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error,
                    Converters = { new StrictNumberConverter() } });
            if (doc == null || doc.schema_version != SchemaVersion || doc.tables == null)
                throw new InvalidOperationException("Unsupported training data schema.");
            var t = doc.tables;
            var moves = Index(t.MovementProfile, r => r.id, r => r.Validate());
            var mana = Index(t.ManaProfile, r => r.id, r => r.Validate());
            var combos = Index(t.ComboProfile, r => r.id, r => r.Validate());
            var charges = Index(t.ChargeProfile, r => r.id, r => r.Validate());
            var roots = Index(t.TrainingLoadout, r => r.id, r => r.Validate());
            if (t.AttackStep == null) throw new InvalidOperationException("AttackStep missing.");
            foreach (var step in t.AttackStep)
            {
                if (step == null) throw new InvalidOperationException("Null attack step.");
                step.Validate();
                if (!combos.ContainsKey(step.combo_id)) throw new InvalidOperationException("AttackStep combo FK missing.");
            }
            foreach (var charge in charges.Values)
                if (charge.damage_min > charge.damage_max || charge.move_ratio > 1f)
                    throw new InvalidOperationException("Invalid charge bounds.");
            var stepsByCombo = new Dictionary<int, AttackStepRow[]>();
            foreach (var combo in combos.Values)
            {
                var steps = t.AttackStep.Where(r => r.combo_id == combo.id).OrderBy(r => r.step_index).ToArray();
                if (steps.Length == 0 || steps.Where((s, i) => s.step_index != i + 1).Any())
                    throw new InvalidOperationException("Combo step sequence is empty or noncontiguous.");
                stepsByCombo.Add(combo.id, steps);
            }
            var result = new Dictionary<int, TrainingLoadoutSnapshot>();
            foreach (var root in roots.Values)
            {
                if (!moves.TryGetValue(root.movement_id, out var move) || !mana.TryGetValue(root.mana_id, out var mp)
                    || !combos.TryGetValue(root.combo_id, out var combo) || !charges.TryGetValue(root.charge_id, out var charge))
                    throw new InvalidOperationException("TrainingLoadout profile FK missing.");
                result.Add(root.id, new TrainingLoadoutSnapshot(move, mp, combo, charge, stepsByCombo[combo.id]));
            }
            if (result.Count == 0) throw new InvalidOperationException("No training loadouts.");
            return new TrainingDataCatalog(result);
        }

        private static Dictionary<int, T> Index<T>(T[] rows, Func<T, int> key, Action<T> validate) where T : class
        {
            if (rows == null) throw new InvalidOperationException(typeof(T).Name + " table missing.");
            var result = new Dictionary<int, T>();
            foreach (var row in rows)
            {
                if (row == null) throw new InvalidOperationException("Null " + typeof(T).Name);
                validate(row);
                if (result.ContainsKey(key(row))) throw new InvalidOperationException("Duplicate " + typeof(T).Name + " ID.");
                result.Add(key(row), row);
            }
            return result;
        }

        // Json.NET otherwise accepts strings and rounds fractional values for integer fields.
        private sealed class StrictNumberConverter : JsonConverter
        {
            public override bool CanConvert(Type type) => type == typeof(int) || type == typeof(float) || type.IsEnum;
            public override bool CanWrite => false;
            public override object ReadJson(JsonReader reader, Type type, object existing, JsonSerializer serializer)
            {
                bool integer = type != typeof(float);
                if (reader.TokenType != JsonToken.Integer && (integer || reader.TokenType != JsonToken.Float))
                    throw new JsonSerializationException(reader.Path + ": expected " + (integer ? "integer" : "number"));
                if (type == typeof(float)) return Convert.ToSingle(reader.Value, System.Globalization.CultureInfo.InvariantCulture);
                int value = Convert.ToInt32(reader.Value, System.Globalization.CultureInfo.InvariantCulture);
                return type.IsEnum ? Enum.ToObject(type, value) : (object)value;
            }
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotSupportedException();
        }
    }
}
