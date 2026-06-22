using TIHubAMEB.Models;

namespace TIHubAMEB.Services
{
    /// <summary>
    /// Executa limpezas no Windows via PowerShell.
    /// Funciona local ou remotamente via PSExec.
    /// Comandos melhorados — mais confiáveis que del/rmdir puro.
    /// </summary>
    public class LimpezaService
    {
        private readonly PsExecService _psExec;
        private readonly LogService _log;

        public LimpezaService(PsExecService psExec, LogService log)
        {
            _psExec = psExec;
            _log = log;
        }

        // ── Execução principal ───────────────────────────────────────────

        /// <summary>
        /// Executa a limpeza conforme o perfil selecionado.
        /// Reporta progresso em tempo real para a barra de progresso.
        /// </summary>
        public async Task ExecutarAsync(
            PerfilLimpeza perfil,
            string? maquina,
            IProgress<(int percent, string etapa)> progresso,
            CancellationToken ct = default)
        {
            var etapas = ConstruirEtapas(perfil);

            if (etapas.Count == 0)
            {
                progresso.Report((100, "Nenhuma etapa selecionada."));
                return;
            }

            int total = etapas.Count;

            for (int i = 0; i < total; i++)
            {
                ct.ThrowIfCancellationRequested();

                var (label, script) = etapas[i];
                int pct = (int)((double)(i + 1) / total * 100);

                progresso.Report((pct - 1, $"⏳ {label}"));

                // Executa via PowerShell — mais confiável que CMD
                var resultado = await _psExec.ExecutarPowerShellAsync(
                    script, maquina);

                // Mostra retorno no log
                if (!string.IsNullOrWhiteSpace(resultado.Saida))
                    _log.Registrar(maquina ?? "LOCAL", label,
                        resultado.Saida.Length > 100
                            ? resultado.Saida[..100] + "..."
                            : resultado.Saida,
                        true, TipoLog.Limpeza);

                if (!resultado.Sucesso &&
                    !string.IsNullOrWhiteSpace(resultado.Erro))
                    _log.Registrar(maquina ?? "LOCAL", label,
                        resultado.Erro, false, TipoLog.Erro);

                progresso.Report((pct, $"✓ {label}"));
                await Task.Delay(150, ct);
            }

            _log.Registrar(
                maquina ?? "LOCAL",
                $"Limpeza {perfil.Nome}",
                $"{total} etapas concluídas",
                true, TipoLog.Limpeza);
        }

        // ── Construção das etapas ────────────────────────────────────────

        private List<(string label, string script)> ConstruirEtapas(
            PerfilLimpeza perfil)
        {
            var etapas = new List<(string, string)>();

            // ── Temp do usuário ──────────────────────────────────────────
            if (perfil.LimparTempUsuario)
                etapas.Add((
                    "Limpando Temp do usuário",
                    @"Remove-Item -Path ""$env:TEMP\*"" " +
                    @"-Recurse -Force -ErrorAction SilentlyContinue; " +
                    @"Write-Output 'Temp usuário limpo'"));

            // ── Temp do Windows ──────────────────────────────────────────
            if (perfil.LimparTempWindows)
                etapas.Add((
                    "Limpando Temp do Windows",
                    @"Remove-Item -Path ""C:\Windows\Temp\*"" " +
                    @"-Recurse -Force -ErrorAction SilentlyContinue; " +
                    @"Write-Output 'Temp Windows limpo'"));

            // ── Prefetch ─────────────────────────────────────────────────
            if (perfil.LimparPrefetch)
                etapas.Add((
                    "Limpando Prefetch",
                    @"Remove-Item -Path ""C:\Windows\Prefetch\*"" " +
                    @"-Recurse -Force -ErrorAction SilentlyContinue; " +
                    @"Write-Output 'Prefetch limpo'"));

            // ── Lixeira ──────────────────────────────────────────────────
            if (perfil.LimparLixeira)
                etapas.Add((
                    "Esvaziando Lixeira",
                    @"Clear-RecycleBin -Force -ErrorAction SilentlyContinue; " +
                    @"Write-Output 'Lixeira esvaziada'"));

            // ── Cache DNS ────────────────────────────────────────────────
            if (perfil.LimparCacheDns)
                etapas.Add((
                    "Limpando Cache DNS",
                    @"ipconfig /flushdns; " +
                    @"Write-Output 'Cache DNS limpo'"));

            // ── Miniaturas ───────────────────────────────────────────────
            if (perfil.LimparMiniaturas)
                etapas.Add((
                    "Limpando Miniaturas",
                    @"Remove-Item -Path " +
                    @"""$env:LOCALAPPDATA\Microsoft\Windows\Explorer\thumbcache_*.db"" " +
                    @"-Force -ErrorAction SilentlyContinue; " +
                    @"Write-Output 'Miniaturas limpas'"));

            // ── Logs antigos ─────────────────────────────────────────────
            if (perfil.LimparLogsAntigos)
                etapas.Add((
                    "Removendo Logs antigos",
                    @"Get-ChildItem -Path ""C:\Windows\Logs"" " +
                    @"-Recurse -File -ErrorAction SilentlyContinue | " +
                    @"Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } | " +
                    @"Remove-Item -Force -ErrorAction SilentlyContinue; " +
                    @"Write-Output 'Logs antigos removidos'"));

            // ── Cache de fontes ──────────────────────────────────────────
            if (perfil.LimparFontCache)
                etapas.Add((
                    "Limpando Cache de Fontes",
                    @"Stop-Service -Name 'FontCache' -Force -ErrorAction SilentlyContinue; " +
                    @"Remove-Item -Path ""C:\Windows\ServiceProfiles\LocalService\AppData\Local\FontCache\*"" " +
                    @"-Force -Recurse -ErrorAction SilentlyContinue; " +
                    @"Start-Service -Name 'FontCache' -ErrorAction SilentlyContinue; " +
                    @"Write-Output 'Cache de fontes limpo'"));

            // ── Cache de navegadores ─────────────────────────────────────
            if (perfil.LimparCacheNavegador)
            {
                // Chrome
                etapas.Add((
                    "Limpando Cache do Chrome",
                    @"Remove-Item -Path " +
                    @"""$env:LOCALAPPDATA\Google\Chrome\User Data\Default\Cache\*"" " +
                    @"-Recurse -Force -ErrorAction SilentlyContinue; " +
                    @"Remove-Item -Path " +
                    @"""$env:LOCALAPPDATA\Google\Chrome\User Data\Default\Code Cache\*"" " +
                    @"-Recurse -Force -ErrorAction SilentlyContinue; " +
                    @"Write-Output 'Cache Chrome limpo'"));

                // Edge
                etapas.Add((
                    "Limpando Cache do Edge",
                    @"Remove-Item -Path " +
                    @"""$env:LOCALAPPDATA\Microsoft\Edge\User Data\Default\Cache\*"" " +
                    @"-Recurse -Force -ErrorAction SilentlyContinue; " +
                    @"Write-Output 'Cache Edge limpo'"));
            }

