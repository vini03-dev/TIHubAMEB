using Guna.UI2.WinForms;

namespace TIHubAMEB.Helpers
{
    /// <summary>
    /// Paleta de cores, fontes e utilitários visuais do TIHub AMEB.
    /// Tema escuro azulado — flat, leve e profissional (v1.1).
    /// Redesign: 100% chapado (sem sombras), cantos suaves e
    /// paleta refinada para melhor harmonização visual.
    /// </summary>
    public static class UIHelper
    {
        // ── Raios de canto padronizados (v1.1) ────────────────────────────
        // Centralizados para manter harmonia: cards maiores recebem cantos
        // um pouco mais suaves; elementos menores recebem cantos menores.
        public const int RaioCard = 10;
        public const int RaioPainel = 10;
        public const int RaioBotao = 8;
        public const int RaioInput = 8;
        public const int RaioBarra = 6;

        // ── Paleta de cores ───────────────────────────────────────────────
        // Base do tema dark azulado, levemente refinada para mais contraste
        // e profundidade entre as camadas (fundo → painel → card).

        public static readonly Color CorFundo = Color.FromArgb(13, 17, 23);
        public static readonly Color CorPainel = Color.FromArgb(22, 27, 34);
        public static readonly Color CorCard = Color.FromArgb(22, 27, 34);
        public static readonly Color CorCardAlt = Color.FromArgb(28, 33, 40);
        public static readonly Color CorBorda = Color.FromArgb(30, 42, 58);
        public static readonly Color CorBordaSuave = Color.FromArgb(25, 32, 42);
        public static readonly Color CorHover = Color.FromArgb(28, 36, 48);
        public static readonly Color CorTexto = Color.FromArgb(201, 209, 217);
        public static readonly Color CorTextoClaro = Color.FromArgb(139, 148, 158);
        public static readonly Color CorTextoEsc = Color.FromArgb(74, 106, 138);
        public static readonly Color CorAzul = Color.FromArgb(77, 158, 240);
        public static readonly Color CorAzulEsc = Color.FromArgb(31, 111, 235);
        public static readonly Color CorAzulFundo = Color.FromArgb(21, 38, 62);
        public static readonly Color CorVerde = Color.FromArgb(63, 185, 80);
        public static readonly Color CorVerdeFundo = Color.FromArgb(10, 42, 22);
        public static readonly Color CorAmarelo = Color.FromArgb(210, 153, 34);
        public static readonly Color CorAmareloFundo = Color.FromArgb(42, 35, 5);
        public static readonly Color CorVermelho = Color.FromArgb(248, 81, 73);
        public static readonly Color CorVermelhoFundo = Color.FromArgb(40, 18, 18);
        public static readonly Color CorRoxo = Color.FromArgb(129, 140, 248);
        public static readonly Color CorRoxoFundo = Color.FromArgb(20, 20, 58);
        public static readonly Color CorCiano = Color.FromArgb(34, 211, 238);
        public static readonly Color CorCianoFundo = Color.FromArgb(5, 42, 48);

        // ── Fontes ────────────────────────────────────────────────────────

        public static readonly Font FonteTitulo = new("Segoe UI", 13f, FontStyle.Bold);
        public static readonly Font FonteSubtit = new("Segoe UI", 11f, FontStyle.Bold);
        public static readonly Font FonteNormal = new("Segoe UI", 9.5f);
        public static readonly Font FontePequena = new("Segoe UI", 8.5f);
        public static readonly Font FontePequenaBold = new("Segoe UI", 8.5f, FontStyle.Bold);
        public static readonly Font FonteCardNome = new("Segoe UI", 10.5f, FontStyle.Bold);
        public static readonly Font FonteMono = new("Consolas", 9f);
        public static readonly Font FonteMetrica = new("Segoe UI Semibold", 26f, FontStyle.Bold);

        // ── Cor por percentual ────────────────────────────────────────────

        public static Color CorPorPercentual(float pct) => pct switch
        {
            < 60 => CorVerde,
            < 85 => CorAmarelo,
            _ => CorVermelho
        };

        public static Color CorFundoPorPercentual(float pct) => pct switch
        {
            < 60 => CorVerdeFundo,
            < 85 => CorAmareloFundo,
            _ => CorVermelhoFundo
        };

        // ── Estilizações Guna 2 ───────────────────────────────────────────
        // Todas com ShadowDecoration.Enabled = false (flat total).

        public static void EstilizarCard(Guna2Panel panel, int borderRadius = RaioCard)
        {
            panel.FillColor = CorCard;
            panel.BorderRadius = borderRadius;
            panel.BorderColor = CorBorda;
            panel.BorderThickness = 1;
            panel.ShadowDecoration.Enabled = false;
        }

        public static void EstilizarPainel(Guna2Panel panel, int borderRadius = RaioPainel)
        {
            panel.FillColor = CorPainel;
            panel.BorderRadius = borderRadius;
            panel.BorderColor = CorBorda;
            panel.BorderThickness = 1;
            panel.ShadowDecoration.Enabled = false;
        }

