using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

public class Term : PersistentSingleton<Term>
{
    public static Term instance => Instance;
    [SerializeField] private string languageCode = "kor";
    [SerializeField] private string fallbackLanguageCode = "eng";
    public Dictionary<string, string> data = new Dictionary<string, string>(StringComparer.Ordinal);
    public string CurrentLanguage => languageCode;
    public bool IsLoaded { get; private set; }
    public event Action<string> LanguageChanged;
    private Dictionary<string, string> fallbackData = new Dictionary<string, string>(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<string, string>> cache = new Dictionary<string, Dictionary<string, string>>();
    private int languageRequest;
    private static readonly Regex Placeholder = new Regex(@"\{\{(?<key>[^{}]+)\}\}", RegexOptions.CultureInvariant);

    public Task InitializeAsync(string defaultLanguage, string fallbackLanguage)
    {
        fallbackLanguageCode = NormalizeLanguage(fallbackLanguage);
        string selected = PlayerPrefs.GetString(GamePreferences.LanguageKey, defaultLanguage);
        return ChangeLangAsync(selected, false);
    }

    // Compatibility entry points for Unity events; code that needs readiness should await the Task variants.
    public async void TermLoad()
    {
        try { await TermLoadAsync(); }
        catch (OperationCanceledException) { }
        catch (Exception exception) { Debug.LogException(exception, this); }
    }

    public Task TermLoadAsync() => ChangeLangAsync(languageCode, false);

    public void ChangeLang()
    {
        ChangeLang(Application.systemLanguage == SystemLanguage.Korean ? "kor" : "eng");
    }

    public async void ChangeLang(string newLanguageCode)
    {
        try { await ChangeLangAsync(newLanguageCode); }
        catch (OperationCanceledException) { }
        catch (Exception exception) { Debug.LogException(exception, this); }
    }

    public async Task ChangeLangAsync(string newLanguageCode, bool persist = true)
    {
        string selected = NormalizeLanguage(newLanguageCode);
        int requestId = ++languageRequest;
        var nextFallback = await LoadLanguageAsync(NormalizeLanguage(fallbackLanguageCode));
        var nextData = selected == NormalizeLanguage(fallbackLanguageCode)
            ? nextFallback : await LoadLanguageAsync(selected);

        ValidateTranslations(nextFallback, nextData);
        LifetimeToken.ThrowIfCancellationRequested();
        if (requestId != languageRequest)
            return;

        fallbackData = nextFallback;
        data = nextData;
        languageCode = selected;
        IsLoaded = true;
        if (persist)
        {
            PlayerPrefs.SetString(GamePreferences.LanguageKey, selected);
            PlayerPrefs.Save();
        }
        LanguageChanged?.Invoke(selected);
    }

    private async Task<Dictionary<string, string>> LoadLanguageAsync(string code)
    {
        if (cache.TryGetValue(code, out var cached))
            return cached;
        var loaded = await AddressableJsonLoader.LoadAsync<Dictionary<string, string>>("Term/term_" + code, LifetimeToken);
        if (loaded.Count == 0)
            throw new InvalidOperationException($"Term language '{code}' is empty.");
        foreach (var entry in loaded)
            if (string.IsNullOrWhiteSpace(entry.Key) || string.IsNullOrWhiteSpace(entry.Value))
                throw new InvalidOperationException($"Term language '{code}' has an empty key or value.");
        cache[code] = loaded;
        return loaded;
    }

    public bool TryGetTerm(string termkey, out string text)
    {
        if (string.IsNullOrWhiteSpace(termkey)) { text = null; return false; }
        return (data != null && data.TryGetValue(termkey, out text)) || fallbackData.TryGetValue(termkey, out text);
    }

    public string GetTerm(string termkey, params object[] parameters)
    {
        var replacements = new Dictionary<string, object>(StringComparer.Ordinal);
        if (parameters != null)
        {
            if (parameters.Length % 2 != 0)
                throw new ArgumentException("Pass placeholder name/value pairs.", nameof(parameters));
            for (int i = 0; i < parameters.Length; i += 2)
            {
                if (!(parameters[i] is string key) || string.IsNullOrWhiteSpace(key))
                    throw new ArgumentException($"Placeholder name at index {i} must be a non-empty string.");
                if (replacements.ContainsKey(key))
                    throw new ArgumentException($"Duplicate placeholder '{key}'.");
                replacements.Add(key, parameters[i + 1]);
            }
        }
        return GetTerm(termkey, replacements);
    }

    public string GetTerm(string termkey, IReadOnlyDictionary<string, object> parameters)
    {
        if (!TryGetTerm(termkey, out string text))
            return termkey ?? string.Empty;
        return Format(text, parameters, languageCode == "kor"
            ? CultureInfo.GetCultureInfo("ko-KR") : CultureInfo.GetCultureInfo("en-US"));
    }

    public static string Format(string text, IReadOnlyDictionary<string, object> parameters, IFormatProvider culture = null)
    {
        if (text == null) return string.Empty;
        if (parameters == null || parameters.Count == 0) return text;
        // One pass: braces supplied as a replacement value are never evaluated a second time.
        return Placeholder.Replace(text, match => parameters.TryGetValue(match.Groups["key"].Value, out var value)
            ? Convert.ToString(value, culture ?? CultureInfo.InvariantCulture) ?? string.Empty
            : match.Value);
    }

    public static void ValidateTranslations(Dictionary<string, string> reference, Dictionary<string, string> translations)
    {
        foreach (var pair in translations)
        {
            if (!reference.TryGetValue(pair.Key, out string source))
                continue;
            var expected = new HashSet<string>(StringComparer.Ordinal);
            var actual = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match match in Placeholder.Matches(source)) expected.Add(match.Groups["key"].Value);
            foreach (Match match in Placeholder.Matches(pair.Value)) actual.Add(match.Groups["key"].Value);
            if (!expected.SetEquals(actual))
                throw new InvalidOperationException($"Term '{pair.Key}' has different placeholders across languages.");
        }
    }

    public static string NormalizeLanguage(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Language code cannot be empty.", nameof(code));
        switch (code.Trim().ToLowerInvariant())
        {
            case "ko": case "ko-kr": case "kor": return "kor";
            case "en": case "en-us": case "en-gb": case "eng": return "eng";
            default:
                string normalized = code.Trim().ToLowerInvariant();
                if (!Regex.IsMatch(normalized, "^[a-z][a-z0-9_-]*$"))
                    throw new ArgumentException("Invalid language code.", nameof(code));
                return normalized;
        }
    }
}
