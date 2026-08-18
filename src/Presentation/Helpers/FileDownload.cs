using Behsazan.Application.DTOs;
using Microsoft.JSInterop;

namespace Behsazan.Presentation.Helpers;

public static class FileDownload
{
    public static async Task SaveAsync(IJSRuntime js, FileDownloadDto file)
    {
        ArgumentNullException.ThrowIfNull(file);

        var base64 = Convert.ToBase64String(file.Content);
        await js.InvokeVoidAsync(
            "behsazanDownloadFile",
            file.FileName,
            file.ContentType,
            base64);
    }
}
