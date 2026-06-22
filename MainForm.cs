using Guna.UI2.WinForms;
using TIHubAMEB.Helpers;
using TIHubAMEB.Models;
using TIHubAMEB.Services;


namespace TIHubAMEB.Forms
{
    public partial class MainForm : Form
    {
        // ── Services ─────────────────────────────────────────────────────
        private readonly LogService _log;
        private readonly PsExecService _psExec;
        private readonly LimpezaService _limpeza;
        private readonly OtimizacaoService _otimizacao;
        private readonly RedeService _rede;
        private readonly MonitoramentoService _monitor;
        private readonly MaquinasService _maquinas;
        private readonly UsuarioService _usuarios;
        private readonly SistemaService _sistema;
        private readonly ImpressoraService _impressoras;
        private readonly System.Windows.Forms.Timer _timerMonitor;
        private readonly DistribuicaoService _distribuicao;
        private CancellationTokenSource? _cts;

        // Cache de impressoras carregadas
        private List<ImpressoraInfo> _listaImpressoras = new();

        // Cache de máquinas do AD
        private List<MaquinaRede> _listaMaquinas = new();

        private int _filtroStatusAtual = 0; // 0 = Todos, 1 = Online, 2 = Offline

        public MainForm()
        {
            InitializeComponent();

            // Ícone da janela (barra de título e taskbar)
            string caminhoIco = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "iconfinder-technologymachineelectronicdevice25-4026435_113356 (1).ico"); // ← troque pelo nome do seu arquivo

            if (File.Exists(caminhoIco))
                this.Icon = new Icon(caminhoIco);

            _log = new LogService();

            // Todos os outros services dependem do _log
            _psExec = new PsExecService(_log);
            _limpeza = new LimpezaService(_psExec, _log);
            _otimizacao = new OtimizacaoService(_psExec, _log);
            _rede = new RedeService(_psExec, _log);
            _monitor = new MonitoramentoService(_log);
            _maquinas = new MaquinasService(_log, _rede);
            _usuarios = new UsuarioService(_psExec, _log);
            _sistema = new SistemaService(_psExec, _log);
            _impressoras = new ImpressoraService(_log);
            _distribuicao = new DistribuicaoService(_log); // ← deve estar aqui

            _log.VincularControle(txtLogs);

            _timerMonitor = new System.Windows.Forms.Timer { Interval = 5000 };
            _timerMonitor.Tick += TimerMonitor_Tick;
            _timerMonitor.Start();

            _log.Registrar("LOCAL", "Sistema iniciado",
                "TIHub AMEB carregado", true, TipoLog.Info); _ = CarregarImpressorasIniciaisAsync();

        }

        // ── DASHBOARD ────────────────────────────────────────────────────

        private void TimerMonitor_Tick(object? sender, EventArgs e)
        {
            try
            {
                var info = _monitor.ObterInfoAtual();
                AtualizarDashboard(info);
            }
            catch { }
        }

        private void AtualizarDashboard(MaquinaInfo info)
        {
            // CPU
            lblCPU.Text = $"{info.CpuPercent:F1}%";
            lblCPU.ForeColor = UIHelper.CorPorPercentual(info.CpuPercent);
            UIHelper.AtualizarProgressBar(progressCpu, info.CpuPercent);

            // RAM
            lblRAM.Text = $"{info.RamPercent:F1}%";
            lblRAM.ForeColor = UIHelper.CorPorPercentual(info.RamPercent);
            UIHelper.AtualizarProgressBar(progressRam, info.RamPercent);

            // Disco
            lblDisco.Text = $"{info.DiscoPercent:F1}%";
            lblDisco.ForeColor = UIHelper.CorPorPercentual(info.DiscoPercent);
            UIHelper.AtualizarProgressBar(progressDisco, info.DiscoPercent);

            // Info
            lblMaquina.Text = info.NomeMaquina;
            lblIP.Text = info.IP;
            lblUsuario.Text = info.UsuarioLogado;
            lblSO.Text = info.SistemaOp;
            lblMode.Text = info.Modo;
            lblStatus.Text = info.Online ? "● ONLINE" : "○ OFFLINE";
            lblStatus.ForeColor = info.Online
                ? UIHelper.CorVerde : UIHelper.CorVermelho;
            lblUptime.Text = info.UptimeFormatado;
            lblRamTotal.Text = $"{info.RamTotalMB} MB";
            lblDiscoLivre.Text = $"{info.DiscoLivreGB} GB livres";

            // Topbar
            lblHeaderMaquina.Text = info.NomeMaquina;
            lblHeaderStatus.Text = info.Online ? "● ONLINE" : "○ OFFLINE";
            lblHeaderStatus.ForeColor = info.Online
                ? UIHelper.CorVerde : UIHelper.CorVermelho;
        }

