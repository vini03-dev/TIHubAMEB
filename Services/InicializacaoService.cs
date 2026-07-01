using TIHubAMEB.Models;

namespace TIHubAMEB.Services
{
    /// <summary>
    /// Lista e gerencia programas de inicialização do Windows,
    /// local ou remotamente, via PowerShell/PSExec.
    /// Desativação é reversível: move o item para uma chave de
    /// backup (Run-TIHubDisabled) em vez de apagar.
    /// IMPORTANTE: scripts em LINHA ÚNICA (separados por ;) porque
    /// scripts multilinha quebram ao passar por cmd /c powershell.
    /// </summary>
    public class InicializacaoService
    {
        private readonly PsExecService _psExec;
        private readonly LogService _log;

        // Separador seguro: "|" é pipe no cmd, então usamos "~~"
        private const string SEP = "~~";

        public InicializacaoService(PsExecService psExec, LogService log)
        {
            _psExec = psExec;
            _log = log;
        }

        // ── Listar itens de inicialização ─────────────────────────────────

        public async Task<List<ItemInicializacao>> ListarAsync(string? maquina)
        {
            // Script em LINHA ÚNICA. Lê as 4 chaves Run e devolve
            // cada item como: ORIGEM~~ESTADO~~NOME~~COMANDO
            string script =
                "$c=@(" +
                "@{P='HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run';O='HKLM';E='Ativo'}," +
                "@{P='HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run';O='HKCU';E='Ativo'}," +
                "@{P='HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run-TIHubDisabled';O='HKLM';E='Desativado'}," +
                "@{P='HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run-TIHubDisabled';O='HKCU';E='Desativado'}" +
                "); " +
                "foreach($k in $c){" +
                "if(Test-Path $k.P){" +
                "$p=Get-ItemProperty -Path $k.P; " +
                "$p.PSObject.Properties | Where-Object {$_.Name -notlike 'PS*'} | ForEach-Object {" +
                "Write-Output ($k.O+'~~'+$k.E+'~~'+$_.Name+'~~'+$_.Value)" +
                "}}}";

            var resultado = await _psExec.ExecutarPowerShellAsync(script, maquina);

            var lista = new List<ItemInicializacao>();

            if (!resultado.Sucesso || string.IsNullOrWhiteSpace(resultado.Saida))
            {
                _log.Registrar(maquina ?? "LOCAL", "Inicialização",
                    "Nenhum item encontrado ou erro ao ler",
                    resultado.Sucesso, TipoLog.Otimizacao);
                return lista;
            }

            var linhas = resultado.Saida.Split(
                new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var linha in linhas)
            {
                if (!linha.Contains(SEP)) continue;

                var partes = linha.Split(new[] { SEP }, StringSplitOptions.None);
                if (partes.Length < 4) continue;

                lista.Add(new ItemInicializacao
                {
                    Origem = partes[0].Trim(),
                    Ativo = partes[1].Trim()
                        .Equals("Ativo", StringComparison.OrdinalIgnoreCase),
                    Nome = partes[2].Trim(),
                    Comando = string.Join(SEP, partes[3..]).Trim()
                });
            }

            _log.Registrar(maquina ?? "LOCAL", "Inicialização",
                $"{lista.Count} itens lidos", true, TipoLog.Otimizacao);

            return lista
                .OrderBy(i => i.Ativo ? 0 : 1)
                .ThenBy(i => i.Nome)
                .ToList();
        }

        // ── Desativar (reversível) ────────────────────────────────────────

        public async Task<bool> DesativarAsync(
            ItemInicializacao item, string? maquina)
        {
            string raiz = item.Origem == "HKLM"
                ? "HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion"
                : "HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion";

            string nomeEsc = item.Nome.Replace("'", "''");
            string cmdEsc = item.Comando.Replace("'", "''");

            // Linha única
            string script =
                $"$o='{raiz}\\Run'; $d='{raiz}\\Run-TIHubDisabled'; " +
                "if(-not(Test-Path $d)){New-Item -Path $d -Force | Out-Null}; " +
                $"Set-ItemProperty -Path $d -Name '{nomeEsc}' -Value '{cmdEsc}'; " +
                $"Remove-ItemProperty -Path $o -Name '{nomeEsc}' -ErrorAction SilentlyContinue; " +
                "Write-Output 'OK'";

            var resultado = await _psExec.ExecutarPowerShellAsync(script, maquina);

            _log.Registrar(maquina ?? "LOCAL", "Desativar inicialização",
                resultado.Sucesso ? $"{item.Nome} desativado" : resultado.Erro,
                resultado.Sucesso, TipoLog.Otimizacao);

            return resultado.Sucesso;
        }

        // ── Reativar ──────────────────────────────────────────────────────

        public async Task<bool> ReativarAsync(
            ItemInicializacao item, string? maquina)
        {
            string raiz = item.Origem == "HKLM"
                ? "HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion"
                : "HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion";

            string nomeEsc = item.Nome.Replace("'", "''");
            string cmdEsc = item.Comando.Replace("'", "''");

            // Linha única
            string script =
                $"$o='{raiz}\\Run-TIHubDisabled'; $d='{raiz}\\Run'; " +
                $"Set-ItemProperty -Path $d -Name '{nomeEsc}' -Value '{cmdEsc}'; " +
                $"Remove-ItemProperty -Path $o -Name '{nomeEsc}' -ErrorAction SilentlyContinue; " +
                "Write-Output 'OK'";

            var resultado = await _psExec.ExecutarPowerShellAsync(script, maquina);

            _log.Registrar(maquina ?? "LOCAL", "Reativar inicialização",
                resultado.Sucesso ? $"{item.Nome} reativado" : resultado.Erro,
                resultado.Sucesso, TipoLog.Otimizacao);

            return resultado.Sucesso;
        }
    }
}