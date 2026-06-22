namespace TIHubAMEB.Models
{
    /// <summary>
    /// Representa uma impressora cadastrada no arquivo externo
    /// Data/impressoras.json, combinada com seu status em tempo real
    /// (SNMP ou fallback de ping/spooler).
    /// </summary>
    public class ImpressoraInfo
    {
        // ── Dados cadastrais (vêm do JSON) ──────────────────────────────
        public string Nome { get; set; } = string.Empty;
        public string IP { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public string Localizacao { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;

        // ── Status em tempo real ────────────────────────────────────────
        public bool Online { get; set; }
        public bool ConsultaSnmpOk { get; set; } // true = dados reais via SNMP
        public int NivelTonerPreto { get; set; } = -1; // -1 = desconhecido
        public long ContadorPaginas { get; set; } = -1;
        public string StatusDescricao { get; set; } = "Não verificado";

        // ── Indicadores derivados ───────────────────────────────────────

        // Toner crítico — usado para destacar o card no topo da lista
        public bool TonerCritico => ConsultaSnmpOk &&
            NivelTonerPreto >= 0 && NivelTonerPreto <= 15;

        public bool TonerBaixo => ConsultaSnmpOk &&
            NivelTonerPreto > 15 && NivelTonerPreto <= 30;

        // Define se o card deve ser destacado (sobe na lista)
        public bool TemAlerta => !Online || TonerCritico;

        public string TonerFormatado =>
            ConsultaSnmpOk && NivelTonerPreto >= 0
                ? $"{NivelTonerPreto}%"
                : "—";

        public string ContadorFormatado =>
            ContadorPaginas >= 0
                ? $"{ContadorPaginas:N0} páginas"
                : "—";

        public string StatusFormatado =>
            !Online ? "○ Offline"
            : TonerCritico ? "⚠ Toner crítico"
            : TonerBaixo ? "⚠ Toner baixo"
            : "● Online";
    }
}