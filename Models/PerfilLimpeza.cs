namespace TIHubAMEB.Models
{
    /// <summary>
    /// Define quais itens serão limpos em cada operação.
    /// Inclui limpeza de e-mails antigos com filtro de data.
    /// </summary>
    public class PerfilLimpeza
    {
        // ── Identificação ────────────────────────────────────────────────
        public string Nome { get; set; } = string.Empty;

        // ── Itens básicos ────────────────────────────────────────────────
        public bool LimparTempUsuario { get; set; }
        public bool LimparTempWindows { get; set; }
        public bool LimparPrefetch { get; set; }
        public bool LimparLixeira { get; set; }
        public bool LimparCacheDns { get; set; }
        public bool LimparMiniaturas { get; set; }
        public bool LimparLogsAntigos { get; set; }

        // ── Itens médios ─────────────────────────────────────────────────
        public bool LimparCacheNavegador { get; set; }
        public bool LimparWindowsUpdate { get; set; }
        public bool LimparSpoolImpressao { get; set; }
        public bool LimparFontCache { get; set; } // cache de fontes

        // ── Itens avançados ──────────────────────────────────────────────
        public bool ExecutarDism { get; set; }
        public bool LimparWinSxS { get; set; }
        public bool LimparMemoriaDump { get; set; } // arquivos de dump

        // ── Presets prontos ──────────────────────────────────────────────

        public static PerfilLimpeza Leve() => new()
        {
            Nome = "Leve",
            LimparTempUsuario = true,
            LimparTempWindows = true,
            LimparPrefetch = true,
            LimparLixeira = true,
            LimparCacheDns = true
        };

        public static PerfilLimpeza Medio() => new()
        {
            Nome = "Médio",
            LimparTempUsuario = true,
            LimparTempWindows = true,
            LimparPrefetch = true,
            LimparLixeira = true,
            LimparCacheDns = true,
            LimparMiniaturas = true,
            LimparLogsAntigos = true,
            LimparCacheNavegador = true,
            LimparWindowsUpdate = true,
            LimparFontCache = true
        };

        public static PerfilLimpeza Avancado() => new()
        {
            Nome = "Avançado",
            LimparTempUsuario = true,
            LimparTempWindows = true,
            LimparPrefetch = true,
            LimparLixeira = true,
            LimparCacheDns = true,
            LimparMiniaturas = true,
            LimparLogsAntigos = true,
            LimparCacheNavegador = true,
            LimparWindowsUpdate = true,
            LimparFontCache = true,
            LimparSpoolImpressao = true,
            LimparMemoriaDump = true,
            ExecutarDism = true,
            LimparWinSxS = true
        };
    }
}