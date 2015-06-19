using System;
using System.Windows.Forms;

namespace VisualAudioPlayer
{
    class TrackListForm : Form
    {
        public Label labelAlbumTitle;
        public ListView TrackList;
        private bool bListViewInit = false;

        public TrackListForm(string strAlbumTitle)
        {
            InitializeComponent();
            this.labelAlbumTitle.Text = strAlbumTitle;
            this.PerformAutoScale();
        }
        private void InitializeComponent()
        {
            this.labelAlbumTitle = new System.Windows.Forms.Label();
            this.TrackList = new System.Windows.Forms.ListView();
            this.SuspendLayout();
            // 
            // labelAlbumTitle
            // 
            this.labelAlbumTitle.AutoSize = true;
            this.labelAlbumTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelAlbumTitle.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelAlbumTitle.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.labelAlbumTitle.Location = new System.Drawing.Point(3, 3);
            this.labelAlbumTitle.Margin = new System.Windows.Forms.Padding(0);
            this.labelAlbumTitle.Name = "labelAlbumTitle";
            this.labelAlbumTitle.Padding = new System.Windows.Forms.Padding(0, 0, 9, 0);
            this.labelAlbumTitle.Size = new System.Drawing.Size(54, 19);
            this.labelAlbumTitle.TabIndex = 0;
            this.labelAlbumTitle.Text = "label";
            // 
            // TrackList
            // 
            this.TrackList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TrackList.BackColor = System.Drawing.SystemColors.Desktop;
            this.TrackList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.TrackList.CheckBoxes = true;
            this.TrackList.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TrackList.ForeColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.TrackList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.TrackList.Location = new System.Drawing.Point(6, 28);
            this.TrackList.Margin = new System.Windows.Forms.Padding(3, 3, 6, 3);
            this.TrackList.MultiSelect = false;
            this.TrackList.Name = "TrackList";
            this.TrackList.ShowGroups = false;
            this.TrackList.Size = new System.Drawing.Size(278, 31);
            this.TrackList.TabIndex = 1;
            this.TrackList.UseCompatibleStateImageBehavior = false;
            this.TrackList.View = System.Windows.Forms.View.List;
            this.TrackList.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.TrackList_ItemCheck);
            this.TrackList.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.TrackList_ItemChecked);
            this.TrackList.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TrackList_KeyDown);
            // 
            // TrackListForm
            // 
            this.BackColor = System.Drawing.SystemColors.Desktop;
            this.ClientSize = new System.Drawing.Size(284, 62);
            this.Controls.Add(this.TrackList);
            this.Controls.Add(this.labelAlbumTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TrackListForm";
            this.Opacity = 0.98D;
            this.Padding = new System.Windows.Forms.Padding(3);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.TrackListForm_FormClosing);
            this.Load += new System.EventHandler(this.TrackListForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        private void TrackListForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            AlbumDB albumDB = new AlbumDB();
            albumDB.OpenConnection();
            Int32 iFirstChangedTrackID = 0;
            foreach (ListViewItem lvItem in TrackList.Items)
            {
                string tag = (string)lvItem.Tag;
                if (tag == "IsDirty")
                {
                    if (iFirstChangedTrackID == 0 && (bool)lvItem.Checked)
                        iFirstChangedTrackID = Convert.ToInt32(lvItem.Name);
                    albumDB.dbUpdateTrackEnabled(lvItem.Name, lvItem.Checked);
                }
                if (iFirstChangedTrackID != 0)
                    albumDB.dbSetMinLastTrackPlayed(iFirstChangedTrackID);
            }
            albumDB.CloseConnection();
            albumDB.Dispose();
        }
        private void TrackList_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (bListViewInit)
                TrackList.Items[e.Index].Tag = "IsDirty";
        }
        private void TrackList_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            //if (bListViewInit)
            //    TrackList.SelectedItems[0].Tag = "IsDirty";
        }
        private void TrackListForm_Load(object sender, EventArgs e)
        {
            bListViewInit = true;
        }
        private void TrackList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                Clipboard.SetText(this.TrackList.SelectedItems[0].Text);
                e.SuppressKeyPress = true;
            }
        }
    }
}