        // ── LIMPEZA ──────────────────────────────────────────────────────

        private async void btnLimpezaLeve_Click(object sender, EventArgs e) =>
            await ExecutarLimpezaAsync(PerfilLimpeza.Leve());

        private async void btnLimpezaMedia_Click(object sender, EventArgs e) =>
            await ExecutarLimpezaAsync(PerfilLimpeza.Medio());

        private async void btnLimpezaAvancada_Click(object sender, EventArgs e) =>
            await ExecutarLimpezaAsync(PerfilLimpeza.Avancado());

        private async void btnExecutar_Click(object sender, EventArgs e)
        {
            var perfil = new PerfilLimpeza
            {
                Nome = "Personalizada",
                LimparTempUsuario = chkTemp.Checked,
                LimparCacheDns = chkDns.Checked,
                LimparPrefetch = chkPrefetch.Checked,
                LimparLixeira = chkLixeira.Checked,
                LimparCacheNavegador = chkCache.Checked,
                LimparWindowsUpdate = chkWinUpdate.Checked,
                LimparSpoolImpressao = chkSpool.Checked
            };
            await ExecutarLimpezaAsync(perfil);
        }

        private async Task ExecutarLimpezaAsync(PerfilLimpeza perfil)
        {
            _cts = new CancellationTokenSource();
            BloqueiarBotoes(true);
            btnCancelar.Enabled = true;

            string? maquina = ObterMaquinaAlvo();

            var progresso = new Progress<(int pct, string etapa)>(p =>
            {
                progressExecucao.Value = p.pct;
                lblEtapaAtual.Text = p.etapa;
                lblEtapaAtual.ForeColor = UIHelper.CorTextoClaro;
            });

            try
            {
                await _limpeza.ExecutarAsync(
                    perfil, maquina, progresso, _cts.Token);
                lblEtapaAtual.Text = "✓ Limpeza concluída!";
                lblEtapaAtual.ForeColor = UIHelper.CorVerde;
            }
            catch (OperationCanceledException)
            {
                lblEtapaAtual.Text = "⚠ Cancelado.";
                lblEtapaAtual.ForeColor = UIHelper.CorAmarelo;
            }
            catch (Exception ex)
            {
                lblEtapaAtual.Text = $"✕ Erro: {ex.Message}";
                lblEtapaAtual.ForeColor = UIHelper.CorVermelho;
            }
            finally
            {
                BloqueiarBotoes(false);
                btnCancelar.Enabled = false;
            }
        }

        private void btnCancelar_Click(object sender, EventArgs e) =>
            _cts?.Cancel();

        // ── OTIMIZAÇÃO ───────────────────────────────────────────────────

        private async void btnOtimizar_Click(object sender, EventArgs e)
        {
            var perfil = rbEscritorio.Checked ? PerfilOtimizacao.Escritorio
                       : rbUltraPerf.Checked ? PerfilOtimizacao.UltraPerformance
                       : PerfilOtimizacao.Hospital;

            BloqueiarBotoes(true);
            string? maquina = ObterMaquinaAlvo();

            var progresso = new Progress<(int pct, string etapa)>(p =>
            {
                progressExecucao.Value = p.pct;
                lblEtapaAtual.Text = p.etapa;
            });

            try
            {
                await _otimizacao.AplicarPerfilAsync(perfil, maquina, progresso);
                lblEtapaAtual.Text = "✓ Otimização concluída!";
                lblEtapaAtual.ForeColor = UIHelper.CorVerde;
            }
            catch (Exception ex)
            {
                lblEtapaAtual.Text = $"✕ Erro: {ex.Message}";
                lblEtapaAtual.ForeColor = UIHelper.CorVermelho;
            }
            finally
            {
                BloqueiarBotoes(false);
            }
        }

        // ── REDE ─────────────────────────────────────────────────────────

        private async void btnPing_Click(object sender, EventArgs e)
        {
            string host = txtMaquina.Text.Trim();
            if (string.IsNullOrEmpty(host))
            {
                lblStatusRede.Text = "⚠ Digite o nome ou IP.";
                lblStatusRede.ForeColor = UIHelper.CorAmarelo;
                return;
            }
            lblStatusRede.Text = "Enviando ping...";
            lblStatusRede.ForeColor = UIHelper.CorTextoClaro;

            var (ok, ms) = await _rede.PingAsync(host);
            lblStatusRede.Text = ok ? $"● Online — {ms}ms" : "● Sem resposta";
            lblStatusRede.ForeColor = ok ? UIHelper.CorVerde : UIHelper.CorVermelho;
        }

