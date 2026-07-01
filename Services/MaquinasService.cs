using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Management;
using System.Net.NetworkInformation;
using TIHubAMEB.Models;

namespace TIHubAMEB.Services
{
    /// <summary>
    /// Busca máquinas no AD filtrando servidores pelo SO,
    /// verifica status online e pega usuário conectado via WMI.
    /// </summary>
    public class MaquinasService
    {
        private readonly LogService _log;
        private readonly RedeService _rede;

        private List<MaquinaRede> _cacheMaquinas = new();
        private DateTime _ultimaBusca = DateTime.MinValue;
        private readonly TimeSpan _intervaloBusca = TimeSpan.FromMinutes(5);

        public MaquinasService(LogService log, RedeService rede)
        {
            _log = log;
            _rede = rede;
        }

        // ── Busca no AD ───────────────────────────────────────────────────

        public async Task<List<MaquinaRede>> BuscarMaquinasADAsync(
            bool forcarAtualizacao = false,
            IProgress<string>? progresso = null)
        {
            bool cacheValido =
                _cacheMaquinas.Count > 0 &&
                DateTime.Now - _ultimaBusca < _intervaloBusca;

            if (cacheValido && !forcarAtualizacao)
            {
                progresso?.Report($"Cache: {_cacheMaquinas.Count} máquinas");
                return _cacheMaquinas;
            }

            progresso?.Report("Conectando ao Active Directory...");

            var maquinas = await Task.Run(() => BuscarNoAD(progresso));

            if (maquinas.Count > 0)
            {
                _cacheMaquinas = maquinas;
                _ultimaBusca = DateTime.Now;
            }

            _log.Registrar("AD", "Busca de máquinas",
                $"{maquinas.Count} máquinas encontradas (servidores filtrados)",
                true, TipoLog.Maquinas);

            return maquinas;
        }

