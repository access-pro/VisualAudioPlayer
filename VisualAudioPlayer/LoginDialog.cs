using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace VisualAudioPlayer
{
    public partial class LoginDialog : Form
    {
        public string UserName { get; set; }
        public string ServerName { get; set; }
        public string Password { get; set; }
        public bool Connected { get; set; }
        public bool Canceled { get; private set; }
        private const int FORM_HIGHT_1_LINE = 316;
        private const int FORM_HIGHT_2_LINE = 334;
        private const int FORM_HIGHT_3_LINE = 378;
        public LoginDialog(string sServerName)
        {
            InitializeComponent();
            Canceled = Connected = false;
            checkBoxHidePW.CheckState = CheckState.Checked;
            // textBoxUserName.Text = getDefaultUserName(sServerName);
            if (sServerName.StartsWith("\\\\"))
                sServerName = FileUtil.GetServerName(sServerName);
            ServerName = sServerName;
            labelServer.Text = "Geben Sie das Kennwort ein, um eine Verbindung herzustellen mit: " + ServerName;
        }
        private void Login()
        {
            UpdateMsg();
            if (!IsValidInput()) // 
                return;
            Cursor.Current = Cursors.WaitCursor;
            labelError.Visible = false;
            pictureBoxError.Visible = false;
            labelInfo.Visible = true;
            pictureBoxInfo.Visible = true;
            Application.DoEvents();
            Password = textBoxPassword.Text;
            UserName = textBoxUserName.Text;

            WinNet.Error ec = WinNet.AddConnection("\\\\" + ServerName, UserName, Password); // Validate login credentials
            if (ec.num == WinNet.NO_ERROR) // login succeeded
            {
                Connected = true;
                if (checkBoxSavePW.CheckState == CheckState.Checked)
                {
                    string sEnryptedPassword = StringIO.Enrypt(Password); // Encrypt Password
                    AlbumDB albumDB = new AlbumDB();
                    albumDB.OpenConnection();
                    albumDB.AddCredentials(ServerName, UserName, sEnryptedPassword); // save credentials in db
                    albumDB.CloseConnection();
                    albumDB.Dispose();
                }
                this.Close();
                Cursor.Current = Cursors.Default;
                return;
            }
            else // Login failed
            {
                labelInfo.Visible = false;
                pictureBoxInfo.Visible = false;
                labelError.Visible = true;
                pictureBoxError.Visible = true;
                switch (ec.num)
                {
                    case WinNet.ERROR_BAD_USERNAME:
                        labelError.Text = "Falscher Benutzername";
                        break;
                    case WinNet.ERROR_INVALID_PASSWORD:
                        labelError.Text = "Falsches Passwort";
                        break;
                    default:
                        labelError.Text = ec.message;
                        break;
                }
                this.Height = FORM_HIGHT_2_LINE;
            }
            Cursor.Current = Cursors.Default;
        }
        private void textBoxUserName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
            {
                Login();
                return;
            }
        }
        private void textBoxPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
            {
                Login();
                return;
            }
        }

        private void textBoxUserName_KeyUp(object sender, KeyEventArgs e)
        {
            UpdateMsg();
        }
        private void textBoxPassword_KeyUp(object sender, KeyEventArgs e)
        {
            UpdateMsg();
        }
        private void checkBoxHidePW_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxHidePW.CheckState == CheckState.Checked) // 
                textBoxPassword.UseSystemPasswordChar = true;    // hide Password
            else
                textBoxPassword.UseSystemPasswordChar = false;   // show Password
        }
        private bool IsValidInput()
        {
            if (textBoxUserName.Text.Length == 0)
            {
                MessageBox.Show(new Form() { TopMost = true }, "Bitte geben Sie einen Benutzernamen ein.",
                    "Kein Benutzername", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
            if (textBoxPassword.Text.Length == 0)
            {
                MessageBox.Show(new Form() { TopMost = true }, "Bitte geben Sie ein Passwort ein.",
                    "Kein Passwort", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
            return true;
        }
        private void UpdateMsg()
        {
            if (textBoxPassword.Text.Length == 0 && textBoxUserName.Text.Length == 0) // 
            {
                labelError.Text = "Keine Eingabe";
            }
            else
            {
                if (textBoxUserName.Text.Length == 0) // 
                {
                    labelError.Text = "Kein Benutzername";
                }
                else if (textBoxPassword.Text.Length == 0)
                {
                    labelError.Text = "Kein Kennwort";
                }
                else
                {
                    labelInfo.Visible = true;
                    pictureBoxInfo.Visible = true;
                    labelError.Visible = false;
                    pictureBoxError.Visible = false;
                    labelInfo.Text = "Mit Eingabetaste oder OK bestätigen.";
                }
            }
            this.Height = FORM_HIGHT_1_LINE;
        }
        private void LoginDialog_Load(object sender, EventArgs e)
        {
            UpdateMsg();
        }
        private void ok_Click(object sender, EventArgs e)
        {
            Login();
        }
        private void checkBoxSavePW_KeyDown(object sender, KeyEventArgs e)
        {
            Login();
        }
        private void Cancel_Click(object sender, EventArgs e)
        {
            Canceled = true;
            this.Close();
        }

    }
}
