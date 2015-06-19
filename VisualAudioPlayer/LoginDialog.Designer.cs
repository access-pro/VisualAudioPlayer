namespace VisualAudioPlayer
{
    partial class LoginDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoginDialog));
            this.labelError = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.checkBoxHidePW = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.checkBoxSavePW = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxPassword = new System.Windows.Forms.TextBox();
            this.textBoxUserName = new System.Windows.Forms.TextBox();
            this.pictureBoxError = new System.Windows.Forms.PictureBox();
            this.labelServer = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.Cancel = new System.Windows.Forms.Button();
            this.ok = new System.Windows.Forms.Button();
            this.labelInfo = new System.Windows.Forms.Label();
            this.pictureBoxInfo = new System.Windows.Forms.PictureBox();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxError)).BeginInit();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxInfo)).BeginInit();
            this.SuspendLayout();
            // 
            // labelError
            // 
            this.labelError.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelError.Location = new System.Drawing.Point(39, 191);
            this.labelError.Name = "labelError";
            this.labelError.Size = new System.Drawing.Size(444, 39);
            this.labelError.TabIndex = 4;
            this.labelError.Text = "Keine Eingabe";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.SystemColors.InactiveCaptionText;
            this.label4.Location = new System.Drawing.Point(12, 14);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(302, 18);
            this.label4.TabIndex = 5;
            this.label4.Text = "Netzwerkkennwort eingeben für:";
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.checkBoxHidePW);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.checkBoxSavePW);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.textBoxPassword);
            this.panel1.Controls.Add(this.textBoxUserName);
            this.panel1.Location = new System.Drawing.Point(13, 57);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(470, 123);
            this.panel1.TabIndex = 9;
            // 
            // checkBoxHidePW
            // 
            this.checkBoxHidePW.AutoSize = true;
            this.checkBoxHidePW.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Bold);
            this.checkBoxHidePW.ForeColor = System.Drawing.SystemColors.InactiveCaptionText;
            this.checkBoxHidePW.Location = new System.Drawing.Point(341, 50);
            this.checkBoxHidePW.Name = "checkBoxHidePW";
            this.checkBoxHidePW.Size = new System.Drawing.Size(121, 22);
            this.checkBoxHidePW.TabIndex = 15;
            this.checkBoxHidePW.Text = "Verdecken";
            this.checkBoxHidePW.UseVisualStyleBackColor = true;
            this.checkBoxHidePW.CheckedChanged += new System.EventHandler(this.checkBoxHidePW_CheckedChanged);
            // 
            // label5
            // 
            this.label5.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label5.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.SystemColors.InactiveCaptionText;
            this.label5.Location = new System.Drawing.Point(13, 15);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(138, 27);
            this.label5.TabIndex = 14;
            this.label5.Text = "Benutzername";
            // 
            // checkBoxSavePW
            // 
            this.checkBoxSavePW.AutoSize = true;
            this.checkBoxSavePW.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Bold);
            this.checkBoxSavePW.ForeColor = System.Drawing.SystemColors.InactiveCaptionText;
            this.checkBoxSavePW.Location = new System.Drawing.Point(16, 87);
            this.checkBoxSavePW.Name = "checkBoxSavePW";
            this.checkBoxSavePW.Size = new System.Drawing.Size(337, 22);
            this.checkBoxSavePW.TabIndex = 13;
            this.checkBoxSavePW.Text = "Anmeldedaten dauerhaft speichern";
            this.checkBoxSavePW.UseVisualStyleBackColor = true;
            this.checkBoxSavePW.KeyDown += new System.Windows.Forms.KeyEventHandler(this.checkBoxSavePW_KeyDown);
            // 
            // label2
            // 
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.SystemColors.InactiveCaptionText;
            this.label2.Location = new System.Drawing.Point(13, 53);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(95, 27);
            this.label2.TabIndex = 12;
            this.label2.Text = "Kennwort";
            // 
            // textBoxPassword
            // 
            this.textBoxPassword.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxPassword.Location = new System.Drawing.Point(157, 47);
            this.textBoxPassword.Name = "textBoxPassword";
            this.textBoxPassword.Size = new System.Drawing.Size(179, 27);
            this.textBoxPassword.TabIndex = 10;
            this.textBoxPassword.UseSystemPasswordChar = true;
            this.textBoxPassword.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxPassword_KeyDown);
            this.textBoxPassword.KeyUp += new System.Windows.Forms.KeyEventHandler(this.textBoxPassword_KeyUp);
            // 
            // textBoxUserName
            // 
            this.textBoxUserName.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxUserName.Location = new System.Drawing.Point(157, 12);
            this.textBoxUserName.Name = "textBoxUserName";
            this.textBoxUserName.Size = new System.Drawing.Size(179, 27);
            this.textBoxUserName.TabIndex = 9;
            this.textBoxUserName.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxUserName_KeyDown);
            this.textBoxUserName.KeyUp += new System.Windows.Forms.KeyEventHandler(this.textBoxUserName_KeyUp);
            // 
            // pictureBoxError
            // 
            this.pictureBoxError.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBoxError.BackgroundImage")));
            this.pictureBoxError.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBoxError.Location = new System.Drawing.Point(15, 191);
            this.pictureBoxError.Name = "pictureBoxError";
            this.pictureBoxError.Size = new System.Drawing.Size(18, 18);
            this.pictureBoxError.TabIndex = 10;
            this.pictureBoxError.TabStop = false;
            // 
            // labelServer
            // 
            this.labelServer.AutoSize = true;
            this.labelServer.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelServer.ForeColor = System.Drawing.SystemColors.InactiveCaptionText;
            this.labelServer.Location = new System.Drawing.Point(12, 36);
            this.labelServer.Name = "labelServer";
            this.labelServer.Size = new System.Drawing.Size(434, 14);
            this.labelServer.TabIndex = 12;
            this.labelServer.Text = "Geben Sie das Kennwort ein, um eine Verbindung herzustellen mit: ";
            // 
            // panel2
            // 
            this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.panel2.BackColor = System.Drawing.SystemColors.Control;
            this.panel2.Controls.Add(this.Cancel);
            this.panel2.Controls.Add(this.ok);
            this.panel2.Location = new System.Drawing.Point(1, 222);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(499, 61);
            this.panel2.TabIndex = 15;
            // 
            // Cancel
            // 
            this.Cancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Cancel.Location = new System.Drawing.Point(373, 11);
            this.Cancel.Name = "Cancel";
            this.Cancel.Size = new System.Drawing.Size(102, 31);
            this.Cancel.TabIndex = 1;
            this.Cancel.Text = "Abbrechen";
            this.Cancel.UseVisualStyleBackColor = true;
            this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // ok
            // 
            this.ok.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ok.Location = new System.Drawing.Point(253, 11);
            this.ok.Name = "ok";
            this.ok.Size = new System.Drawing.Size(102, 31);
            this.ok.TabIndex = 0;
            this.ok.Text = "OK";
            this.ok.UseVisualStyleBackColor = true;
            this.ok.Click += new System.EventHandler(this.ok_Click);
            // 
            // labelInfo
            // 
            this.labelInfo.AutoSize = true;
            this.labelInfo.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelInfo.ForeColor = System.Drawing.SystemColors.InactiveCaptionText;
            this.labelInfo.Location = new System.Drawing.Point(39, 191);
            this.labelInfo.Name = "labelInfo";
            this.labelInfo.Size = new System.Drawing.Size(197, 18);
            this.labelInfo.TabIndex = 16;
            this.labelInfo.Text = "Baue Verbindung auf...";
            this.labelInfo.Visible = false;
            // 
            // pictureBoxInfo
            // 
            this.pictureBoxInfo.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBoxInfo.BackgroundImage")));
            this.pictureBoxInfo.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBoxInfo.Location = new System.Drawing.Point(15, 191);
            this.pictureBoxInfo.Name = "pictureBoxInfo";
            this.pictureBoxInfo.Size = new System.Drawing.Size(18, 18);
            this.pictureBoxInfo.TabIndex = 17;
            this.pictureBoxInfo.TabStop = false;
            this.pictureBoxInfo.Visible = false;
            // 
            // LoginDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ClientSize = new System.Drawing.Size(500, 283);
            this.Controls.Add(this.pictureBoxInfo);
            this.Controls.Add(this.labelInfo);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.labelServer);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.pictureBoxError);
            this.Controls.Add(this.labelError);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "LoginDialog";
            this.Opacity = 0.98D;
            this.Text = "VisualAudioPlayer Sicherheit";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.LoginDialog_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxError)).EndInit();
            this.panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxInfo)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelError;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.CheckBox checkBoxSavePW;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxPassword;
        private System.Windows.Forms.TextBox textBoxUserName;
        private System.Windows.Forms.PictureBox pictureBoxError;
        private System.Windows.Forms.CheckBox checkBoxHidePW;
        private System.Windows.Forms.Label labelServer;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button Cancel;
        private System.Windows.Forms.Button ok;
        private System.Windows.Forms.Label labelInfo;
        private System.Windows.Forms.PictureBox pictureBoxInfo;

    }
}