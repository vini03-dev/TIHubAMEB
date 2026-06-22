using System.Net.NetworkInformation;
using System.Text.Json;
using SnmpSharpNet;
using TIHubAMEB.Models;

namespace TIHubAMEB.Services
{
    /// <summary>
    /// Carrega o cadastro de impressoras do arquivo externo
    /// Data/impressoras.json (fora do controle de versão) e consulta
    /// o status real de cada uma via SNMP, com fallback para ping
    /// quando o SNMP não responde.
    /// </summary>
    public class ImpressoraService
    {
        private readonly LogService _log;

        // Caminho do arquivo real, ao lado do executável
        private static readonly string CaminhoArquivoReal = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Data", "impressoras.json");

        // Caminho do arquivo de exemplo — usado como aviso se o real não existir
        private static readonly string CaminhoArquivoExemplo = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Data", "impressoras.exemplo.json");

        // OIDs padrão do Printer-MIB (RFC 1759) — funcionam na
        // grande maioria das multifuncionais Lexmark, HP, Brother etc.
        private const string OidDescricao = "1.3.6.1.2.1.1.1.0";
        private const string OidNivelTonerAtual = "1.3.6.1.2.1.43.11.1.1.9.1.1";
        private const string OidNivelTonerMax = "1.3.6.1.2.1.43.11.1.1.8.1.1";
        private const string OidContadorPaginas = "1.3.6.1.2.1.43.10.2.1.4.1.1";

        public ImpressoraService(LogService log)
        {
            _log = log;
        }

        // ── Carregar cadastro do JSON ────────────────────────────────────

        /// <summary>
        /// Lê Data/impressoras.json. Se não existir, registra aviso
        /// no log orientando a copiar o arquivo de exemplo.
        /// </summary>
        public async Task<List<ImpressoraInfo>> CarregarCadastroAsync()
        {
            if (!File.Exists(CaminhoArquivoReal))
            {
                bool temExemplo = File.Exists(CaminhoArquivoExemplo);

                _log.Registrar("LOCAL", "Impressoras",
                    temExemplo
                        ? "Arquivo Data/impressoras.json não encontrado. " +
                          "Copie Data/impressoras.exemplo.json, renomeie " +
                          "para impressoras.json e preencha com seus dados reais."
                        : "Arquivo Data/impressoras.json não encontrado.",
                    false, TipoLog.Erro);

                return new List<ImpressoraInfo>();
            }

            try
            {
                string json = await File.ReadAllTextAsync(CaminhoArquivoReal);

                var cadastros = JsonSerializer.Deserialize<List<ImpressoraCadastroJson>>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<ImpressoraCadastroJson>();

                var lista = cadastros.Select(c => new ImpressoraInfo
                {
                    Nome = c.Nome,
                    IP = c.Ip,
                    Modelo = c.Modelo,
                    Localizacao = c.Localizacao,
                    SerialNumber = c.SerialNumber,
                    StatusDescricao = "Não verificado"
                }).ToList();

                _log.Registrar("LOCAL", "Impressoras",
                    $"{lista.Count} impressoras carregadas do cadastro",
                    true, TipoLog.Info);

                return lista;
            }
            catch (Exception ex)
            {
                _log.RegistrarErro("LOCAL", "Carregar cadastro de impressoras", ex);
                return new List<ImpressoraInfo>();
            }
        }

        // ── Verificar status de todas (paralelo) ─────────────────────────

        /// <summary>
        /// Consulta status (SNMP, com fallback de ping) de todas as
        /// impressoras em paralelo, sem travar a UI.
        /// </summary>
        public async Task VerificarTodasAsync(
            List<ImpressoraInfo> impressoras,
            IProgress<(int pct, string nome)>? progresso = null)
        {
            if (impressoras.Count == 0) return;

            int total = impressoras.Count;
            int concluido = 0;
            var semaforo = new SemaphoreSlim(15); // SNMP é mais pesado que ping

            var tarefas = impressoras.Select(async imp =>
            {
                await semaforo.WaitAsync();
                try
                {
                    await VerificarUmaAsync(imp);

                    Interlocked.Increment(ref concluido);
                    int pct = (int)((double)concluido / total * 100);
                    progresso?.Report((pct, imp.Nome));
                }
                finally
                {
                    semaforo.Release();
                }
            });

            await Task.WhenAll(tarefas);

            int online = impressoras.Count(i => i.Online);
            int alerta = impressoras.Count(i => i.TemAlerta);

            _log.Registrar("REDE", "Verificar impressoras",
                $"{online}/{total} online, {alerta} com alerta",
                true, TipoLog.Info);
        }

        /// <summary>
        /// Verifica uma impressora: tenta SNMP primeiro (dados ricos),
        /// se não responder cai para ping simples (status básico).
        /// </summary>
        private async Task VerificarUmaAsync(ImpressoraInfo imp)
        {
            // Tenta SNMP primeiro — é mais informativo
            bool snmpOk = await TentarSnmpAsync(imp);

            if (snmpOk)
            {
                imp.Online = true;
                imp.ConsultaSnmpOk = true;
                imp.StatusDescricao = "Consultado via SNMP";
                return;
            }

            // Fallback — ping simples
            bool online = await PingAsync(imp.IP);
            imp.Online = online;
            imp.ConsultaSnmpOk = false;
            imp.NivelTonerPreto = -1;
            imp.ContadorPaginas = -1;
            imp.StatusDescricao = online
                ? "Online (SNMP não respondeu — verifique se está habilitado)"
                : "Sem resposta";
        }

        // ── SNMP ─────────────────────────────────────────────────────────

