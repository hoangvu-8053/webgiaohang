using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace webgiaohang.Services;

public interface INominatimGeocodingService
{
    /// <summary>Geocode địa chỉ, ưu tiên kết quả trong phạm vi Việt Nam (theo Nominatim + countrycodes=vn).</summary>
    Task<(decimal lat, decimal lng)?> GeocodeVietnamAsync(string address, CancellationToken ct = default);
}

/// <summary>
/// OpenStreetMap Nominatim — cần User-Agent hợp lệ. Dùng khi tạo đơn để lưu Pickup/Delivery lat lng khớp địa chỉ.
/// </summary>
public sealed class NominatimGeocodingService : INominatimGeocodingService
{
    private readonly HttpClient _http;

    private const double VnMinLat = 8.15;
    private const double VnMaxLat = 23.5;
    private const double VnMinLng = 102.0;
    private const double VnMaxLng = 109.65;

    public NominatimGeocodingService(HttpClient http)
    {
        _http = http;
        _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "webgiaohang/1.0 (order address geocoding)");
        _http.Timeout = TimeSpan.FromSeconds(20);
    }

    public async Task<(decimal lat, decimal lng)?> GeocodeVietnamAsync(string address, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;

        var q = address.Trim();
        if (!Regex.IsMatch(q, @"việt\s*nam|viet\s*nam|\bvn\b", RegexOptions.IgnoreCase))
            q += ", Việt Nam";

        var url =
            "https://nominatim.openstreetmap.org/search?format=json" +
            "&limit=8&countrycodes=vn&addressdetails=1&q=" +
            Uri.EscapeDataString(q);

        using var resp = await _http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode) return null;

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0) return null;

        foreach (var el in root.EnumerateArray())
        {
            if (!TryParseLatLon(el, out var la, out var lo)) continue;
            if (la >= VnMinLat && la <= VnMaxLat && lo >= VnMinLng && lo <= VnMaxLng)
                return ((decimal)la, (decimal)lo);
        }

        if (!TryParseLatLon(root[0], out var la0, out var lo0)) return null;
        return ((decimal)la0, (decimal)lo0);
    }

    private static bool TryParseLatLon(JsonElement el, out double lat, out double lng)
    {
        lat = 0;
        lng = 0;
        if (!el.TryGetProperty("lat", out var latEl) || !el.TryGetProperty("lon", out var lonEl))
            return false;
        try
        {
            lat = latEl.ValueKind == JsonValueKind.Number
                ? latEl.GetDouble()
                : double.Parse(latEl.GetString()!, CultureInfo.InvariantCulture);
            lng = lonEl.ValueKind == JsonValueKind.Number
                ? lonEl.GetDouble()
                : double.Parse(lonEl.GetString()!, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
