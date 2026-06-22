namespace TIHubAMEB.Models
{
    /// <summary>
    /// Representa um usuário encontrado no sistema Windows.
    /// Usado na aba Usuários para listar e gerenciar perfis.
    /// </summary>
    public class UsuarioInfo
    {
        // ── Identificação ────────────────────────────────────────────────
        public string NomeUsuario { get; set; } = string.Empty;
        public string NomeCompleto { get; set; } = string.Empty;
        public string CaminhoPerfilC { get; set; } = string.Empty; // C:\Users\nome

        // ── Datas ────────────────────────────────────────────────────────
        public DateTime? UltimoLogin { get; set; }
        public DateTime? CriadoEm { get; set; }

        // ── Status ───────────────────────────────────────────────────────
        public bool EstaLogado { get; set; } // logado agora
        public bool EhAdministrador { get; set; }
        public bool EhSistema { get; set; } // SYSTEM, PUBLIC, etc
        public bool PerfilExiste { get; set; } // pasta existe no disco
        public long TamanhoPastaKB { get; set; } // tamanho em KB

        // ── Proteção ─────────────────────────────────────────────────────

        // Retorna true se este usuário NÃO pode ser excluído
        public bool Protegido =>
            EstaLogado ||
            EhSistema ||
            NomeUsuario.Equals("Administrator", StringComparison.OrdinalIgnoreCase) ||
            NomeUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase) ||
            NomeUsuario.Equals("DefaultAccount", StringComparison.OrdinalIgnoreCase) ||
            NomeUsuario.Equals("Guest", StringComparison.OrdinalIgnoreCase) ||
            NomeUsuario.Equals("WDAGUtilityAccount", StringComparison.OrdinalIgnoreCase) ||
            NomeUsuario.StartsWith("SISTEMA", StringComparison.OrdinalIgnoreCase);

        // Motivo pelo qual está protegido (para exibir na tela)
        public string MotivoProtecao
        {
            get
            {
                if (EstaLogado) return "Usuário logado agora";
                if (EhSistema) return "Conta do sistema";
                if (NomeUsuario.Equals("Administrator", StringComparison.OrdinalIgnoreCase) ||
                    NomeUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
                    return "Conta de administrador";
                if (NomeUsuario.Equals("Guest", StringComparison.OrdinalIgnoreCase))
                    return "Conta convidado";
                return "Conta protegida";
            }
        }

        // ── Formatação ───────────────────────────────────────────────────

        // Formata tamanho da pasta ex: 1.2 GB ou 850 MB
        public string TamanhoPastaFormatado
        {
            get
            {
                if (TamanhoPastaKB <= 0) return "—";
                if (TamanhoPastaKB >= 1048576)
                    return $"{TamanhoPastaKB / 1048576.0:F1} GB";
                if (TamanhoPastaKB >= 1024)
                    return $"{TamanhoPastaKB / 1024.0:F0} MB";
                return $"{TamanhoPastaKB} KB";
            }
        }

        // Formata último login ex: 10/05/2026 ou "Nunca"
        public string UltimoLoginFormatado =>
            UltimoLogin.HasValue
                ? UltimoLogin.Value.ToString("dd/MM/yyyy")
                : "Nunca";
    

        // Adicione esta propriedade no UsuarioInfo.cs:
        public string NomeMaquina { get; set; } = string.Empty;
    }

}