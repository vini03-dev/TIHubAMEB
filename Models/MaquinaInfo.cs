namespace TIHubAMEB.Models
{
    /// <summary>
    /// Representa os dados em tempo real de uma máquina monitorada.
    /// Usado tanto para PC local quanto remoto via WMI.
    /// </summary>
    public class MaquinaInfo
    {
        // ── Identificação ────────────────────────────────────────────────
        public string NomeMaquina { get; set; } = Environment.MachineName;
        public string IP { get; set; } = string.Empty;
        public string UsuarioLogado { get; set; } = Environment.UserName;
        public string SistemaOp { get; set; } = string.Empty; // Win 10 / Win 11
        public string Dominio { get; set; } = string.Empty;
        public string Setor { get; set; } = string.Empty; // detectado pelo nome
        public string Modelo { get; set; } = "—";      // ex: Dell OptiPlex 7090
        public string ServiceTag { get; set; } = "—";  // número de série / service tag

        // ── Métricas de hardware ─────────────────────────────────────────
        public float CpuPercent { get; set; }
        public float RamPercent { get; set; }
        public float DiscoPercent { get; set; }
        public long RamTotalMB { get; set; }
        public long DiscoTotalGB { get; set; }
        public long DiscoLivreGB { get; set; }

        // ── Status ───────────────────────────────────────────────────────
        public bool Online { get; set; } = true;
        public string Modo { get; set; } = "LOCAL"; // LOCAL ou REMOTO
        public TimeSpan TempoLigado { get; set; }
        public DateTime UltimaAtualizacao { get; set; } = DateTime.Now;

        // ── Utilitário ───────────────────────────────────────────────────

        // Detecta o setor automaticamente pelo nome da máquina
        // Ex: VIVO-NOT-INF-01 → INF
        public static string DetectarSetor(string nomeMaquina) =>
            MaquinaRede.DetectarSetor(nomeMaquina);

        // Formata o uptime ex: 04h 32m
        public string UptimeFormatado =>
            TempoLigado.TotalSeconds > 0
                ? $"{(int)TempoLigado.TotalHours:D2}h {TempoLigado.Minutes:D2}m"
                : "—";
    }
}