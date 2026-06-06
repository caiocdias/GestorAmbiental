using GestorAmbiental.Domain.Common;

namespace GestorAmbiental.Domain.Entities;

public sealed class Endereco : Entity
{
    public string Cep { get; set; } = string.Empty;
    public string Logradouro { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Complemento { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Pais { get; set; } = "Brasil";

    public bool Validar()
    {
        var cepValido = string.IsNullOrWhiteSpace(Cep) || ApenasDigitos(Cep).Length == 8;

        return cepValido;
    }

    public string Formatar()
    {
        var possuiEndereco = !string.IsNullOrWhiteSpace(Cep)
            || !string.IsNullOrWhiteSpace(Logradouro)
            || !string.IsNullOrWhiteSpace(Numero)
            || !string.IsNullOrWhiteSpace(Complemento)
            || !string.IsNullOrWhiteSpace(Bairro)
            || !string.IsNullOrWhiteSpace(Cidade)
            || !string.IsNullOrWhiteSpace(Estado);

        if (!possuiEndereco)
        {
            return string.Empty;
        }

        var partes = new List<string>
        {
            $"{Logradouro}, {Numero}".Trim(' ', ','),
            Complemento,
            Bairro,
            $"{Cidade}/{Estado}".Trim('/'),
            Pais,
            string.IsNullOrWhiteSpace(Cep) ? string.Empty : $"CEP {Cep}"
        };

        return string.Join(" - ", partes.Where(parte => !string.IsNullOrWhiteSpace(parte)));
    }

    private static string ApenasDigitos(string valor)
    {
        return new string(valor.Where(char.IsDigit).ToArray());
    }
}
