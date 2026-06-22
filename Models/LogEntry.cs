namespace TIHubAMEB.Models
{
    /// <summary>
    /// Representa um registro de log do sistema.
    /// Cada ação executada gera um LogEntry automaticamente.
    /// </summary>
    public class LogEntry
    {
        // ── Dados do registro ────────────────────────────────────────────
        public DateTime Horario { get; set; } = DateTime.Now;
        public string Maquina { get; set; } = string.Empty;
        public string Usuario { get; set; } = Environment.UserName;
        public string Acao { get; set; } = string.Empty;
        public string Resultado { get; set; } = string.Empty;
        public bool Sucesso { get; set; } = true;
        public TipoLog Tipo { get; set; } = TipoLog.Info;

        // ── Formatação ───────────────────────────────────────────────────

        // Linha completa para arquivo .log
        // Ex: [10/05/2026 14:32:01] [OK] [LIMPEZA] PC-REC | joao → Flush DNS: Sucesso
        public override string ToString() =>
            $"[{Horario:dd/MM/yyyy HH:mm:ss}] " +
            $"[{(Sucesso ? "OK  " : "ERRO")}] " +
            $"[{Tipo.ToString().ToUpper(),-10}] " +
            $"{Maquina,-20} | {Usuario,-15} → {Acao}: {Resultado}";

        // Linha curta para exibir na tela
        public string ToStringCurto() =>
            $"[{Horario:HH:mm:ss}] {Acao} → {Resultado}";
    }

    /// <summary>
    /// Categorias de log para facilitar filtragem futura.
    /// </summary>
    public enum TipoLog
    {
        Info,
        Limpeza,
        Otimizacao,
        Rede,
        Maquinas,
        Usuarios,
        Sistema,
        Erro
    }
}