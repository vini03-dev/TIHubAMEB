namespace TIHubAMEB.Models
{
    /// <summary>
    /// Espelha exatamente a estrutura do arquivo Data/impressoras.json.
    /// Usado apenas na desserialização — depois é convertido para
    /// ImpressoraInfo, que carrega também o status em tempo real.
    /// </summary>
    public class ImpressoraCadastro
    {
        public string Nome { get; set; } = string.Empty;
        public string Ip { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public string Localizacao { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
    }
}