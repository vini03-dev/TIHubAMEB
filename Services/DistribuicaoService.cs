using TIHubAMEB.Models;

namespace TIHubAMEB.Services
{
    /// <summary>
    /// Copia arquivos para múltiplas máquinas via compartilhamento C$.
    /// Suporta Desktop Público em português e inglês automaticamente.
    /// </summary>
    public class DistribuicaoService
    {
        private readonly LogService _log;

        public DistribuicaoService(LogService log)
        {
            _log = log;
        }

        // ── Distribuição principal ────────────────────────────────────────

        public async Task<ResultadoDistribuicao> DistribuirAsync(
            IEnumerable<string> maquinas,
            IEnumerable<string> caminhoArquivos,
            IProgress<(int pct, string maquina, bool sucesso)>? progresso = null,
            CancellationToken ct = default)
        {
            var listaMaquinas = maquinas.ToList();
            var listaArquivos = caminhoArquivos.ToList();

            if (listaMaquinas.Count == 0 || listaArquivos.Count == 0)
                return new ResultadoDistribuicao();

            var resultado = new ResultadoDistribuicao();
            var semaforo = new SemaphoreSlim(15);
            int concluido = 0;

            var tarefas = listaMaquinas.Select(async maquina =>
            {
                await semaforo.WaitAsync(ct);
                try
                {
                    bool ok = await CopiarParaMaquinaAsync(
                        maquina, listaArquivos);

                    Interlocked.Increment(ref concluido);
                    int pct = (int)((double)concluido /
                        listaMaquinas.Count * 100);

                    progresso?.Report((pct, maquina, ok));

                    lock (resultado)
                    {
                        if (ok) resultado.Sucesso.Add(maquina);
                        else resultado.Falhou.Add(maquina);
                    }
                }
                finally
                {
                    semaforo.Release();
                }
            });

            await Task.WhenAll(tarefas);

            _log.Registrar("REDE", "Distribuição de arquivos",
                $"{resultado.Sucesso.Count}/{listaMaquinas.Count} máquinas — " +
                string.Join(", ", listaArquivos.Select(Path.GetFileName)),
                resultado.Falhou.Count == 0,
                TipoLog.Rede);

            return resultado;
        }

        // ── Cópia para uma máquina ────────────────────────────────────────

        private async Task<bool> CopiarParaMaquinaAsync(
            string maquina, List<string> arquivos)
        {
            int totalArqs = arquivos.Count;
            var copias = arquivos.ToList();
            LogService log = _log; // captura local para o lambda

            return await Task.Run(() =>
            {
                try
                {
                    // ── Detecta idioma da pasta Desktop Público ───────────
                    string raiz = $@"\\{maquina}\C$\Users\Public";
                    string desktopPT = Path.Combine(raiz, "Área de Trabalho");
                    string desktopEN = Path.Combine(raiz, "Desktop");
                    string destino;

                    if (Directory.Exists(desktopPT))
                        destino = desktopPT;
                    else if (Directory.Exists(desktopEN))
                        destino = desktopEN;
                    else
                    {
                        log.Registrar(maquina, "Distribuição",
                            $"Desktop Público não encontrado em {raiz} " +
                            "(nem PT nem EN) — C$ inacessível ou offline",
                            false, TipoLog.Erro);
                        return false;
                    }

                    // ── Copia cada arquivo ───────────────────────────────
                    int copiados = 0;
                    foreach (string arquivo in copias)
                    {
                        if (!File.Exists(arquivo))
                        {
                            log.Registrar(maquina, "Distribuição",
                                $"Arquivo não encontrado: " +
                                $"{Path.GetFileName(arquivo)}",
                                false, TipoLog.Erro);
                            continue;
                        }

                        string nome = Path.GetFileName(arquivo);
                        string alvoCaminho = Path.Combine(destino, nome);

                        File.Copy(arquivo, alvoCaminho, overwrite: true);
                        copiados++;
                    }

                    string pastaUsada = destino.Contains("Área")
                        ? "Desktop Público (PT)"
                        : "Desktop Público (EN)";

                    log.Registrar(maquina, "Distribuição",
                        $"{copiados}/{totalArqs} arquivo(s) → {pastaUsada}",
                        copiados > 0, TipoLog.Rede);

                    return copiados > 0;
                }
                catch (Exception ex)
                {
                    log.RegistrarErro(maquina, "Distribuição", ex);
                    return false;
                }
            });
        }
    }

    // ── Resultado da distribuição ─────────────────────────────────────────

    public class ResultadoDistribuicao
    {
        public List<string> Sucesso { get; } = new();
        public List<string> Falhou { get; } = new();
        public int Total => Sucesso.Count + Falhou.Count;
    }
}