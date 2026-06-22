using System.Management;
using TIHubAMEB.Models;

namespace TIHubAMEB.Services
{
    public enum PerfilOtimizacao
    {
        Escritorio,
        UltraPerformance,
        Hospital
    }

    /// <summary>
    /// Aplica perfis de otimização no Windows via PowerShell.
    /// Detecta o usuário logado e aplica ajustes HKCU no perfil
    /// correto via HKEY_USERS\{SID}, não no SYSTEM.
    /// </summary>
    public class OtimizacaoService
    {
        private readonly PsExecService _psExec;
        private readonly LogService _log;

        public OtimizacaoService(PsExecService psExec, LogService log)
        {
            _psExec = psExec;
            _log = log;
        }

        // ── Execução principal ───────────────────────────────────────────

        public async Task AplicarPerfilAsync(
            PerfilOtimizacao perfil,
            string? maquina,
            IProgress<(int percent, string etapa)> progresso,
            CancellationToken ct = default)
        {
            // Detecta o SID do usuário logado na máquina
            string? sid = await Task.Run(() =>
                ObterSidUsuarioLogado(maquina));

            if (string.IsNullOrEmpty(sid))
            {
                _log.Registrar(
                    maquina ?? "LOCAL",
                    "Otimização",
                    "⚠ Nenhum usuário logado encontrado — " +
                    "ajustes visuais serão ignorados, apenas " +
                    "ajustes de máquina serão aplicados",
                    false, TipoLog.Otimizacao);
            }

            var etapas = ObterEtapas(perfil, sid);

            for (int i = 0; i < etapas.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var (label, script) = etapas[i];
                int pct = (int)((double)(i + 1) / etapas.Count * 100);

                progresso.Report((pct - 1, $"⏳ {label}"));

                var resultado = await _psExec.ExecutarPowerShellAsync(
                    script, maquina);

                _log.Registrar(
                    maquina ?? "LOCAL",
                    label,
                    resultado.Sucesso ? "Aplicado" : resultado.Erro,
                    resultado.Sucesso,
                    TipoLog.Otimizacao);

                progresso.Report((pct, $"✓ {label}"));
                await Task.Delay(200, ct);
            }

            _log.Registrar(
                maquina ?? "LOCAL",
                $"Otimização {perfil}",
                sid != null
                    ? $"{etapas.Count} ajustes aplicados no usuário logado"
                    : $"{etapas.Count} ajustes aplicados (sem usuário logado)",
                true, TipoLog.Otimizacao);
        }

        // ── Detecta SID do usuário logado ─────────────────────────────────

        /// <summary>
        /// Busca o SID do usuário atualmente logado.
        /// Funciona local (Environment) ou remoto (WMI).
        /// </summary>
        private string? ObterSidUsuarioLogado(string? maquina)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(maquina))
                {
                    // Local — usa a identidade do usuário atual
                    return System.Security.Principal.WindowsIdentity
                        .GetCurrent().User?.Value;
                }

                // Remoto via WMI
                var options = new ConnectionOptions
                {
                    Impersonation = ImpersonationLevel.Impersonate,
                    Authentication = AuthenticationLevel.PacketPrivacy,
                    EnablePrivileges = true,
                    Timeout = TimeSpan.FromSeconds(6)
                };

                var scope = new ManagementScope(
                    $@"\\{maquina}\root\cimv2", options);
                scope.Connect();

                // Busca processos do explorer.exe — dono é o usuário logado
                using var q = new ManagementObjectSearcher(scope,
                    new ObjectQuery(
                        "SELECT Handle FROM Win32_Process " +
                        "WHERE Name='explorer.exe'"));
                q.Options.Timeout = TimeSpan.FromSeconds(5);

