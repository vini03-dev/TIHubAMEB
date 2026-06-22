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
    }
}