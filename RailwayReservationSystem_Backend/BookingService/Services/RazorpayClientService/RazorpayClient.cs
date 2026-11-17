using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using BookingService.Exceptions;

namespace BookingService.Services.RazorpayClientService;

public sealed class RazorpayClient : IRazorpayClient
{
    private readonly HttpClient _httpClient;
    private readonly string? _keyId;
    private readonly string? _keySecret;

    public RazorpayClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _keyId = configuration["RAZORPAY_KEY_ID"] ?? configuration["Razorpay:KeyId"];
        _keySecret = configuration["RAZORPAY_KEY_SECRET"] ?? configuration["Razorpay:KeySecret"];
    }

    public string KeyId => RequireSetting(_keyId, "RAZORPAY_KEY_ID");

    public bool VerifySignature(string orderId, string paymentId, string signature)
    {
        var secret = RequireSetting(_keySecret, "RAZORPAY_KEY_SECRET");
        var payload = Encoding.UTF8.GetBytes($"{orderId}|{paymentId}");
        var expectedSignature = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), payload);
        byte[] suppliedSignature;
        try
        {
            suppliedSignature = Convert.FromHexString(signature);
        }
        catch (FormatException)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expectedSignature, suppliedSignature);
    }

    public async Task<RazorpayOrderResult> CreateOrderAsync(long amount, string receipt, object notes)
    {
        using var request = CreateRequest(HttpMethod.Post, "orders");
        request.Content = JsonContent.Create(new { amount, currency = "INR", receipt, notes });
        using var response = await _httpClient.SendAsync(request);
        await EnsureSuccessAsync(response);

        var order = await response.Content.ReadFromJsonAsync<RazorpayOrderResponse>()
            ?? throw new RazorpayProviderException("Razorpay returned an empty order response.");
        return new RazorpayOrderResult(order.Id, order.Amount, order.Currency);
    }

    public async Task<RazorpayPaymentDetails> GetPaymentAsync(string paymentId)
    {
        using var request = CreateRequest(HttpMethod.Get, $"payments/{Uri.EscapeDataString(paymentId)}");
        using var response = await _httpClient.SendAsync(request);
        await EnsureSuccessAsync(response);
        return await ReadPaymentAsync(response);
    }

    public async Task<RazorpayPaymentDetails> CapturePaymentAsync(string paymentId, long amount, string currency)
    {
        using var request = CreateRequest(HttpMethod.Post, $"payments/{Uri.EscapeDataString(paymentId)}/capture");
        request.Content = JsonContent.Create(new { amount, currency });
        using var response = await _httpClient.SendAsync(request);
        await EnsureSuccessAsync(response);
        return await ReadPaymentAsync(response);
    }

    public async Task RefundPaymentAsync(string paymentId, long amount)
    {
        using var request = CreateRequest(HttpMethod.Post, $"payments/{Uri.EscapeDataString(paymentId)}/refund");
        request.Content = JsonContent.Create(new { amount });
        using var response = await _httpClient.SendAsync(request);
        await EnsureSuccessAsync(response);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            $"{RequireSetting(_keyId, "RAZORPAY_KEY_ID")}:{RequireSetting(_keySecret, "RAZORPAY_KEY_SECRET")}"));
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        return request;
    }

    private static string RequireSetting(string? value, string name) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new RazorpayConfigurationException($"Razorpay is not configured. Set {name} in BookingService/.env.");

    private static Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            throw new RazorpayProviderException($"Razorpay request failed with status {(int)response.StatusCode}.");
        return Task.CompletedTask;
    }

    private static async Task<RazorpayPaymentDetails> ReadPaymentAsync(HttpResponseMessage response)
    {
        var payment = await response.Content.ReadFromJsonAsync<RazorpayPaymentResponse>()
            ?? throw new RazorpayProviderException("Razorpay returned an empty payment response.");
        return new RazorpayPaymentDetails(payment.Id, payment.OrderId, payment.Amount, payment.Currency, payment.Status);
    }

    private sealed class RazorpayOrderResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;
    }

    private sealed class RazorpayPaymentResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("order_id")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}