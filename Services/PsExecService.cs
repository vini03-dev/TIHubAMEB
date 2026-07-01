using System.Diagnostics;
using TIHubAMEB.Models;

namespace TIHubAMEB.Services
{
    /// <summary>
    /// Executa comandos local ou remotamente via PSExec.
    /// Melhorado com timeout, retry automático e log detalhado.
    /// NUNCA abre janela CMD visível ao usuário.
    /// </summary>
    public class PsExecService
    {
        // ── Configuração ─────────────────────────────────────────────────

        // Caminho do PSExec — ajuste se necessário
        // Depois — busca dentro da pasta onde o .exe está rodando:
        private static readonly string CaminhoPsExec = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Tools", "PsExec64.exe");

        // Timeout padrão por comando (segundos)
        private const int TimeoutSegundos = 120;

        // Tentativas em caso de falha
        private const int MaxTentativas = 2;

        private readonly LogService _log;

        public PsExecService(LogService log)
        {
            _log = log;
        }

        // ── Execução principal ───────────────────────────────────────────

        /// <summary>
        /// Executa um comando CMD local ou remoto.
        /// Deixar maquina null/vazio = executa localmente.
        /// </summary>
        public async Task<ResultadoExecucao> ExecutarAsync(
            string comando,
            string? maquina = null,
            IProgress<string>? progresso = null,
            int tentativas = MaxTentativas)
        {
            bool remoto = !string.IsNullOrWhiteSpace(maquina);

            // Monta programa e argumentos
            string programa, argumentos;

            if (remoto)
            {
                // Verifica se PSExec existe antes de tentar
                if (!File.Exists(CaminhoPsExec))
                {
                    var erro = new ResultadoExecucao
                    {
                        Maquina = maquina!,
                        Sucesso = false,
                        Erro = $"PSExec não encontrado em: {CaminhoPsExec}"
                    };
                    _log.Registrar(maquina!, "PSExec",
                        erro.Erro, false, TipoLog.Erro);
                    return erro;
                }

                programa = CaminhoPsExec;
                // -h  = elevado | -s = SYSTEM | -accepteula = sem popup
                argumentos = $@"\\{maquina} -h -s -accepteula cmd /c ""{comando}""";
            }
            else
            {
                programa = "cmd.exe";
                argumentos = $"/c {comando}";
            }

            progresso?.Report($"Executando: {comando}");

            // Retry automático em caso de falha
            ResultadoExecucao resultado = new();
            for (int i = 1; i <= tentativas; i++)
            {
                resultado = await RodarProcessoAsync(
                    programa, argumentos, maquina ?? "LOCAL");

                if (resultado.Sucesso) break;

                if (i < tentativas)
                {
                    progresso?.Report($"Tentativa {i} falhou, tentando novamente...");
                    await Task.Delay(1500);
                }
            }

            return resultado;
        }

        /// <summary>
        /// Executa um script PowerShell local ou remoto.
        /// </summary>
        public async Task<ResultadoExecucao> ExecutarPowerShellAsync(
            string script,
            string? maquina = null,
            IProgress<string>? progresso = null)
        {
            // Escapa aspas duplas dentro do script
            string scriptEscapado = script.Replace("\"", "\\\"");

            string comando =
                $"powershell.exe -NonInteractive -ExecutionPolicy Bypass " +
                $"-Command \"{scriptEscapado}\"";

            return await ExecutarAsync(comando, maquina, progresso);
        }

        /// <summary>
        /// Executa múltiplos comandos em sequência no mesmo PC.
        /// Retorna lista de resultados.
        /// </summary>
        public async Task<List<ResultadoExecucao>> ExecutarVariosAsync(
            IEnumerable<string> comandos,
            string? maquina = null,
            IProgress<(int pct, string etapa)>? progresso = null,
            CancellationToken ct = default)
        {
            var lista = comandos.ToList();
            var resultados = new List<ResultadoExecucao>();

            for (int i = 0; i < lista.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                int pct = (int)((double)(i + 1) / lista.Count * 100);
                progresso?.Report((pct, $"Executando {i + 1}/{lista.Count}..."));

                var r = await ExecutarAsync(lista[i], maquina);
                resultados.Add(r);
            }

            return resultados;
        }

        // ── Processo oculto ──────────────────────────────────────────────

        private async Task<ResultadoExecucao> RodarProcessoAsync(
            string programa, string argumentos, string maquinaLabel)
        {
            var resultado = new ResultadoExecucao { Maquina = maquinaLabel };

            try
            {
                using var processo = new Process();

                processo.StartInfo = new ProcessStartInfo
                {
                    FileName = programa,
                    Arguments = argumentos,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                };

                processo.Start();

                // Lê saída e erro em paralelo — evita deadlock
                var saida = processo.StandardOutput.ReadToEndAsync();
                var erro = processo.StandardError.ReadToEndAsync();

                // Guarda a referência do delay para comparar corretamente no WhenAny
                var taskTimeout = Task.Delay(TimeSpan.FromSeconds(TimeoutSegundos));
                var concluiu = await Task.WhenAny(Task.WhenAll(saida, erro), taskTimeout);

                if (concluiu == taskTimeout)
                {
                    // Timeout — mata o processo
                    try { processo.Kill(true); } catch { }
                    resultado.Sucesso = false;
                    resultado.Erro = $"Timeout após {TimeoutSegundos}s";

                    _log.Registrar(maquinaLabel, "PSExec",
                        resultado.Erro, false, TipoLog.Erro);
                    return resultado;
                }

                await processo.WaitForExitAsync();

                resultado.Saida = (await saida).Trim();
                resultado.Erro = (await erro).Trim();
                resultado.CodigoSaida = processo.ExitCode;

                // PSExec retorna 0 ou o código do processo remoto
                // Código 1 do PSExec pode ser aviso, não necessariamente erro
                resultado.Sucesso = processo.ExitCode == 0 ||
                                    processo.ExitCode == 1 &&
                                    string.IsNullOrEmpty(resultado.Erro);

                // Log resumido
                _log.Registrar(
                    maquinaLabel,
                    Path.GetFileName(programa),
                    resultado.Sucesso
                        ? $"OK (código {resultado.CodigoSaida})"
                        : $"Erro {resultado.CodigoSaida}: {resultado.Erro}",
                    resultado.Sucesso,
                    TipoLog.Info);
            }
            catch (Exception ex)
            {
                resultado.Sucesso = false;
                resultado.Erro = ex.Message;
                _log.RegistrarErro(maquinaLabel, programa, ex);
            }

            return resultado;
        }
    }

    // ── Resultado da execução ─────────────────────────────────────────────

    /// <summary>
    /// Retorno de qualquer execução via PsExecService.
    /// </summary>
    public class ResultadoExecucao
    {
        public string Maquina { get; set; } = string.Empty;
        public string Saida { get; set; } = string.Empty;
        public string Erro { get; set; } = string.Empty;
        public int CodigoSaida { get; set; }
        public bool Sucesso { get; set; }

        // Retorna saída ou erro — o que tiver conteúdo
        public string SaidaOuErro =>
            !string.IsNullOrWhiteSpace(Saida) ? Saida :
            !string.IsNullOrWhiteSpace(Erro) ? Erro : "Sem retorno";
    }
}