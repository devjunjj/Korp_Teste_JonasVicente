using System.Net.Http.Json;
using System.Text.Json;

namespace EstoqueService.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GeminiApiKey"]
                ?? throw new InvalidOperationException("GeminiApiKey não configurada nos User Secrets.");
        }

        public async Task<string> SugerirDescricaoAsync(string codigo)
        {
            var prompt = $"Sugira uma descrição curta (máximo 8 palavras) e profissional para um produto de estoque com o código '{codigo}'. Responda APENAS com a descrição, sem explicações, sem aspas, sem pontuação final.";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var url = $"v1beta/models/gemini-3.6-flash:generateContent?key={_apiKey}";

            var response = await _httpClient.PostAsJsonAsync(url, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var erro = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Erro ao chamar a API do Gemini: {response.StatusCode} - {erro}");
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            var texto = json
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return texto?.Trim() ?? string.Empty;
        }
    }
}