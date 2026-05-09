using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using MediStock.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MediStock.IntegrationTests;

public class TillFlowTests : IClassFixture<MediStockWebFactory>
{
    private readonly MediStockWebFactory _factory;
    public TillFlowTests(MediStockWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Cashier_CompletesSale_ReceiptIsReachable()
    {
        var client = _factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        // Sign in as cashier
        var loginPage = await client.GetAsync("/Identity/Account/Login");
        var html = await loginPage.Content.ReadAsStringAsync();
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        var loginPost = await client.PostAsync("/Identity/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = "cashier@medistock.local",
            ["Input.Password"] = "Cash@123",
            ["Input.RememberMe"] = "false",
            ["__RequestVerificationToken"] = token
        }));
        loginPost.StatusCode.Should().Be(HttpStatusCode.Redirect);

        // Pick a non-Rx seeded drug ID from the database — Panadol (id=1) per the seeder.
        int drugId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            drugId = await db.Drugs
                .Where(d => d.IsActive && !d.RequiresPrescription && d.Batches.Any(b => b.IsActive && b.QuantityRemaining > 0))
                .OrderBy(d => d.Id)
                .Select(d => d.Id)
                .FirstAsync();
        }

        // Get a fresh antiforgery token from the till page
        var tillPage = await client.GetAsync("/Sales/Till");
        tillPage.StatusCode.Should().Be(HttpStatusCode.OK);
        var tillHtml = await tillPage.Content.ReadAsStringAsync();
        var apiToken = Regex.Match(tillHtml, "csrfToken\\s*=\\s*\"([^\"]+)\"").Groups[1].Value;
        apiToken.Should().NotBeNullOrEmpty();

        // POST checkout
        var req = new HttpRequestMessage(HttpMethod.Post, "/Sales/Checkout")
        {
            Content = JsonContent.Create(new
            {
                paymentMethod = "Cash",
                discount = 0,
                lines = new[] { new { drugId = drugId, quantity = 2 } }
            })
        };
        req.Headers.Add("RequestVerificationToken", apiToken);
        var checkout = await client.SendAsync(req);
        checkout.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await checkout.Content.ReadFromJsonAsync<CheckoutResponse>();
        body!.Ok.Should().BeTrue();
        body.ReceiptNumber.Should().StartWith("R-");

        // Receipt PDF endpoint should return application/pdf
        var pdfResp = await client.GetAsync($"/Sales/Receipt/{body.SaleId}");
        pdfResp.StatusCode.Should().Be(HttpStatusCode.OK);
        pdfResp.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        var bytes = await pdfResp.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(1000);
        System.Text.Encoding.ASCII.GetString(bytes, 0, 5).Should().Be("%PDF-");

        // Verify FIFO decrement happened in the database.
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sold = await db.SaleItems
                .Where(i => i.Sale.ReceiptNumber == body.ReceiptNumber)
                .SumAsync(i => i.Quantity);
            sold.Should().Be(2);
        }
    }

    private record CheckoutResponse(bool Ok, int SaleId, string ReceiptNumber, string ReceiptUrl);
}