            // ── Windows Update ───────────────────────────────────────────
            if (perfil.LimparWindowsUpdate)
                etapas.Add((
                    "Limpando Cache Windows Update",
                    @"Stop-Service -Name wuauserv -Force -ErrorAction SilentlyContinue; " +
                    @"Stop-Service -Name bits -Force -ErrorAction SilentlyContinue; " +
                    @"Remove-Item -Path ""C:\Windows\SoftwareDistribution\Download\*"" " +
                    @"-Recurse -Force -ErrorAction SilentlyContinue; " +
                    @"Start-Service -Name wuauserv -ErrorAction SilentlyContinue; " +
                    @"Start-Service -Name bits -ErrorAction SilentlyContinue; " +
                    @"Write-Output 'Cache Windows Update limpo'"));

            // ── Spool de impressão ───────────────────────────────────────
            if (perfil.LimparSpoolImpressao)
                etapas.Add((
                    "Limpando Spool de Impressão",
                    @"Stop-Service -Name spooler -Force -ErrorAction SilentlyContinue; " +
                    @"Remove-Item -Path ""C:\Windows\System32\spool\PRINTERS\*"" " +
                    @"-Force -Recurse -ErrorAction SilentlyContinue; " +
                    @"Start-Service -Name spooler -ErrorAction SilentlyContinue; " +
                    @"Write-Output 'Spool limpo'"));

            // ── Memory dump ──────────────────────────────────────────────
            if (perfil.LimparMemoriaDump)
                etapas.Add((
                    "Removendo arquivos de Dump",
                    @"Remove-Item -Path ""C:\Windows\Minidump\*"" " +
                    @"-Force -Recurse -ErrorAction SilentlyContinue; " +
                    @"Remove-Item -Path ""C:\Windows\memory.dmp"" " +
                    @"-Force -ErrorAction SilentlyContinue; " +
                    @"Write-Output 'Dumps removidos'"));

            

            // ── DISM Cleanup ─────────────────────────────────────────────
            if (perfil.ExecutarDism)
                etapas.Add((
                    "Executando DISM Cleanup (pode demorar)",
                    @"DISM /Online /Cleanup-Image /StartComponentCleanup /ResetBase; " +
                    @"Write-Output 'DISM concluído'"));

            // ── WinSxS ──────────────────────────────────────────────────
            if (perfil.LimparWinSxS)
                etapas.Add((
                    "Limpando WinSxS (pode demorar)",
                    @"DISM /Online /Cleanup-Image /SPSuperseded; " +
                    @"Write-Output 'WinSxS limpo'"));

            return etapas;
        }

        // ── Script de e-mails ────────────────────────────────────────────

        private static string ConstruirScriptEmailsAntigos(DateTime dataLimite)
        {
            // Formata a data para PowerShell
            string data = dataLimite.ToString("yyyy-MM-dd");

            // Remove e-mails do Outlook anteriores à data
            // Funciona com perfis .ost e .pst locais
            return
                $@"Add-Type -Assembly 'Microsoft.Office.Interop.Outlook' " +
                $@"-ErrorAction SilentlyContinue; " +
                $@"try {{ " +
                $@"  $outlook = New-Object -ComObject Outlook.Application; " +
                $@"  $ns = $outlook.GetNamespace('MAPI'); " +
                $@"  $inbox = $ns.GetDefaultFolder(6); " +
                $@"  $data = [DateTime]'{data}'; " +
                $@"  $antigos = $inbox.Items | Where-Object {{ $_.ReceivedTime -lt $data }}; " +
                $@"  $count = 0; " +
                $@"  foreach ($email in @($antigos)) {{ " +
                $@"    $email.Delete(); $count++ " +
                $@"  }} " +
                $@"  Write-Output ""$count e-mails removidos anteriores a {data}"" " +
                $@"}} catch {{ " +
                $@"  Write-Output 'Outlook não encontrado ou sem permissão' " +
                $@"}}";
        }
    }
}