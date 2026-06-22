using System.Net.NetworkInformation;
using TIHubAMEB.Models;

namespace TIHubAMEB.Services
{
    /// <summary>
    /// Operações de rede — ping, flush DNS, controle remoto de máquinas.
    /// Todas as operações são assíncronas e não travam a UI.
    /// </summary>
    public class RedeService
    {
        private readonly PsExecService _psExec;
        private readonly LogService _log;

        public RedeService(PsExecService psExec, LogService log)
        {
            _psExec = psExec;
            _log = log;
        }

        // ── Ping ─────────────────────────────────────────────────────────

        /// <summary>
        /// Envia ping para um host. Retorna (online, ms).
        /// Timeout de 2 segundos.
        /// </summary>
        public async Task<(bool online, long ms)> PingAsync(string host)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(
                    host.Trim(), 2000);

                bool ok = reply.Status == IPStatus.Success;
                long ms = reply.RoundtripTime;

                _log.Registrar(host, "Ping",
                    ok ? $"{ms}ms" : "Sem resposta",
                    ok, TipoLog.Rede);

                return (ok, ms);
            }
            catch (Exception ex)
            {
                _log.RegistrarErro(host, "Ping", ex);
                return (false, 0);
            }
        }

        /// <summary>
        /// Faz ping em vários hosts ao mesmo tempo (paralelo).
        /// Útil para verificar toda a lista de máquinas do AD.
        /// </summary>
        public async Task<Dictionary<string, (bool online, long ms)>>
            PingEmMassaAsync(
                IEnumerable<string> hosts,
                IProgress<(int pct, string host)>? progresso = null)
        {
            var lista = hosts.ToList();
            var resultados = new Dictionary<string, (bool, long)>();
            var semaforo = new SemaphoreSlim(20); // máx 20 pings paralelos
            int concluidos = 0;

            var tarefas = lista.Select(async host =>
            {
                await semaforo.WaitAsync();
                try
                {
                    var (ok, ms) = await PingUnicoAsync(host);
                    lock (resultados)
                    {
                        resultados[host] = (ok, ms);
                        concluidos++;
                        int pct = (int)((double)concluidos / lista.Count * 100);
                        progresso?.Report((pct, host));
                    }
                }
                finally
                {
                    semaforo.Release();
                }
            });

            await Task.WhenAll(tarefas);
            return resultados;
        }

        // Ping simples sem log (usado no ping em massa)
        private static async Task<(bool, long)> PingUnicoAsync(string host)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(host.Trim(), 1500);
                return (reply.Status == IPStatus.Success,
                        reply.RoundtripTime);
            }
            catch
            {
                return (false, 0);
            }
        }

        // ── Compartilhamento ─────────────────────────────────────────────

        /// <summary>Abre o C$ da máquina remota no Explorer.</summary>
        public void AbrirCompartilhamento(string maquina)
        {
            try
            {
                System.Diagnostics.Process.Start(
                    "explorer.exe", $@"\\{maquina}\C$");
                _log.Registrar(maquina, "Abrir C$",
                    "Aberto no Explorer", true, TipoLog.Rede);
            }
            catch (Exception ex)
            {
                _log.RegistrarErro(maquina, "Abrir C$", ex);
            }
        }

        // ── Comandos remotos ─────────────────────────────────────────────

        /// <summary>Limpa o cache DNS da máquina remota.</summary>
        public async Task FlushDnsAsync(string maquina)
        {
            var r = await _psExec.ExecutarAsync(
                "ipconfig /flushdns", maquina);

            _log.Registrar(maquina, "Flush DNS",
                r.Sucesso ? "Cache DNS limpo" : r.Erro,
                r.Sucesso, TipoLog.Rede);
        }

        /// <summary>Reinicia o Explorer.exe na máquina remota.</summary>
        public async Task ReiniciarExplorerAsync(string maquina)
        {
            string script =
                "Stop-Process -Name explorer -Force " +
                "-ErrorAction SilentlyContinue; " +
                "Start-Sleep -Seconds 1; " +
                "Start-Process explorer";

            var r = await _psExec.ExecutarPowerShellAsync(script, maquina);

            _log.Registrar(maquina, "Reiniciar Explorer",
                r.Sucesso ? "Explorer reiniciado" : r.Erro,
                r.Sucesso, TipoLog.Rede);
        }

        /// <summary>Reinicia a máquina remota em X segundos.</summary>
        public async Task ReiniciarMaquinaAsync(
            string maquina, int segundos = 30)
        {
            var r = await _psExec.ExecutarAsync(
                $"shutdown /r /t {segundos} /c \"Reinício pelo TIHub AMEB\"",
                maquina);

            _log.Registrar(maquina, "Reiniciar Máquina",
                r.Sucesso
                    ? $"Agendado em {segundos}s"
                    : r.Erro,
                r.Sucesso, TipoLog.Rede);
        }

        /// <summary>Desliga a máquina remota em X segundos.</summary>
        public async Task DesligarMaquinaAsync(
            string maquina, int segundos = 30)
        {
            var r = await _psExec.ExecutarAsync(
                $"shutdown /s /t {segundos} /c \"Desligamento pelo TIHub AMEB\"",
                maquina);

            _log.Registrar(maquina, "Desligar Máquina",
                r.Sucesso
                    ? $"Agendado em {segundos}s"
                    : r.Erro,
                r.Sucesso, TipoLog.Rede);
        }

        /// <summary>Cancela um shutdown/restart agendado.</summary>
        public async Task CancelarShutdownAsync(string maquina)
        {
            var r = await _psExec.ExecutarAsync(
                "shutdown /a", maquina);

            _log.Registrar(maquina, "Cancelar Shutdown",
                r.Sucesso ? "Cancelado" : r.Erro,
                r.Sucesso, TipoLog.Rede);
        }

        /// <summary>
        /// Executa um script PowerShell customizado na máquina remota.
        /// </summary>
        public async Task<ResultadoExecucao> ExecutarPowerShellRemotoAsync(
            string maquina, string script)
        {
            var r = await _psExec.ExecutarPowerShellAsync(script, maquina);

            _log.Registrar(maquina, "PowerShell Remoto",
                r.Sucesso
                    ? r.Saida.Length > 80
                        ? r.Saida[..80] + "..."
                        : r.Saida
                    : r.Erro,
                r.Sucesso, TipoLog.Rede);

            return r;
        }

        /// <summary>
        /// Abre o Gerenciamento do Computador remoto (compmgmt).
        /// </summary>
        public void AbrirGerenciamento(string maquina)
        {
            try
            {
                System.Diagnostics.Process.Start(
                    "compmgmt.msc",
                    $"/computer:\\\\{maquina}");
                _log.Registrar(maquina, "Gerenciamento",
                    "Aberto", true, TipoLog.Rede);
            }
            catch (Exception ex)
            {
                _log.RegistrarErro(maquina, "Gerenciamento", ex);
            }
        }
            

            /// <summary>
            /// Abre o RDP (mstsc.exe) com o nome da máquina já preenchido.
            /// O usuário só precisa clicar em Conectar.
            /// </summary>