        private async Task<bool> TentarSnmpAsync(ImpressoraInfo imp)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Depois:
                    var param = new AgentParameters(SnmpVersion.Ver1,
                        new OctetString("public")); // padrão de fábrica

                    var target = new UdpTarget(
                        System.Net.IPAddress.Parse(imp.IP), 161, 2000, 1);

                    var pdu = new Pdu(PduType.Get);
                    pdu.VbList.Add(OidNivelTonerAtual);
                    pdu.VbList.Add(OidNivelTonerMax);
                    pdu.VbList.Add(OidContadorPaginas);

                    var resultado = target.Request(pdu, param);
                    target.Close();

                    if (resultado == null || resultado.Pdu.ErrorStatus != 0)
                        return false;

                    var vbs = resultado.Pdu.VbList;
                    if (vbs.Count < 2) return false;

                    int atual = ExtrairInteiro(vbs[0].Value);
                    int max = ExtrairInteiro(vbs[1].Value);

                    if (atual < 0 || max <= 0) return false;

                    imp.NivelTonerPreto = (int)Math.Round(
                        (double)atual / max * 100);

                    if (vbs.Count >= 3)
                        imp.ContadorPaginas = ExtrairInteiro(vbs[2].Value);

                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        private static int ExtrairInteiro(AsnType valor)
        {
            try
            {
                return valor switch
                {
                    Integer32 i => i.Value,
                    Counter32 c => (int)c.Value,
                    Gauge32 g => (int)g.Value,
                    _ => -1
                };
            }
            catch { return -1; }
        }

        // ── Ping fallback ────────────────────────────────────────────────

        private static async Task<bool> PingAsync(string ip)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ip, 1500);
                return reply.Status == IPStatus.Success;
            }
            catch { return false; }
        }

        // ── Filtro de busca ──────────────────────────────────────────────

        /// <summary>
        /// Filtra por nome, localização, modelo ou IP simultaneamente.
        /// </summary>
        public List<ImpressoraInfo> Filtrar(
            List<ImpressoraInfo> lista, string termo)
        {
            if (string.IsNullOrWhiteSpace(termo)) return lista;

            string t = termo.Trim();

            return lista.Where(i =>
                Contem(i.Nome, t) ||
                Contem(i.Localizacao, t) ||
                Contem(i.Modelo, t) ||
                Contem(i.IP, t))
                .ToList();
        }

        private static bool Contem(string valor, string termo) =>
            !string.IsNullOrEmpty(valor) &&
            valor.Contains(termo, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Ordena com impressoras em alerta (offline ou toner crítico)
        /// no topo da lista — para chamar atenção imediata.
        /// </summary>
        public List<ImpressoraInfo> OrdenarComAlertasNoTopo(
            List<ImpressoraInfo> lista)
        {
            return lista
                .OrderByDescending(i => i.TemAlerta)
                .ThenBy(i => i.Localizacao)
                .ToList();
        }

        // ── Ações ────────────────────────────────────────────────────────

        /// <summary>Abre o painel web da impressora no navegador padrão.</summary>
        public void AbrirPainelWeb(ImpressoraInfo imp)
        {
            try
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = $"http://{imp.IP}",
                        UseShellExecute = true
                    });

                _log.Registrar(imp.Nome, "Painel Web",
                    $"Aberto http://{imp.IP}", true, TipoLog.Info);
            }
            catch (Exception ex)
            {
                _log.RegistrarErro(imp.Nome, "Painel Web", ex);
            }
        }

        /// <summary>Abre a fila de impressão do Windows para a impressora.</summary>
        public void AbrirFilaImpressao(ImpressoraInfo imp)
        {
            try
            {
                // Abre o compartilhamento de rede no servidor de impressão
                // (mesmo caminho usado manualmente: \\islandia\NomeDaImpressora)
                string caminhoRede = $@"\\islandia\{imp.Nome}";

                System.Diagnostics.Process.Start("explorer.exe", caminhoRede);

                _log.Registrar(imp.Nome, "Fila de Impressão",
                    $"Aberta via {caminhoRede}", true, TipoLog.Info);
            }
            catch (Exception ex)
            {
                _log.RegistrarErro(imp.Nome, "Fila de Impressão", ex);
            }
        }

        /// <summary>
        /// Limpa a fila de impressão travada via PowerShell
        /// (reinicia o serviço de spooler e limpa arquivos pendentes).
        /// </summary>
        public async Task<bool> LimparFilaAsync(ImpressoraInfo imp)
        {
            string script =
                "Stop-Service -Name spooler -Force -ErrorAction SilentlyContinue; " +
                "Remove-Item -Path \"C:\\Windows\\System32\\spool\\PRINTERS\\*\" " +
                "-Force -ErrorAction SilentlyContinue; " +
                "Start-Service -Name spooler -ErrorAction SilentlyContinue; " +
                "Write-Output 'Fila limpa'";

            try
            {
                using var ps = new System.Diagnostics.Process();
                ps.StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NonInteractive -ExecutionPolicy Bypass -Command \"{script}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas"
                };
                ps.Start();
                await ps.WaitForExitAsync();

                _log.Registrar(imp.Nome, "Limpar Fila",
                    "Spooler reiniciado e fila limpa", true, TipoLog.Info);
                return true;
            }
            catch (Exception ex)
            {
                _log.RegistrarErro(imp.Nome, "Limpar Fila", ex);
                return false;
            }
        }
    }

    // Classe interna usada apenas para casar com os nomes do JSON
    // (ip, serialNumber em minúsculo) sem expor isso no Model público
    internal class ImpressoraCadastroJson
    {
        public string Nome { get; set; } = string.Empty;
        public string Ip { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public string Localizacao { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
    }
}