        private void btnMonitorar_Click(object sender, EventArgs e)
        {
            string maquina = txtMaquina.Text.Trim();
            _monitor.DefinirMaquina(maquina);
            lblHeaderMaquina.Text = string.IsNullOrEmpty(maquina)
                ? "LOCAL" : maquina.ToUpper();
            lblStatusRede.Text = string.IsNullOrEmpty(maquina)
                ? "Monitorando PC local"
                : $"● Monitorando: {maquina.ToUpper()}";
            lblStatusRede.ForeColor = UIHelper.CorAzul;
        }

        private void btnConectar_Click(object sender, EventArgs e)
        {
            string host = txtMaquina.Text.Trim();
            if (!string.IsNullOrEmpty(host))
                _rede.AbrirCompartilhamento(host);
        }

        private async void btnFlushDns_Click(object sender, EventArgs e)
        {
            string host = txtMaquina.Text.Trim();
            if (!string.IsNullOrEmpty(host))
                await _rede.FlushDnsAsync(host);
        }

        private async void btnReiniciarExplorer_Click(object sender, EventArgs e)
        {
            string host = txtMaquina.Text.Trim();
            if (!string.IsNullOrEmpty(host))
                await _rede.ReiniciarExplorerAsync(host);
        }

        private async void btnReiniciar_Click(object sender, EventArgs e)
        {
            string host = txtMaquina.Text.Trim();
            if (string.IsNullOrEmpty(host)) return;
            if (MessageBox.Show(
                $"Reiniciar {host} em 30 segundos?",
                "Confirmar", MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) == DialogResult.Yes)
                await _rede.ReiniciarMaquinaAsync(host);
        }

        private async void btnDesligar_Click(object sender, EventArgs e)
        {
            string host = txtMaquina.Text.Trim();
            if (string.IsNullOrEmpty(host)) return;
            if (MessageBox.Show(
                $"Desligar {host} em 30 segundos?",
                "Confirmar", MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) == DialogResult.Yes)
                await _rede.DesligarMaquinaAsync(host);
        }

        private async void btnCancelarShutdown_Click(object sender, EventArgs e)
        {
            string host = txtMaquina.Text.Trim();
            if (!string.IsNullOrEmpty(host))
                await _rede.CancelarShutdownAsync(host);
        }

        private async void btnPowerShell_Click(object sender, EventArgs e)
        {
            string host = txtMaquina.Text.Trim();
            if (string.IsNullOrEmpty(host)) return;

            using var form = new Form();
            using var txtBox = new TextBox();
            using var btn = new Button();

            form.Text = "PowerShell Remoto";
            form.Size = new Size(500, 200);
            form.StartPosition = FormStartPosition.CenterParent;
            form.BackColor = UIHelper.CorFundo;
            form.ForeColor = UIHelper.CorTexto;

            txtBox.Text = "Get-Process";
            txtBox.Location = new Point(12, 12);
            txtBox.Size = new Size(460, 100);
            txtBox.Multiline = true;
            txtBox.BackColor = UIHelper.CorPainel;
            txtBox.ForeColor = UIHelper.CorTexto;
            txtBox.BorderStyle = BorderStyle.FixedSingle;
            txtBox.Font = UIHelper.FonteMono;

            btn.Text = "Executar";
            btn.Location = new Point(12, 124);
            btn.Size = new Size(460, 34);
            btn.BackColor = UIHelper.CorAzulFundo;
            btn.ForeColor = UIHelper.CorAzul;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.DialogResult = DialogResult.OK;

            form.Controls.Add(txtBox);
            form.Controls.Add(btn);
            form.AcceptButton = btn;

            if (form.ShowDialog() == DialogResult.OK &&
                !string.IsNullOrWhiteSpace(txtBox.Text))
                await _rede.ExecutarPowerShellRemotoAsync(host, txtBox.Text.Trim());
        }

        private void btnGerenciamento_Click(object sender, EventArgs e)
        {
            string host = txtMaquina.Text.Trim();
            if (!string.IsNullOrEmpty(host))
                _rede.AbrirGerenciamento(host);
        }

        // ── MÁQUINAS ─────────────────────────────────────────────────────

        private async void btnBuscarAD_Click(object sender, EventArgs e)
        {
            btnBuscarAD.Enabled = false;
            lblTotalMaq.Text = "Buscando...";

            var progresso = new Progress<string>(msg =>
                lblTotalMaq.Text = msg);

            _listaMaquinas = await _maquinas.BuscarMaquinasADAsync(
                true, progresso);

            AtualizarListaMaquinas(_listaMaquinas);
            CriarBotoesSetores();
            btnBuscarAD.Enabled = true;
        }

        private async void btnVerificarStatus_Click(object sender, EventArgs e)
        {
            if (_listaMaquinas.Count == 0) return;

            btnVerificarStatus.Enabled = false;
            lblOnlineMaq.Text = "Verificando...";

            var progresso = new Progress<(int pct, string maq)>(p =>
                lblOnlineMaq.Text = $"Verificando... {p.pct}%");

            await _maquinas.VerificarStatusAsync(_listaMaquinas, progresso);

            AtualizarListaMaquinas(_listaMaquinas);
            btnVerificarStatus.Enabled = true;
        }

