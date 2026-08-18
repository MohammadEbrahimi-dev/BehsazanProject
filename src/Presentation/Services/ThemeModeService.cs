using Microsoft.JSInterop;

namespace Behsazan.Presentation.Services;

public sealed class ThemeModeService
{
    public const string StorageKey = "behsazan.theme.dark";

    public bool IsDarkMode { get; private set; }

    public event Action? Changed;

    public void SetDarkMode(bool isDark)
    {
        if (IsDarkMode == isDark)
            return;

        IsDarkMode = isDark;
        Changed?.Invoke();
    }

    public void Toggle() => SetDarkMode(!IsDarkMode);

    public async Task RestoreAsync(IJSRuntime js, CancellationToken cancellationToken = default)
    {
        try
        {
            var raw = await js.InvokeAsync<string?>("localStorage.getItem", cancellationToken, StorageKey);
            if (string.Equals(raw, "1", StringComparison.Ordinal) ||
                string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase))
            {
                SetDarkMode(true);
            }
            else if (string.Equals(raw, "0", StringComparison.Ordinal) ||
                     string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase))
            {
                SetDarkMode(false);
            }
        }
        catch
        {
        }
    }

    public async Task PersistAsync(IJSRuntime js, CancellationToken cancellationToken = default)
    {
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", cancellationToken, StorageKey, IsDarkMode ? "1" : "0");
        }
        catch
        {
        }
    }
}
