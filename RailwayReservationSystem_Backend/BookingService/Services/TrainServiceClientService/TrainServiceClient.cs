using System.Net.Http.Json;
using Microsoft.Identity.Client;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using BookingService.DTOs;

namespace BookingService.Services.TrainServiceClientService;

public class TrainServiceClient : ITrainServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TrainServiceClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<decimal?> GetFareAsync(int trainId, string classType)
    {
        AddAuthorizationHeader();

        var response = await _httpClient.GetAsync(
            $"/api/trains/{trainId}/fare");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"TrainService returned {(int) response.StatusCode} - {response.ReasonPhrase}");
        }

        var fares = await response.Content.ReadFromJsonAsync<List<FareResponse>>();

        var fare = fares?.FirstOrDefault(f => f.ClassType.Equals(classType,
        StringComparison.OrdinalIgnoreCase));

        return fare?.Amount;
    }

    public async Task<List<SeatResponse>> GetAvailableSeatsAsync(
    int trainId,
    int scheduleId,
    string classType,
    DateOnly journeyDate,
    decimal startDistanceKm,
    decimal endDistanceKm)
    {
        AddAuthorizationHeader();

        var response = await _httpClient.GetAsync(
            $"/api/trains/{trainId}/seats" +
            $"?scheduleId={scheduleId}" +
            $"&journeyDate={journeyDate:yyyy-MM-dd}" +
            $"&startDistanceKm={startDistanceKm}" +
            $"&endDistanceKm={endDistanceKm}");

        if (!response.IsSuccessStatusCode)
            return new List<SeatResponse>();

        var seats =
            await response.Content.ReadFromJsonAsync<List<SeatResponse>>();

        return seats?
            .Where(s => s.IsAvailable &&
                        s.ClassType.Equals(
                            classType,
                            StringComparison.OrdinalIgnoreCase))
            .ToList()
            ?? new List<SeatResponse>();
    }

    public async Task<bool> ReserveSeatAsync(
    int seatId,
    DateOnly journeyDate,
    decimal startDistanceKm,
    decimal endDistanceKm)
    {
        AddAuthorizationHeader();

        var response = await _httpClient.PutAsync(
            $"/api/trains/seats/{seatId}/reserve" +
            $"?journeyDate={journeyDate:yyyy-MM-dd}" +
            $"&startDistanceKm={startDistanceKm}" +
            $"&endDistanceKm={endDistanceKm}",
            null);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ReleaseSeatAsync(
    int seatId,
    DateOnly journeyDate,
    decimal startDistanceKm,
    decimal endDistanceKm)
    {
        AddAuthorizationHeader();

        var response = await _httpClient.PutAsync(
            $"/api/trains/seats/{seatId}/release" +
            $"?journeyDate={journeyDate:yyyy-MM-dd}" +
            $"&startDistanceKm={startDistanceKm}" +
            $"&endDistanceKm={endDistanceKm}",
            null);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> IsScheduleAvailableAsync(
    int trainId,
    int scheduleId,
    DateOnly journeyDate)
    {
        AddAuthorizationHeader();

        var response = await _httpClient.GetAsync(
            $"/api/trains/{trainId}/schedules/{scheduleId}/availability?journeyDate={journeyDate:yyyy-MM-dd}");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new HttpRequestException($"Train Service availability API failed. " + $"Status: {(int) response.StatusCode}." + $"Response: {error}");
        }

        return await response.Content.ReadFromJsonAsync<bool>();
    }

    public async Task<TrainResponseDto?> GetTrainAsync(int trainId)
    {
        AddAuthorizationHeader();

        var response = await _httpClient.GetAsync(
            $"/api/trains/{trainId}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content
            .ReadFromJsonAsync<TrainResponseDto>();
    }

    public void AddAuthorizationHeader()
    {
        var authorizationHeader = _httpContextAccessor.HttpContext?
        .Request.Headers.Authorization.ToString();

        if (!string.IsNullOrEmpty(authorizationHeader))
        {
            _httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authorizationHeader);
        }
    }

    private class FareResponse
    {
        public int FareId {get; set;}
        public int TrainId {get; set;}
        public string ClassType {get; set;} = string.Empty;
        public decimal Amount {get; set;}
    }
}