        private void txtBuscaMaq_TextChanged(object sender, EventArgs e)
        {
            AplicarFiltrosMaquinas();
        }

        private void AtualizarListaMaquinas(List<MaquinaRede> lista)
        {
            lstMaquinas.BeginUpdate();
            lstMaquinas.Items.Clear();

            foreach (var m in lista)
            {
                var item = new ListViewItem(m.Nome);
                item.SubItems.Add(m.Setor);
                item.SubItems.Add(m.IP);
                item.SubItems.Add(m.StatusFormatado);
                item.SubItems.Add(m.SistemaOp);
                item.SubItems.Add(m.UsuarioConectado);  // ← novo
                item.SubItems.Add(m.UltimaVezFormatado);
                item.SubItems.Add(m.UO);
                item.ForeColor = m.Online
                    ? UIHelper.CorVerde : UIHelper.CorTextoClaro;
                item.Tag = m;
                lstMaquinas.Items.Add(item);
            }

            lstMaquinas.EndUpdate();

            int online = lista.Count(m => m.Online);
            lblTotalMaq.Text = $"Total: {lista.Count}";
            lblOnlineMaq.Text = $"Online: {online}";
        }

        private void CriarBotoesSetores()
        {
            pnlSetores.Controls.Clear();
            int x = 0;

            var setores = _maquinas.ObterSetores(_listaMaquinas);
            setores.Insert(0, "Todos");

            foreach (var setor in setores)
            {
                var btn = new Button
                {
                    Text = $"{setor} ({(setor == "Todos" ? _listaMaquinas.Count : _listaMaquinas.Count(m => m.Setor == setor))})",
                    Location = new Point(x, 4),
                    Size = new Size(90, 28),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = UIHelper.CorPainel,
                    ForeColor = UIHelper.CorTextoClaro,
                    Font = UIHelper.FontePequena,
                    Cursor = Cursors.Hand,
                    Tag = setor
                };
                btn.FlatAppearance.BorderColor = UIHelper.CorBorda;
                btn.Click += (s, e) =>
                {
                    string setorSel = (string)((Button)s!).Tag!;
                    var filtrado = _maquinas.FiltrarPorSetor(
                        _listaMaquinas, setorSel);
                    AtualizarListaMaquinas(filtrado);
                };
                pnlSetores.Controls.Add(btn);
                x += 96;
            }
        }

        private void lstMaquinas_DoubleClick(object sender, EventArgs e)
        {
            if (lstMaquinas.SelectedItems.Count == 0) return;
            if (lstMaquinas.SelectedItems[0].Tag is MaquinaRede maq)
            {
                txtMaquina.Text = maq.Nome;
                tabMain.SelectedTab = tabRede;
                _log.Registrar("LOCAL", "Selecionou máquina",
                    maq.Nome, true, TipoLog.Maquinas);
            }
        }

        // ── USUÁRIOS ─────────────────────────────────────────────────────

        private async void btnListarUsuarios_Click(object sender, EventArgs e)
        {
            btnListarUsuarios.Enabled = false;
            lstUsuarios.Items.Clear();

            string? maquina = string.IsNullOrWhiteSpace(txtMaquinaUsr.Text)
                ? null : txtMaquinaUsr.Text.Trim();

            _listaUsuarios = await _usuarios.ListarUsuariosAsync(maquina);
            AtualizarListaUsuarios(_listaUsuarios);

            btnListarUsuarios.Enabled = true;
        }

