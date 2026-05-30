namespace Gambling5.Web.Services;

public class LanguageService
{
    private string _current = "de";

    public event Action? OnChange;

    public string Current => _current;

    public void SetLanguage(string language)
    {
        var normalized = language?.ToLowerInvariant() switch
        {
            "en" => "en",
            "nl" => "nl",
            _ => "de"
        };

        if (normalized == _current)
        {
            return;
        }

        _current = normalized;
        OnChange?.Invoke();
    }

    public string T(string german, string english, string dutch)
    {
        return _current switch
        {
            "en" => english,
            "nl" => dutch,
            _ => german
        };
    }
}