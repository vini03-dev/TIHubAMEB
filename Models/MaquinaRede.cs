namespace TIHubAMEB.Models
{
    /// <summary>
    /// Representa uma máquina encontrada no Active Directory.
    /// Usado na aba Máquinas para listar e conectar remotamente.
    /// </summary>
    public class MaquinaRede
    {
        // ── Identificação ────────────────────────────────────────────────
        public string Nome { get; set; } = string.Empty;
        public string IP { get; set; } = string.Empty;
        public string SistemaOp { get; set; } = string.Empty;
        public string Setor { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Modelo { get; set; } = "—";      // ex: Dell OptiPlex 7090
        public string ServiceTag { get; set; } = "—";  // número de série

        // ── Status ───────────────────────────────────────────────────────
        public bool Online { get; set; }
        public long PingMs { get; set; }
        public DateTime UltimaVez { get; set; } // último login no AD

        // ── AD ───────────────────────────────────────────────────────────
        public string DistinguishedName { get; set; } = string.Empty;
        public string UO { get; set; } = string.Empty; // Unidade Organizacional

        // ── Formatação ───────────────────────────────────────────────────
        public string UsuarioConectado { get; set; } = "—";

        public string StatusFormatado =>
    Online ? $"● Online  {PingMs}ms" : "○ Offline";

        public string UltimaVezFormatado =>
            UltimaVez == DateTime.MinValue
                ? "—"
                : UltimaVez.ToString("dd/MM/yyyy");

        // Detecta setor pelo nome da máquina automaticamente
        // Ex: VIVO-NOT-INF-01 → INF
        public static string DetectarSetor(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome)) return "Outros";
            var partes = nome.ToUpper().Split('-');
            foreach (var parte in partes)
                if (parte.Length >= 2 && parte.Length <= 5 &&
                    parte.All(char.IsLetter))
                    return parte;
            return partes.Length >= 2 ? partes[^2] : "Outros";
        }

        // Junta fabricante + modelo de forma limpa, evitando repetição.
        // Remove palavras genéricas dos fabricantes para caber na tela.
        // Definido aqui uma vez só — usado por MaquinasService e MonitoramentoService.
        public static string MontarModelo(string fabricante, string modelo)
        {
            string[] lixo =
            {
                "Inc.", "Inc", "Corporation", "Corp.", "Corp", "Co.",
                "LLC", "Ltd.", "Ltd", "Computer", "Computers",
                "Technologies", "Technology", "International",
                "To Be Filled By O.E.M.", "System Manufacturer",
                "Default string", "O.E.M.", "(R)", "(TM)"
            };

            string Limpar(string txt)
            {
                if (string.IsNullOrWhiteSpace(txt)) return "";
                foreach (var termo in lixo)
                    txt = txt.Replace(termo, "", StringComparison.OrdinalIgnoreCase);
                return string.Join(" ", txt.Split(' ',
                    StringSplitOptions.RemoveEmptyEntries)).Trim();
            }

            fabricante = Limpar(fabricante);
            modelo = Limpar(modelo);

            if (string.IsNullOrEmpty(fabricante) && string.IsNullOrEmpty(modelo))
                return "—";
            if (string.IsNullOrEmpty(fabricante)) return modelo;
            if (string.IsNullOrEmpty(modelo)) return fabricante;

            if (modelo.Contains(fabricante, StringComparison.OrdinalIgnoreCase))
                return modelo;

            string[] modelosAutoexplicativos =
            {
                "OptiPlex", "Latitude", "Precision", "Inspiron", "Vostro",
                "ThinkCentre", "ThinkPad", "IdeaPad", "ThinkStation",
                "EliteDesk", "ProDesk", "EliteBook", "ProBook", "Pavilion",
                "Aspire", "Veriton"
            };
            foreach (var m in modelosAutoexplicativos)
                if (modelo.Contains(m, StringComparison.OrdinalIgnoreCase))
                    return modelo;

            return $"{fabricante} {modelo}";
        }
    }
}