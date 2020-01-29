namespace AdminConsole.Controls
{
    partial class SNMPElement
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

        #region Código generado por el Diseñador de componentes

        /// <summary> 
        /// Método necesario para admitir el Diseñador. No se puede modificar 
        /// el contenido del método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.LabelId = new System.Windows.Forms.Label();
            this.LabelIdValue = new System.Windows.Forms.Label();
            this.LabelIP = new System.Windows.Forms.Label();
            this.LabelPort = new System.Windows.Forms.Label();
            this.LabelIPValue = new System.Windows.Forms.Label();
            this.LabelPortValue = new System.Windows.Forms.Label();
            this.StatusPanel = new System.Windows.Forms.Panel();
            this.OrderRecievedList = new System.Windows.Forms.ListBox();
            this.ToggleButton = new System.Windows.Forms.Button();
            this.labelFrecuency = new System.Windows.Forms.Label();
            this.labelFrecuencyValue = new System.Windows.Forms.Label();
            this.SessionStatus = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.StatusPanel.SuspendLayout();
            this.SessionStatus.SuspendLayout();
            this.SuspendLayout();
            // 
            // LabelId
            // 
            this.LabelId.AutoSize = true;
            this.LabelId.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LabelId.Location = new System.Drawing.Point(15, 16);
            this.LabelId.Name = "LabelId";
            this.LabelId.Size = new System.Drawing.Size(29, 19);
            this.LabelId.TabIndex = 0;
            this.LabelId.Text = "Id:";
            // 
            // LabelIdValue
            // 
            this.LabelIdValue.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.LabelIdValue.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LabelIdValue.Location = new System.Drawing.Point(51, 17);
            this.LabelIdValue.Name = "LabelIdValue";
            this.LabelIdValue.Size = new System.Drawing.Size(151, 18);
            this.LabelIdValue.TabIndex = 1;
            this.LabelIdValue.Text = "Id";
            this.LabelIdValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // LabelIP
            // 
            this.LabelIP.AutoSize = true;
            this.LabelIP.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LabelIP.Location = new System.Drawing.Point(15, 35);
            this.LabelIP.Name = "LabelIP";
            this.LabelIP.Size = new System.Drawing.Size(30, 19);
            this.LabelIP.TabIndex = 2;
            this.LabelIP.Text = "IP:";
            // 
            // LabelPort
            // 
            this.LabelPort.AutoSize = true;
            this.LabelPort.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LabelPort.Location = new System.Drawing.Point(15, 54);
            this.LabelPort.Name = "LabelPort";
            this.LabelPort.Size = new System.Drawing.Size(47, 19);
            this.LabelPort.TabIndex = 3;
            this.LabelPort.Text = "Port:";
            // 
            // LabelIPValue
            // 
            this.LabelIPValue.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.LabelIPValue.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LabelIPValue.Location = new System.Drawing.Point(51, 36);
            this.LabelIPValue.Name = "LabelIPValue";
            this.LabelIPValue.Size = new System.Drawing.Size(151, 18);
            this.LabelIPValue.TabIndex = 4;
            this.LabelIPValue.Text = "IP";
            this.LabelIPValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // LabelPortValue
            // 
            this.LabelPortValue.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.LabelPortValue.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LabelPortValue.Location = new System.Drawing.Point(87, 55);
            this.LabelPortValue.Name = "LabelPortValue";
            this.LabelPortValue.Size = new System.Drawing.Size(115, 18);
            this.LabelPortValue.TabIndex = 5;
            this.LabelPortValue.Text = "Port";
            this.LabelPortValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // StatusPanel
            // 
            this.StatusPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.StatusPanel.Controls.Add(this.label2);
            this.StatusPanel.Location = new System.Drawing.Point(19, 238);
            this.StatusPanel.Name = "StatusPanel";
            this.StatusPanel.Size = new System.Drawing.Size(82, 18);
            this.StatusPanel.TabIndex = 7;
            // 
            // OrderRecievedList
            // 
            this.OrderRecievedList.FormattingEnabled = true;
            this.OrderRecievedList.Location = new System.Drawing.Point(19, 124);
            this.OrderRecievedList.Name = "OrderRecievedList";
            this.OrderRecievedList.ScrollAlwaysVisible = true;
            this.OrderRecievedList.Size = new System.Drawing.Size(180, 108);
            this.OrderRecievedList.TabIndex = 8;
            this.OrderRecievedList.DoubleClick += new System.EventHandler(this.OrderRecievedList_DoubleClick);
            // 
            // ToggleButton
            // 
            this.ToggleButton.Location = new System.Drawing.Point(19, 95);
            this.ToggleButton.Name = "ToggleButton";
            this.ToggleButton.Size = new System.Drawing.Size(180, 23);
            this.ToggleButton.TabIndex = 9;
            this.ToggleButton.Text = "button1";
            this.ToggleButton.UseVisualStyleBackColor = true;
            this.ToggleButton.Click += new System.EventHandler(this.ToggleButton_Click);
            // 
            // labelFrecuency
            // 
            this.labelFrecuency.AutoSize = true;
            this.labelFrecuency.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelFrecuency.Location = new System.Drawing.Point(15, 73);
            this.labelFrecuency.Name = "labelFrecuency";
            this.labelFrecuency.Size = new System.Drawing.Size(96, 19);
            this.labelFrecuency.TabIndex = 10;
            this.labelFrecuency.Text = "Frecuency:";
            // 
            // labelFrecuencyValue
            // 
            this.labelFrecuencyValue.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.labelFrecuencyValue.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelFrecuencyValue.Location = new System.Drawing.Point(117, 74);
            this.labelFrecuencyValue.Name = "labelFrecuencyValue";
            this.labelFrecuencyValue.Size = new System.Drawing.Size(85, 18);
            this.labelFrecuencyValue.TabIndex = 11;
            this.labelFrecuencyValue.Text = "Frecunecy";
            this.labelFrecuencyValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // SessionStatus
            // 
            this.SessionStatus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.SessionStatus.Controls.Add(this.label1);
            this.SessionStatus.Location = new System.Drawing.Point(117, 238);
            this.SessionStatus.Name = "SessionStatus";
            this.SessionStatus.Size = new System.Drawing.Size(82, 18);
            this.SessionStatus.TabIndex = 12;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(5, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(71, 19);
            this.label1.TabIndex = 1;
            this.label1.Text = "Session";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(8, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(58, 19);
            this.label2.TabIndex = 13;
            this.label2.Text = "Status";
            // 
            // SNMPElement
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.Controls.Add(this.SessionStatus);
            this.Controls.Add(this.labelFrecuencyValue);
            this.Controls.Add(this.labelFrecuency);
            this.Controls.Add(this.ToggleButton);
            this.Controls.Add(this.OrderRecievedList);
            this.Controls.Add(this.StatusPanel);
            this.Controls.Add(this.LabelPortValue);
            this.Controls.Add(this.LabelIPValue);
            this.Controls.Add(this.LabelPort);
            this.Controls.Add(this.LabelIP);
            this.Controls.Add(this.LabelIdValue);
            this.Controls.Add(this.LabelId);
            this.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Name = "SNMPElement";
            this.Size = new System.Drawing.Size(219, 267);
            this.StatusPanel.ResumeLayout(false);
            this.StatusPanel.PerformLayout();
            this.SessionStatus.ResumeLayout(false);
            this.SessionStatus.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label LabelId;
        private System.Windows.Forms.Label LabelIdValue;
        private System.Windows.Forms.Label LabelIP;
        private System.Windows.Forms.Label LabelPort;
        private System.Windows.Forms.Label LabelIPValue;
        private System.Windows.Forms.Label LabelPortValue;
        private System.Windows.Forms.Panel StatusPanel;
        private System.Windows.Forms.ListBox OrderRecievedList;
        private System.Windows.Forms.Button ToggleButton;
        private System.Windows.Forms.Label labelFrecuency;
        private System.Windows.Forms.Label labelFrecuencyValue;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel SessionStatus;
        private System.Windows.Forms.Label label1;
    }
}
