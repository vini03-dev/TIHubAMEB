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
        private readonly InicializacaoService _inicializacao;
        private readonly RedeService _rede;
        private readonly MonitoramentoService _monitor;
        private readonly MaquinasService _maquinas;
        private readonly UsuarioService _usuarios;
        private readonly SistemaService _sistema;
        private readonly ImpressoraService _impressoras;
        private readonly ProgramasService _programas;
        private readonly System.Windows.Forms.Timer _timerMonitor;
        private readonly DistribuicaoService _distribuicao;
        private CancellationTokenSource? _cts;

        // Cache de impressoras carregadas
        private List<ImpressoraInfo> _listaImpressoras = new();

        // Cache de máquinas do AD
        private List<MaquinaRede> _listaMaquinas = new();

        private int _filtroStatusAtual = 0; // 0 = Todos, 1 = Online, 2 = Offline

        // ── Auto-refresh do status das máquinas ──
        // ── Auto-refresh do status das máquinas ──
        private System.Windows.Forms.Timer _timerAutoRefresh;
        private bool _verificandoStatus = false;
        private bool _sistemaOcupado = false;

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
            _inicializacao = new InicializacaoService(_psExec, _log);
            _rede = new RedeService(_psExec, _log);
            _monitor = new MonitoramentoService(_log);
            _maquinas = new MaquinasService(_log, _rede);
            _usuarios = new UsuarioService(_psExec, _log);
            _sistema = new SistemaService(_psExec, _log);
            _impressoras = new ImpressoraService(_log);
            _programas = new ProgramasService(_psExec, _log);
            _distribuicao = new DistribuicaoService(_log); // ← deve estar aqui

            _log.VincularControle(txtLogs);

           

            _timerMonitor = new System.Windows.Forms.Timer { Interval = 5000 };
            _timerMonitor.Tick += TimerMonitor_Tick;
            _timerMonitor.Start();

            ConfigurarCopiaMaquinas();

            // ── Auto-refresh do status das máquinas ──
            _timerAutoRefresh = new System.Windows.Forms.Timer { Interval = 180000 }; // 3 min
            _timerAutoRefresh.Tick += TimerAutoRefresh_Tick;
            chkAutoRefresh.CheckedChanged += chkAutoRefresh_CheckedChanged;
            if (chkAutoRefresh.Checked) _timerAutoRefresh.Start();

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

        // ── GERENCIADOR DE INICIALIZAÇÃO ─────────────────────────────────

        // Guarda a lista atual para saber qual item foi selecionado
        private List<ItemInicializacao> _itensInit = new();

        private async void btnListarInit_Click(object sender, EventArgs e)
        {
            string? maquina = string.IsNullOrWhiteSpace(txtMaquinaInit.Text)
                ? null : txtMaquinaInit.Text.Trim();

            btnListarInit.Enabled = false;
            lblInitInfo.Text = "Lendo inicialização...";
            lstInicializacao.Items.Clear();

            _itensInit = await _inicializacao.ListarAsync(maquina);

            foreach (var item in _itensInit)
            {
                var lvi = new ListViewItem(item.Nome);
                lvi.SubItems.Add(item.CaminhoLimpo);
                lvi.SubItems.Add(item.Origem);
                lvi.SubItems.Add(item.EstadoFormatado);

                // Cor diferente para itens desativados
                if (!item.Ativo)
                    lvi.ForeColor = UIHelper.CorTextoClaro;

                lstInicializacao.Items.Add(lvi);
            }

            int ativos = _itensInit.Count(i => i.Ativo);
            int desativados = _itensInit.Count(i => !i.Ativo);
            lblInitInfo.Text =
                $"{_itensInit.Count} itens — {ativos} ativos, " +
                $"{desativados} desativados" +
                (maquina != null ? $"  ·  {maquina}" : "  ·  este PC");

            btnListarInit.Enabled = true;
        }

        private async void btnDesativarInit_Click(object sender, EventArgs e)
        {
            var item = ObterItemInitSelecionado();
            if (item == null) return;

            if (!item.Ativo)
            {
                MessageBox.Show("Esse item já está desativado.",
                    "Inicialização", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            string? maquina = string.IsNullOrWhiteSpace(txtMaquinaInit.Text)
                ? null : txtMaquinaInit.Text.Trim();

            btnDesativarInit.Enabled = false;
            bool ok = await _inicializacao.DesativarAsync(item, maquina);
            btnDesativarInit.Enabled = true;

            if (ok)
            {
                // Recarrega a lista para refletir a mudança
                btnListarInit_Click(sender, e);
            }
            else
            {
                MessageBox.Show("Não foi possível desativar o item.",
                    "Inicialização", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private async void btnReativarInit_Click(object sender, EventArgs e)
        {
            var item = ObterItemInitSelecionado();
            if (item == null) return;

            if (item.Ativo)
            {
                MessageBox.Show("Esse item já está ativo.",
                    "Inicialização", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            string? maquina = string.IsNullOrWhiteSpace(txtMaquinaInit.Text)
                ? null : txtMaquinaInit.Text.Trim();

            btnReativarInit.Enabled = false;
            bool ok = await _inicializacao.ReativarAsync(item, maquina);
            btnReativarInit.Enabled = true;

            if (ok)
            {
                btnListarInit_Click(sender, e);
            }
            else
            {
                MessageBox.Show("Não foi possível reativar o item.",
                    "Inicialização", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        // Pega o item correspondente à linha selecionada na lista
        private ItemInicializacao? ObterItemInitSelecionado()
        {
            if (lstInicializacao.SelectedItems.Count == 0)
            {
                MessageBox.Show("Selecione um item da lista primeiro.",
                    "Inicialização", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return null;
            }

            int idx = lstInicializacao.SelectedIndices[0];
            if (idx < 0 || idx >= _itensInit.Count) return null;

            return _itensInit[idx];
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
            // Clique manual no botão: chama o motor central (modo manual)
            await VerificarStatusMaquinasAsync(automatico: false);
        }

        // ── Motor central da verificação de status ───────────────────────
        // Chamado tanto pelo botão (manual) quanto pelo timer (automático).
        private async Task VerificarStatusMaquinasAsync(bool automatico)
        {
            // Trava: nunca roda duas verificações ao mesmo tempo
            // (seja manual + automática, ou duas automáticas seguidas).
            if (_verificandoStatus) return;

            // Sem máquinas carregadas → nada a fazer.
            if (_listaMaquinas.Count == 0) return;

            // Se for automática, respeita condições extras pra não pesar:
            if (automatico)
            {
                if (!chkAutoRefresh.Checked) return;            // checkbox desligado
                if (tabMain.SelectedTab != tabMaquinas) return; // usuário em outra aba

                // Trava de recursos: se o sistema está rodando algo pesado
                // (limpeza, otimização, verificação de sistema, distribuição
                // ou busca de programas em massa), pula a vez e tenta depois.
                if (_sistemaOcupado)
                {
                    _log.Registrar("LOCAL", "Auto-refresh",
                        "Pulou a vez (sistema ocupado)", true, TipoLog.Info);
                    return;
                }
            }

            _verificandoStatus = true;
            btnVerificarStatus.Enabled = false;

            try
            {
                lblOnlineMaq.Text = "Verificando...";

                var progresso = new Progress<(int pct, string maq)>(p =>
                    lblOnlineMaq.Text = $"Verificando... {p.pct}%");

                await _maquinas.VerificarStatusAsync(_listaMaquinas, progresso);

                AtualizarListaMaquinas(_listaMaquinas);

                // Carimba a hora da última verificação no rodapé
                lblUltimaAtualizacao.Text =
                    $"Última atualização automática: {DateTime.Now:HH:mm}";
            }
            finally
            {
                // Libera a trava mesmo se der erro no meio
                _verificandoStatus = false;
                btnVerificarStatus.Enabled = true;
            }
        }

        // Liga/desliga o timer quando o usuário marca/desmarca o checkbox
        private void chkAutoRefresh_CheckedChanged(object? sender, EventArgs e)
        {
            if (chkAutoRefresh.Checked)
                _timerAutoRefresh.Start();
            else
                _timerAutoRefresh.Stop();
        }

        // Disparo automático do timer (a cada intervalo definido)
        private async void TimerAutoRefresh_Tick(object? sender, EventArgs e)
        {
            await VerificarStatusMaquinasAsync(automatico: true);
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
                item.SubItems.Add(m.IP);
                item.SubItems.Add(m.StatusFormatado);
                item.SubItems.Add(m.SistemaOp);
                item.SubItems.Add(m.UsuarioConectado);  // ← novo
                item.SubItems.Add(m.Modelo);
                item.SubItems.Add(m.ServiceTag);
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

        // ── CÓPIA DE DADOS (Ctrl+C e clique direito) ─────────────────────

        private void ConfigurarCopiaMaquinas()
        {
            // Ctrl+C copia a linha inteira
            lstMaquinas.KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.C)
                {
                    CopiarLinhaMaquina();
                    e.Handled = true;
                }
            };

            // Clique direito abre menu de cópia
            lstMaquinas.MouseClick += (s, e) =>
            {
                if (e.Button != MouseButtons.Right) return;

                var hit = lstMaquinas.HitTest(e.Location);
                if (hit.Item == null) return;

                hit.Item.Selected = true;
                MontarMenuCopia(hit).Show(lstMaquinas, e.Location);
            };
        }

        // Copia a linha inteira, colunas separadas por TAB (cola direto no Excel)
        private void CopiarLinhaMaquina()
        {
            if (lstMaquinas.SelectedItems.Count == 0) return;

            var subs = lstMaquinas.SelectedItems[0].SubItems
                .Cast<ListViewItem.ListViewSubItem>()
                .Select(s => s.Text);

            CopiarTexto(string.Join("\t", subs), "Linha copiada!");
        }

        // Monta o menu de contexto baseado na célula clicada
        // Monta o menu de contexto baseado na célula clicada
        private ContextMenuStrip MontarMenuCopia(ListViewHitTestInfo hit)
        {
            var menu = new ContextMenuStrip();

            // O chamador já garante que hit.Item não é nulo, mas verificamos
            // aqui também pra tranquilizar o compilador (e por segurança).
            var item = hit.Item;
            if (item == null) return menu;

            // Copiar a célula específica que foi clicada (se houver uma)
            var subItem = hit.SubItem;
            if (subItem != null)
            {
                int col = item.SubItems.IndexOf(subItem);
                if (col >= 0 && col < lstMaquinas.Columns.Count)
                {
                    string nomeCol = lstMaquinas.Columns[col].Text;
                    string valor = subItem.Text;

                    menu.Items.Add($"Copiar {nomeCol}", null,
                        (s, e) => CopiarTexto(valor, $"{nomeCol} copiado!"));
                    menu.Items.Add(new ToolStripSeparator());
                }
            }

            // Copiar a linha inteira
            menu.Items.Add("Copiar linha inteira", null,
                (s, e) => CopiarLinhaMaquina());

            return menu;
        }

        // Método único de cópia com feedback — evita repetir try/catch
        private void CopiarTexto(string texto, string feedback)
        {
            if (string.IsNullOrEmpty(texto)) return;
            try
            {
                Clipboard.SetText(texto);
                lblTotalMaq.Text = feedback;
            }
            catch { /* clipboard ocupado por outro app, ignora */ }
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
            _sistemaOcupado = bloquear;
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
                Font = UIHelper.FonteCardNome,
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
                Font = UIHelper.FontePequenaBold,
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
            _sistemaOcupado = bloquear;
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
            _sistemaOcupado = true;
            btnDistribuir.Enabled = false;
            lblTotalMaq.Text = "Distribuindo...";

            try
            {
                var progresso = new Progress<(int pct, string maq, bool ok)>(p =>
                {
                    lblTotalMaq.Text = $"Distribuindo... {p.pct}%";
                    _log.Registrar(p.maq, "Distribuição",
                        p.ok ? "✓ Copiado" : "✕ Falhou",
                        p.ok, TipoLog.Rede);
                });

                var resultado = await _distribuicao.DistribuirAsync(
                    maquinas, arquivos, progresso);

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
            catch (Exception ex)
            {
                _log.Registrar("LOCAL", "Distribuição",
                    $"Erro: {ex.Message}", false, TipoLog.Info);
                MessageBox.Show(
                    $"Ocorreu um erro durante a distribuição:\n\n{ex.Message}",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Sempre roda, com erro ou não: libera a trava e o botão
                _sistemaOcupado = false;
                btnDistribuir.Enabled = true;
                lblTotalMaq.Text = $"Total: {_listaMaquinas.Count}";
            }
        }

        // ── PROGRAMAS ────────────────────────────────────────────────────

        // Guarda a lista atual do modo "uma máquina" para o filtro funcionar
        private List<ProgramaInfo> _programasMaquina = new();

        // --- Alternância de modo ---

        private void btnModoBusca_Click(object sender, EventArgs e)
        {
            // Ativa modo busca em massa
            panelProgBusca.Visible = true;
            panelProgLista.Visible = false;

            // Destaca o botão ativo
            btnModoBusca.FillColor = UIHelper.CorAzulFundo;
            btnModoBusca.ForeColor = UIHelper.CorAzul;
            btnModoBusca.BorderColor = UIHelper.CorAzul;
            btnModoLista.FillColor = UIHelper.CorPainel;
            btnModoLista.ForeColor = UIHelper.CorTextoClaro;
            btnModoLista.BorderColor = UIHelper.CorBorda;

            // Ajusta colunas para o modo busca (Máquina primeiro)
            lstProgramas.Items.Clear();
            lstProgramas.Columns.Clear();
            lstProgramas.Columns.Add("Máquina", 200);
            lstProgramas.Columns.Add("Programa", 480);
            lstProgramas.Columns.Add("Versão", 200);
            lstProgramas.Columns.Add("Fabricante", 240);
            lblProgInfo.Text = "";
        }

        private void btnModoLista_Click(object sender, EventArgs e)
        {
            // Ativa modo lista de uma máquina
            panelProgBusca.Visible = false;
            panelProgLista.Visible = true;

            // Destaca o botão ativo
            btnModoLista.FillColor = UIHelper.CorAzulFundo;
            btnModoLista.ForeColor = UIHelper.CorAzul;
            btnModoLista.BorderColor = UIHelper.CorAzul;
            btnModoBusca.FillColor = UIHelper.CorPainel;
            btnModoBusca.ForeColor = UIHelper.CorTextoClaro;
            btnModoBusca.BorderColor = UIHelper.CorBorda;

            // Ajusta colunas para o modo lista (sem máquina, com data)
            lstProgramas.Items.Clear();
            lstProgramas.Columns.Clear();
            lstProgramas.Columns.Add("Programa", 460);
            lstProgramas.Columns.Add("Versão", 180);
            lstProgramas.Columns.Add("Fabricante", 300);
            lstProgramas.Columns.Add("Instalado", 180);
            lblProgInfo.Text = "";
        }

        // --- MODO 2: Listar programas de uma máquina ---

        private async void btnListarProg_Click(object sender, EventArgs e)
        {
            string? maquina = string.IsNullOrWhiteSpace(txtMaquinaProg.Text)
                ? null : txtMaquinaProg.Text.Trim();

            btnListarProg.Enabled = false;
            lblProgInfo.Text = "Lendo programas...";
            lstProgramas.Items.Clear();

            _programasMaquina = await _programas.ListarProgramasAsync(maquina);

            PreencherListaProgramasMaquina(_programasMaquina);

            lblProgInfo.Text =
                $"{_programasMaquina.Count} programas instalados" +
                (maquina != null ? $"  ·  {maquina}" : "  ·  este PC");

            btnListarProg.Enabled = true;
        }

        // Preenche a lista no modo "uma máquina"
        private void PreencherListaProgramasMaquina(List<ProgramaInfo> lista)
        {
            lstProgramas.BeginUpdate();
            lstProgramas.Items.Clear();

            foreach (var p in lista)
            {
                var item = new ListViewItem(p.Nome);
                item.SubItems.Add(p.Versao);
                item.SubItems.Add(p.Fabricante);
                item.SubItems.Add(p.DataFormatada);
                lstProgramas.Items.Add(item);
            }

            lstProgramas.EndUpdate();
        }

        // Filtro em tempo real do modo "uma máquina"
        private void txtFiltroProg_TextChanged(object sender, EventArgs e)
        {
            string termo = txtFiltroProg.Text.Trim();

            if (string.IsNullOrWhiteSpace(termo))
            {
                PreencherListaProgramasMaquina(_programasMaquina);
                return;
            }

            var filtrado = _programasMaquina
                .Where(p => p.Nome.Contains(
                    termo, StringComparison.OrdinalIgnoreCase) ||
                    p.Fabricante.Contains(
                    termo, StringComparison.OrdinalIgnoreCase))
                .ToList();

            PreencherListaProgramasMaquina(filtrado);
            lblProgInfo.Text = $"{filtrado.Count} de " +
                $"{_programasMaquina.Count} programas";
        }

        // --- MODO 1: Buscar programa em massa ---

        private async void btnBuscarPrograma_Click(object sender, EventArgs e)
        {
            string termo = txtBuscaPrograma.Text.Trim();

            if (string.IsNullOrWhiteSpace(termo))
            {
                MessageBox.Show("Digite o nome do programa para buscar.",
                    "Buscar programa", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Precisa ter a lista de máquinas carregada do AD
            if (_listaMaquinas == null || _listaMaquinas.Count == 0)
            {
                MessageBox.Show(
                    "Primeiro vá na aba Máquinas e clique em 'Buscar no AD' " +
                    "e 'Verificar Status' para carregar as máquinas online.",
                    "Buscar programa", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Pega só as máquinas online
            var online = _listaMaquinas
                .Where(m => m.Online)
                .Select(m => m.Nome)
                .ToList();

            if (online.Count == 0)
            {
                MessageBox.Show(
                    "Nenhuma máquina online encontrada. Clique em " +
                    "'Verificar Status' na aba Máquinas primeiro.",
                    "Buscar programa", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Confirmação (pode demorar)
            var confirma = MessageBox.Show(
                $"Buscar '{termo}' em {online.Count} máquinas online?\n\n" +
                "Isso pode levar alguns minutos.",
                "Confirmar busca", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirma != DialogResult.Yes) return;

            // Executa
            _sistemaOcupado = true;
            btnBuscarPrograma.Enabled = false;
            progressProg.Visible = true;
            progressProg.Value = 0;
            lstProgramas.Items.Clear();
            lblProgInfo.Text = "Iniciando busca...";

            try
            {
                var progresso = new Progress<(int pct, string maq, bool achou)>(p =>
                {
                    progressProg.Value = Math.Min(p.pct, 100);
                    lblProgInfo.Text = $"Verificando... {p.pct}%";
                });

                var encontrados = await _programas.BuscarProgramaEmMassaAsync(
                    online, termo, progresso);

                // Preenche resultados
                lstProgramas.BeginUpdate();
                lstProgramas.Items.Clear();
                foreach (var p in encontrados)
                {
                    var item = new ListViewItem(p.Maquina);
                    item.SubItems.Add(p.Nome);
                    item.SubItems.Add(p.Versao);
                    item.SubItems.Add(p.Fabricante);
                    lstProgramas.Items.Add(item);
                }
                lstProgramas.EndUpdate();

                lblProgInfo.Text = encontrados.Count > 0
                    ? $"✓ {encontrados.Count} máquina(s) com '{termo}'"
                    : $"Nenhuma máquina online tem '{termo}'";
            }
            catch (Exception ex)
            {
                _log.Registrar("LOCAL", "Busca de programas",
                    $"Erro: {ex.Message}", false, TipoLog.Info);
                lblProgInfo.Text = "Erro na busca.";
                MessageBox.Show(
                    $"Ocorreu um erro durante a busca:\n\n{ex.Message}",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Sempre roda, com erro ou não: esconde a barra,
                // libera a trava e o botão
                progressProg.Visible = false;
                _sistemaOcupado = false;
                btnBuscarPrograma.Enabled = true;
            }
        }
    }
}