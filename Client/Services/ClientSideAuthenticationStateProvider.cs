#nullable enable
using System.Net.Http.Json;
using System.Security.Claims;
using ClickOnceGet.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace ClickOnceGet.Client.Services;

public class ClientSideAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient HttpClient;

    private readonly PersistentComponentState ApplicationState;

    public ClientSideAuthenticationStateProvider(
        HttpClient httpClient,
        PersistentComponentState applicationState)
    {
        this.HttpClient = httpClient;
        this.ApplicationState = applicationState;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!this.ApplicationState.TryTakeFromJson<AuthUserInfo>("AuthUserInfo", out var user))
        {
            user = await this.HttpClient.GetFromJsonAsync<AuthUserInfo>("/api/auth/currentuser");
        }

        var identity = default(ClaimsIdentity);
        if (!string.IsNullOrEmpty(user?.Name))
        {
            identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, user.Name) }, authenticationType: "GitHub");
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
