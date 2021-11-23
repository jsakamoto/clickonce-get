using ClickOnceGet.Server.Services;
using ClickOnceGet.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;

namespace ClickOnceGet.Server.Controllers;

public class FallbackViewsController : Controller
{
    private HttpsRedirecter HttpsRedirecter { get; }

    private PersistentComponentState ApplicationState { get; }

    private PersistingComponentStateSubscription? PersistingSubscription { get; set; }

    public FallbackViewsController(
        HttpsRedirecter httpsRedirecter,
        PersistentComponentState applicationState)
    {
        this.HttpsRedirecter = httpsRedirecter;
        this.ApplicationState = applicationState;
    }

    public IActionResult Index()
    {
        if (this.HttpsRedirecter.ShouldRedirect(this.Request, out var actionResult)) return actionResult;

        var name = this.User.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? this.User.Identity?.Name ?? "";
        var authUserInfo = new AuthUserInfo { Name = name };
        this.ApplicationState.RegisterOnPersisting(() =>
        {
            this.ApplicationState.PersistAsJson("AuthUserInfo", authUserInfo);
            return Task.CompletedTask;
        });

        return this.View();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            this.PersistingSubscription?.Dispose();
        }
    }
}
