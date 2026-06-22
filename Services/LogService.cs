using TIHubAMEB.Models;

namespace TIHubAMEB.Services
{
    /// <summary>
    /// Gerencia todos os logs do sistema.
    /// Salva em arquivo diário e exibe na tela em tempo real com cores.
    /// </summary>
    public class LogService
    {
        private readonly string _pastaLogs;
        private RichTextBox? _rtbAlvo;

        public LogService()
        {
            _pastaLogs = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TIHubAMEB", "Logs");

            Directory.CreateDirectory(_pastaLogs);
        }

        // ── Configuração ─────────────────────────────────────────────────

        /// <summary>Vincula um RichTextBox para exibição em tempo real.</summary>
        public void VincularControle(RichTextBox rtb) => _rtbAlvo = rtb;

        // ── Registrar ────────────────────────────────────────────────────

        /// <summary>Registra um LogEntry completo.</summary>
        public void Registrar(LogEntry entry)
        {
            SalvarEmArquivo(entry);
            ExibirNaTela(entry);
        }

        /// <summary>
        /// Versão simplificada — monta o LogEntry automaticamente.
        /// Uso: _log.Registrar("PC-REC", "Flush DNS", "OK", true, TipoLog.Rede)
        /// </summary>
        public void Registrar(
            string maquina,
            string acao,
            string resultado,
            bool sucesso = true,
            TipoLog tipo = TipoLog.Info)
        {
            Registrar(new LogEntry
            {
                Maquina = maquina,
                Acao = acao,
                Resultado = resultado,
                Sucesso = sucesso,
                Tipo = tipo
            });
        }

        /// <summary>Registra erro com exceção.</summary>
        public void RegistrarErro(string maquina, string acao, Exception ex)
        {
            Registrar(new LogEntry
            {
                Maquina = maquina,
                Acao = acao,
                Resultado = $"Exceção: {ex.Message}",
                Sucesso = false,
                Tipo = TipoLog.Erro
            });
        }

        // ── Arquivo ──────────────────────────────────────────────────────

        private void SalvarEmArquivo(LogEntry entry)
        {
            try
            {
                string arquivo = Path.Combine(
                    _pastaLogs,
                    $"{DateTime.Now:yyyy-MM-dd}.log");

                File.AppendAllText(
                    arquivo,
                    entry.ToString() + Environment.NewLine);
            }
            catch { /* nunca propaga erro de log */ }
        }

        /// <summary>Abre a pasta de logs no Explorer.</summary>
        public void AbrirPastaLogs()
        {
            if (Directory.Exists(_pastaLogs))
                System.Diagnostics.Process.Start("explorer.exe", _pastaLogs);
        }

        // ── Tela ─────────────────────────────────────────────────────────

        private void ExibirNaTela(LogEntry entry)
        {
            if (_rtbAlvo == null) return;

            if (_rtbAlvo.InvokeRequired)
            {
                _rtbAlvo.Invoke(() => ExibirNaTela(entry));
                return;
            }

            // Cor baseada no tipo e resultado
            Color cor = entry.Tipo switch
            {
                TipoLog.Erro => Color.FromArgb(248, 81, 73),   // vermelho
                TipoLog.Limpeza => Color.FromArgb(63, 185, 80),   // verde
                TipoLog.Otimizacao => Color.FromArgb(210, 153, 34),  // amarelo
                TipoLog.Rede => Color.FromArgb(77, 158, 240),  // azul
                TipoLog.Maquinas => Color.FromArgb(129, 140, 248), // roxo
                TipoLog.Usuarios => Color.FromArgb(244, 114, 182), // rosa
                TipoLog.Sistema => Color.FromArgb(34, 211, 238),  // ciano
                _ => entry.Sucesso
                    ? Color.FromArgb(139, 148, 158)   // cinza (info)
                    : Color.FromArgb(248, 81, 73)      // vermelho (erro)
            };

            // Escreve o horário em cinza
            _rtbAlvo.SelectionStart = _rtbAlvo.TextLength;
            _rtbAlvo.SelectionLength = 0;
            _rtbAlvo.SelectionColor = Color.FromArgb(72, 84, 96);
            _rtbAlvo.AppendText($"[{entry.Horario:HH:mm:ss}] ");

            // Escreve a mensagem colorida
            _rtbAlvo.SelectionColor = cor;
            _rtbAlvo.AppendText($"{entry.Acao} → {entry.Resultado}");

            // Volta cor padrão e quebra linha
            _rtbAlvo.SelectionColor = _rtbAlvo.ForeColor;
            _rtbAlvo.AppendText(Environment.NewLine);
            _rtbAlvo.ScrollToCaret();
        }
    }
}