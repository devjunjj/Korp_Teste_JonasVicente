using FaturamentoService.Dtos;

namespace FaturamentoService.Services
{
    public class EstoqueApiClient
    {
        private readonly HttpClient _httpClient;

        public EstoqueApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ProdutoDto?> BuscarProdutoAsync(int produtoId)
        {
            var response = await _httpClient.GetAsync($"/api/produtos/{produtoId}");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ProdutoDto>();
        }

        public async Task<bool> BaixarEstoqueAsync(int produtoId, int quantidade)
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"/api/produtos/{produtoId}/baixa",
                new { quantidade }
            );

            return response.IsSuccessStatusCode;
        }
    }
}