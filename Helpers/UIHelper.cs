using Guna.UI2.WinForms;

namespace TIHubAMEB.Helpers
{
    /// <summary>
    /// Paleta de cores, fontes e utilitários visuais do TIHub AMEB.
    /// Tema escuro azulado — chapado, leve e profissional.
    /// </summary>
    public static class UIHelper
    {
        // ── Paleta de cores ───────────────────────────────────────────────

        public static readonly Color CorFundo = Color.FromArgb(13, 17, 23);
        public static readonly Color CorPainel = Color.FromArgb(22, 27, 34);
        public static readonly Color CorCard = Color.FromArgb(30, 38, 50);
        public static readonly Color CorBorda = Color.FromArgb(48, 54, 61);
        public static readonly Color CorHover = Color.FromArgb(33, 41, 54);
        public static readonly Color CorTexto = Color.FromArgb(201, 209, 217);
        public static readonly Color CorTextoClaro = Color.FromArgb(139, 148, 158);
        public static readonly Color CorTextoEsc = Color.FromArgb(72, 84, 96);
        public static readonly Color CorAzul = Color.FromArgb(77, 158, 240);
        public static readonly Color CorAzulEsc = Color.FromArgb(31, 111, 235);
        public static readonly Color CorAzulFundo = Color.FromArgb(21, 38, 62);
        public static readonly Color CorVerde = Color.FromArgb(63, 185, 80);
        public static readonly Color CorVerdeFundo = Color.FromArgb(10, 42, 22);
        public static readonly Color CorAmarelo = Color.FromArgb(210, 153, 34);
        public static readonly Color CorAmareloFundo = Color.FromArgb(42, 35, 5);
        public static readonly Color CorVermelho = Color.FromArgb(248, 81, 73);
        public static readonly Color CorVermelhoFundo = Color.FromArgb(28, 5, 5);
        public static readonly Color CorRoxo = Color.FromArgb(129, 140, 248);
        public static readonly Color CorRoxoFundo = Color.FromArgb(20, 20, 58);
        public static readonly Color CorCiano = Color.FromArgb(34, 211, 238);
        public static readonly Color CorCianoFundo = Color.FromArgb(5, 42, 48);

        // ── Fontes ────────────────────────────────────────────────────────

        public static readonly Font FonteTitulo = new("Segoe UI", 13f, FontStyle.Bold);
        public static readonly Font FonteSubtit = new("Segoe UI", 11f, FontStyle.Bold);
        public static readonly Font FonteNormal = new("Segoe UI", 9.5f);
        public static readonly Font FontePequena = new("Segoe UI", 8.5f);
        public static readonly Font FonteMono = new("Consolas", 9f);
        public static readonly Font FonteMetrica = new("Segoe UI", 26f, FontStyle.Bold);

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

        public static void EstilizarCard(Guna2Panel panel, int borderRadius = 8)
        {
            panel.FillColor = CorCard;
            panel.BorderRadius = borderRadius;
            panel.BorderColor = CorBorda;
            panel.BorderThickness = 1;
            panel.ShadowDecoration.Enabled = false;
        }

        public static void EstilizarPainel(Guna2Panel panel, int borderRadius = 6)
        {
            panel.FillColor = CorPainel;
            panel.BorderRadius = borderRadius;
            panel.BorderColor = CorBorda;
            panel.BorderThickness = 1;
            panel.ShadowDecoration.Enabled = false;
        }

        public static void EstilizarBotaoPrimario(Guna2Button btn,
            int borderRadius = 6)
        {
            btn.FillColor = CorAzulFundo;
            btn.ForeColor = CorAzul;
            btn.BorderColor = CorAzul;
            btn.BorderThickness = 1;
            btn.BorderRadius = borderRadius;
            btn.Font = FonteNormal;
            btn.Cursor = Cursors.Hand;
            btn.HoverState.FillColor = Color.FromArgb(26, 52, 84);
        }

        public static void EstilizarBotaoSucesso(Guna2Button btn,
            int borderRadius = 6)
        {
            btn.FillColor = CorVerdeFundo;
            btn.ForeColor = CorVerde;
            btn.BorderColor = CorVerde;
            btn.BorderThickness = 1;
            btn.BorderRadius = borderRadius;
            btn.Font = FonteNormal;
            btn.Cursor = Cursors.Hand;
        }

        public static void EstilizarBotaoAviso(Guna2Button btn,
            int borderRadius = 6)
        {
            btn.FillColor = CorAmareloFundo;
            btn.ForeColor = CorAmarelo;
            btn.BorderColor = CorAmarelo;
            btn.BorderThickness = 1;
            btn.BorderRadius = borderRadius;
            btn.Font = FonteNormal;
            btn.Cursor = Cursors.Hand;
        }

        public static void EstilizarBotaoPerigo(Guna2Button btn,
            int borderRadius = 6)
        {
            btn.FillColor = CorVermelhoFundo;
            btn.ForeColor = CorVermelho;
            btn.BorderColor = CorVermelho;
            btn.BorderThickness = 1;
            btn.BorderRadius = borderRadius;
            btn.Font = FonteNormal;
            btn.Cursor = Cursors.Hand;
        }

        public static void EstilizarTextBox(Guna2TextBox txt)
        {
            txt.FillColor = CorFundo;
            txt.ForeColor = CorTexto;
            txt.BorderColor = CorBorda;
            txt.FocusedState.BorderColor = CorAzul;
            txt.PlaceholderForeColor = CorTextoEsc;
            txt.BorderRadius = 6;
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
            pb.FillColor = Color.FromArgb(33, 38, 45);
            pb.ProgressColor = cor ?? CorAzul;
            pb.BorderRadius = 4;
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