namespace AdminConsole
{
    partial class TestUDP
    {
        /// <summary>
        /// Variable del diseñador requerida.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén utilizando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben eliminar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido del método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.test1Button = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.test1Ip = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.test1Port = new System.Windows.Forms.TextBox();
            this.test1Frecuency = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // test1Button
            // 
            this.test1Button.Location = new System.Drawing.Point(196, 128);
            this.test1Button.Name = "test1Button";
            this.test1Button.Size = new System.Drawing.Size(75, 23);
            this.test1Button.TabIndex = 0;
            this.test1Button.Text = "Send";
            this.test1Button.UseVisualStyleBackColor = true;
            this.test1Button.Click += new System.EventHandler(this.test1Button_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(16, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(30, 19);
            this.label1.TabIndex = 1;
            this.label1.Text = "IP:";
            // 
            // test1Ip
            // 
            this.test1Ip.Location = new System.Drawing.Point(117, 25);
            this.test1Ip.Name = "test1Ip";
            this.test1Ip.Size = new System.Drawing.Size(154, 20);
            this.test1Ip.TabIndex = 2;
            this.test1Ip.Text = "127.0.0.1";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(16, 58);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(47, 19);
            this.label2.TabIndex = 3;
            this.label2.Text = "Port:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(16, 93);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(96, 19);
            this.label3.TabIndex = 4;
            this.label3.Text = "Frecuency:";
            // 
            // test1Port
            // 
            this.test1Port.Location = new System.Drawing.Point(117, 57);
            this.test1Port.Name = "test1Port";
            this.test1Port.Size = new System.Drawing.Size(154, 20);
            this.test1Port.TabIndex = 5;
            this.test1Port.Text = "160";
            // 
            // test1Frecuency
            // 
            this.test1Frecuency.Location = new System.Drawing.Point(117, 92);
            this.test1Frecuency.Name = "test1Frecuency";
            this.test1Frecuency.Size = new System.Drawing.Size(154, 20);
            this.test1Frecuency.TabIndex = 6;
            this.test1Frecuency.Text = "119.000";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.test1Frecuency);
            this.groupBox1.Controls.Add(this.test1Button);
            this.groupBox1.Controls.Add(this.test1Port);
            this.groupBox1.Controls.Add(this.test1Ip);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(286, 161);
            this.groupBox1.TabIndex = 7;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Simple UPD Send";
            // 
            // TestUPD
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(682, 326);
            this.Controls.Add(this.groupBox1);
            this.Name = "TestUPD";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Title";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button test1Button;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox test1Ip;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox test1Port;
        private System.Windows.Forms.TextBox test1Frecuency;
        private System.Windows.Forms.GroupBox groupBox1;


    }
}

