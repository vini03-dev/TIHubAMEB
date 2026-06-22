using System.Management;
using System.Net;
using TIHubAMEB.Models;

namespace TIHubAMEB.Services
{
    /// <summary>
    /// Coleta métricas de hardware local ou remoto via WMI.
    /// Roda em background — nunca trava a interface.
    /// </summary>
    public class MonitoramentoService : IDisposable
    {
        private readonly LogService _log;

        // Máquina sendo monitorada (null = local)
        private string? _maquinaAtual;

        // Último resultado coletado — UI lê daqui sem esperar
        private MaquinaInfo _ultimoResultado = new();

        // Evita coletas paralelas
        private bool _coletando = false;

        public MonitoramentoService(LogService log)
        {
            _log = log;
        }

        // ── Controle de máquina ──────────────────────────────────────────

        /// <summary>
        /// Define qual PC monitorar.
        /// Passar null ou vazio = volta para o PC local.
        /// </summary>
        public void DefinirMaquina(string? maquina)
        {
            _maquinaAtual = string.IsNullOrWhiteSpace(maquina)
                ? null : maquina.Trim();
            _ultimoResultado = new MaquinaInfo(); // limpa dados antigos

            _log.Registrar(
                _maquinaAtual ?? "LOCAL",
                "Monitoramento",
                $"Monitorando: {_maquinaAtual ?? "PC local"}",
                true, TipoLog.Info);
        }

        public bool EstaMonitorandoRemoto =>
            !string.IsNullOrWhiteSpace(_maquinaAtual);

        public string MaquinaAtual =>
            _maquinaAtual ?? Environment.MachineName;

        // ── Coleta ───────────────────────────────────────────────────────

        /// <summary>
        /// Retorna último resultado coletado (instantâneo).
        /// Dispara nova coleta em background para a próxima chamada.
        /// </summary>
        public MaquinaInfo ObterInfoAtual()
        {
            if (!_coletando)
                _ = ColetarEmBackgroundAsync();

            return _ultimoResultado;
        }

        private async Task ColetarEmBackgroundAsync()
        {
            _coletando = true;
            try
            {
                _ultimoResultado = await Task.Run(() =>
                    EstaMonitorandoRemoto
                        ? ObterInfoRemota(_maquinaAtual!)
                        : ObterInfoLocal());
            }
            catch (Exception ex)
            {
                _log.RegistrarErro(
                    _maquinaAtual ?? "LOCAL", "Monitoramento", ex);
            }
            finally
            {
                _coletando = false;
            }
        }

        // ── Local ────────────────────────────────────────────────────────

