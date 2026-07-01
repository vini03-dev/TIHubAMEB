namespace TIHubAMEB.Models
{
    /// <summary>
    /// Representa um programa de inicialização do Windows.
    /// </summary>
    public class ItemInicializacao
    {
        public string Nome { get; set; } = string.Empty;
        public string Comando { get; set; } = string.Empty;
        public string Origem { get; set; } = string.Empty;  // HKLM ou HKCU
        public bool Ativo { get; set; } = true;

        // Extrai só o caminho do executável do comando completo
        // (remove aspas e argumentos para exibição limpa)
        public string CaminhoLimpo
        {
            get
            {
                string c = Comando.Trim().Trim('"');
                int exe = c.IndexOf(".exe",
                    StringComparison.OrdinalIgnoreCase);
                if (exe > 0) return c[..(exe + 4)];
                return c;
            }
        }

        public string EstadoFormatado => Ativo ? "● Ativo" : "○ Desativado";
    }
}