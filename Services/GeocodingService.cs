using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Globalization;

/// <summary>
/// Serviço de Geocodificação responsável por converter endereços de texto em coordenadas geográficas (Latitude e Longitude).
/// Utiliza a API (OpenStreetMap) para obter dados geoespaciais precisos.
/// </summary>
public class GeocodingService
{
    private readonly HttpClient _http;

    /// <summary>
    /// Injeta o cliente HTTP para realizar pedidos externos à API de mapas.
    /// </summary>
    public GeocodingService(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Obtém as coordenadas GPS para uma morada específica.
    /// Adiciona automaticamente o contexto geográfico (Setúbal, Portugal) para aumentar a precisão da pesquisa.
    /// </summary>
    /// <param name="address">A morada ou local introduzido pelo utilizador.</param>
    /// <returns>Um tuplo contendo a Latitude e a Longitude. Devolve (0,0) se a morada não for encontrada.</returns>
    public async Task<(double lat, double lon)> GetCoordinates(string address)
    {
        // Codifica a morada para garantir que caracteres especiais ou espaços não quebram a URL
        var encodedAddress = Uri.EscapeDataString(address + ", Setubal, Portugal");

        // Configuração do URL da API Nominatim (formato JSON, limitado ao melhor resultado)
        var url = $"https://nominatim.openstreetmap.org/search?q={encodedAddress}&format=json&limit=1";

        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // A API Nominatim exige a identificação da aplicação (User-Agent) por questões de segurança e política de uso
        request.Headers.UserAgent.ParseAdd("EcoCityWasteApp/1.0");

        var response = await _http.SendAsync(request);

        // Garante que o pedido foi bem sucedido antes de tentar ler o conteúdo
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        // Desserializa a resposta JSON para a classe auxiliar interna
        var result = JsonSerializer.Deserialize<List<NominatimResult>>(json);

        if (result != null && result.Count > 0)
        {
            // Converte as strings de latitude/longitude para double usando a cultura Invariante (ponto como separador decimal)
            // Isto evita erros de conversão em sistemas configurados com a vírgula decimal portuguesa.
            return (
                double.Parse(result[0].lat, CultureInfo.InvariantCulture),
                double.Parse(result[0].lon, CultureInfo.InvariantCulture)
            );
        }

        return (0, 0);
    }

    /// <summary>
    /// Classe interna auxiliar para mapear a estrutura de resposta JSON da API Nominatim.
    /// </summary>
    private class NominatimResult
    {
        /// <summary> Latitude devolvida pela API. </summary>
        public string lat { get; set; } = string.Empty;

        /// <summary> Longitude devolvida pela API. </summary>
        public string lon { get; set; } = string.Empty;
    }
}