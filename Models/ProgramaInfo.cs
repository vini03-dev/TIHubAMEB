namespace TIHubAMEB.Models
{
    /// <summary>
    /// Representa um programa instalado no Windows.
    /// Usado na aba Programas (inventário e busca em massa).
    /// </summary>
    public class ProgramaInfo
    {
        public string Nome { get; set; } = string.Empty;
        public string Versao { get; set; } = "—";
        public string Fabricante { get; set; } = "—";
        public string DataInstalacao { get; set; } = "—";

        // Usado no Modo 1 (busca em massa) para saber de qual máquina veio
        public string Maquina { get; set; } = string.Empty;

        // Formata a data de instalação (vem como AAAAMMDD do registro)
        public string DataFormatada
        {
            get
            {
                if (DataInstalacao.Length == 8 &&
                    long.TryParse(DataInstalacao, out _))
                {
                    string ano = DataInstalacao[..4];
                    string mes = DataInstalacao.Substring(4, 2);
                    string dia = DataInstalacao.Substring(6, 2);
                    return $"{dia}/{mes}/{ano}";
                }
                return DataInstalacao;
            }
        }
    }
}