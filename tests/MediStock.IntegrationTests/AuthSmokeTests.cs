using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace MediStock.IntegrationTests;

public class AuthSmokeTests : IClassFixture<MediStockWebFactory>
{
    private readonly MediStockWebFactory _factory;
    public AuthSmokeTests(MediStockWebFactory factory) => _factory = factory;

    [Fact]
    public async Task LoginPage_IsReachable_AndContainsForm()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });
        var resp = await client.GetAsync("/Identity/Account/Login");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await resp.Content.ReadAsStringAsync();
        html.Should().Contain("Input.Email").And.Contain("Input.Password");
    }

    [Fact]
    public async Task ProtectedRoot_RedirectsToLogin_WhenAnonymous()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });
        var resp = await client.GetAsync("/");
        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resp.Headers.Location!.OriginalString.Should().Contain("/Identity/Account/Login");
    }

    [Fact]
    public async Task SignIn_WithSeededCashier_RedirectsToHome()
    {
        var client = _factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        // Get login page + extract antiforgery token
        var loginPage = await client.GetAsync("/Identity/Account/Login");
        var html = await loginPage.Content.ReadAsStringAsync();
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        token.Should().NotBeNullOrEmpty();

        // Submit login form
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = "cashier@medistock.local",
            ["Input.Password"] = "Cash@123",
            ["Input.RememberMe"] = "false",
            ["__RequestVerificationToken"] = token
        });
        var post = await client.PostAsync("/Identity/Account/Login", form);
        post.StatusCode.Should().Be(HttpStatusCode.Redirect);
        post.Headers.Location!.OriginalString.Should().Be("/");

        // Following the redirect should now reach the dashboard.
        var dashboard = await client.GetAsync("/");
        dashboard.StatusCode.Should().Be(HttpStatusCode.OK);
        var dash = await dashboard.Content.ReadAsStringAsync();
        dash.Should().Contain("Welcome").And.Contain("Cashier User");
    }
}
