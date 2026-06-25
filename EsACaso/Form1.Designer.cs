namespace EsACaso
{
    partial class Form1
    {
        /// <summary>
        /// Variabile di progettazione necessaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Pulire le risorse in uso.
        /// </summary>
        /// <param name="disposing">ha valore true se le risorse gestite devono essere eliminate, false in caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Codice generato da Progettazione Windows Form

        /// <summary>
        /// Metodo necessario per il supporto della finestra di progettazione. Non modificare
        /// il contenuto del metodo con l'editor di codice.
        /// </summary>
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
            this.SuspendLayout();
            // 
            // btnDebloat
            // 
            this.btnDebloat.Location = new System.Drawing.Point(12, 55);
            this.btnDebloat.Name = "btnDebloat";
            this.btnDebloat.Size = new System.Drawing.Size(776, 36);
            this.btnDebloat.TabIndex = 0;
            this.btnDebloat.Text = "DEBLOAT NOW";
            this.btnDebloat.UseVisualStyleBackColor = true;
            this.btnDebloat.Click += new System.EventHandler(this.btnDebloat_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Ravie", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(243, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(319, 30);
            this.label1.TabIndex = 1;
            this.label1.Text = "CHRIS TITUS DEBLOAT";
            // 
            // richTextBox1
            // 
            this.richTextBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBox1.Location = new System.Drawing.Point(12, 147);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(319, 144);
            this.richTextBox1.TabIndex = 2;
            this.richTextBox1.Text = "";
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Ravie", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(7, 113);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(303, 26);
            this.label2.TabIndex = 4;
            this.label2.Text = "Componenti di sistema";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Ravie", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(635, 113);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(153, 26);
            this.label3.TabIndex = 5;
            this.label3.Text = "Dispositivo";
            // 
            // rtbDispositivo
            // 
            this.rtbDispositivo.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rtbDispositivo.Location = new System.Drawing.Point(469, 147);
            this.rtbDispositivo.Name = "rtbDispositivo";
            this.rtbDispositivo.Size = new System.Drawing.Size(319, 144);
            this.rtbDispositivo.TabIndex = 6;
            this.rtbDispositivo.Text = "";
            // 
            // btnRiavvia
            // 
            this.btnRiavvia.Location = new System.Drawing.Point(12, 313);
            this.btnRiavvia.Name = "btnRiavvia";
            this.btnRiavvia.Size = new System.Drawing.Size(776, 36);
            this.btnRiavvia.TabIndex = 7;
            this.btnRiavvia.Text = "RIAVVIA IL PC ";
            this.btnRiavvia.UseVisualStyleBackColor = true;
            this.btnRiavvia.Click += new System.EventHandler(this.btnRiavvia_Click);
            // 
            // btnInfo
            // 
            this.btnInfo.Location = new System.Drawing.Point(760, 17);
            this.btnInfo.Name = "btnInfo";
            this.btnInfo.Size = new System.Drawing.Size(28, 23);
            this.btnInfo.TabIndex = 8;
            this.btnInfo.Text = "?";
            this.btnInfo.UseVisualStyleBackColor = true;
            this.btnInfo.Click += new System.EventHandler(this.btnInfo_Click);
            // 
            // btnImmagine
            // 
            this.btnImmagine.Location = new System.Drawing.Point(469, 255);
            this.btnImmagine.Name = "btnImmagine";
            this.btnImmagine.Size = new System.Drawing.Size(319, 36);
            this.btnImmagine.TabIndex = 9;
            this.btnImmagine.Text = "VISUALIZZA IMMAGINE SUL WEB";
            this.btnImmagine.UseVisualStyleBackColor = true;
            this.btnImmagine.Click += new System.EventHandler(this.btnImmagine_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 360);
            this.Controls.Add(this.btnImmagine);
            this.Controls.Add(this.btnInfo);
            this.Controls.Add(this.btnRiavvia);
            this.Controls.Add(this.rtbDispositivo);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnDebloat);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

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
    }
}

