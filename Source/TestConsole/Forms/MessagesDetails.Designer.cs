﻿namespace AdminConsole.Forms
{
    partial class MessagesDetails
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.OrderRecievedList = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // OrderRecievedList
            // 
            this.OrderRecievedList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.OrderRecievedList.FormattingEnabled = true;
            this.OrderRecievedList.Location = new System.Drawing.Point(0, 0);
            this.OrderRecievedList.Name = "OrderRecievedList";
            this.OrderRecievedList.ScrollAlwaysVisible = true;
            this.OrderRecievedList.Size = new System.Drawing.Size(707, 511);
            this.OrderRecievedList.TabIndex = 11;
            this.OrderRecievedList.DoubleClick += new System.EventHandler(this.OrderRecievedList_DoubleClick);
            // 
            // MessagesDetails
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(707, 511);
            this.Controls.Add(this.OrderRecievedList);
            this.Name = "MessagesDetails";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Detalle de los Mensajes";
            this.TopMost = true;
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox OrderRecievedList;
    }
}