        public static void EstilizarBotaoPrimario(Guna2Button btn,
            int borderRadius = RaioBotao)
        {
            btn.FillColor = CorAzulFundo;
            btn.ForeColor = CorAzul;
            btn.BorderColor = CorAzul;
            btn.BorderThickness = 1;
            btn.BorderRadius = borderRadius;
            btn.Font = FonteNormal;
            btn.Cursor = Cursors.Hand;
            btn.ShadowDecoration.Enabled = false;
            btn.HoverState.FillColor = Color.FromArgb(26, 52, 84);
            btn.HoverState.BorderColor = CorAzul;
        }

        public static void EstilizarBotaoSucesso(Guna2Button btn,
            int borderRadius = RaioBotao)
        {
            btn.FillColor = CorVerdeFundo;
            btn.ForeColor = CorVerde;
            btn.BorderColor = CorVerde;
            btn.BorderThickness = 1;
            btn.BorderRadius = borderRadius;
            btn.Font = FonteNormal;
            btn.Cursor = Cursors.Hand;
            btn.ShadowDecoration.Enabled = false;
            btn.HoverState.FillColor = Color.FromArgb(14, 56, 30);
            btn.HoverState.BorderColor = CorVerde;
        }

        public static void EstilizarBotaoAviso(Guna2Button btn,
            int borderRadius = RaioBotao)
        {
            btn.FillColor = CorAmareloFundo;
            btn.ForeColor = CorAmarelo;
            btn.BorderColor = CorAmarelo;
            btn.BorderThickness = 1;
            btn.BorderRadius = borderRadius;
            btn.Font = FonteNormal;
            btn.Cursor = Cursors.Hand;
            btn.ShadowDecoration.Enabled = false;
            btn.HoverState.FillColor = Color.FromArgb(56, 47, 8);
            btn.HoverState.BorderColor = CorAmarelo;
        }

        public static void EstilizarBotaoPerigo(Guna2Button btn,
            int borderRadius = RaioBotao)
        {
            btn.FillColor = CorVermelhoFundo;
            btn.ForeColor = CorVermelho;
            btn.BorderColor = CorVermelho;
            btn.BorderThickness = 1;
            btn.BorderRadius = borderRadius;
            btn.Font = FonteNormal;
            btn.Cursor = Cursors.Hand;
            btn.ShadowDecoration.Enabled = false;
            btn.HoverState.FillColor = Color.FromArgb(56, 24, 24);
            btn.HoverState.BorderColor = CorVermelho;
        }

        public static void EstilizarTextBox(Guna2TextBox txt)
        {
            txt.FillColor = CorFundo;
            txt.ForeColor = CorTexto;
            txt.BorderColor = CorBorda;
            txt.FocusedState.BorderColor = CorAzul;
            txt.HoverState.BorderColor = CorTextoEsc;
            txt.PlaceholderForeColor = CorTextoEsc;
            txt.BorderRadius = RaioInput;
            txt.BorderThickness = 1;
            txt.Font = FonteNormal;
        }

        public static void EstilizarRichTextBox(RichTextBox rtb)
        {
            rtb.BackColor = Color.FromArgb(2, 6, 23);
            rtb.ForeColor = CorTexto;
            rtb.BorderStyle = BorderStyle.None;
            rtb.Font = FonteMono;
            rtb.ReadOnly = true;
        }

        public static void EstilizarLabelSecao(Label lbl)
        {
            lbl.Font = FontePequena;
            lbl.ForeColor = CorTextoClaro;
            lbl.BackColor = Color.Transparent;
        }

        public static void EstilizarLabelMetrica(Label lbl)
        {
            lbl.Font = FonteMetrica;
            lbl.ForeColor = CorVerde;
            lbl.BackColor = Color.Transparent;
        }

        public static void EstilizarProgressBar(Guna2ProgressBar pb,
            Color? cor = null)
        {
            pb.FillColor = Color.FromArgb(30, 38, 50);
            pb.ProgressColor = cor ?? CorAzul;
            pb.BorderRadius = RaioBarra;
            pb.BackColor = Color.Transparent;
            pb.ProgressColor2 = cor ?? CorAzul;
        }

        public static void AtualizarProgressBar(Guna2ProgressBar pb,
            float valor)
        {
            int v = (int)Math.Clamp(valor, 0, 100);
            pb.Value = v;
            Color cor = CorPorPercentual(valor);
            pb.ProgressColor = cor;
            pb.ProgressColor2 = cor;
        }

        public static void EstilizarTabControl(Guna2TabControl tab)
        {
            tab.ForeColor = CorTextoClaro;
            tab.Font = FonteNormal;
        }

        public static Label CriarLabel(string texto, int x, int y,
            int w, int h, Color? cor = null,
            float size = 9.5f, bool bold = false)
        {
            return new Label
            {
                Text = texto,
                Location = new Point(x, y),
                Size = new Size(w, h),
                ForeColor = cor ?? CorTexto,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", size,
                    bold ? FontStyle.Bold : FontStyle.Regular)
            };
        }

        public static Panel CriarSeparador(int x, int y, int w)
        {
            return new Panel
            {
                Location = new Point(x, y),
                Size = new Size(w, 1),
                BackColor = CorBorda
            };
        }
    }
}