public void AbrirRDP(string maquina)
        {
            try
            {
                System.Diagnostics.Process.Start(
                    "mstsc.exe", $"/v:{maquina}");
                _log.Registrar(maquina, "RDP",
                    "Aberto com máquina preenchida", true, TipoLog.Rede);
            }
            catch (Exception ex)
            {
                _log.RegistrarErro(maquina, "RDP", ex);
            }
        }

        /// <summary>
        /// Abre a Assistência Remota do Windows já com o IP/nome preenchido.
        /// Usa o modo de oferecer ajuda diretamente — igual ao que você faz manualmente.
        /// </summary>
        public void AbrirAssistenciaRemota(string maquina)
        {
            try
            {
                // Copia o IP/nome para o clipboard
                // Assim o usuário só cola no campo e clica Avançar
                if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                {
                    Clipboard.SetText(maquina);
                }

                // Abre o msra com elevação
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "msra.exe",
                        UseShellExecute = true,
                        Verb = "runas"
                    });

                _log.Registrar(maquina, "Assistência Remota",
                    $"Aberto — IP {maquina} copiado para o clipboard",
                    true, TipoLog.Rede);

                MessageBox.Show(
                    $"Assistência Remota aberta!\n\n" +
                    $"O nome/IP já está copiado:\n{maquina}\n\n" +
                    $"Cole no campo com Ctrl+V e clique em Avançar.",
                    "Assistência Remota",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _log.RegistrarErro(maquina, "Assistência Remota", ex);
                MessageBox.Show(
                    $"Erro ao abrir Assistência Remota:\n{ex.Message}",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
        }
    
    
    
