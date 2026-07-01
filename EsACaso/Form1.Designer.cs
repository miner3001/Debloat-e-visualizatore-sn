namespace EsACaso
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.btnDebloat = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.rtbDispositivo = new System.Windows.Forms.RichTextBox();
            this.btnRiavvia = new System.Windows.Forms.Button();
            this.btnInfo = new System.Windows.Forms.Button();
            this.btnImmagine = new System.Windows.Forms.Button();
            this.btnReport = new System.Windows.Forms.Button();
            this.btnWinget = new System.Windows.Forms.Button();
            this.btnManutenzione = new System.Windows.Forms.Button();
            this.btnStartup = new System.Windows.Forms.Button();
            this.btnRete = new System.Windows.Forms.Button();
            this.panelTop = new System.Windows.Forms.Panel();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.panelTop.SuspendLayout();
            this.panelBottom.SuspendLayout();
            this.SuspendLayout();

            // ── PALETTE ────────────────────────────────────────────────
            var bg = System.Drawing.Color.FromArgb(20, 20, 20);
            var bgDeep = System.Drawing.Color.FromArgb(15, 15, 15);
            var border = System.Drawing.Color.FromArgb(42, 42, 42);
            var textMain = System.Drawing.Color.FromArgb(204, 204, 204);
            var textMuted = System.Drawing.Color.FromArgb(100, 100, 100);
            var accent = System.Drawing.Color.FromArgb(229, 57, 53);
            var accentBg = System.Drawing.Color.FromArgb(26, 10, 10);
            var green = System.Drawing.Color.FromArgb(46, 125, 50);
            var greenBg = System.Drawing.Color.FromArgb(10, 22, 10);

            // ── panelTop ───────────────────────────────────────────────
            this.panelTop.BackColor = bgDeep;
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Height = 56;
            this.panelTop.Controls.Add(this.label1);
            this.panelTop.Controls.Add(this.btnInfo);

            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(20, 17);
            this.label1.Text = "// EsACaso — System Info";

            this.btnInfo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInfo.FlatAppearance.BorderColor = border;
            this.btnInfo.FlatAppearance.BorderSize = 1;
            this.btnInfo.BackColor = bgDeep;
            this.btnInfo.ForeColor = textMuted;
            this.btnInfo.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnInfo.Location = new System.Drawing.Point(858, 14);
            this.btnInfo.Size = new System.Drawing.Size(28, 28);
            this.btnInfo.Text = "?";
            this.btnInfo.TabIndex = 8;
            this.btnInfo.Click += new System.EventHandler(this.btnInfo_Click);

            // ── Labels ─────────────────────────────────────────────────
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.label2.ForeColor = textMuted;
            this.label2.Location = new System.Drawing.Point(145, 70);
            this.label2.Text = "COMPONENTI DI SISTEMA";

            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.label3.ForeColor = textMuted;
            this.label3.Location = new System.Drawing.Point(530, 70);
            this.label3.Text = "DISPOSITIVO";

            // ── richTextBox1 ───────────────────────────────────────────
            this.richTextBox1.BackColor = bgDeep;
            this.richTextBox1.ForeColor = textMain;
            this.richTextBox1.Font = new System.Drawing.Font("Consolas", 9.5F);
            this.richTextBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.richTextBox1.Location = new System.Drawing.Point(150, 92);
            this.richTextBox1.Size = new System.Drawing.Size(300, 160);
            this.richTextBox1.TabIndex = 2;
            this.richTextBox1.Text = "";

            // ── rtbDispositivo ─────────────────────────────────────────
            this.rtbDispositivo.BackColor = bgDeep;
            this.rtbDispositivo.ForeColor = textMain;
            this.rtbDispositivo.Font = new System.Drawing.Font("Consolas", 9.5F);
            this.rtbDispositivo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.rtbDispositivo.Location = new System.Drawing.Point(530, 92);
            this.rtbDispositivo.Size = new System.Drawing.Size(360, 160);
            this.rtbDispositivo.TabIndex = 6;
            this.rtbDispositivo.Text = "";

            // ── panelBottom ────────────────────────────────────────────
            // 4 righe × 48px + padding = 210px
            this.panelBottom.BackColor = bgDeep;
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Height = 214;
            this.panelBottom.Controls.Add(this.btnDebloat);
            this.panelBottom.Controls.Add(this.btnImmagine);
            this.panelBottom.Controls.Add(this.btnReport);
            this.panelBottom.Controls.Add(this.btnWinget);
            this.panelBottom.Controls.Add(this.btnManutenzione);
            this.panelBottom.Controls.Add(this.btnStartup);
            this.panelBottom.Controls.Add(this.btnRete);
            this.panelBottom.Controls.Add(this.btnRiavvia);

            System.Action<System.Windows.Forms.Button> styleBtn = (b) =>
            {
                b.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                b.FlatAppearance.BorderColor = border;
                b.FlatAppearance.BorderSize = 1;
                b.BackColor = bg;
                b.ForeColor = System.Drawing.Color.White;
                b.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            };

            // riga 1 — Debloat | Supporto Web
            styleBtn(this.btnDebloat);
            this.btnDebloat.Location = new System.Drawing.Point(20, 14);
            this.btnDebloat.Size = new System.Drawing.Size(430, 38);
            this.btnDebloat.TabIndex = 0;
            this.btnDebloat.Text = "⚙   DEBLOAT NOW";
            this.btnDebloat.Click += new System.EventHandler(this.btnDebloat_Click);

            styleBtn(this.btnImmagine);
            this.btnImmagine.Location = new System.Drawing.Point(460, 14);
            this.btnImmagine.Size = new System.Drawing.Size(430, 38);
            this.btnImmagine.TabIndex = 9;
            this.btnImmagine.Text = "🌐   SUPPORTO WEB";
            this.btnImmagine.Click += new System.EventHandler(this.btnImmagine_Click);

            // riga 2 — Report | Winget
            styleBtn(this.btnReport);
            this.btnReport.Location = new System.Drawing.Point(20, 62);
            this.btnReport.Size = new System.Drawing.Size(430, 38);
            this.btnReport.TabIndex = 10;
            this.btnReport.Text = "📋   REPORT COMPONENTI";
            this.btnReport.Click += new System.EventHandler(this.btnReport_Click);

            this.btnWinget.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnWinget.FlatAppearance.BorderColor = green;
            this.btnWinget.FlatAppearance.BorderSize = 1;
            this.btnWinget.BackColor = greenBg;
            this.btnWinget.ForeColor = System.Drawing.Color.FromArgb(102, 187, 106);
            this.btnWinget.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnWinget.Location = new System.Drawing.Point(460, 62);
            this.btnWinget.Size = new System.Drawing.Size(430, 38);
            this.btnWinget.TabIndex = 11;
            this.btnWinget.Text = "↑   AGGIORNA TUTTO (winget)";
            this.btnWinget.Click += new System.EventHandler(this.btnWinget_Click);

            // riga 3 — Manutenzione | Startup | Rete
            styleBtn(this.btnManutenzione);
            this.btnManutenzione.Location = new System.Drawing.Point(20, 110);
            this.btnManutenzione.Size = new System.Drawing.Size(274, 38);
            this.btnManutenzione.TabIndex = 12;
            this.btnManutenzione.Text = "🔧   MANUTENZIONE";
            this.btnManutenzione.Click += new System.EventHandler(this.btnManutenzione_Click);

            styleBtn(this.btnStartup);
            this.btnStartup.Location = new System.Drawing.Point(304, 110);
            this.btnStartup.Size = new System.Drawing.Size(274, 38);
            this.btnStartup.TabIndex = 13;
            this.btnStartup.Text = "🚀   STARTUP MGR";
            this.btnStartup.Click += new System.EventHandler(this.btnStartup_Click);

            styleBtn(this.btnRete);
            this.btnRete.Location = new System.Drawing.Point(588, 110);
            this.btnRete.Size = new System.Drawing.Size(302, 38);
            this.btnRete.TabIndex = 14;
            this.btnRete.Text = "🌐   RETE";
            this.btnRete.Click += new System.EventHandler(this.btnRete_Click);

            // riga 4 — Riavvia centrato
            this.btnRiavvia.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRiavvia.FlatAppearance.BorderColor = accent;
            this.btnRiavvia.FlatAppearance.BorderSize = 1;
            this.btnRiavvia.BackColor = accentBg;
            this.btnRiavvia.ForeColor = System.Drawing.Color.FromArgb(239, 83, 80);
            this.btnRiavvia.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnRiavvia.Location = new System.Drawing.Point(215, 158);
            this.btnRiavvia.Size = new System.Drawing.Size(480, 38);
            this.btnRiavvia.TabIndex = 7;
            this.btnRiavvia.Text = "↺   RIAVVIA IL PC";
            this.btnRiavvia.Click += new System.EventHandler(this.btnRiavvia_Click);

            // ── Form1 ──────────────────────────────────────────────────
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = bg;
            this.ClientSize = new System.Drawing.Size(1000, 610);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = true;
            this.Text = "EsACaso v1.0";
            this.Controls.Add(this.label2);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.rtbDispositivo);
            this.Controls.Add(this.panelTop);
            this.Controls.Add(this.panelBottom);

            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.panelBottom.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Button btnDebloat;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.RichTextBox rtbDispositivo;
        private System.Windows.Forms.Button btnRiavvia;
        private System.Windows.Forms.Button btnInfo;
        private System.Windows.Forms.Button btnImmagine;
        private System.Windows.Forms.Button btnReport;
        private System.Windows.Forms.Button btnWinget;
        private System.Windows.Forms.Button btnManutenzione;
        private System.Windows.Forms.Button btnStartup;
        private System.Windows.Forms.Button btnRete;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Panel panelBottom;
    }
}