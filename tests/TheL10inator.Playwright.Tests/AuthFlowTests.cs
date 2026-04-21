using Microsoft.Playwright;
using Shouldly;

namespace TheL10inator.Playwright.Tests;

/// <summary>
/// Exercises the dev-bypass login happy path against the Docker Compose stack.
/// The stack is started by CI (or locally via <c>docker compose -f docker-compose.yml -f docker-compose.playwright.yml up</c>)
/// with <c>Authentication__UseDevBypass=true</c> and an <c>Administration__FirstAdminEmail</c>
/// that matches <see cref="SeededAdminEmail"/>.
/// </summary>
[Trait("Category", "Playwright")]
public class AuthFlowTests : IAsyncLifetime
{
    private const string SeededAdminEmail = "kevin.phifer@theoreticallyimpossible.org";

    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;

    public async Task InitializeAsync()
    {
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
    }

    public async Task DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }

    [Fact]
    public async Task Dev_bypass_login_with_seeded_admin_email_reaches_dashboard()
    {
        var baseUrl = Environment.GetEnvironmentVariable("THEL10INATOR_BASE_URL") ?? "http://localhost";

        await using var context = await _browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync(baseUrl);

        // Unauthenticated root should redirect to /login.
        await page.WaitForURLAsync(url => url.Contains("/login", StringComparison.OrdinalIgnoreCase));

        var emailInput = page.GetByLabel("Email");
        await emailInput.FillAsync(SeededAdminEmail);

        await page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        await page.WaitForURLAsync(url => url.Contains("/dashboard", StringComparison.OrdinalIgnoreCase));

        var welcome = page.GetByText("Welcome,", new() { Exact = false });
        (await welcome.CountAsync()).ShouldBeGreaterThan(0);

        var teamLine = page.GetByText("Team: Leadership", new() { Exact = false });
        (await teamLine.CountAsync()).ShouldBeGreaterThan(0);
    }
}
