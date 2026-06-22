using System.Management;
using TIHubAMEB.Models;

namespace TIHubAMEB.Services
{
    /// <summary>
    /// Lista e gerencia usuários do Windows.
    /// Funciona local ou remotamente via WMI.
    /// Protege contas de sistema e usuário logado.
    /// </summary>
    public class UsuarioService
    {
        private readonly PsExecService _psExec;
        private readonly LogService _log;

        public UsuarioService(PsExecService psExec, LogService log)
        {
            _psExec = psExec;
            _log = log;
        }

        // ── Listar usuários ───────────────────────────────────────────────

        /// <summary>
        /// Lista todos os usuários do PC (local ou remoto via WMI).
        /// Inclui último login, tamanho da pasta e status de proteção.
        /// </summary>
        public async Task<List<UsuarioInfo>> ListarUsuariosAsync(
            string? maquina = null)
        {
            return await Task.Run(() =>
                string.IsNullOrWhiteSpace(maquina)
                    ? ListarLocal()
                    : ListarRemoto(maquina));
        }

        // ── Local ─────────────────────────────────────────────────────────

        private List<UsuarioInfo> ListarLocal()
        {
            var lista = new List<UsuarioInfo>();

            try
            {
                // Busca usuários via WMI local
                using var query = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_UserProfile " +
                    "WHERE Special=FALSE");

                foreach (ManagementObject obj in query.Get())
                {
                    try
                    {
                        string? sid = obj["SID"]?.ToString();
                        string? caminho = obj["LocalPath"]?.ToString();

                        if (string.IsNullOrEmpty(caminho)) continue;

                        string nome = Path.GetFileName(caminho);

                        // Ignora pastas de sistema
                        if (EhPastaSistema(nome)) continue;

                        var usuario = new UsuarioInfo
                        {
                            NomeMaquina = Environment.MachineName,
                            NomeUsuario = nome,
                            CaminhoPerfilC = caminho,
                            PerfilExiste = Directory.Exists(caminho),
                            EstaLogado = nome.Equals(
                                Environment.UserName,
                                StringComparison.OrdinalIgnoreCase)
                        };

                        // Tamanho da pasta
                        if (usuario.PerfilExiste)
                            usuario.TamanhoPastaKB =
                                ObterTamanhoPastaKB(caminho);

                        // Último login via registro
                        if (!string.IsNullOrEmpty(sid))
                            usuario.UltimoLogin =
                                ObterUltimoLoginRegistro(sid);

                        // Verifica se é admin
                        usuario.EhAdministrador =
                            VerificarSeAdmin(nome);

                        lista.Add(usuario);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                _log.RegistrarErro("LOCAL", "Listar usuários", ex);
            }

            return lista.OrderBy(u => u.NomeUsuario).ToList();
        }

        // ── Remoto via WMI ────────────────────────────────────────────────

        private List<UsuarioInfo> ListarRemoto(string maquina)
        {
            var lista = new List<UsuarioInfo>();

            try
            {
                var options = new ConnectionOptions
                {
                    Impersonation = ImpersonationLevel.Impersonate,
                    Authentication = AuthenticationLevel.PacketPrivacy,
                    EnablePrivileges = true,
                    Timeout = TimeSpan.FromSeconds(10)
                };

                var scope = new ManagementScope(
                    $@"\\{maquina}\root\cimv2", options);
                scope.Connect();

                // Usuário logado agora
                string usuarioLogado = ObterUsuarioLogadoWmi(scope);

                using var query = new ManagementObjectSearcher(
                    scope,
                    new ObjectQuery(
                        "SELECT * FROM Win32_UserProfile " +
                        "WHERE Special=FALSE"));
                query.Options.Timeout = TimeSpan.FromSeconds(10);

                foreach (ManagementObject obj in query.Get())
                {
                    try
                    {
                        string? caminho = obj["LocalPath"]?.ToString();
                        if (string.IsNullOrEmpty(caminho)) continue;

                        string nome = caminho.Split('\\').Last();
                        if (EhPastaSistema(nome)) continue;

                        // Data último uso
                        DateTime? ultimoUso = null;
                        if (obj["LastUseTime"] != null)
                        {
                            try
                            {
                                ultimoUso = ManagementDateTimeConverter
                                    .ToDateTime(obj["LastUseTime"].ToString()!);
                            }
                            catch { }
                        }

                        var usuario = new UsuarioInfo
                        {
                            NomeMaquina = maquina.ToUpper(),
                            NomeUsuario = nome,
                            CaminhoPerfilC = caminho,
                            PerfilExiste = true,
                            UltimoLogin = ultimoUso,
                            EstaLogado = nome.Equals(
                                usuarioLogado,
                                StringComparison.OrdinalIgnoreCase)
                        };

                        lista.Add(usuario);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                _log.RegistrarErro(maquina, "Listar usuários remoto", ex);
            }

            return lista.OrderBy(u => u.NomeUsuario).ToList();
        }

        // ── Excluir pasta do usuário ──────────────────────────────────────

        /// <summary>
        /// Exclui a pasta do perfil do usuário (C:\Users\nome).
        /// Protege contas de sistema e usuário logado.
        /// </summary>
        public async Task<bool> ExcluirPastaUsuarioAsync(
            UsuarioInfo usuario,
            string? maquina = null)
        {
            // Proteção
            if (usuario.Protegido)
            {
                _log.Registrar(
                    maquina ?? "LOCAL",
                    $"Excluir pasta: {usuario.NomeUsuario}",
                    $"Bloqueado — {usuario.MotivoProtecao}",
                    false, TipoLog.Usuarios);
                return false;
            }

            // Script PowerShell para excluir com força
            string script =
                $"$caminho = '{usuario.CaminhoPerfilC}'; " +
                $"if (Test-Path $caminho) {{ " +
                $"  takeown /f $caminho /r /d y | Out-Null; " +
                $"  icacls $caminho /grant administrators:F /t /q | Out-Null; " +
                $"  Remove-Item -Path $caminho -Recurse -Force " +
                $"  -ErrorAction SilentlyContinue; " +
                $"  if (Test-Path $caminho) {{ " +
                $"    Write-Output 'ERRO: Pasta ainda existe' " +
                $"  }} else {{ " +
                $"    Write-Output 'Pasta excluída com sucesso' " +
                $"  }} " +
                $"}} else {{ " +
                $"  Write-Output 'Pasta não encontrada' " +
                $"}}";

            var resultado = await _psExec.ExecutarPowerShellAsync(
                script, maquina);

            bool sucesso = resultado.Sucesso &&
                resultado.Saida.Contains("sucesso");

            _log.Registrar(
                maquina ?? "LOCAL",
                $"Excluir pasta: {usuario.NomeUsuario}",
                resultado.SaidaOuErro,
                sucesso, TipoLog.Usuarios);

            return sucesso;
        }

        // ── Utilitários ──────────────────────────────────────────────────

        private static bool EhPastaSistema(string nome)
        {
            var sistematicos = new[]
            {
                "All Users", "Default", "Default User",
                "Public", "SYSTEM", "LocalService",
                "NetworkService", "systemprofile"
            };

            return sistematicos.Any(s =>
                s.Equals(nome, StringComparison.OrdinalIgnoreCase));
        }

        private static long ObterTamanhoPastaKB(string caminho)
        {
            try
            {
                return new DirectoryInfo(caminho)
                    .GetFiles("*", SearchOption.AllDirectories)
                    .Sum(f =>
                    {
                        try { return f.Length; }
                        catch { return 0L; }
                    }) / 1024;
            }
            catch { return 0; }
        }

        private static DateTime? ObterUltimoLoginRegistro(string sid)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine
                    .OpenSubKey(
                        $@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList\{sid}");

                if (key == null) return null;

                // Lê a data do último uso do perfil
                var valor = key.GetValue("ProfileLoadTimeLow");
                if (valor == null) return null;

                long low = Convert.ToInt64(
                    key.GetValue("ProfileLoadTimeLow") ?? 0);
                long high = Convert.ToInt64(
                    key.GetValue("ProfileLoadTimeHigh") ?? 0);

                long fileTime = (high << 32) | (low & 0xFFFFFFFFL);
                if (fileTime == 0) return null;

                return DateTime.FromFileTime(fileTime);
            }
            catch { return null; }
        }

        private static bool VerificarSeAdmin(string nomeUsuario)
        {
            try
            {
                using var query = new ManagementObjectSearcher(
                    $"SELECT * FROM Win32_GroupUser WHERE GroupComponent=" +
                    $"\"Win32_Group.Domain='{Environment.MachineName}'," +
                    $"Name='Administrators'\"");

                foreach (ManagementObject obj in query.Get())
                {
                    string? parte = obj["PartComponent"]?.ToString();
                    if (parte != null &&
                        parte.Contains(nomeUsuario,
                            StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch { }
            return false;
        }

        private static string ObterUsuarioLogadoWmi(ManagementScope scope)
        {
            try
            {
                using var q = new ManagementObjectSearcher(scope,
                    new ObjectQuery(
                        "SELECT UserName FROM Win32_ComputerSystem"));
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
            return string.Empty;
        }

       
    }
}