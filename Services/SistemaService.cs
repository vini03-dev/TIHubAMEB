using TIHubAMEB.Models;

namespace TIHubAMEB.Services
{
    /// <summary>
    /// Verifica e restaura a imagem do Windows.
    /// Executa SFC e DISM em sequência com progresso em tempo real.
    /// </summary>
    public class SistemaService
    {
        private readonly PsExecService _psExec;
        private readonly LogService _log;

        public SistemaService(PsExecService psExec, LogService log)
        {
            _psExec = psExec;
            _log = log;
        }

        // ── Verificação completa ──────────────────────────────────────────

        /// <summary>
        /// Executa SFC + DISM em sequência.
        /// Pode demorar 20-40 minutos — roda em background.
        /// </summary>
        public async Task ExecutarVerificacaoCompletaAsync(
            string? maquina,
            IProgress<(int pct, string etapa, string detalhe)> progresso,
            CancellationToken ct = default)
        {
            var etapas = new List<(string label, string detalhe, string cmd, bool ehPS)>
            {
                (
                    "DISM — Verificação rápida",
                    "Verifica integridade da imagem (rápido ~1 min)",
                    "DISM /Online /Cleanup-Image /CheckHealth",
                    false
                ),
                (
                    "DISM — Scan completo",
                    "Scan completo da imagem (~5-10 min)",
                    "DISM /Online /Cleanup-Image /ScanHealth",
                    false
                ),
                (
                    "SFC — Verificação de arquivos",
                    "Verifica arquivos protegidos do sistema (~10-15 min)",
                    "sfc /scannow",
                    false
                ),
                (
                    "DISM — Restauração da imagem",
                    "Restaura arquivos corrompidos (~15-30 min)",
                    "DISM /Online /Cleanup-Image /RestoreHealth",
                    false
                )
            };

            for (int i = 0; i < etapas.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var (label, detalhe, cmd, ehPS) = etapas[i];
                int pct = (int)((double)(i + 1) / etapas.Count * 100);

                progresso.Report((pct - 1, $"⏳ {label}", detalhe));

                _log.Registrar(
                    maquina ?? "LOCAL",
                    label,
                    "Iniciando...",
                    true, TipoLog.Sistema);

                ResultadoExecucao resultado;

                if (ehPS)
                    resultado = await _psExec.ExecutarPowerShellAsync(
                        cmd, maquina);
                else
                    resultado = await _psExec.ExecutarAsync(
                        cmd, maquina);

                // Analisa resultado
                string status = AnalisarResultado(label, resultado);

                _log.Registrar(
                    maquina ?? "LOCAL",
                    label,
                    status,
                    resultado.Sucesso,
                    resultado.Sucesso ? TipoLog.Sistema : TipoLog.Erro);

                progresso.Report((pct, $"✓ {label}", status));
                await Task.Delay(500, ct);
            }

            _log.Registrar(
                maquina ?? "LOCAL",
                "Verificação completa",
                "SFC + DISM concluídos",
                true, TipoLog.Sistema);
        }

        // ── Etapas individuais ────────────────────────────────────────────

        /// <summary>DISM /CheckHealth — verificação rápida.</summary>
        public async Task<ResultadoExecucao> CheckHealthAsync(
            string? maquina = null)
        {
            var r = await _psExec.ExecutarAsync(
                "DISM /Online /Cleanup-Image /CheckHealth",
                maquina);

            _log.Registrar(
                maquina ?? "LOCAL",
                "DISM CheckHealth",
                AnalisarResultadoDISM(r.Saida),
                r.Sucesso, TipoLog.Sistema);

            return r;
        }

        /// <summary>DISM /ScanHealth — scan completo.</summary>
        public async Task<ResultadoExecucao> ScanHealthAsync(
            string? maquina = null)
        {
            var r = await _psExec.ExecutarAsync(
                "DISM /Online /Cleanup-Image /ScanHealth",
                maquina);

            _log.Registrar(
                maquina ?? "LOCAL",
                "DISM ScanHealth",
                AnalisarResultadoDISM(r.Saida),
                r.Sucesso, TipoLog.Sistema);

            return r;
        }

