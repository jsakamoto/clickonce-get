using ClickOnceGet.Client.Components;
using MudBlazor;

namespace ClickOnceGet.Client;

public static class Extensions
{
    public static async ValueTask EnsureSuccessStatusCodeAsync(this HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var message = $"Server respond the status code {(int)response.StatusCode}";
            if (response.Content.Headers.ContentType.MediaType?.StartsWith("text") == true)
            {
                message = await response.Content.ReadAsStringAsync();
            }
            throw new HttpRequestException(message, null, response.StatusCode);
        }
    }

    public static Task ShowErrorMessageAsync(this IDialogService dialog, string message)
    {
        return dialog.Show<ErrorDialog>(title: "Error", new DialogParameters { { "Message", message } }).Result;
    }
}