        private async void btnExcluirPasta_Click(object sender, EventArgs e)
        {
            if (lstUsuarios.SelectedItems.Count == 0)
            {
                MessageBox.Show("Selecione um usuário na lista.",
                    "Aviso", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var u = lstUsuarios.SelectedItems[0].Tag as UsuarioInfo;

            if (u == null) return;

            if (u.Protegido)
            {
                MessageBox.Show(
                    $"Não é possível excluir: {u.MotivoProtecao}",
                    "Bloqueado", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show(
                $"Excluir pasta de {u.NomeUsuario}?\n\n" +
                $"Caminho: {u.CaminhoPerfilC}\n" +
                $"Tamanho: {u.TamanhoPastaFormatado}\n\n" +
                "Esta ação não pode ser desfeita!",
                "Confirmar exclusão",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes) return;

            string? maquina = string.IsNullOrWhiteSpace(txtMaquinaUsr.Text)
                ? null : txtMaquinaUsr.Text.Trim();

            btnExcluirPasta.Enabled = false;
            bool ok = await _usuarios.ExcluirPastaUsuarioAsync(u, maquina);

            MessageBox.Show(
                ok ? "Pasta excluída com sucesso!"
                   : "Erro ao excluir. Verifique os logs.",
                ok ? "Sucesso" : "Erro",
                MessageBoxButtons.OK,
                ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);

            btnExcluirPasta.Enabled = true;
            if (ok) btnListarUsuarios_Click(sender, e);
        }

        // ── SISTEMA ──────────────────────────────────────────────────────

        private async void btnCheckHealth_Click(object sender, EventArgs e) =>
            await ExecutarSistemaAsync(() => _sistema.CheckHealthAsync(
                ObterMaquinaSistema()), "DISM CheckHealth");

        private async void btnScanHealth_Click(object sender, EventArgs e) =>
            await ExecutarSistemaAsync(() => _sistema.ScanHealthAsync(
                ObterMaquinaSistema()), "DISM ScanHealth");

        private async void btnSfcScannow_Click(object sender, EventArgs e) =>
            await ExecutarSistemaAsync(() => _sistema.SfcScannowAsync(
                ObterMaquinaSistema()), "SFC /scannow");

        private async void btnRestoreHealth_Click(object sender, EventArgs e) =>
            await ExecutarSistemaAsync(() => _sistema.RestoreHealthAsync(
                ObterMaquinaSistema()), "DISM RestoreHealth");

        private async void btnVerifCompleta_Click(object sender, EventArgs e)
        {
            BloqueiarBotoesSistema(true);
            string? maquina = ObterMaquinaSistema();

            var progresso = new Progress<(int pct, string etapa, string detalhe)>(p =>
            {
                progressSistema.Value = p.pct;
                lblEtapaSistema.Text = $"{p.etapa} — {p.detalhe}";
                AppendSistemaLog($"[{p.etapa}] {p.detalhe}");
            });

            try
            {
                await _sistema.ExecutarVerificacaoCompletaAsync(
                    maquina, progresso);
                lblSistemaStatus.Text = "✓ Verificação completa concluída!";
                lblSistemaStatus.ForeColor = UIHelper.CorVerde;
            }
            catch (Exception ex)
            {
                lblSistemaStatus.Text = $"✕ Erro: {ex.Message}";
                lblSistemaStatus.ForeColor = UIHelper.CorVermelho;
            }
            finally
            {
                BloqueiarBotoesSistema(false);
            }
        }

        private async Task ExecutarSistemaAsync(
            Func<Task<ResultadoExecucao>> acao, string nomeAcao)
        {
            BloqueiarBotoesSistema(true);
            lblSistemaStatus.Text = $"⏳ Executando {nomeAcao}...";
            lblSistemaStatus.ForeColor = UIHelper.CorTextoClaro;
            progressSistema.Value = 0;

            try
            {
                var r = await acao();
                lblSistemaStatus.Text = r.Sucesso
                    ? $"✓ {nomeAcao} concluído"
                    : $"✕ Erro: {r.Erro}";
                lblSistemaStatus.ForeColor = r.Sucesso
                    ? UIHelper.CorVerde : UIHelper.CorVermelho;
                progressSistema.Value = 100;
                AppendSistemaLog($"[{nomeAcao}]\n{r.SaidaOuErro}");
            }
            catch (Exception ex)
            {
                lblSistemaStatus.Text = $"✕ Erro: {ex.Message}";
                lblSistemaStatus.ForeColor = UIHelper.CorVermelho;
            }
            finally
            {
                BloqueiarBotoesSistema(false);
            }
        }

        private void AppendSistemaLog(string texto)
        {
            rtbSistemaLog.SelectionStart = rtbSistemaLog.TextLength;
            rtbSistemaLog.SelectionColor = UIHelper.CorTextoClaro;
            rtbSistemaLog.AppendText(
                $"[{DateTime.Now:HH:mm:ss}] {texto}\n");
            rtbSistemaLog.ScrollToCaret();
        }

        private string? ObterMaquinaSistema()
        {
            string v = txtMaquinaSis.Text.Trim();
            return string.IsNullOrEmpty(v) ? null : v;
        }

        private void BloqueiarBotoesSistema(bool bloquear)
        {
            btnCheckHealth.Enabled = !bloquear;
            btnScanHealth.Enabled = !bloquear;
            btnSfcScannow.Enabled = !bloquear;
            btnRestoreHealth.Enabled = !bloquear;
            btnVerifCompleta.Enabled = !bloquear;
        }

        // ── IMPRESSORAS ──────────────────────────────────────────────────

        private async Task CarregarImpressorasIniciaisAsync()
        {
            _listaImpressoras = await _impressoras.CarregarCadastroAsync();
            AtualizarListaImpressoras(_listaImpressoras);
        }

        private async void btnAtualizarImp_Click(object sender, EventArgs e)
        {
            btnAtualizarImp.Enabled = false;
            lblTotalImp.Text = "Verificando...";

            // Recarrega o cadastro (caso tenha sido editado) e verifica status
            _listaImpressoras = await _impressoras.CarregarCadastroAsync();

            var progresso = new Progress<(int pct, string nome)>(p =>
                lblTotalImp.Text = $"Verificando... {p.pct}%");

            await _impressoras.VerificarTodasAsync(_listaImpressoras, progresso);

            AtualizarListaImpressoras(_listaImpressoras);
            btnAtualizarImp.Enabled = true;
        }

        private void txtBuscaImp_TextChanged(object sender, EventArgs e)
        {
            var filtrado = _impressoras.Filtrar(_listaImpressoras, txtBuscaImp.Text);
            AtualizarListaImpressoras(filtrado);
        }

        private void AtualizarListaImpressoras(List<ImpressoraInfo> lista)
        {
            // Alertas (offline / toner crítico) sobem para o topo
            var ordenada = _impressoras.OrdenarComAlertasNoTopo(lista);

            flowImpressoras.SuspendLayout();
            flowImpressoras.Controls.Clear();

            foreach (var imp in ordenada)
                flowImpressoras.Controls.Add(CriarCardImpressora(imp));

            flowImpressoras.ResumeLayout();

            int online = lista.Count(i => i.Online);
            lblTotalImp.Text = $"Total: {lista.Count}";
            lblOnlineImp.Text = $"Online: {online}";
        }

        private Guna2Panel CriarCardImpressora(ImpressoraInfo imp)
        {
            var card = new Guna2Panel
            {
                Size = new Size(266, 168),
                Margin = new Padding(6),
                FillColor = UIHelper.CorPainel,
                BorderColor = imp.TemAlerta ? UIHelper.CorVermelho : UIHelper.CorBorda,
                BorderThickness = imp.TemAlerta ? 2 : 1,
                BorderRadius = 10
            };
            card.ShadowDecoration.Enabled = false;

            // Nome
            var lblNome = new Label
            {
                Text = imp.Nome,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = UIHelper.CorTexto,
                BackColor = Color.Transparent,
                Location = new Point(12, 10),
                Size = new Size(180, 20)
            };

            // Localização
            var lblLoc = new Label
            {
                Text = $"📍 {imp.Localizacao}",
                Font = UIHelper.FontePequena,
                ForeColor = UIHelper.CorTextoClaro,
                BackColor = Color.Transparent,
                Location = new Point(12, 30),
                Size = new Size(242, 16),
                AutoEllipsis = true
            };

            // Status
            Color corStatus = !imp.Online ? UIHelper.CorTextoClaro
                             : imp.TonerCritico ? UIHelper.CorVermelho
                             : imp.TonerBaixo ? UIHelper.CorAmarelo
                             : UIHelper.CorVerde;

            var lblStatus = new Label
            {
                Text = imp.StatusFormatado,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = corStatus,
                BackColor = Color.Transparent,
                Location = new Point(12, 50),
                Size = new Size(242, 18)
            };


            // Linha única: "Toner: 63%"
            var lblTonerInfo = new Label
            {
                Text = $"Toner: {imp.TonerFormatado}",
                Font = UIHelper.FontePequena,
                ForeColor = UIHelper.CorTextoClaro,
                BackColor = Color.Transparent,
                Location = new Point(12, 72),
                Size = new Size(242, 16)
            };

            // Barra ocupando a linha de baixo, sozinha, sem nada ao lado
            var progressToner = new Guna2ProgressBar
            {
                Location = new Point(12, 84),
                Size = new Size(242, 8),
                Maximum = 100,
                Value = imp.ConsultaSnmpOk && imp.NivelTonerPreto >= 0
                                    ? imp.NivelTonerPreto : 0,
                FillColor = Color.FromArgb(33, 38, 45),
                ProgressColor = corStatus,
                ProgressColor2 = corStatus,
                BorderRadius = 4,
                BackColor = Color.Transparent
            };

            // Modelo + IP
            var lblInfo = new Label
            {
                Text = $"{imp.Modelo}  •  {imp.IP}",
                Font = UIHelper.FontePequena,
                ForeColor = UIHelper.CorTextoClaro,
                BackColor = Color.Transparent,
                Location = new Point(12, 100),
                Size = new Size(242, 16)
            };

            // Botões de ação
            var btnWeb = new Guna2Button
            {
                Text = "🌐 Painel",
                Location = new Point(12, 122),
                Size = new Size(80, 30),
                FillColor = UIHelper.CorAzulFundo,
                ForeColor = UIHelper.CorAzul,
                BorderColor = UIHelper.CorAzul,
                BorderThickness = 1,
                BorderRadius = 6,
                Font = UIHelper.FontePequena,
                Cursor = Cursors.Hand
            };
            btnWeb.Click += (s, e) => _impressoras.AbrirPainelWeb(imp);

            var btnFila = new Guna2Button
            {
                Text = "📋 Fila",
                Location = new Point(96, 122),
                Size = new Size(80, 30),
                FillColor = UIHelper.CorPainel,
                ForeColor = UIHelper.CorTextoClaro,
                BorderColor = UIHelper.CorBorda,
                BorderThickness = 1,
                BorderRadius = 6,
                Font = UIHelper.FontePequena,
                Cursor = Cursors.Hand
            };
            btnFila.Click += (s, e) => _impressoras.AbrirFilaImpressao(imp);

            var btnLimpar = new Guna2Button
            {
                Text = "Limpar",
                Location = new Point(180, 122),
                Size = new Size(74, 30),
                FillColor = UIHelper.CorVermelhoFundo,
                ForeColor = UIHelper.CorVermelho,
                BorderColor = UIHelper.CorVermelho,
                BorderThickness = 1,
                BorderRadius = 6,
                Font = UIHelper.FontePequena,
                Cursor = Cursors.Hand
            };
            btnLimpar.Click += async (s, e) =>
            {
                btnLimpar.Enabled = false;
                await _impressoras.LimparFilaAsync(imp);
                btnLimpar.Enabled = true;
            };

            card.Controls.Add(lblNome);
            card.Controls.Add(lblLoc);
            card.Controls.Add(lblStatus);
            card.Controls.Add(progressToner);
            card.Controls.Add(lblInfo);
            card.Controls.Add(btnWeb);
            card.Controls.Add(btnFila);
            card.Controls.Add(btnLimpar);

            return card;
        }

        // ── LOGS ─────────────────────────────────────────────────────────

        private void btnLimparLogs_Click(object sender, EventArgs e)
        {
            txtLogs.Clear();
            _log.Registrar("LOCAL", "Limpar tela", "Log limpo", true);
        }

        private void btnAbrirLogs_Click(object sender, EventArgs e) =>
            _log.AbrirPastaLogs();

        // ── UTILITÁRIOS ──────────────────────────────────────────────────

        private string? ObterMaquinaAlvo()
        {
            string v = txtMaquina.Text.Trim();
            return string.IsNullOrEmpty(v) ? null : v;
        }

        private void BloqueiarBotoes(bool bloquear)
        {
            btnLimpezaLeve.Enabled = !bloquear;
            btnLimpezaMedia.Enabled = !bloquear;
            btnLimpezaAvancada.Enabled = !bloquear;
            btnExecutar.Enabled = !bloquear;
            btnOtimizar.Enabled = !bloquear;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _timerMonitor.Stop();
            _monitor.Dispose();
            _cts?.Cancel();
            base.OnFormClosing(e);
        }

        // Campo no topo da classe:
        private List<UsuarioInfo> _listaUsuarios = new();

        // Método de filtro:
        private void txtBuscaUsr_TextChanged(object sender, EventArgs e)
        {
            string termo = txtBuscaUsr.Text.Trim();

            var filtrado = string.IsNullOrEmpty(termo)
                ? _listaUsuarios
                : _listaUsuarios.Where(u =>
                    u.NomeUsuario.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                    u.UltimoLoginFormatado.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                    u.CaminhoPerfilC.Contains(termo, StringComparison.OrdinalIgnoreCase))
                .ToList();

            AtualizarListaUsuarios(filtrado);
        }

        // Extraia a atualização da lista para método separado:
        private void AtualizarListaUsuarios(List<UsuarioInfo> lista)
        {
            lstUsuarios.BeginUpdate();
            lstUsuarios.Items.Clear();

            foreach (var u in lista)
            {
                var item = new ListViewItem(u.NomeUsuario);
                item.SubItems.Add(u.UltimoLoginFormatado);
                item.SubItems.Add(u.TamanhoPastaFormatado);
                item.SubItems.Add(u.EhAdministrador ? "Sim" : "Não");
                item.SubItems.Add(u.Protegido
                    ? $"🔒 {u.MotivoProtecao}" : "✓ Disponível");
                item.SubItems.Add(u.CaminhoPerfilC);
                item.ForeColor = u.Protegido
                    ? UIHelper.CorTextoClaro : UIHelper.CorTexto;
                item.Tag = u;
                lstUsuarios.Items.Add(item);
            }

            lstUsuarios.EndUpdate();
            lblTotalUsr.Text = $"Total: {lista.Count} usuários";
        }
        private void btnRDP_Click(object sender, EventArgs e)
        {
            string host = txtMaquina.Text.Trim();
            if (string.IsNullOrEmpty(host))
            {
                lblStatusRede.Text = "⚠ Digite o nome ou IP da máquina.";
                lblStatusRede.ForeColor = UIHelper.CorAmarelo;
                return;
            }
            _rede.AbrirRDP(host);
        }

        private void btnAssistencia_Click(object sender, EventArgs e)
        {
            string host = txtMaquina.Text.Trim();
            if (string.IsNullOrEmpty(host))
            {
                lblStatusRede.Text = "⚠ Digite o nome ou IP da máquina.";
                lblStatusRede.ForeColor = UIHelper.CorAmarelo;
                return;
            }
            _rede.AbrirAssistenciaRemota(host);
        }

        // Novo evento:
        private void btnFiltroStatus_Click(object sender, EventArgs e)
        {
            // Alterna entre os 3 estados: Todos → Online → Offline → Todos
            _filtroStatusAtual = (_filtroStatusAtual + 1) % 3;

            string texto = _filtroStatusAtual switch
            {
                1 => "🟢 Mostrando: Online",
                2 => "⚪ Mostrando: Offline",
                _ => "🔵 Mostrando: Todos"
            };

            btnFiltroStatus.Text = texto;
            AplicarFiltrosMaquinas();
        }

        // Método central que aplica TODOS os filtros juntos
        // (texto + status), sem quebrar nada que já existia
        private void AplicarFiltrosMaquinas()
        {
            var filtrado = _maquinas.Filtrar(_listaMaquinas, txtBuscaMaq.Text);

            filtrado = _filtroStatusAtual switch
            {
                1 => filtrado.Where(m => m.Online).ToList(),
                2 => filtrado.Where(m => !m.Online).ToList(),
                _ => filtrado
            };

            AtualizarListaMaquinas(filtrado);
        }

        private async void btnDistribuir_Click(object sender, EventArgs e)
        {
            // Verifica se tem máquinas selecionadas
            if (lstMaquinas.SelectedItems.Count == 0)
            {
                MessageBox.Show(
                    "Selecione pelo menos uma máquina na lista.\n\n" +
                    "Dica: use Ctrl+Clique para selecionar várias,\n" +
                    "ou Ctrl+A para selecionar todas.",
                    "Nenhuma máquina selecionada",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Abre janela para escolher os arquivos
            using var dialog = new OpenFileDialog
            {
                Title = "Selecione os arquivos para distribuir",
                Multiselect = true,
                Filter = "Todos os arquivos (*.*)|*.*|" +
                                   "Scripts VBS (*.vbs)|*.vbs|" +
                                   "Atalhos (*.lnk)|*.lnk|" +
                                   "Executáveis (*.exe)|*.exe"
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            // Monta lista de máquinas selecionadas
            var maquinas = lstMaquinas.SelectedItems
                .Cast<ListViewItem>()
                .Select(i => i.Text)
                .ToList();

            string[] arquivos = dialog.FileNames;

            // Confirmação
            var confirma = MessageBox.Show(
                $"Copiar {arquivos.Length} arquivo(s) para o Desktop Público de " +
                $"{maquinas.Count} máquina(s)?\n\n" +
                $"Arquivos:\n" +
                string.Join("\n", arquivos.Select(Path.GetFileName)) +
                $"\n\nMáquinas:\n" +
                string.Join(", ", maquinas.Take(5)) +
                (maquinas.Count > 5 ? $"\n... e mais {maquinas.Count - 5}" : ""),
                "Confirmar distribuição",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirma != DialogResult.Yes) return;

            // Executa
            btnDistribuir.Enabled = false;
            lblTotalMaq.Text = "Distribuindo...";

            var progresso = new Progress<(int pct, string maq, bool ok)>(p =>
            {
                lblTotalMaq.Text = $"Distribuindo... {p.pct}%";
                _log.Registrar(p.maq, "Distribuição",
                    p.ok ? "✓ Copiado" : "✕ Falhou",
                    p.ok, TipoLog.Rede);
            });

            var resultado = await _distribuicao.DistribuirAsync(
                maquinas, arquivos, progresso);

            btnDistribuir.Enabled = true;
            lblTotalMaq.Text = $"Total: {_listaMaquinas.Count}";

            // Resultado final
            MessageBox.Show(
                $"Distribuição concluída!\n\n" +
                $"✓ Sucesso: {resultado.Sucesso.Count} máquinas\n" +
                $"✕ Falhou: {resultado.Falhou.Count} máquinas\n\n" +
                (resultado.Falhou.Count > 0
                    ? $"Máquinas com falha:\n" +
                      string.Join("\n", resultado.Falhou)
                    : "Todos os arquivos foram copiados com sucesso!"),
                "Resultado da Distribuição",
                MessageBoxButtons.OK,
                resultado.Falhou.Count == 0
                    ? MessageBoxIcon.Information
                    : MessageBoxIcon.Warning);
        }
    }
}