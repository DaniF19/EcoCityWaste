using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Globalization;

public class GeocodingService
{
    private readonly HttpClient _http;

    public GeocodingService(HttpClient http)
    {
        _http = http;
    }

    public async Task<(double lat, double lon)> GetCoordinates(string address)
    {
        var encodedAddress = Uri.EscapeDataString(address + ", Setubal, Portugal");

        var url = $"https://nominatim.openstreetmap.org/search?q={encodedAddress}&format=json&limit=1";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("EcoCityWasteApp/1.0");

        var response = await _http.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<List<NominatimResult>>(json);

        if (result != null && result.Count > 0)
        {
            return (
                double.Parse(result[0].lat, CultureInfo.InvariantCulture),
                double.Parse(result[0].lon, CultureInfo.InvariantCulture)
            );
        }

        return (0, 0);
    }

    private class NominatimResult
    {
        public string lat { get; set; }
        public string lon { get; set; }
    }
}