using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using GestorAmbiental.Domain.Entities;

namespace GestorAmbiental.Infrastructure.ExternalServices;

public sealed class ViaCepAddressLookup
{
    private static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri("https://viacep.com.br/")
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Endereco?> ConsultarAsync(string cep, CancellationToken cancellationToken = default)
    {
        var digitos = new string(cep.Where(char.IsDigit).ToArray());

        if (digitos.Length != 8)
        {
            throw new ArgumentException("Informe um CEP com 8 digitos.", nameof(cep));
        }

        using var response = await HttpClient.GetAsync($"ws/{digitos}/json/", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"O ViaCEP retornou HTTP {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var viaCep = await JsonSerializer.DeserializeAsync<ViaCepResponse>(stream, JsonOptions, cancellationToken);

        if (viaCep is null || viaCep.CepNaoEncontrado)
        {
            return null;
        }

        return new Endereco
        {
            Cep = viaCep.Cep ?? digitos,
            Logradouro = viaCep.Logradouro ?? string.Empty,
            Complemento = viaCep.Complemento ?? string.Empty,
            Bairro = viaCep.Bairro ?? string.Empty,
            Cidade = viaCep.Localidade ?? string.Empty,
            Estado = viaCep.Uf ?? string.Empty,
            Pais = "Brasil"
        };
    }

    private sealed class ViaCepResponse
    {
        public string? Cep { get; init; }

        public string? Logradouro { get; init; }

        public string? Complemento { get; init; }

        public string? Bairro { get; init; }

        public string? Localidade { get; init; }

        public string? Uf { get; init; }

        [JsonPropertyName("erro")]
        public JsonElement? Erro { get; init; }

        [JsonIgnore]
        public bool CepNaoEncontrado => Erro?.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.String => bool.TryParse(Erro.Value.GetString(), out var erro) && erro,
            _ => false
        };
    }
}