        private MaquinaInfo ObterInfoLocal()
        {
            try
            {
                // CPU
                using var cpu = new System.Diagnostics.PerformanceCounter(
                    "Processor", "% Processor Time", "_Total");
                cpu.NextValue();
                System.Threading.Thread.Sleep(300);
                float cpuVal = cpu.NextValue();

                // RAM
                using var ramCounter = new System.Diagnostics.PerformanceCounter(
                    "Memory", "Available MBytes");
                float dispMB = ramCounter.NextValue();
                long totalBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
                float totalMB = totalBytes / 1024f / 1024f;
                float ramPct = totalMB > 0
                    ? Math.Min(100f, (totalMB - dispMB) / totalMB * 100f)
                    : 0f;

                // Disco
                var drive = new DriveInfo("C");
                float discoPct = drive.IsReady
                    ? (float)(drive.TotalSize - drive.AvailableFreeSpace)
                        / drive.TotalSize * 100f
                    : 0f;
                long discoTotal = drive.IsReady
                    ? drive.TotalSize / 1024 / 1024 / 1024 : 0;
                long discoLivre = drive.IsReady
                    ? drive.AvailableFreeSpace / 1024 / 1024 / 1024 : 0;

                // Sistema operacional
                string so = ObterSistemaOperacionalLocal();

                return new MaquinaInfo
                {
                    NomeMaquina = Environment.MachineName,
                    IP = ObterIPLocal(),
                    UsuarioLogado = Environment.UserName,
                    SistemaOp = so,
                    Dominio = Environment.UserDomainName,
                    Setor = MaquinaInfo.DetectarSetor(
                        Environment.MachineName),
                    CpuPercent = cpuVal,
                    RamPercent = ramPct,
                    RamTotalMB = (long)totalMB,
                    DiscoPercent = discoPct,
                    DiscoTotalGB = discoTotal,
                    DiscoLivreGB = discoLivre,
                    TempoLigado = TimeSpan.FromMilliseconds(
                        Environment.TickCount64),
                    Online = true,
                    Modo = "LOCAL",
                    UltimaAtualizacao = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _log.RegistrarErro("LOCAL", "Monitoramento local", ex);
                return new MaquinaInfo { Online = false, Modo = "LOCAL" };
            }
        }

        // ── Remoto via WMI ───────────────────────────────────────────────

        private MaquinaInfo ObterInfoRemota(string maquina)
        {
            try
            {
                var options = new ConnectionOptions
                {
                    Impersonation = ImpersonationLevel.Impersonate,
                    Authentication = AuthenticationLevel.PacketPrivacy,
                    EnablePrivileges = true,
                    Timeout = TimeSpan.FromSeconds(8)
                };

                var scope = new ManagementScope(
                    $@"\\{maquina}\root\cimv2", options);
                scope.Connect();

                // Coleta todos os dados
                float cpu = ObterCpuWmi(scope);
                float ram = ObterRamWmi(scope, out long totalRam);
                float disco = ObterDiscoWmi(scope,
                    out long totalDisco, out long livreDisco);
                string usuario = ObterUsuarioWmi(scope);
                string ip = ObterIPWmi(scope);
                string so = ObterSistemaWmi(scope);
                TimeSpan uptime = ObterUptimeWmi(scope);

                return new MaquinaInfo
                {
                    NomeMaquina = maquina.ToUpper(),
                    IP = ip,
                    UsuarioLogado = usuario,
                    SistemaOp = so,
                    Setor = MaquinaInfo.DetectarSetor(maquina),
                    CpuPercent = cpu,
                    RamPercent = ram,
                    RamTotalMB = totalRam,
                    DiscoPercent = disco,
                    DiscoTotalGB = totalDisco,
                    DiscoLivreGB = livreDisco,
                    TempoLigado = uptime,
                    Online = true,
                    Modo = "REMOTO",
                    UltimaAtualizacao = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _log.RegistrarErro(maquina, "WMI", ex);
                return new MaquinaInfo
                {
                    NomeMaquina = maquina.ToUpper(),
                    Online = false,
                    Modo = "REMOTO"
                };
            }
        }

        // ── Queries WMI individuais ───────────────────────────────────────

        private static float ObterCpuWmi(ManagementScope scope)
        {
            using var q = new ManagementObjectSearcher(scope,
                new ObjectQuery(
                    "SELECT LoadPercentage FROM Win32_Processor"));
            q.Options.Timeout = TimeSpan.FromSeconds(5);
            float t = 0; int c = 0;
            foreach (ManagementObject o in q.Get())
            { t += Convert.ToSingle(o["LoadPercentage"]); c++; }
            return c > 0 ? t / c : 0f;
        }

        private static float ObterRamWmi(ManagementScope scope,
            out long totalMB)
        {
            using var q = new ManagementObjectSearcher(scope,
                new ObjectQuery(
                    "SELECT TotalVisibleMemorySize,FreePhysicalMemory " +
                    "FROM Win32_OperatingSystem"));
            q.Options.Timeout = TimeSpan.FromSeconds(5);
            foreach (ManagementObject o in q.Get())
            {
                long total = Convert.ToInt64(o["TotalVisibleMemorySize"]);
                long free = Convert.ToInt64(o["FreePhysicalMemory"]);
                totalMB = total / 1024;
                return total > 0
                    ? Math.Min(100f, (float)(total - free) / total * 100f)
                    : 0f;
            }
            totalMB = 0;
            return 0f;
        }

        private static float ObterDiscoWmi(ManagementScope scope,
            out long totalGB, out long livreGB)
        {
            using var q = new ManagementObjectSearcher(scope,
                new ObjectQuery(
                    "SELECT Size,FreeSpace FROM Win32_LogicalDisk " +
                    "WHERE DeviceID='C:'"));
            q.Options.Timeout = TimeSpan.FromSeconds(5);
            foreach (ManagementObject o in q.Get())
            {
                long size = Convert.ToInt64(o["Size"]);
                long free = Convert.ToInt64(o["FreeSpace"]);
                totalGB = size / 1024 / 1024 / 1024;
                livreGB = free / 1024 / 1024 / 1024;
                return size > 0
                    ? (float)(size - free) / size * 100f
                    : 0f;
            }
            totalGB = 0; livreGB = 0;
            return 0f;
        }

        private static string ObterUsuarioWmi(ManagementScope scope)
        {
            try
            {
                using var q = new ManagementObjectSearcher(scope,
                    new ObjectQuery(
                        "SELECT UserName FROM Win32_ComputerSystem"));
                q.Options.Timeout = TimeSpan.FromSeconds(5);
                foreach (ManagementObject o in q.Get())
                {
                    string? u = o["UserName"]?.ToString();
                    if (!string.IsNullOrEmpty(u))
                    {
                        int b = u.IndexOf('\\');
                        return b >= 0 ? u[(b + 1)..] : u;
                    }
                }
            }
            catch { }
            return "—";
        }

        private static string ObterIPWmi(ManagementScope scope)
        {
            try
            {
                using var q = new ManagementObjectSearcher(scope,
                    new ObjectQuery(
                        "SELECT IPAddress FROM " +
                        "Win32_NetworkAdapterConfiguration " +
                        "WHERE IPEnabled=TRUE"));
                q.Options.Timeout = TimeSpan.FromSeconds(5);
                foreach (ManagementObject o in q.Get())
                {
                    if (o["IPAddress"] is string[] ips)
                    {
                        string? ip = ips.FirstOrDefault(i =>
                            i.Contains('.') &&
                            !i.StartsWith("169.254") &&
                            !i.StartsWith("127."));
                        if (ip != null) return ip;
                    }
                }
            }
            catch { }
            return "—";
        }

        private static string ObterSistemaWmi(ManagementScope scope)
        {
            try
            {
                using var q = new ManagementObjectSearcher(scope,
                    new ObjectQuery(
                        "SELECT Caption FROM Win32_OperatingSystem"));
                q.Options.Timeout = TimeSpan.FromSeconds(5);
                foreach (ManagementObject o in q.Get())
                {
                    string? caption = o["Caption"]?.ToString();
                    if (!string.IsNullOrEmpty(caption))
                    {
                        // Simplifica: "Microsoft Windows 10 Pro" → "Win 10"
                        if (caption.Contains("11")) return "Win 11";
                        if (caption.Contains("10")) return "Win 10";
                        if (caption.Contains("7")) return "Win 7";
                        return caption;
                    }
                }
            }
            catch { }
            return "—";
        }

        private static TimeSpan ObterUptimeWmi(ManagementScope scope)
        {
            try
            {
                using var q = new ManagementObjectSearcher(scope,
                    new ObjectQuery(
                        "SELECT LastBootUpTime " +
                        "FROM Win32_OperatingSystem"));
                q.Options.Timeout = TimeSpan.FromSeconds(5);
                foreach (ManagementObject o in q.Get())
                {
                    string? raw = o["LastBootUpTime"]?.ToString();
                    if (raw != null)
                    {
                        var boot = ManagementDateTimeConverter.ToDateTime(raw);
                        return DateTime.Now - boot;
                    }
                }
            }
            catch { }
            return TimeSpan.Zero;
        }

        // ── Utilitários locais ───────────────────────────────────────────

        private static string ObterIPLocal()
        {
            try
            {
                return Dns.GetHostAddresses(Dns.GetHostName())
                    .FirstOrDefault(a =>
                        a.AddressFamily ==
                        System.Net.Sockets.AddressFamily.InterNetwork &&
                        !a.ToString().StartsWith("169.254") &&
                        !a.ToString().StartsWith("127."))
                    ?.ToString() ?? "—";
            }
            catch { return "—"; }
        }

        private static string ObterSistemaOperacionalLocal()
        {
            try
            {
                var so = System.Runtime.InteropServices
                    .RuntimeInformation.OSDescription;
                if (so.Contains("11")) return "Win 11";
                if (so.Contains("10")) return "Win 10";
                return so;
            }
            catch { return "—"; }
        }

        public void Dispose() { }
    }
}