        private List<MaquinaRede> BuscarNoAD(IProgress<string>? progresso)
        {
            var lista = new List<MaquinaRede>();

            try
            {
                using var contexto = new PrincipalContext(ContextType.Domain);
                using var buscador = new ComputerPrincipal(contexto);
                buscador.Name = "*";

                using var resultados = new PrincipalSearcher(buscador);
                int total = 0;

                foreach (var resultado in resultados.FindAll())
                {
                    if (resultado is not ComputerPrincipal pc) continue;

                    string nome = pc.Name?.ToUpper() ?? string.Empty;
                    if (string.IsNullOrEmpty(nome)) continue;

                    try
                    {
                        // ── Lê o sistema operacional do AD ───────────────
                        string so = string.Empty;
                        if (resultado.GetUnderlyingObject() is DirectoryEntry entry)
                        {
                            so = entry.Properties["operatingSystem"]
                                .Value?.ToString() ?? string.Empty;
                        }

                        // Ignora servidores pelo SO — muito mais confiável
                        // que filtrar por nome
                        if (so.Contains("Server",
                            StringComparison.OrdinalIgnoreCase))
                            continue;

                        // Monta objeto da máquina
                        var maquina = new MaquinaRede
                        {
                            Nome = nome,
                            Descricao = pc.Description ?? string.Empty,
                            UltimaVez = pc.LastLogon ?? DateTime.MinValue,
                            Setor = MaquinaRede.DetectarSetor(nome),
                            DistinguishedName = pc.DistinguishedName
                                ?? string.Empty,
                            UO = ExtrairOU(
                                pc.DistinguishedName ?? string.Empty),

                            // Simplifica nome do SO
                            SistemaOp = so.Contains("11") ? "Win 11"
                                      : so.Contains("10") ? "Win 10"
                                      : so.Contains("7") ? "Win 7"
                                      : string.IsNullOrEmpty(so) ? "—"
                                      : so
                        };

                        lista.Add(maquina);
                        total++;

                        if (total % 10 == 0)
                            progresso?.Report(
                                $"Encontradas {total} máquinas...");
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                _log.RegistrarErro("AD", "Busca AD", ex);
            }

            return lista
                .OrderBy(m => m.Setor)
                .ThenBy(m => m.Nome)
                .ToList();
        }

        // ── Verificar status + usuário conectado ──────────────────────────

        public async Task VerificarStatusAsync(
            List<MaquinaRede> maquinas,
            IProgress<(int pct, string maquina)>? progresso = null)
        {
            if (maquinas.Count == 0) return;

            int total = maquinas.Count;
            int concluido = 0;
            var semaforoPing = new SemaphoreSlim(20);
            var semaforoWmi = new SemaphoreSlim(10);
            var tarefasWmi = new System.Collections.Concurrent.ConcurrentBag<Task>();

            var tarefas = maquinas.Select(async maquina =>
            {
                await semaforoPing.WaitAsync();
                try
                {
                    var (online, ms) = await PingRapidoAsync(maquina.Nome);
                    maquina.Online = online;
                    maquina.PingMs = ms;

                    if (online)
                    {
                        // IP via DNS
                        try
                        {
                            var ips = await System.Net.Dns
                                .GetHostAddressesAsync(maquina.Nome);
                            maquina.IP = ips
                                .FirstOrDefault(a =>
                                    a.AddressFamily ==
                                    System.Net.Sockets.AddressFamily
                                        .InterNetwork)
                                ?.ToString() ?? string.Empty;
                        }
                        catch { }

                        // WMI com semáforo próprio — não bloqueia o ping em massa
                        tarefasWmi.Add(BuscarDadosWmiAsync(maquina, semaforoWmi));
                    }

                    Interlocked.Increment(ref concluido);
                    int pct = (int)((double)concluido / total * 100);
                    progresso?.Report((pct, maquina.Nome));
                }
                finally
                {
                    semaforoPing.Release();
                }
            });

            await Task.WhenAll(tarefas);

            // Aguarda todos os WMI terminarem antes de retornar —
            // elimina race condition com nova verificação e garante
            // que usuário/modelo estejam preenchidos ao atualizar a lista
            if (!tarefasWmi.IsEmpty)
                await Task.WhenAll(tarefasWmi);

            int online2 = maquinas.Count(m => m.Online);
            _log.Registrar("REDE", "Verificar status",
                $"{online2}/{total} online", true, TipoLog.Maquinas);
        }

        private static async Task BuscarDadosWmiAsync(
            MaquinaRede maquina, SemaphoreSlim semaforo)
        {
            await semaforo.WaitAsync();
            try
            {
                await Task.Run(() =>
                {
                    ObterDadosWmi(maquina.Nome,
                        out string usuario,
                        out string modelo,
                        out string serviceTag);
                    maquina.UsuarioConectado = usuario;
                    maquina.Modelo = modelo;
                    maquina.ServiceTag = serviceTag;
                });
            }
            finally
            {
                semaforo.Release();
            }
        }

        // Busca usuário conectado, modelo e service tag numa só conexão WMI.
        // Mais eficiente que abrir 3 conexões separadas por máquina.
        private static void ObterDadosWmi(string maquina,
            out string usuario, out string modelo, out string serviceTag)
        {
            usuario = "—";
            modelo = "—";
            serviceTag = "—";

            try
            {
                var options = new ConnectionOptions
                {
                    Impersonation = ImpersonationLevel.Impersonate,
                    Authentication = AuthenticationLevel.PacketPrivacy,
                    EnablePrivileges = true,
                    Timeout = TimeSpan.FromSeconds(5)
                };

                var scope = new ManagementScope(
                    $@"\\{maquina}\root\cimv2", options);
                scope.Connect();

                // Usuário + Modelo (mesma classe Win32_ComputerSystem)
                using (var q = new ManagementObjectSearcher(scope,
                    new ObjectQuery(
                        "SELECT UserName,Manufacturer,Model " +
                        "FROM Win32_ComputerSystem")))
                {
                    q.Options.Timeout = TimeSpan.FromSeconds(4);
                    foreach (ManagementObject o in q.Get())
                    {
                        string? u = o["UserName"]?.ToString();
                        if (!string.IsNullOrEmpty(u))
                        {
                            int b = u.IndexOf('\\');
                            usuario = b >= 0 ? u[(b + 1)..] : u;
                        }

                        string fab = o["Manufacturer"]?.ToString()?.Trim() ?? "";
                        string mod = o["Model"]?.ToString()?.Trim() ?? "";
                        modelo = MaquinaRede.MontarModelo(fab, mod);
                        break;
                    }
                }

                // Service Tag via Win32_BIOS
                using (var q = new ManagementObjectSearcher(scope,
                    new ObjectQuery(
                        "SELECT SerialNumber FROM Win32_BIOS")))
                {
                    q.Options.Timeout = TimeSpan.FromSeconds(4);
                    foreach (ManagementObject o in q.Get())
                    {
                        string sn = o["SerialNumber"]?.ToString()?.Trim() ?? "";
                        if (!string.IsNullOrEmpty(sn)) serviceTag = sn;
                        break;
                    }
                }
            }
            catch { }
        }


        // ── Filtros ───────────────────────────────────────────────────────

        /// <summary>
        /// Filtra em TODAS as colunas simultaneamente.
        /// Busca em: nome, setor, IP, sistema, usuário conectado, OU.
        /// </summary>
        public List<MaquinaRede> Filtrar(
            List<MaquinaRede> maquinas, string termo)
        {
            if (string.IsNullOrWhiteSpace(termo))
                return maquinas;

            string t = termo.Trim();

            return maquinas.Where(m =>
                Contem(m.Nome, t) ||
                Contem(m.Setor, t) ||
                Contem(m.IP, t) ||
                Contem(m.SistemaOp, t) ||
                Contem(m.UsuarioConectado, t) ||
                Contem(m.UO, t) ||
                Contem(m.UltimaVezFormatado, t))
            .ToList();
        }

        private static bool Contem(string valor, string termo) =>
            !string.IsNullOrEmpty(valor) &&
            valor.Contains(termo, StringComparison.OrdinalIgnoreCase);

        public List<MaquinaRede> FiltrarPorSetor(
            List<MaquinaRede> maquinas, string setor)
        {
            if (string.IsNullOrWhiteSpace(setor) ||
                setor.Equals("Todos", StringComparison.OrdinalIgnoreCase))
                return maquinas;

            return maquinas
                .Where(m => m.Setor.Equals(
                    setor, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public List<string> ObterSetores(List<MaquinaRede> maquinas) =>
            maquinas
                .Select(m => m.Setor)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .OrderBy(s => s)
                .ToList();

        // ── Utilitários ──────────────────────────────────────────────────

        private static async Task<(bool, long)> PingRapidoAsync(string host)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(host, 1500);
                return (reply.Status == IPStatus.Success,
                        reply.RoundtripTime);
            }
            catch { return (false, 0); }
        }

        private static string ExtrairOU(string dn)
        {
            if (string.IsNullOrEmpty(dn)) return string.Empty;
            var partes = dn.Split(',');
            foreach (var parte in partes)
                if (parte.TrimStart().StartsWith("OU=",
                    StringComparison.OrdinalIgnoreCase))
                    return parte.Trim()[3..];
            return string.Empty;
        }
    }
}