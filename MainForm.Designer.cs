using Guna.UI2.WinForms;
using System.Xml.Linq;
using TIHubAMEB.Helpers;
using static Guna.UI2.WinForms.Suite.Descriptions;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Font = System.Drawing.Font;
using ListView = System.Windows.Forms.ListView;


namespace TIHubAMEB.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        // ── Helpers privados ──────────────────────────────────────────────

        private Guna2Panel CriarCard(int x, int y, int w, int h,
            int radius = 8)
        {
            var p = new Guna2Panel();
            p.FillColor = UIHelper.CorCard;
            p.BorderColor = UIHelper.CorBorda;
            p.BorderThickness = 1;
            p.BorderRadius = radius;
            p.Location = new Point(x, y);
            p.Size = new Size(w, h);
            p.ShadowDecoration.Enabled = false;
            return p;
        }

        private Guna2Button CriarBotao(string texto, int x, int y,
            int w, int h, Color bg, Color fg, int radius = 6)
        {
            var b = new Guna2Button();
            b.Text = texto;
            b.Location = new Point(x, y);
            b.Size = new Size(w, h);
            b.FillColor = bg;
            b.ForeColor = fg;
            b.BorderColor = fg;
            b.BorderThickness = 1;
            b.BorderRadius = radius;
            b.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
            b.TextAlign = HorizontalAlignment.Center;
            return b;
        }

        private Label CriarLabel(string texto, int x, int y,
            int w, int h, Color? cor = null,
            float size = 9.5f, bool bold = false)
        {
            var l = new Label();
            l.Text = texto;
            l.Location = new Point(x, y);
            l.Size = new Size(w, h);
            l.ForeColor = cor ?? UIHelper.CorTexto;
            l.BackColor = Color.Transparent;
            l.Font = new Font("Segoe UI", size,
                bold ? FontStyle.Bold : FontStyle.Regular);
            return l;
        }

        private Guna2ProgressBar CriarProgressBar(int x, int y,
            int w, int h, Color? cor = null)
        {
            var pb = new Guna2ProgressBar();
            pb.Location = new Point(x, y);
            pb.Size = new Size(w, h);
            pb.Maximum = 100;
            pb.Value = 0;
            pb.FillColor = Color.FromArgb(33, 38, 45);
            pb.ProgressColor = cor ?? UIHelper.CorAzul;
            pb.ProgressColor2 = cor ?? UIHelper.CorAzul;
            pb.BorderRadius = 4;
            pb.BackColor = Color.Transparent;
            return pb;
        }

        private Guna2TextBox CriarTextBox(string placeholder,
            int x, int y, int w, int h)
        {
            var t = new Guna2TextBox();
            t.PlaceholderText = placeholder;
            t.FillColor = UIHelper.CorFundo;
            t.ForeColor = UIHelper.CorTexto;
            t.BorderColor = UIHelper.CorBorda;
            t.FocusedState.BorderColor = UIHelper.CorAzul;
            t.PlaceholderForeColor = UIHelper.CorTextoEsc;
            t.BorderRadius = 6;
            t.BorderThickness = 1;
            t.Font = UIHelper.FonteNormal;
            t.Location = new Point(x, y);
            t.Size = new Size(w, h);
            return t;
        }

        private void InitializeComponent()
        {

            panelHeader = new Guna2Panel();
            panelFooter = new Guna2Panel();
            lblHeaderTitulo = new Label();
            lblHeaderSub = new Label();
            lblHeaderMaquina = new Label();
            lblHeaderStatus = new Label();
            lblFooterDev = new Label();
            lblFooterVer = new Label();

            // ── Declaração ────────────────────────────────────────────────
            tabMain = new Guna2TabControl();
            tabDashboard = new TabPage();
            tabLimpeza = new TabPage();
            tabOtimizacao = new TabPage();
            tabRede = new TabPage();
            tabMaquinas = new TabPage();
            tabUsuarios = new TabPage();
            tabSistema = new TabPage();
            tabLogs = new TabPage();
            tabImpressoras = new TabPage();

            lblMaquinaTopbar = new Label();
            lblMaquinaTopbar.Text = "LOCAL";
            lblMaquinaTopbar.ForeColor = UIHelper.CorTextoClaro;
            lblMaquinaTopbar.BackColor = Color.Transparent;
            lblMaquinaTopbar.Font = UIHelper.FonteNormal;
            lblMaquinaTopbar.Location = new Point(900, 0);
            lblMaquinaTopbar.Size = new Size(200, 36);
            lblMaquinaTopbar.TextAlign = ContentAlignment.MiddleRight;
            lblMaquinaTopbar.Name = "lblMaquinaTopbar";

            lblStatusTopbar = new Label();
            lblStatusTopbar.Text = "● ONLINE";
            lblStatusTopbar.ForeColor = UIHelper.CorVerde;
            lblStatusTopbar.BackColor = Color.Transparent;
            lblStatusTopbar.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblStatusTopbar.Location = new Point(1110, 0);
            lblStatusTopbar.Size = new Size(150, 36);
            lblStatusTopbar.TextAlign = ContentAlignment.MiddleRight;
            lblStatusTopbar.Name = "lblStatusTopbar";

            // Dashboard
            panelCpu = new Guna2Panel();
            panelRam = new Guna2Panel();
            panelDisco = new Guna2Panel();
            panelInfoMaquina = new Guna2Panel();
            panelInfoSistema = new Guna2Panel();
            lblCpuTit = new Label();
            lblCPU = new Label();
            progressCpu = new Guna2ProgressBar();
            lblRamTit = new Label();
            lblRAM = new Label();
            progressRam = new Guna2ProgressBar();
            lblDiscoTit = new Label();
            lblDisco = new Label();
            progressDisco = new Guna2ProgressBar();
            lblNomeMaqTit = new Label();
            lblMaquina = new Label();
            lblIPTit = new Label();
            lblIP = new Label();
            lblUsuarioTit = new Label();
            lblUsuario = new Label();
            lblSOTit = new Label();
            lblSO = new Label();
            lblModeTit = new Label();
            lblMode = new Label();
            lblStatusTit = new Label();
            lblStatus = new Label();
            lblUptimeTit = new Label();
            lblUptime = new Label();
            lblRamTotalTit = new Label();
            lblRamTotal = new Label();
            lblDiscoLivreTit = new Label();
            lblDiscoLivre = new Label();

            // Limpeza
            panelPresets = new Guna2Panel();
            panelPersonalizado = new Guna2Panel();
            panelProgresso = new Guna2Panel();
            btnLimpezaLeve = new Guna2Button();
            btnLimpezaMedia = new Guna2Button();
            btnLimpezaAvancada = new Guna2Button();
            chkTemp = new Guna2CheckBox();
            chkDns = new Guna2CheckBox();
            chkPrefetch = new Guna2CheckBox();
            chkLixeira = new Guna2CheckBox();
            chkCache = new Guna2CheckBox();
            chkWinUpdate = new Guna2CheckBox();
            chkSpool = new Guna2CheckBox();
            btnExecutar = new Guna2Button();
            lblEtapaAtual = new Label();
            progressExecucao = new Guna2ProgressBar();
            btnCancelar = new Guna2Button();

            // Otimização
            panelOtimPerfis = new Guna2Panel();
            panelOtimDesc = new Guna2Panel();
            rbEscritorio = new Guna2RadioButton();
            rbUltraPerf = new Guna2RadioButton();
            rbHospital = new Guna2RadioButton();
            btnOtimizar = new Guna2Button();
            lblOtimDesc = new Label();

            // Rede
            panelBusca = new Guna2Panel();
            panelStatusRede = new Guna2Panel();
            panelAcoes = new Guna2Panel();
            txtMaquina = new Guna2TextBox();
            btnPing = new Guna2Button();
            btnMonitorar = new Guna2Button();
            lblStatusRede = new Label();
            btnConectar = new Guna2Button();
            btnFlushDns = new Guna2Button();
            btnReiniciarExplorer = new Guna2Button();
            btnReiniciar = new Guna2Button();
            btnDesligar = new Guna2Button();
            btnCancelarShutdown = new Guna2Button();
            btnPowerShell = new Guna2Button();
            btnGerenciamento = new Guna2Button();

            // Máquinas
            panelFiltroMaq = new Guna2Panel();
            panelListaMaq = new Guna2Panel();
            txtBuscaMaq = new Guna2TextBox();
            btnBuscarAD = new Guna2Button();
            btnVerificarStatus = new Guna2Button();
            lstMaquinas = new ListView();
            lblTotalMaq = new Label();
            lblOnlineMaq = new Label();
            pnlSetores = new Panel();

            // Usuários
            panelFiltroUsr = new Guna2Panel();
            panelListaUsr = new Guna2Panel();
            txtMaquinaUsr = new Guna2TextBox();
            btnListarUsuarios = new Guna2Button();
            lstUsuarios = new ListView();
            btnExcluirPasta = new Guna2Button();
            lblTotalUsr = new Label();

            // Sistema
            panelSistemaInfo = new Guna2Panel();
            panelSistemaAcoes = new Guna2Panel();
            panelSistemaLog = new Guna2Panel();
            btnCheckHealth = new Guna2Button();
            btnScanHealth = new Guna2Button();
            btnSfcScannow = new Guna2Button();
            btnRestoreHealth = new Guna2Button();
            btnVerifCompleta = new Guna2Button();
            txtMaquinaSis = new Guna2TextBox();
            lblSistemaStatus = new Label();
            progressSistema = new Guna2ProgressBar();
            lblEtapaSistema = new Label();
            rtbSistemaLog = new RichTextBox();

            // ════════════════════════════════════════════════════════════
            // ABA IMPRESSORAS
            // ════════════════════════════════════════════════════════════

            panelFiltroImp = CriarCard(10, 10, 1130, 60);
            panelFiltroImp.Name = "panelFiltroImp";

            txtBuscaImp = CriarTextBox(
                "🔍  Pesquisar por nome, localização, modelo ou IP...",
                14, 14, 560, 32);
            txtBuscaImp.Name = "txtBuscaImp";
            txtBuscaImp.TextChanged += txtBuscaImp_TextChanged;

            btnAtualizarImp = CriarBotao("🔄  Atualizar Tudo",
                584, 14, 170, 32,
                UIHelper.CorAzulFundo, UIHelper.CorAzul);
            btnAtualizarImp.Name = "btnAtualizarImp";
            btnAtualizarImp.Font = UIHelper.FonteNormal;
            btnAtualizarImp.Click += btnAtualizarImp_Click;

            // Adicione estas 2 linhas ANTES de usar lblTotalImp.Text = "Total: 0"; etc
            lblTotalImp = new Label();
            lblOnlineImp = new Label();

            lblTotalImp.Text = "Total: 0";
            lblTotalImp.ForeColor = UIHelper.CorTextoClaro;
            lblTotalImp.BackColor = Color.Transparent;
            lblTotalImp.Font = UIHelper.FonteNormal;
            lblTotalImp.Location = new Point(900, 20);
            lblTotalImp.Size = new Size(100, 20);
            lblTotalImp.Name = "lblTotalImp";

            lblOnlineImp.Text = "Online: 0";
            lblOnlineImp.ForeColor = UIHelper.CorVerde;
            lblOnlineImp.BackColor = Color.Transparent;
            lblOnlineImp.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblOnlineImp.Location = new Point(1010, 20);
            lblOnlineImp.Size = new Size(110, 20);
            lblOnlineImp.Name = "lblOnlineImp";

            panelFiltroImp.Controls.Add(txtBuscaImp);
            panelFiltroImp.Controls.Add(btnAtualizarImp);
            panelFiltroImp.Controls.Add(lblTotalImp);
            panelFiltroImp.Controls.Add(lblOnlineImp);

            // Painel com scroll que vai conter os cards
            panelListaImp = CriarCard(10, 80, 1130, 540);
            panelListaImp.Name = "panelListaImp";

            flowImpressoras = new FlowLayoutPanel();
            flowImpressoras.Name = "flowImpressoras";
            flowImpressoras.Location = new Point(1, 1);
            flowImpressoras.Size = new Size(1126, 536);
            flowImpressoras.AutoScroll = true;
            flowImpressoras.BackColor = UIHelper.CorCard;
            flowImpressoras.FlowDirection = FlowDirection.LeftToRight;
            flowImpressoras.WrapContents = true;
            flowImpressoras.Padding = new Padding(8);

            panelListaImp.Controls.Add(flowImpressoras);

            tabImpressoras.Controls.Add(panelFiltroImp);
            tabImpressoras.Controls.Add(panelListaImp);

            // Logs
            panelToolbarLogs = new Guna2Panel();
            btnAbrirLogs = new Guna2Button();
            btnLimparLogs = new Guna2Button();
            txtLogs = new RichTextBox();

            SuspendLayout();

            // ── TAB CONTROL ───────────────────────────────────────────────
            tabMain.Controls.Add(tabDashboard);
            tabMain.Controls.Add(tabLimpeza);
            tabMain.Controls.Add(tabOtimizacao);
            tabMain.Controls.Add(tabRede);
            tabMain.Controls.Add(tabMaquinas);
            tabMain.Controls.Add(tabUsuarios);
            tabMain.Controls.Add(tabSistema);
            tabMain.Controls.Add(tabImpressoras);
            tabMain.Controls.Add(tabLogs);
            tabMain.Dock = DockStyle.Fill;
            tabMain.ItemSize = new Size(120, 36);
            tabMain.Name = "tabMain";
            tabMain.SelectedIndex = 0;
            tabMain.ForeColor = UIHelper.CorTextoClaro;
            tabMain.Font = UIHelper.FonteNormal;
            tabMain.Font = UIHelper.FonteNormal;

            // ── Configuração das abas ─────────────────────────────────────
            void ConfigTab(TabPage tab, string nome, string texto)
            {
                tab.Name = nome;
                tab.Text = texto;
                tab.BackColor = UIHelper.CorFundo;
                tab.Padding = new Padding(10);
            }

            ConfigTab(tabDashboard, "tabDashboard", "  Dashboard");
            ConfigTab(tabLimpeza, "tabLimpeza", "  Limpeza");
            ConfigTab(tabOtimizacao, "tabOtimizacao", "  Otimização");
            ConfigTab(tabRede, "tabRede", "  Rede");
            ConfigTab(tabMaquinas, "tabMaquinas", "  Máquinas");
            ConfigTab(tabUsuarios, "tabUsuarios", "  Usuários");
            ConfigTab(tabSistema, "tabSistema", "  Sistema");
            ConfigTab(tabImpressoras, "tabImpressoras", "  Impressoras");
            ConfigTab(tabLogs, "tabLogs", "  Logs");

            // ════════════════════════════════════════════════════════════
            // ABA DASHBOARD
            // ════════════════════════════════════════════════════════════

            int cW = 360; int cH = 110;

            // Card CPU
            panelCpu = CriarCard(10, 10, cW, cH);
            panelCpu.Name = "panelCpu";
            lblCpuTit = CriarLabel("CPU", 14, 10, 200, 18,
                UIHelper.CorTextoClaro, 9f);
            lblCpuTit.Name = "lblCpuTit";
            lblCPU.Text = "0%";
            lblCPU.Font = UIHelper.FonteMetrica;
            lblCPU.ForeColor = UIHelper.CorVerde;
            lblCPU.BackColor = Color.Transparent;
            lblCPU.Location = new Point(14, 26);
            lblCPU.Size = new Size(180, 50);
            lblCPU.Name = "lblCPU";
            progressCpu = CriarProgressBar(14, 88, cW - 28, 8, UIHelper.CorVerde);
            progressCpu.Name = "progressCpu";
            panelCpu.Controls.Add(lblCpuTit);
            panelCpu.Controls.Add(lblCPU);
            panelCpu.Controls.Add(progressCpu);

            // Card RAM
            panelRam = CriarCard(380, 10, cW, cH);
            panelRam.Name = "panelRam";
            lblRamTit = CriarLabel("RAM", 14, 10, 200, 18,
                UIHelper.CorTextoClaro, 9f);
            lblRamTit.Name = "lblRamTit";
            lblRAM.Text = "0%";
            lblRAM.Font = UIHelper.FonteMetrica;
            lblRAM.ForeColor = UIHelper.CorVerde;
            lblRAM.BackColor = Color.Transparent;
            lblRAM.Location = new Point(14, 26);
            lblRAM.Size = new Size(180, 50);
            lblRAM.Name = "lblRAM";
            progressRam = CriarProgressBar(14, 88, cW - 28, 8, UIHelper.CorVerde);
            progressRam.Name = "progressRam";
            panelRam.Controls.Add(lblRamTit);
            panelRam.Controls.Add(lblRAM);
            panelRam.Controls.Add(progressRam);

            // Card Disco
            panelDisco = CriarCard(750, 10, cW, cH);
            panelDisco.Name = "panelDisco";
            lblDiscoTit = CriarLabel("Disco C:", 14, 10, 200, 18,
                UIHelper.CorTextoClaro, 9f);
            lblDiscoTit.Name = "lblDiscoTit";
            lblDisco.Text = "0%";
            lblDisco.Font = UIHelper.FonteMetrica;
            lblDisco.ForeColor = UIHelper.CorVerde;
            lblDisco.BackColor = Color.Transparent;
            lblDisco.Location = new Point(14, 26);
            lblDisco.Size = new Size(180, 50);
            lblDisco.Name = "lblDisco";
            progressDisco = CriarProgressBar(14, 88, cW - 28, 8, UIHelper.CorVerde);
            progressDisco.Name = "progressDisco";
            panelDisco.Controls.Add(lblDiscoTit);
            panelDisco.Controls.Add(lblDisco);
            panelDisco.Controls.Add(progressDisco);

            // Card Info Máquina
            panelInfoMaquina = CriarCard(10, 130, 540, 200);
            panelInfoMaquina.Name = "panelInfoMaquina";
            var lblSecMaq = CriarLabel("MÁQUINA", 14, 10, 200, 16,
                UIHelper.CorTextoClaro, 8.5f);

            void AddInfo(Guna2Panel p, ref Label lblK, ref Label lblV,
                string keyTxt, string valTxt, string nomeV,
                int y, Color? cor = null)
            {
                lblK = CriarLabel(keyTxt, 14, y, 120, 20,
                    UIHelper.CorTextoClaro, 9f);
                lblV.Text = valTxt;
                lblV.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                lblV.ForeColor = cor ?? UIHelper.CorTexto;
                lblV.BackColor = Color.Transparent;
                lblV.Location = new Point(140, y);
                lblV.Size = new Size(380, 20);
                lblV.Name = nomeV;
                p.Controls.Add(lblK);
                p.Controls.Add(lblV);
            }

            AddInfo(panelInfoMaquina, ref lblNomeMaqTit, ref lblMaquina,
                "Máquina:", "—", "lblMaquina", 34);
            AddInfo(panelInfoMaquina, ref lblIPTit, ref lblIP,
                "IP:", "—", "lblIP", 62);
            AddInfo(panelInfoMaquina, ref lblUsuarioTit, ref lblUsuario,
                "Usuário:", "—", "lblUsuario", 90);
            AddInfo(panelInfoMaquina, ref lblSOTit, ref lblSO,
                "Sistema:", "—", "lblSO", 118);
            AddInfo(panelInfoMaquina, ref lblModeTit, ref lblMode,
                "Modo:", "LOCAL", "lblMode", 146, UIHelper.CorAzul);
            panelInfoMaquina.Controls.Add(lblSecMaq);

            // Card Info Sistema
            panelInfoSistema = CriarCard(560, 130, 550, 200);
            panelInfoSistema.Name = "panelInfoSistema";
            var lblSecSis = CriarLabel("SISTEMA", 14, 10, 200, 16,
                UIHelper.CorTextoClaro, 8.5f);

            AddInfo(panelInfoSistema, ref lblStatusTit, ref lblStatus,
                "Status:", "● ONLINE", "lblStatus", 34, UIHelper.CorVerde);
            AddInfo(panelInfoSistema, ref lblUptimeTit, ref lblUptime,
                "Uptime:", "—", "lblUptime", 62);
            AddInfo(panelInfoSistema, ref lblRamTotalTit, ref lblRamTotal,
                "RAM Total:", "—", "lblRamTotal", 90);
            AddInfo(panelInfoSistema, ref lblDiscoLivreTit, ref lblDiscoLivre,
                "Disco Livre:", "—", "lblDiscoLivre", 118);
            panelInfoSistema.Controls.Add(lblSecSis);

            tabDashboard.Controls.Add(panelCpu);
            tabDashboard.Controls.Add(panelRam);
            tabDashboard.Controls.Add(panelDisco);
            tabDashboard.Controls.Add(panelInfoMaquina);
            tabDashboard.Controls.Add(panelInfoSistema);

            // ════════════════════════════════════════════════════════════
            // ABA LIMPEZA
            // ════════════════════════════════════════════════════════════

            panelPresets = CriarCard(10, 10, 380, 250);
            panelPresets.Name = "panelPresets";
            var lblPresetsTit = CriarLabel("PRESETS RÁPIDOS",
                14, 10, 340, 16, UIHelper.CorTextoClaro, 8.5f);

            btnLimpezaLeve = CriarBotao("🌿  Limpeza Leve",
                14, 34, 350, 52,
                UIHelper.CorVerdeFundo, UIHelper.CorVerde);
            btnLimpezaLeve.Name = "btnLimpezaLeve";
            btnLimpezaLeve.Click += btnLimpezaLeve_Click;

            btnLimpezaMedia = CriarBotao("🔥  Limpeza Média",
                14, 96, 350, 52,
                UIHelper.CorAmareloFundo, UIHelper.CorAmarelo);
            btnLimpezaMedia.Name = "btnLimpezaMedia";
            btnLimpezaMedia.Click += btnLimpezaMedia_Click;

            btnLimpezaAvancada = CriarBotao("⚡  Limpeza Avançada",
                14, 158, 350, 52,
                UIHelper.CorVermelhoFundo, UIHelper.CorVermelho);
            btnLimpezaAvancada.Name = "btnLimpezaAvancada";
            btnLimpezaAvancada.Click += btnLimpezaAvancada_Click;

            panelPresets.Controls.Add(lblPresetsTit);
            panelPresets.Controls.Add(btnLimpezaLeve);
            panelPresets.Controls.Add(btnLimpezaMedia);
            panelPresets.Controls.Add(btnLimpezaAvancada);

            // Personalizado
            panelPersonalizado = CriarCard(400, 10, 740, 250);
            panelPersonalizado.Name = "panelPersonalizado";
            var lblPersTit = CriarLabel("PERSONALIZADA",
                14, 10, 700, 16, UIHelper.CorTextoClaro, 8.5f);

            void AddChk(Guna2CheckBox chk, string texto,
                string nome, int x, int y)
            {
                chk.Text = texto;
                chk.ForeColor = UIHelper.CorTexto;
                chk.Font = UIHelper.FonteNormal;
                chk.Location = new Point(x, y);
                chk.Size = new Size(220, 26);
                chk.Name = nome;
                chk.CheckedState.FillColor = UIHelper.CorAzul;
                panelPersonalizado.Controls.Add(chk);
            }

            AddChk(chkTemp, "Temp do usuário", "chkTemp", 14, 34);
            AddChk(chkDns, "Cache DNS", "chkDns", 14, 66);
            AddChk(chkPrefetch, "Prefetch", "chkPrefetch", 14, 98);
            AddChk(chkLixeira, "Lixeira", "chkLixeira", 14, 130);
            AddChk(chkCache, "Cache navegadores", "chkCache", 250, 34);
            AddChk(chkWinUpdate, "Windows Update", "chkWinUpdate", 250, 66);
            AddChk(chkSpool, "Spool impressão", "chkSpool", 250, 98);

    

            btnExecutar = CriarBotao("▶  Executar Selecionados",
                14, 206, 500, 34,
                UIHelper.CorAzulFundo, UIHelper.CorAzul);
            btnExecutar.Name = "btnExecutar";
            btnExecutar.Click += btnExecutar_Click;

            panelPersonalizado.Controls.Add(lblPersTit);
            panelPersonalizado.Controls.Add(chkEmails);
            panelPersonalizado.Controls.Add(dtpEmails);
            panelPersonalizado.Controls.Add(btnExecutar);

            // Progresso
            panelProgresso = CriarCard(10, 270, 1130, 70);
            panelProgresso.Name = "panelProgresso";

            lblEtapaAtual.Text = "Aguardando...";
            lblEtapaAtual.ForeColor = UIHelper.CorTextoClaro;
            lblEtapaAtual.BackColor = Color.Transparent;
            lblEtapaAtual.Font = UIHelper.FonteNormal;
            lblEtapaAtual.Location = new Point(14, 10);
            lblEtapaAtual.Size = new Size(900, 18);
            lblEtapaAtual.Name = "lblEtapaAtual";

            progressExecucao = CriarProgressBar(14, 34, 1000, 16);
            progressExecucao.Name = "progressExecucao";

            btnCancelar = CriarBotao("Cancelar",
                1020, 18, 100, 34,
                UIHelper.CorVermelhoFundo, UIHelper.CorVermelho);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Enabled = false;
            btnCancelar.Click += btnCancelar_Click;

            panelProgresso.Controls.Add(lblEtapaAtual);
            panelProgresso.Controls.Add(progressExecucao);
            panelProgresso.Controls.Add(btnCancelar);

            tabLimpeza.Controls.Add(panelPresets);
            tabLimpeza.Controls.Add(panelPersonalizado);
            tabLimpeza.Controls.Add(panelProgresso);

            // ════════════════════════════════════════════════════════════
            // ABA OTIMIZAÇÃO
            // ════════════════════════════════════════════════════════════

            panelOtimPerfis = CriarCard(10, 10, 380, 210);
            panelOtimPerfis.Name = "panelOtimPerfis";
            var lblOtimTit = CriarLabel("PERFIL DE OTIMIZAÇÃO",
                14, 10, 340, 16, UIHelper.CorTextoClaro, 8.5f);

            void AddRadio(Guna2RadioButton rb, string texto,
    string nome, int y, bool check = false)
            {
                rb.Text = texto;
                rb.ForeColor = UIHelper.CorTexto;
                rb.Font = UIHelper.FonteNormal;
                rb.Location = new Point(14, y); // era 14 → agora 20
                rb.Size = new Size(350, 40);
                rb.Checked = check;
                rb.Cursor = Cursors.Hand;
                rb.Name = nome;
                rb.ImageAlign = ContentAlignment.MiddleLeft;
                rb.CheckedState.FillColor = UIHelper.CorAzul;
                panelOtimPerfis.Controls.Add(rb);
            }

            AddRadio(rbEscritorio, "💼  Escritório",
                "rbEscritorio", 34, true);
            AddRadio(rbUltraPerf, "⚡  Ultra Performance",
                "rbUltraPerf", 94);
            AddRadio(rbHospital, "🏥  Hospital",
                "rbHospital", 154);

            rbEscritorio.CheckedChanged += (s, e) => AtualizarDescOtim();
            rbUltraPerf.CheckedChanged += (s, e) => AtualizarDescOtim();
            rbHospital.CheckedChanged += (s, e) => AtualizarDescOtim();

            panelOtimPerfis.Controls.Add(lblOtimTit);

            panelOtimDesc = CriarCard(400, 10, 740, 210);
            panelOtimDesc.Name = "panelOtimDesc";
            var lblDescTit = CriarLabel("DETALHES DO PERFIL",
                14, 10, 700, 16, UIHelper.CorTextoClaro, 8.5f);

            lblOtimDesc.Text = ObterDescricaoPerfil(0);
            lblOtimDesc.ForeColor = UIHelper.CorTextoClaro;
            lblOtimDesc.BackColor = Color.Transparent;
            lblOtimDesc.Font = UIHelper.FonteNormal;
            lblOtimDesc.Location = new Point(14, 34);
            lblOtimDesc.Size = new Size(710, 165);
            lblOtimDesc.Name = "lblOtimDesc";

            panelOtimDesc.Controls.Add(lblDescTit);
            panelOtimDesc.Controls.Add(lblOtimDesc);

            btnOtimizar = CriarBotao("🚀  Aplicar Otimização",
                10, 230, 1130, 52,
                UIHelper.CorAzulFundo, UIHelper.CorAzul, 8);
            btnOtimizar.Name = "btnOtimizar";
            btnOtimizar.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            btnOtimizar.Click += btnOtimizar_Click;

            tabOtimizacao.Controls.Add(panelOtimPerfis);
            tabOtimizacao.Controls.Add(panelOtimDesc);
            tabOtimizacao.Controls.Add(btnOtimizar);

            // ════════════════════════════════════════════════════════════
            // ABA REDE
            // ════════════════════════════════════════════════════════════

            panelBusca = CriarCard(10, 10, 1130, 60);
            panelBusca.Name = "panelBusca";
            var lblMaqLbl = CriarLabel("Máquina ou IP:",
                14, 18, 130, 22, UIHelper.CorTextoClaro);

            txtMaquina = CriarTextBox(
                "Ex: PC-INF-01 ou 192.168.1.50",
                152, 14, 500, 32);
            txtMaquina.Name = "txtMaquina";

            btnPing = CriarBotao("📡 Ping",
                662, 14, 100, 32,
                UIHelper.CorAzulFundo, UIHelper.CorAzul);
            btnPing.Name = "btnPing";
            btnPing.Font = UIHelper.FonteNormal;
            btnPing.Click += btnPing_Click;

            btnMonitorar = CriarBotao("📺 Monitorar",
                772, 14, 120, 32,
                UIHelper.CorAzulFundo, UIHelper.CorAzul);
            btnMonitorar.Name = "btnMonitorar";
            btnMonitorar.Font = UIHelper.FonteNormal;
            btnMonitorar.Click += btnMonitorar_Click;

            panelBusca.Controls.Add(lblMaqLbl);
            panelBusca.Controls.Add(txtMaquina);
            panelBusca.Controls.Add(btnPing);
            panelBusca.Controls.Add(btnMonitorar);

            panelStatusRede = CriarCard(10, 80, 1130, 36);
            panelStatusRede.Name = "panelStatusRede";
            var lblStLbl = CriarLabel("Status:",
                14, 8, 60, 20, UIHelper.CorTextoClaro);

            lblStatusRede.Text = "Aguardando...";
            lblStatusRede.ForeColor = UIHelper.CorTextoClaro;
            lblStatusRede.BackColor = Color.Transparent;
            lblStatusRede.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblStatusRede.Location = new Point(80, 8);
            lblStatusRede.Size = new Size(1030, 20);
            lblStatusRede.Name = "lblStatusRede";

            panelStatusRede.Controls.Add(lblStLbl);
            panelStatusRede.Controls.Add(lblStatusRede);

            panelAcoes = CriarCard(10, 126, 1130, 340);
            panelAcoes.Name = "panelAcoes";
            var lblAcoesTit = CriarLabel("AÇÕES REMOTAS",
                14, 10, 400, 16, UIHelper.CorTextoClaro, 8.5f);

            int bW = 358; int bH = 52;
            int bY1 = 32; int bY2 = 94; int bY3 = 156;

            // ANTES — linha bY3 tinha só 2 botões:
            btnPowerShell = CriarBotao("⚡  PowerShell Remoto",
                14, bY3, bW, bH,
                UIHelper.CorRoxoFundo, UIHelper.CorRoxo);
            btnPowerShell.Name = "btnPowerShell";
            btnPowerShell.Click += btnPowerShell_Click;

            btnGerenciamento = CriarBotao("🔧  Gerenciamento",
                390, bY3, bW, bH,
                UIHelper.CorAzulFundo, UIHelper.CorAzul);
            btnGerenciamento.Name = "btnGerenciamento";
            btnGerenciamento.Click += btnGerenciamento_Click;

            // DEPOIS — linha bY3 com 2 + nova linha bY4 com RDP e Assistência:
            btnPowerShell = CriarBotao("⚡  PowerShell Remoto",
                14, bY3, bW, bH,
                UIHelper.CorRoxoFundo, UIHelper.CorRoxo);
            btnPowerShell.Name = "btnPowerShell";
            btnPowerShell.Click += btnPowerShell_Click;

            btnGerenciamento = CriarBotao("🔧  Gerenciamento",
                390, bY3, bW, bH,
                UIHelper.CorAzulFundo, UIHelper.CorAzul);
            btnGerenciamento.Name = "btnGerenciamento";
            btnGerenciamento.Click += btnGerenciamento_Click;

            // Nova linha — Acesso Remoto
            int bY4 = bY3 + 62;

            btnRDP = CriarBotao("🖥  Área de Trabalho Remota (RDP)",
                14, bY4, bW, bH,
                UIHelper.CorCianoFundo, UIHelper.CorCiano);
            btnRDP.Name = "btnRDP";
            btnRDP.Click += btnRDP_Click;

            btnAssistencia = CriarBotao("🤝  Assistência Remota",
                390, bY4, bW, bH,
                UIHelper.CorVerdeFundo, UIHelper.CorVerde);
            btnAssistencia.Name = "btnAssistencia";
            btnAssistencia.Click += btnAssistencia_Click;

            panelAcoes.Controls.Add(btnRDP);
            panelAcoes.Controls.Add(btnAssistencia);

            btnConectar = CriarBotao("📁  Abrir C$",
                14, bY1, bW, bH,
                UIHelper.CorAzulFundo, UIHelper.CorAzul);
            btnConectar.Name = "btnConectar";
            btnConectar.Click += btnConectar_Click;

            btnFlushDns = CriarBotao("🔄  Flush DNS",
                382, bY1, bW, bH,
                UIHelper.CorVerdeFundo, UIHelper.CorVerde);
            btnFlushDns.Name = "btnFlushDns";
            btnFlushDns.Click += btnFlushDns_Click;

            btnReiniciarExplorer = CriarBotao("🖥  Reiniciar Explorer",
                750, bY1, bW, bH,
                UIHelper.CorAmareloFundo, UIHelper.CorAmarelo);
            btnReiniciarExplorer.Name = "btnReiniciarExplorer";
            btnReiniciarExplorer.Click += btnReiniciarExplorer_Click;

            btnReiniciar = CriarBotao("🔁  Reiniciar Máquina",
                14, bY2, bW, bH,
                UIHelper.CorVermelhoFundo, UIHelper.CorVermelho);
            btnReiniciar.Name = "btnReiniciar";
            btnReiniciar.Click += btnReiniciar_Click;

            btnDesligar = CriarBotao("⏻  Desligar Máquina",
                382, bY2, bW, bH,
                UIHelper.CorVermelhoFundo, UIHelper.CorVermelho);
            btnDesligar.Name = "btnDesligar";
            btnDesligar.Click += btnDesligar_Click;

            btnCancelarShutdown = CriarBotao("⊘  Cancelar Shutdown",
                750, bY2, bW, bH,
                UIHelper.CorAmareloFundo, UIHelper.CorAmarelo);
            btnCancelarShutdown.Name = "btnCancelarShutdown";
            btnCancelarShutdown.Click += btnCancelarShutdown_Click;

            btnPowerShell = CriarBotao("⚡  PowerShell Remoto",
                14, bY3, bW, bH,
                UIHelper.CorRoxoFundo, UIHelper.CorRoxo);
            btnPowerShell.Name = "btnPowerShell";
            btnPowerShell.Click += btnPowerShell_Click;

            btnGerenciamento = CriarBotao("🔧  Gerenciamento",
                382, bY3, bW, bH,
                UIHelper.CorAzulFundo, UIHelper.CorAzul);
            btnGerenciamento.Name = "btnGerenciamento";
            btnGerenciamento.Click += btnGerenciamento_Click;

            panelAcoes.Controls.Add(lblAcoesTit);
            panelAcoes.Controls.Add(btnConectar);
            panelAcoes.Controls.Add(btnFlushDns);
            panelAcoes.Controls.Add(btnReiniciarExplorer);
            panelAcoes.Controls.Add(btnReiniciar);
            panelAcoes.Controls.Add(btnDesligar);
            panelAcoes.Controls.Add(btnCancelarShutdown);
            panelAcoes.Controls.Add(btnPowerShell);
            panelAcoes.Controls.Add(btnGerenciamento);

            tabRede.Controls.Add(panelBusca);
            tabRede.Controls.Add(panelStatusRede);
            tabRede.Controls.Add(panelAcoes);

            // ════════════════════════════════════════════════════════════
            // ABA MÁQUINAS
            // ════════════════════════════════════════════════════════════

            panelFiltroMaq = CriarCard(10, 10, 1130, 60);
            panelFiltroMaq.Name = "panelFiltroMaq";

            txtBuscaMaq = CriarTextBox(
                "🔍  Pesquisar por nome, setor ou IP...",
                14, 14, 500, 32);
            txtBuscaMaq.Name = "txtBuscaMaq";
            txtBuscaMaq.TextChanged += txtBuscaMaq_TextChanged;
            txtBuscaMaq.Location = new Point(14, 14);
            txtBuscaMaq.Size = new Size(280, 32);

            btnBuscarAD = CriarBotao("🔄  Buscar no AD",
                524, 14, 160, 32,
                UIHelper.CorAzulFundo, UIHelper.CorAzul);
            btnBuscarAD.Name = "btnBuscarAD";
            btnBuscarAD.Font = UIHelper.FonteNormal;
            btnBuscarAD.Click += btnBuscarAD_Click;
            btnBuscarAD.Location = new Point(304, 14);
            btnBuscarAD.Size = new Size(150, 32);

            btnVerificarStatus = CriarBotao("📡  Verificar Status",
                694, 14, 160, 32,
                UIHelper.CorVerdeFundo, UIHelper.CorVerde);
            btnVerificarStatus.Name = "btnVerificarStatus";
            btnVerificarStatus.Font = UIHelper.FonteNormal;
            btnVerificarStatus.Click += btnVerificarStatus_Click;
            btnVerificarStatus.Location = new Point(464, 14);
            btnVerificarStatus.Size = new Size(150, 32);

            btnDistribuir = CriarBotao("📤  Distribuir Arquivos",
    14, 56, 260, 32,
    UIHelper.CorRoxoFundo, UIHelper.CorRoxo);
            btnDistribuir.Name = "btnDistribuir";
            btnDistribuir.Font = UIHelper.FontePequena;
            btnDistribuir.Click += btnDistribuir_Click;
            btnDistribuir.Location = new Point(784, 14);
            btnDistribuir.Size = new Size(170, 32);

            panelFiltroMaq.Controls.Add(btnDistribuir);

            lblTotalMaq.Text = "Total: 0";
            lblTotalMaq.ForeColor = UIHelper.CorTextoClaro;
            lblTotalMaq.BackColor = Color.Transparent;
            lblTotalMaq.Font = UIHelper.FonteNormal;
            lblTotalMaq.Location = new Point(970, 8);
            lblTotalMaq.Size = new Size(150, 20);
            lblTotalMaq.Name = "lblTotalMaq";

            lblOnlineMaq.Text = "Online: 0";
            lblOnlineMaq.ForeColor = UIHelper.CorVerde;
            lblOnlineMaq.BackColor = Color.Transparent;
            lblOnlineMaq.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblOnlineMaq.Location = new Point(970, 28);
            lblOnlineMaq.Size = new Size(150, 20);
            lblOnlineMaq.Name = "lblOnlineMaq";

            panelFiltroMaq.Controls.Add(txtBuscaMaq);
            panelFiltroMaq.Controls.Add(btnBuscarAD);
            panelFiltroMaq.Controls.Add(btnVerificarStatus);
            panelFiltroMaq.Controls.Add(lblTotalMaq);
            panelFiltroMaq.Controls.Add(lblOnlineMaq);

            // Adicione logo após panelFiltroMaq.Controls.Add(lblOnlineMaq); :

            btnFiltroStatus = CriarBotao("🔵 Mostrando: Todos",
                14, 56, 250, 32,
                UIHelper.CorAzulFundo, UIHelper.CorAzul, 6);
            btnFiltroStatus.Name = "btnFiltroStatus";
            btnFiltroStatus.Font = UIHelper.FontePequena;
            btnFiltroStatus.Click += btnFiltroStatus_Click;
            btnFiltroStatus.Location = new Point(624, 14);
            btnFiltroStatus.Size = new Size(150, 32);

            panelFiltroMaq.Controls.Add(btnFiltroStatus);

            pnlSetores.BackColor = UIHelper.CorFundo;
            pnlSetores.Location = new Point(10, 80);
            pnlSetores.Size = new Size(1130, 36);
            pnlSetores.Name = "pnlSetores";

            panelListaMaq = CriarCard(10, 124, 1130, 460);
            panelListaMaq.Name = "panelListaMaq";

            lstMaquinas.View = View.Details;
            // Adicione esta linha na configuração do lstMaquinas:
            lstMaquinas.MultiSelect = true;
            lstMaquinas.FullRowSelect = true;
            lstMaquinas.GridLines = false;
            lstMaquinas.BackColor = UIHelper.CorCard;
            lstMaquinas.ForeColor = UIHelper.CorTexto;
            lstMaquinas.BorderStyle = BorderStyle.None;
            lstMaquinas.Font = UIHelper.FonteNormal;
            lstMaquinas.Dock = DockStyle.Fill;
            lstMaquinas.Size = new Size(1126, 436);
            lstMaquinas.Name = "lstMaquinas";
            lstMaquinas.DoubleClick += lstMaquinas_DoubleClick;
            lstMaquinas.Columns.Add("Máquina", 200);
            lstMaquinas.Columns.Add("Setor", 80);
            lstMaquinas.Columns.Add("IP", 130);
            lstMaquinas.Columns.Add("Status", 120);
            lstMaquinas.Columns.Add("Sistema", 90);
            lstMaquinas.Columns.Add("Usuário", 130);  // ← novo
            lstMaquinas.Columns.Add("Último Login", 110);
            lstMaquinas.Columns.Add("OU", 150);

            panelListaMaq.Controls.Add(lstMaquinas);

            tabMaquinas.Controls.Add(panelFiltroMaq);
            tabMaquinas.Controls.Add(pnlSetores);
            tabMaquinas.Controls.Add(panelListaMaq);

            // ════════════════════════════════════════════════════════════
            // ABA USUÁRIOS
            // ════════════════════════════════════════════════════════════

            panelFiltroUsr = CriarCard(10, 10, 1130, 60);
            panelFiltroUsr.Name = "panelFiltroUsr";
            var lblMaqUsr = CriarLabel("Máquina ou IP:",
                14, 18, 130, 22, UIHelper.CorTextoClaro);

            txtMaquinaUsr = CriarTextBox(
                "Deixe vazio para listar local",
                152, 14, 400, 32);
            txtMaquinaUsr.Name = "txtMaquinaUsr";

            btnListarUsuarios = CriarBotao("👥  Listar Usuários",
                562, 14, 180, 32,
                UIHelper.CorAzulFundo, UIHelper.CorAzul);
            btnListarUsuarios.Name = "btnListarUsuarios";
            btnListarUsuarios.Font = UIHelper.FonteNormal;
            btnListarUsuarios.Click += btnListarUsuarios_Click;

            // Adicione após btnListarUsuarios:
            txtBuscaUsr = CriarTextBox(
                "🔍 Filtrar por nome, último login...",
                752, 14, 360, 32);
            txtBuscaUsr.Name = "txtBuscaUsr";
            txtBuscaUsr.TextChanged += txtBuscaUsr_TextChanged;
            panelFiltroUsr.Controls.Add(txtBuscaUsr);

            lblTotalUsr.Text = "Total: 0 usuários";
            lblTotalUsr.ForeColor = UIHelper.CorTextoClaro;
            lblTotalUsr.BackColor = Color.Transparent;
            lblTotalUsr.Font = UIHelper.FonteNormal;
            lblTotalUsr.Location = new Point(900, 20);
            lblTotalUsr.Size = new Size(200, 20);
            lblTotalUsr.Name = "lblTotalUsr";

            panelFiltroUsr.Controls.Add(lblMaqUsr);
            panelFiltroUsr.Controls.Add(txtMaquinaUsr);
            panelFiltroUsr.Controls.Add(btnListarUsuarios);
            panelFiltroUsr.Controls.Add(lblTotalUsr);

            panelListaUsr = CriarCard(10, 80, 1130, 480);
            panelListaUsr.Name = "panelListaUsr";

            lstUsuarios.View = View.Details;
            lstUsuarios.FullRowSelect = true;
            lstUsuarios.BackColor = UIHelper.CorCard;
            lstUsuarios.ForeColor = UIHelper.CorTexto;
            lstUsuarios.BorderStyle = BorderStyle.None;
            lstUsuarios.Font = UIHelper.FonteNormal;
            lstUsuarios.Location = new Point(1, 1);
            lstUsuarios.Size = new Size(1126, 430);
            lstUsuarios.Name = "lstUsuarios";
            lstUsuarios.Columns.Add("Usuário", 200);
            lstUsuarios.Columns.Add("Último Login", 140);
            lstUsuarios.Columns.Add("Tamanho", 100);
            lstUsuarios.Columns.Add("Admin", 70);
            lstUsuarios.Columns.Add("Status", 120);
            lstUsuarios.Columns.Add("Caminho", 460);

            btnExcluirPasta = CriarBotao("🗑  Excluir Pasta do Usuário",
                14, 438, 380, 36,
                UIHelper.CorVermelhoFundo, UIHelper.CorVermelho);
            btnExcluirPasta.Name = "btnExcluirPasta";
            btnExcluirPasta.Font = UIHelper.FonteNormal;
            btnExcluirPasta.Click += btnExcluirPasta_Click;

            panelListaUsr.Controls.Add(lstUsuarios);
            panelListaUsr.Controls.Add(btnExcluirPasta);

            tabUsuarios.Controls.Add(panelFiltroUsr);
            tabUsuarios.Controls.Add(panelListaUsr);

            // ════════════════════════════════════════════════════════════
            // ABA SISTEMA
            // ════════════════════════════════════════════════════════════

            panelSistemaInfo = CriarCard(10, 10, 1130, 60);
            panelSistemaInfo.Name = "panelSistemaInfo";
            var lblSisMaqLbl = CriarLabel("Máquina ou IP:",
                14, 18, 130, 22, UIHelper.CorTextoClaro);

            txtMaquinaSis = CriarTextBox(
                "Deixe vazio para verificar local",
                152, 14, 400, 32);
            txtMaquinaSis.Name = "txtMaquinaSis";

            lblSistemaStatus.Text = "Aguardando...";
            lblSistemaStatus.ForeColor = UIHelper.CorTextoClaro;
            lblSistemaStatus.BackColor = Color.Transparent;
            lblSistemaStatus.Font = UIHelper.FonteNormal;
            lblSistemaStatus.Location = new Point(600, 18);
            lblSistemaStatus.Size = new Size(520, 22);
            lblSistemaStatus.Name = "lblSistemaStatus";

            panelSistemaInfo.Controls.Add(lblSisMaqLbl);
            panelSistemaInfo.Controls.Add(txtMaquinaSis);
            panelSistemaInfo.Controls.Add(lblSistemaStatus);

            panelSistemaAcoes = CriarCard(10, 80, 1130, 160);
            panelSistemaAcoes.Name = "panelSistemaAcoes";
            var lblSisAcoesTit = CriarLabel(
                "VERIFICAÇÃO DA IMAGEM DO WINDOWS",
                14, 10, 700, 16, UIHelper.CorTextoClaro, 8.5f);

            int sbW = 268; int sbH = 52;

            btnCheckHealth = CriarBotao("🔍  DISM CheckHealth",
                14, 34, sbW, sbH,
                UIHelper.CorVerdeFundo, UIHelper.CorVerde);
            btnCheckHealth.Name = "btnCheckHealth";
            btnCheckHealth.Click += btnCheckHealth_Click;

            btnScanHealth = CriarBotao("🔎  DISM ScanHealth",
                292, 34, sbW, sbH,
                UIHelper.CorAmareloFundo, UIHelper.CorAmarelo);
            btnScanHealth.Name = "btnScanHealth";
            btnScanHealth.Click += btnScanHealth_Click;

            btnSfcScannow = CriarBotao("🛡  SFC /scannow",
                570, 34, sbW, sbH,
                UIHelper.CorAzulFundo, UIHelper.CorAzul);
            btnSfcScannow.Name = "btnSfcScannow";
            btnSfcScannow.Click += btnSfcScannow_Click;

            btnRestoreHealth = CriarBotao("🔧  DISM RestoreHealth",
                848, 34, sbW, sbH,
                UIHelper.CorVermelhoFundo, UIHelper.CorVermelho);
            btnRestoreHealth.Name = "btnRestoreHealth";
            btnRestoreHealth.Click += btnRestoreHealth_Click;

            btnVerifCompleta = CriarBotao(
                "⚡  Verificação Completa (SFC + DISM em sequência)",
                14, 100, 1100, 48,
                UIHelper.CorCianoFundo, UIHelper.CorCiano, 8);
            btnVerifCompleta.Name = "btnVerifCompleta";
            btnVerifCompleta.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnVerifCompleta.Click += btnVerifCompleta_Click;

            panelSistemaAcoes.Controls.Add(lblSisAcoesTit);
            panelSistemaAcoes.Controls.Add(btnCheckHealth);
            panelSistemaAcoes.Controls.Add(btnScanHealth);
            panelSistemaAcoes.Controls.Add(btnSfcScannow);
            panelSistemaAcoes.Controls.Add(btnRestoreHealth);
            panelSistemaAcoes.Controls.Add(btnVerifCompleta);

            panelSistemaLog = CriarCard(10, 250, 1130, 360);
            panelSistemaLog.Name = "panelSistemaLog";
            var lblSisLogTit = CriarLabel("RESULTADO",
                14, 10, 300, 16, UIHelper.CorTextoClaro, 8.5f);

            progressSistema = CriarProgressBar(14, 32, 1100, 10,
                UIHelper.CorCiano);
            progressSistema.Name = "progressSistema";

            lblEtapaSistema.Text = "Aguardando...";
            lblEtapaSistema.ForeColor = UIHelper.CorTextoClaro;
            lblEtapaSistema.BackColor = Color.Transparent;
            lblEtapaSistema.Font = UIHelper.FonteNormal;
            lblEtapaSistema.Location = new Point(14, 48);
            lblEtapaSistema.Size = new Size(1100, 18);
            lblEtapaSistema.Name = "lblEtapaSistema";

            rtbSistemaLog.BackColor = Color.FromArgb(2, 6, 23);
            rtbSistemaLog.ForeColor = UIHelper.CorTexto;
            rtbSistemaLog.BorderStyle = BorderStyle.None;
            rtbSistemaLog.Font = UIHelper.FonteMono;
            rtbSistemaLog.Location = new Point(14, 72);
            rtbSistemaLog.Size = new Size(1100, 278);
            rtbSistemaLog.Name = "rtbSistemaLog";
            rtbSistemaLog.ReadOnly = true;
            rtbSistemaLog.ScrollBars = RichTextBoxScrollBars.Vertical;

            panelSistemaLog.Controls.Add(lblSisLogTit);
            panelSistemaLog.Controls.Add(progressSistema);
            panelSistemaLog.Controls.Add(lblEtapaSistema);
            panelSistemaLog.Controls.Add(rtbSistemaLog);

            tabSistema.Controls.Add(panelSistemaInfo);
            tabSistema.Controls.Add(panelSistemaAcoes);
            tabSistema.Controls.Add(panelSistemaLog);

            // ════════════════════════════════════════════════════════════
            // ABA LOGS
            // ════════════════════════════════════════════════════════════

            panelToolbarLogs = CriarCard(10, 10, 1130, 50);
            panelToolbarLogs.Name = "panelToolbarLogs";

            btnAbrirLogs = CriarBotao("📁  Abrir Pasta",
                10, 9, 180, 32,
                UIHelper.CorPainel, UIHelper.CorTextoClaro);
            btnAbrirLogs.Name = "btnAbrirLogs";
            btnAbrirLogs.Font = UIHelper.FonteNormal;
            btnAbrirLogs.Click += btnAbrirLogs_Click;

            btnLimparLogs = CriarBotao("🗑  Limpar Tela",
                200, 9, 160, 32,
                UIHelper.CorVermelhoFundo, UIHelper.CorVermelho);
            btnLimparLogs.Name = "btnLimparLogs";
            btnLimparLogs.Font = UIHelper.FonteNormal;
            btnLimparLogs.Click += btnLimparLogs_Click;

            panelToolbarLogs.Controls.Add(btnAbrirLogs);
            panelToolbarLogs.Controls.Add(btnLimparLogs);

            txtLogs.BackColor = Color.FromArgb(2, 6, 23);
            txtLogs.ForeColor = UIHelper.CorTexto;
            txtLogs.BorderStyle = BorderStyle.None;
            txtLogs.Font = UIHelper.FonteMono;
            txtLogs.Location = new Point(10, 70);
            txtLogs.Size = new Size(1130, 540);
            txtLogs.Name = "txtLogs";
            txtLogs.ReadOnly = true;
            txtLogs.ScrollBars = RichTextBoxScrollBars.Vertical;

            tabLogs.Controls.Add(panelToolbarLogs);
            tabLogs.Controls.Add(txtLogs);

            // ── FORM PRINCIPAL ────────────────────────────────────────────
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = UIHelper.CorFundo;
            ClientSize = new Size(1280, 720);
            Controls.Add(tabMain);
            Font = UIHelper.FonteNormal;
            ForeColor = UIHelper.CorTexto;
            MinimumSize = new Size(1100, 640);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "TIHub AMEB — Sistema de Suporte de TI";


            // ── HEADER ───────────────────────────────────────────────────────
            panelHeader.FillColor = Color.FromArgb(13, 17, 23);
            panelHeader.BorderColor = UIHelper.CorBorda;
            panelHeader.BorderThickness = 1;
            panelHeader.BorderRadius = 0;
            panelHeader.ShadowDecoration.Enabled = false;
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Size = new Size(1280, 52);
            panelHeader.Name = "panelHeader";

            // Ícone
            var panelIcone = new Guna2Panel();
            panelIcone.FillColor = UIHelper.CorAzulFundo;
            panelIcone.BorderColor = UIHelper.CorAzul;
            panelIcone.BorderThickness = 1;
            panelIcone.BorderRadius = 8;
            panelIcone.ShadowDecoration.Enabled = false;
            panelIcone.Location = new Point(14, 10);
            panelIcone.Size = new Size(32, 32);

            var lblIcone = new Label
            {
                Text = "TI",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = UIHelper.CorAzul,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            panelIcone.Controls.Add(lblIcone);

            // Título
            lblHeaderTitulo.Text = "TIHub AMEB";
            lblHeaderTitulo.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
            lblHeaderTitulo.ForeColor = UIHelper.CorTexto;
            lblHeaderTitulo.BackColor = Color.Transparent;
            lblHeaderTitulo.Location = new Point(54, 8);
            lblHeaderTitulo.Size = new Size(200, 20);
            lblHeaderTitulo.Name = "lblHeaderTitulo";

            // Subtítulo
            lblHeaderSub.Text = "SUPORTE DE TI";
            lblHeaderSub.Font = new Font("Segoe UI", 7.5f, FontStyle.Regular);
            lblHeaderSub.ForeColor = UIHelper.CorTextoEsc;
            lblHeaderSub.BackColor = Color.Transparent;
            lblHeaderSub.Location = new Point(55, 28);
            lblHeaderSub.Size = new Size(200, 14);
            lblHeaderSub.Name = "lblHeaderSub";

            // Separador vertical
            var sepHeader = new Panel
            {
                BackColor = UIHelper.CorBorda,
                Location = new Point(1100, 12),
                Size = new Size(1, 28)
            };

            // Máquina
            lblHeaderMaquina.Text = "LOCAL";
            lblHeaderMaquina.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblHeaderMaquina.ForeColor = UIHelper.CorTexto;
            lblHeaderMaquina.BackColor = Color.Transparent;
            lblHeaderMaquina.Location = new Point(1112, 10);
            lblHeaderMaquina.Size = new Size(160, 16);
            lblHeaderMaquina.Name = "lblMaquinaTopbar";

            // Status
            lblHeaderStatus.Text = "● Online";
            lblHeaderStatus.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblHeaderStatus.ForeColor = UIHelper.CorVerde;
            lblHeaderStatus.BackColor = Color.Transparent;
            lblHeaderStatus.Location = new Point(1112, 28);
            lblHeaderStatus.Size = new Size(160, 14);
            lblHeaderStatus.Name = "lblStatusTopbar";

            // Linha azul no rodapé do header
            var linhaAzul = new Panel
            {
                BackColor = UIHelper.CorAzul,
                Dock = DockStyle.Bottom,
                Size = new Size(1280, 2)
            };

            panelHeader.Controls.Add(panelIcone);
            panelHeader.Controls.Add(lblHeaderTitulo);
            panelHeader.Controls.Add(lblHeaderSub);
            panelHeader.Controls.Add(sepHeader);
            panelHeader.Controls.Add(lblHeaderMaquina);
            panelHeader.Controls.Add(lblHeaderStatus);
            panelHeader.Controls.Add(linhaAzul);

            // ── RODAPÉ ───────────────────────────────────────────────────────
            panelFooter.FillColor = Color.FromArgb(13, 17, 23);
            panelFooter.BorderColor = UIHelper.CorBorda;
            panelFooter.BorderThickness = 1;
            panelFooter.BorderRadius = 0;
            panelFooter.ShadowDecoration.Enabled = false;
            panelFooter.Dock = DockStyle.Bottom;
            panelFooter.Size = new Size(1280, 24);
            panelFooter.Name = "panelFooter";

            lblFooterDev.Text = "Desenvolvido por Vinicius Santos da Silva  ·  TI AMEB";
            lblFooterDev.Font = new Font("Segoe UI", 8f, FontStyle.Regular);
            lblFooterDev.ForeColor = UIHelper.CorTextoEsc;
            lblFooterDev.BackColor = Color.Transparent;
            lblFooterDev.Location = new Point(14, 4);
            lblFooterDev.Size = new Size(600, 16);
            lblFooterDev.Name = "lblFooterDev";

            lblFooterVer.Text = "v1.0.0";
            lblFooterVer.Font = new Font("Segoe UI", 8f, FontStyle.Regular);
            lblFooterVer.ForeColor = UIHelper.CorTextoClaro;
            lblFooterVer.BackColor = Color.Transparent;
            lblFooterVer.Location = new Point(1090, 4);
            lblFooterVer.Size = new Size(80, 16);
            lblFooterVer.TextAlign = ContentAlignment.MiddleRight;
            lblFooterVer.Name = "lblFooterVer";

            panelFooter.Controls.Add(lblFooterDev);
            panelFooter.Controls.Add(lblFooterVer);

            // ── Adicionar ao form (header e rodapé primeiro, TabControl depois)
            Controls.Clear();
            Controls.Add(tabMain);
            Controls.Add(panelFooter);
            Controls.Add(panelHeader);



            ResumeLayout(false);
        }

        // ── Descrições dos perfis ─────────────────────────────────────────

        private static string ObterDescricaoPerfil(int perfil) => perfil switch
        {
            0 => "💼  ESCRITÓRIO\n\n" +
                 "• Plano de energia Balanceado\n" +
                 "• Reduz animações e efeitos visuais\n" +
                 "• Remove transparência\n" +
                 "• Desativa animações de janela\n" +
                 "• Otimiza menu iniciar\n" +
                 "• Desativa notificações desnecessárias\n\n" +
                 "✓ Ideal para uso corporativo diário",

            1 => "⚡  ULTRA PERFORMANCE\n\n" +
                 "• Plano Máximo Desempenho\n" +
                 "• Remove TODOS os efeitos visuais\n" +
                 "• Prioriza CPU para programas\n" +
                 "• Otimiza memória virtual\n" +
                 "• Otimiza Prefetch e Superfetch\n" +
                 "• Desativa hibernação\n" +
                 "• Desativa serviços desnecessários\n\n" +
                 "⚠ Não recomendado para ambiente hospitalar",

            2 => "🏥  HOSPITAL\n\n" +
                 "• Plano Balanceado (seguro)\n" +
                 "• Reduz apenas animações visuais\n" +
                 "• Remove transparência\n" +
                 "• Ajusta timeout de tela\n" +
                 "• NÃO mexe em serviços críticos\n" +
                 "• NÃO desativa nada importante\n\n" +
                 "✓ Seguro para sistemas hospitalares",

            _ => string.Empty
        };

        private void AtualizarDescOtim()
        {
            int idx = rbEscritorio.Checked ? 0
                    : rbUltraPerf.Checked ? 1
                    : 2;
            lblOtimDesc.Text = ObterDescricaoPerfil(idx);
        }

        // ── Declaração das variáveis ──────────────────────────────────────

        private Guna2TabControl tabMain;
        private TabPage tabDashboard, tabLimpeza, tabOtimizacao,
                tabRede, tabMaquinas, tabUsuarios,
                tabSistema, tabLogs, tabImpressoras;

        // Dashboard
        private Guna2Panel panelCpu, panelRam, panelDisco;
        private Guna2Panel panelInfoMaquina, panelInfoSistema;
        private Label lblCpuTit, lblCPU, lblRamTit, lblRAM,
                      lblDiscoTit, lblDisco;
        private Label lblNomeMaqTit, lblMaquina, lblIPTit, lblIP;
        private Label lblUsuarioTit, lblUsuario, lblSOTit, lblSO;
        private Label lblModeTit, lblMode, lblStatusTit, lblStatus;
        private Label lblUptimeTit, lblUptime;
        private Label lblRamTotalTit, lblRamTotal;
        private Label lblDiscoLivreTit, lblDiscoLivre;
        private Guna2ProgressBar progressCpu, progressRam, progressDisco;

        // Limpeza
        private Guna2Panel panelPresets, panelPersonalizado, panelProgresso;
        private Guna2Button btnLimpezaLeve, btnLimpezaMedia, btnLimpezaAvancada;
        private Guna2Button btnExecutar, btnCancelar;
        private Guna2CheckBox chkTemp, chkDns, chkPrefetch, chkLixeira;
        private Guna2CheckBox chkCache, chkWinUpdate, chkSpool, chkEmails;
        private DateTimePicker dtpEmails;
        private Label lblEtapaAtual;
        private Guna2ProgressBar progressExecucao;

        // Otimização
        private Guna2Panel panelOtimPerfis, panelOtimDesc;
        private Guna2RadioButton rbEscritorio, rbUltraPerf, rbHospital;
        private Guna2Button btnOtimizar;
        private Label lblOtimDesc;

        // Rede
        private Guna2Panel panelBusca, panelStatusRede, panelAcoes;
        private Guna2TextBox txtMaquina;
        private Guna2Button btnPing, btnMonitorar, btnConectar;
        private Guna2Button btnFlushDns, btnReiniciarExplorer;
        private Guna2Button btnReiniciar, btnDesligar, btnCancelarShutdown;
        private Guna2Button btnPowerShell, btnGerenciamento;
        private Label lblStatusRede;

        // Máquinas
        private Guna2Panel panelFiltroMaq, panelListaMaq;
        private Guna2TextBox txtBuscaMaq;
        private Guna2Button btnBuscarAD, btnVerificarStatus;
        private ListView lstMaquinas;
        private Label lblTotalMaq, lblOnlineMaq;
        private Panel pnlSetores;

        // Usuários
        private Guna2Panel panelFiltroUsr, panelListaUsr;
        private Guna2TextBox txtMaquinaUsr;
        private Guna2Button btnListarUsuarios, btnExcluirPasta;
        private ListView lstUsuarios;
        private Label lblTotalUsr;

        // Sistema
        private Guna2Panel panelSistemaInfo, panelSistemaAcoes, panelSistemaLog;
        private Guna2TextBox txtMaquinaSis;
        private Guna2Button btnCheckHealth, btnScanHealth;
        private Guna2Button btnSfcScannow, btnRestoreHealth, btnVerifCompleta;
        private Label lblSistemaStatus, lblEtapaSistema;
        private Guna2ProgressBar progressSistema;
        private RichTextBox rtbSistemaLog;

        // Logs
        private Guna2Panel panelToolbarLogs;
        private Guna2Button btnAbrirLogs, btnLimparLogs;
        private RichTextBox txtLogs;

        private Label lblMaquinaTopbar;
        private Label lblStatusTopbar;

        private Guna2TextBox txtBuscaUsr;

        private Guna2Button btnRDP;
        private Guna2Button btnAssistencia;

        private Guna2Button btnFiltroStatus;

        // Impressoras
        private Guna2Panel panelFiltroImp, panelListaImp;
        private Guna2TextBox txtBuscaImp;
        private Guna2Button btnAtualizarImp;
        private Label lblTotalImp, lblOnlineImp;
        private FlowLayoutPanel flowImpressoras;

        private Guna2Button btnDistribuir;

        // Header e Rodapé
        private Guna2Panel panelHeader;
        private Guna2Panel panelFooter;
        private Label lblHeaderTitulo;
        private Label lblHeaderSub;
        private Label lblHeaderMaquina;
        private Label lblHeaderStatus;
        private Label lblFooterDev;
        private Label lblFooterVer;

    }
}