using System.Net.Http.Json;
using System.Text.RegularExpressions;
using InvestmentGame.Shared.Models;

namespace InvestmentGame.Client.Services;

public class LanguageFactory
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, Dictionary<string, string>> _cache = new();
    private string _currentKey = "indonesia-casual";
    private const string FallbackKey = "english-casual";
    private bool _initialized;

    public LanguageFactory(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        var files = new[] { "english-casual", "english-formal-detail", "indonesia-casual", "indonesia-formal-detail" };
        foreach (var file in files)
        {
            try
            {
                var dict = await _httpClient.GetFromJsonAsync<Dictionary<string, string>>($"localization/{file}.json");
                _cache[file] = dict ?? new();
            }
            catch
            {
                _cache[file] = new();
            }
        }
        _initialized = true;
    }

    public void SetLanguage(Language language, AgeMode mode)
    {
        var lang = language == Language.Indonesian ? "indonesia" : "english";
        var tone = mode == AgeMode.Kids ? "casual" : "formal-detail";
        _currentKey = $"{lang}-{tone}";
    }

    public string T(string token)
    {
        // Before the locale files finish loading, return empty rather than
        // flashing the raw key name (e.g. "START_CONNECTING") to the user.
        if (!_initialized)
            return string.Empty;

        if (_cache.TryGetValue(_currentKey, out var currentDict) && currentDict.TryGetValue(token, out var text))
            return text;

        if (_currentKey != FallbackKey && _cache.TryGetValue(FallbackKey, out var fallbackDict) && fallbackDict.TryGetValue(token, out var fallback))
            return fallback;

        return token;
    }

    /// <summary>
    /// Localized sector name. Sector strings in the game data stay English because they double as
    /// the lookup key: "Heavy Equipment" -> SECTOR_HEAVY_EQUIPMENT, "Food &amp; Beverage" -> SECTOR_FOOD_BEVERAGE.
    /// Falls back to the raw English sector when no token exists, rather than showing the key.
    /// </summary>
    public string Sector(string sector)
    {
        if (string.IsNullOrWhiteSpace(sector))
            return string.Empty;

        var token = "SECTOR_" + Regex.Replace(sector.ToUpperInvariant(), "[^A-Z0-9]+", "_").Trim('_');
        var text = T(token);
        return text == token ? sector : text;
    }

    public string T(string token, Dictionary<string, object> vars)
    {
        var text = T(token);
        foreach (var (key, value) in vars)
            text = text.Replace($"{{{key}}}", value?.ToString() ?? "");
        return text;
    }
}
