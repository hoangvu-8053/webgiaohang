using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Options;
using webgiaohang.Models;

namespace webgiaohang.Services
{
    public interface IDistanceService
    {
        Task<decimal?> GetDistanceKmAsync(string originAddress, string destinationAddress, CancellationToken ct = default);
    }

    public class DistanceService : IDistanceService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GoogleMapsSettings _settings;

        public DistanceService(IHttpClientFactory httpClientFactory, IOptions<GoogleMapsSettings> settings)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
        }

        public async Task<decimal?> GetDistanceKmAsync(string originAddress, string destinationAddress, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey)) return null;
            if (string.IsNullOrWhiteSpace(originAddress) || string.IsNullOrWhiteSpace(destinationAddress)) return null;

            var http = _httpClientFactory.CreateClient();
            var origins = Uri.EscapeDataString(originAddress);
            var destinations = Uri.EscapeDataString(destinationAddress);
            var url = $"https://maps.googleapis.com/maps/api/distancematrix/json?origins={origins}&destinations={destinations}&key={_settings.ApiKey}&region=vn&language=vi&units=metric";

            try
            {
                using var resp = await http.GetAsync(url, ct);
                resp.EnsureSuccessStatusCode();
                await using var stream = await resp.Content.ReadAsStreamAsync(ct);
                var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                if (!doc.RootElement.TryGetProperty("rows", out var rows) || rows.GetArrayLength() == 0)
                    return null;
                var elements = rows[0].GetProperty("elements");
                if (elements.GetArrayLength() == 0) return null;
                var element = elements[0];
                if (element.GetProperty("status").GetString() != "OK") return null;
                var meters = element.GetProperty("distance").GetProperty("value").GetDecimal();
                var km = meters / 1000m;
                // làm tròn 0.1 km
                km = Math.Round(km, 1, MidpointRounding.AwayFromZero);
                return km;
            }
            catch
            {
                return null;
            }
        }
    }
}