                foreach (ManagementObject obj in q.Get())
                {
                    var outParams = obj.InvokeMethod(
                        "GetOwnerSid", null, null);

                    string? sid = outParams?.Properties["Sid"]
                        ?.Value?.ToString();

                    if (!string.IsNullOrEmpty(sid))
                        return sid;
                }
            }
            catch (Exception ex)
            {
                _log.RegistrarErro(
                    maquina ?? "LOCAL", "Detectar SID usuário", ex);
            }

            return null;
        }

        // ── Perfis ───────────────────────────────────────────────────────

        private static List<(string label, string script)> ObterEtapas(
            PerfilOtimizacao perfil, string? sid) => perfil switch
            {
                PerfilOtimizacao.Escritorio => EtapasEscritorio(sid),
                PerfilOtimizacao.UltraPerformance => EtapasUltraPerformance(sid),
                PerfilOtimizacao.Hospital => EtapasHospital(sid),
                _ => new()
            };

        // Monta o caminho correto do registro.
        // Se tiver SID, usa HKEY_USERS\{sid} (perfil do usuário real).
        // Se não tiver, usa HKCU (fallback — afeta quem executa o script).
        private static string CaminhoRegistro(string? sid, string subcaminho)
        {
            return string.IsNullOrEmpty(sid)
                ? $"HKCU:\\{subcaminho}"
                : $"Registry::HKEY_USERS\\{sid}\\{subcaminho}";
        }

        // ── PERFIL ESCRITÓRIO ─────────────────────────────────────────────

        private static List<(string, string)> EtapasEscritorio(string? sid)
        {
            string rVisual = CaminhoRegistro(sid,
                "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects");
            string rTema = CaminhoRegistro(sid,
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
            string rMetrics = CaminhoRegistro(sid,
                "Control Panel\\Desktop\\WindowMetrics");
            string rExplorer = CaminhoRegistro(sid,
                "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced");
            string rContent = CaminhoRegistro(sid,
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager");

            return new()
            {
                ("Ajustando plano de energia Balanceado",
                    "powercfg /setactive SCHEME_BALANCED"),

                ("Reduzindo efeitos visuais (perfil do usuário)",
                    $"Set-ItemProperty -Path '{rVisual}' " +
                    "-Name VisualFXSetting -Value 2 -ErrorAction SilentlyContinue"),

                ("Removendo transparência (perfil do usuário)",
                    $"Set-ItemProperty -Path '{rTema}' " +
                    "-Name EnableTransparency -Value 0 -ErrorAction SilentlyContinue"),

                ("Desativando animações de janela (perfil do usuário)",
                    $"Set-ItemProperty -Path '{rMetrics}' " +
                    "-Name MinAnimate -Value 0 -ErrorAction SilentlyContinue"),

                ("Otimizando menu iniciar (perfil do usuário)",
                    $"Set-ItemProperty -Path '{rExplorer}' " +
                    "-Name Start_TrackProgs -Value 0 -ErrorAction SilentlyContinue"),

                ("Desativando dicas e notificações (perfil do usuário)",
                    $"Set-ItemProperty -Path '{rContent}' " +
                    "-Name SubscribedContent-338389Enabled " +
                    "-Value 0 -ErrorAction SilentlyContinue"),

                ("Limpando tarefas agendadas desnecessárias (máquina)",
                    "Get-ScheduledTask | " +
                    "Where-Object { $_.TaskPath -like '*Microsoft*' -and " +
                    "$_.State -eq 'Ready' -and " +
                    "$_.TaskName -like '*Update*' } | " +
                    "Disable-ScheduledTask -ErrorAction SilentlyContinue; " +
                    "Write-Output 'Tarefas desativadas'")
            };
        }

        // ── PERFIL ULTRA PERFORMANCE ──────────────────────────────────────

        private static List<(string, string)> EtapasUltraPerformance(string? sid)
        {
            string rVisual = CaminhoRegistro(sid,
                "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects");
            string rDesktop = CaminhoRegistro(sid,
                "Control Panel\\Desktop");
            string rTema = CaminhoRegistro(sid,
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
            string rMetrics = CaminhoRegistro(sid,
                "Control Panel\\Desktop\\WindowMetrics");

            return new()
            {
                ("Ativando plano Máximo Desempenho (máquina)",
                    "powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c; " +
                    "if ($LASTEXITCODE -ne 0) { powercfg /duplicatescheme " +
                    "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c; " +
                    "powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c }"),

                ("Desabilitando efeitos visuais (perfil do usuário)",
                    $"Set-ItemProperty -Path '{rVisual}' " +
                    "-Name VisualFXSetting -Value 2 -ErrorAction SilentlyContinue; " +
                    $"Set-ItemProperty -Path '{rDesktop}' " +
                    "-Name UserPreferencesMask " +
                    "-Value ([byte[]](0x90,0x12,0x03,0x80,0x10,0x00,0x00,0x00)) " +
                    "-ErrorAction SilentlyContinue"),

                ("Removendo transparência e animações (perfil do usuário)",
                    $"Set-ItemProperty -Path '{rTema}' " +
                    "-Name EnableTransparency -Value 0 -ErrorAction SilentlyContinue; " +
                    $"Set-ItemProperty -Path '{rMetrics}' " +
                    "-Name MinAnimate -Value 0 -ErrorAction SilentlyContinue"),

                ("Priorizando CPU para programas em uso (máquina)",
                    "Set-ItemProperty -Path " +
                    "'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl' " +
                    "-Name Win32PrioritySeparation -Value 38 " +
                    "-ErrorAction SilentlyContinue"),

                ("Otimizando memória virtual (máquina)",
                    "Set-ItemProperty -Path " +
                    "'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management' " +
                    "-Name LargeSystemCache -Value 0 " +
                    "-ErrorAction SilentlyContinue; " +
                    "Set-ItemProperty -Path " +
                    "'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management' " +
                    "-Name DisablePagingExecutive -Value 1 " +
                    "-ErrorAction SilentlyContinue"),

                ("Otimizando prefetch e superfetch (máquina)",
                    "Set-ItemProperty -Path " +
                    "'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters' " +
                    "-Name EnablePrefetcher -Value 3 " +
                    "-ErrorAction SilentlyContinue; " +
                    "Set-ItemProperty -Path " +
                    "'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters' " +
                    "-Name EnableSuperfetch -Value 3 " +
                    "-ErrorAction SilentlyContinue"),

                ("Desativando hibernação (máquina)",
                    "powercfg /hibernate off; " +
                    "Write-Output 'Hibernação desativada'"),

                ("Desativando serviços desnecessários (máquina)",
                    "$servicos = @('DiagTrack','dmwappushservice','SysMain','WSearch'); " +
                    "foreach ($s in $servicos) { " +
                    "  try { " +
                    "    Stop-Service -Name $s -Force -ErrorAction SilentlyContinue; " +
                    "    Set-Service -Name $s -StartupType Disabled " +
                    "    -ErrorAction SilentlyContinue " +
                    "  } catch {} " +
                    "}; Write-Output 'Serviços otimizados'")
            };
        }

        // ── PERFIL HOSPITAL ───────────────────────────────────────────────

        private static List<(string, string)> EtapasHospital(string? sid)
        {
            string rVisual = CaminhoRegistro(sid,
                "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects");

            return new()
            {
                ("Aplicando plano Balanceado (máquina)",
                    "powercfg /setactive SCHEME_BALANCED"),

                ("Reduzindo animações — apenas visual (perfil do usuário)",
                    $"Set-ItemProperty -Path '{rVisual}' " +
                    "-Name VisualFXSetting -Value 2 -ErrorAction SilentlyContinue"),

                ("Ajustando timeout de tela (máquina)",
                    "powercfg /change monitor-timeout-ac 30; " +
                    "powercfg /change monitor-timeout-dc 15; " +
                    "Write-Output 'Timeout de tela ajustado'"),

                ("Verificando atualizações pendentes (informativo)",
                    "Get-HotFix | Sort-Object InstalledOn -Descending | " +
                    "Select-Object -First 3 | " +
                    "ForEach-Object { Write-Output $_.HotFixID }")
            };
        }
    }
}