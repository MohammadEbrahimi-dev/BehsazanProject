namespace Behsazan.Presentation.Services;

public class TokenAccessor
{
    public event EventHandler? TokenChanged;

    public string? CurrentToken { get; private set; }

    public bool HasToken => !string.IsNullOrWhiteSpace(CurrentToken);

    public void SetToken(string? token)
    {
        CurrentToken = token;
        TokenChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear() => SetToken(null);
}