        /// <summary>SFC /scannow — verifica arquivos do sistema.</summary>
        public async Task<ResultadoExecucao> SfcScannowAsync(
            string? maquina = null)
        {
            var r = await _psExec.ExecutarAsync(
                "sfc /scannow", maquina);

            _log.Registrar(
                maquina ?? "LOCAL",
                "SFC Scannow",
                AnalisarResultadoSFC(r.Saida),
                r.Sucesso, TipoLog.Sistema);

            return r;
        }

        /// <summary>DISM /RestoreHealth — restaura a imagem.</summary>
        public async Task<ResultadoExecucao> RestoreHealthAsync(
            string? maquina = null)
        {
            var r = await _psExec.ExecutarAsync(
                "DISM /Online /Cleanup-Image /RestoreHealth",
                maquina);

            _log.Registrar(
                maquina ?? "LOCAL",
                "DISM RestoreHealth",
                AnalisarResultadoDISM(r.Saida),
                r.Sucesso, TipoLog.Sistema);

            return r;
        }

        // ── Análise de resultados ─────────────────────────────────────────

        private static string AnalisarResultado(
            string label, ResultadoExecucao r)
        {
            if (!r.Sucesso)
                return $"Erro: {r.Erro}";

            if (label.Contains("SFC"))
                return AnalisarResultadoSFC(r.Saida);

            return AnalisarResultadoDISM(r.Saida);
        }

        private static string AnalisarResultadoDISM(string saida)
        {
            if (string.IsNullOrWhiteSpace(saida))
                return "Sem retorno";

            // Mensagens típicas do DISM
            if (saida.Contains("no component store corruption",
                StringComparison.OrdinalIgnoreCase) ||
                saida.Contains("nenhum dano",
                StringComparison.OrdinalIgnoreCase))
                return "✓ Imagem íntegra — sem corrupção";

            if (saida.Contains("The restore operation completed",
                StringComparison.OrdinalIgnoreCase) ||
                saida.Contains("restauração foi concluída",
                StringComparison.OrdinalIgnoreCase))
                return "✓ Restauração concluída com sucesso";

            if (saida.Contains("repairable",
                StringComparison.OrdinalIgnoreCase))
                return "⚠ Corrupção encontrada — restaurável";

            if (saida.Contains("not repairable",
                StringComparison.OrdinalIgnoreCase))
                return "✕ Corrupção encontrada — não restaurável";

            if (saida.Contains("The operation completed successfully",
                StringComparison.OrdinalIgnoreCase))
                return "✓ Operação concluída";

            // Retorna as últimas 2 linhas da saída
            var linhas = saida.Split('\n')
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .TakeLast(2)
                .ToArray();

            return string.Join(" | ", linhas).Trim();
        }

        private static string AnalisarResultadoSFC(string saida)
        {
            if (string.IsNullOrWhiteSpace(saida))
                return "Sem retorno";

            if (saida.Contains("did not find any integrity violations",
                StringComparison.OrdinalIgnoreCase) ||
                saida.Contains("não encontrou nenhuma violação",
                StringComparison.OrdinalIgnoreCase))
                return "✓ Nenhum problema encontrado";

            if (saida.Contains("found corrupt files and successfully repaired",
                StringComparison.OrdinalIgnoreCase) ||
                saida.Contains("reparou com êxito",
                StringComparison.OrdinalIgnoreCase))
                return "✓ Arquivos corrompidos reparados com sucesso";

            if (saida.Contains("found corrupt files but was unable to fix",
                StringComparison.OrdinalIgnoreCase))
                return "✕ Arquivos corrompidos encontrados — não foi possível reparar";

            if (saida.Contains("Verification",
                StringComparison.OrdinalIgnoreCase))
                return "✓ Verificação concluída";

            return saida.Length > 100 ? saida[..100] + "..." : saida;
        }
    }
}