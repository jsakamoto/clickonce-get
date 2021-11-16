#nullable enable
using System.Net.Http.Json;
using System.Security.Claims;
using ClickOnceGet.Shared.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace ClickOnceGet.Client.Services;

public class ClientSideAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient HttpClient;

    public ClientSideAuthenticationStateProvider(HttpClient httpClient)
    {
        this.HttpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = await this.HttpClient.GetFromJsonAsync<AuthUserInfo>("/api/auth/currentuser");
        var identity = default(ClaimsIdentity);
        if (!string.IsNullOrEmpty(user?.Name))
        {
            identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, user.Name) }, authenticationType: "Twitter");
        }
        return new AuthenticationState(new ClaimsPrincipal(identity ?? new ClaimsIdentity()));
    }

    public Task<AuthenticationState> RefreshAsync()
    {
        var stateAsync = this.GetAuthenticationStateAsync();
        this.NotifyAuthenticationStateChanged(stateAsync);
        return stateAsync;
    }
}
