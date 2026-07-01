using TIHubAMEB.Models;

namespace TIHubAMEB.Services
{
    /// <summary>
    /// Lista programas instalados (local ou remoto) e busca um
    /// programa específico em várias máquinas online.
    /// Lê o registro de Uninstall (HKLM 64/32 bits + HKCU).
    /// IMPORTANTE: scripts em LINHA ÚNICA porque multilinha quebra
    /// ao passar por cmd /c powershell.
    /// </summary>
    public class ProgramasService
    {
        private readonly PsExecService _psExec;
        private readonly LogService _log;

        private const string SEP = "~~";

        // Timeout por máquina na busca em massa (segundos).
        // Curto de propósito: se a máquina não responde rápido,
        // desiste dela e segue para não travar a busca toda.
        private const int TimeoutMaquinaSegundos = 25;

        public ProgramasService(PsExecService psExec, LogService log)
        {
            _psExec = psExec;
            _log = log;
        }

        // ── MODO 2: Listar todos os programas de uma máquina ─────────────

        public async Task<List<ProgramaInfo>> ListarProgramasAsync(
            string? maquina)
        {
            string script = MontarScriptLista(filtro: null);

            var resultado = await _psExec.ExecutarPowerShellAsync(script, maquina);

            var lista = ParseProgramas(resultado.Saida, maquina);

            _log.Registrar(maquina ?? "LOCAL", "Listar programas",
                $"{lista.Count} programas encontrados",
                resultado.Sucesso, TipoLog.Info);

            return lista
                .OrderBy(p => p.Nome, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // ── MODO 1: Buscar um programa em várias máquinas ────────────────

        public async Task<List<ProgramaInfo>> BuscarProgramaEmMassaAsync(
            IEnumerable<string> maquinasOnline,
            string termoBusca,
            IProgress<(int pct, string maquina, bool achou)>? progresso = null,
            CancellationToken ct = default)
        {
            var maquinas = maquinasOnline.ToList();
            var encontrados = new System.Collections.Concurrent.ConcurrentBag<ProgramaInfo>();

            if (maquinas.Count == 0 || string.IsNullOrWhiteSpace(termoBusca))
                return new List<ProgramaInfo>();

            int total = maquinas.Count;
            int concluido = 0;
            var semaforo = new SemaphoreSlim(20);
            string script = MontarScriptLista(filtro: termoBusca);

            var tarefas = maquinas.Select(async maquina =>
            {
                await semaforo.WaitAsync(ct);
                try
                {

                    // Timeout POR MÁQUINA: corta máquinas penduradas.
                    // Se passar do tempo, desiste dessa e segue.
                    using var ctsMaquina = CancellationTokenSource
                        .CreateLinkedTokenSource(ct);
                    ctsMaquina.CancelAfter(
                        TimeSpan.FromSeconds(TimeoutMaquinaSegundos));

                    var achados = new List<ProgramaInfo>();

                    try
                    {
                        var tarefaConsulta = _psExec.ExecutarPowerShellAsync(
                            script, maquina);

                        // Corrida: ou a consulta responde, ou estoura o tempo
                        var concluiu = await Task.WhenAny(
                            tarefaConsulta,
                            Task.Delay(
                                TimeSpan.FromSeconds(TimeoutMaquinaSegundos),
                                ctsMaquina.Token));

                        if (concluiu == tarefaConsulta)
                        {
                            var r = await tarefaConsulta;
                            achados = ParseProgramas(r.Saida, maquina);
                        }
                        // senão: estourou o tempo, ignora essa máquina
                    }
                    catch { /* máquina falhou, ignora e segue */ }

                    bool achou = achados.Count > 0;
                    if (achou)
                    {
                        foreach (var p in achados)
                            encontrados.Add(p);
                    }

                    int feitos = Interlocked.Increment(ref concluido);
                    int pct = (int)((double)feitos / total * 100);
                    progresso?.Report((pct, maquina, achou));
                }
                catch
                {
                    int feitos = Interlocked.Increment(ref concluido);
                    int pct = (int)((double)feitos / total * 100);
                    progresso?.Report((pct, maquina, false));
                }
                finally
                {
                    semaforo.Release();
                }
            });

            await Task.WhenAll(tarefas);

            _log.Registrar("REDE", "Buscar programa",
                $"'{termoBusca}': {encontrados.Count} máquina(s) com o programa",
                true, TipoLog.Maquinas);

            return encontrados
                .OrderBy(p => p.Maquina, StringComparer.OrdinalIgnoreCase)
                .ToList();

        }

        // ── Script compartilhado (linha única) ───────────────────────────

        private static string MontarScriptLista(string? filtro)
        {
            string condFiltro = string.IsNullOrWhiteSpace(filtro)
                ? ""
                : $" -and $_.DisplayName -like '*{filtro.Replace("'", "''")}*'";

            return
                "$caminhos=@(" +
                "'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\*'," +
                "'HKLM:\\SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\*'," +
                "'HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\*'" +
                "); " +
                "Get-ItemProperty $caminhos -ErrorAction SilentlyContinue | " +
                "Where-Object {$_.DisplayName" + condFiltro + "} | " +
                "ForEach-Object {" +
                "Write-Output ($_.DisplayName+'~~'+" +
                "$_.DisplayVersion+'~~'+" +
                "$_.Publisher+'~~'+" +
                "$_.InstallDate)" +
                "}";
        }

        private static List<ProgramaInfo> ParseProgramas(
            string? saida, string? maquina)
        {
            var lista = new List<ProgramaInfo>();

            if (string.IsNullOrWhiteSpace(saida)) return lista;

            var linhas = saida.Split(
                new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var linha in linhas)
            {
                if (!linha.Contains(SEP)) continue;

                var p = linha.Split(new[] { SEP }, StringSplitOptions.None);
                if (p.Length < 1 || string.IsNullOrWhiteSpace(p[0])) continue;

                string nome = p[0].Trim();
                if (!vistos.Add(nome)) continue;

                lista.Add(new ProgramaInfo
                {
                    Nome = nome,
                    Versao = p.Length > 1 && !string.IsNullOrWhiteSpace(p[1])
                        ? p[1].Trim() : "—",
                    Fabricante = p.Length > 2 && !string.IsNullOrWhiteSpace(p[2])
                        ? p[2].Trim() : "—",
                    DataInstalacao = p.Length > 3 && !string.IsNullOrWhiteSpace(p[3])
                        ? p[3].Trim() : "—",
                    Maquina = maquina ?? "LOCAL"
                });
            }

            return lista;
        }
    }
}