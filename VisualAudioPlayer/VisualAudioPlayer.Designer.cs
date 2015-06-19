using System.Windows.Forms;
namespace VisualAudioPlayer
{
    partial class VisualAudioPlayer
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
                if (PlayTimer != null)
                    PlayTimer.Dispose();
                if (PauseTimer != null)
                    PauseTimer.Dispose();
                if (HelpTimer != null)
                    HelpTimer.Dispose();
                if (FadeTimer != null)
                    FadeTimer.Dispose();
                if (aPlayer != null)
                    aPlayer.DisposeSound();
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VisualAudioPlayer));
            this.coverImageList = new System.Windows.Forms.ImageList(this.components);
            this.albumsContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.MenuItemEditPlaylist = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemChooseCover = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemOpenFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemDeleteAlbum = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemAddFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemWatchDir = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemReload = new System.Windows.Forms.ToolStripMenuItem();
            this.labelAlbumTitle = new System.Windows.Forms.Label();
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.albumListView = new ListViewNF();
            this.albumsContextMenu.SuspendLayout();
            this.tableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // coverImageList
            // 
            this.coverImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            resources.ApplyResources(this.coverImageList, "coverImageList");
            this.coverImageList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // albumsContextMenu
            // 
            this.albumsContextMenu.BackColor = System.Drawing.SystemColors.ControlDark;
            this.albumsContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItemEditPlaylist,
            this.MenuItemChooseCover,
            this.MenuItemOpenFolder,
            this.MenuItemDeleteAlbum,
            this.MenuItemAddFolder,
            this.MenuItemWatchDir,
            this.MenuItemReload});
            this.albumsContextMenu.Name = "albumsContextMenuStrip";
            resources.ApplyResources(this.albumsContextMenu, "albumsContextMenu");
            this.albumsContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.albumsContextMenu_Opening);
            // 
            // MenuItemEditPlaylist
            // 
            this.MenuItemEditPlaylist.ForeColor = System.Drawing.SystemColors.ControlText;
            resources.ApplyResources(this.MenuItemEditPlaylist, "MenuItemEditPlaylist");
            this.MenuItemEditPlaylist.Name = "MenuItemEditPlaylist";
            this.MenuItemEditPlaylist.Click += new System.EventHandler(this.MenuItemEditPlaylist_Click);
            // 
            // MenuItemChooseCover
            // 
            resources.ApplyResources(this.MenuItemChooseCover, "MenuItemChooseCover");
            this.MenuItemChooseCover.Name = "MenuItemChooseCover";
            this.MenuItemChooseCover.Click += new System.EventHandler(this.MenuItemChooseCover_Click);
            // 
            // MenuItemOpenFolder
            // 
            resources.ApplyResources(this.MenuItemOpenFolder, "MenuItemOpenFolder");
            this.MenuItemOpenFolder.Name = "MenuItemOpenFolder";
            this.MenuItemOpenFolder.Click += new System.EventHandler(this.OpenFolderMenuItem_Click);
            // 
            // MenuItemDeleteAlbum
            // 
            resources.ApplyResources(this.MenuItemDeleteAlbum, "MenuItemDeleteAlbum");
            this.MenuItemDeleteAlbum.Name = "MenuItemDeleteAlbum";
            this.MenuItemDeleteAlbum.Click += new System.EventHandler(this.MenuItemDeleteAlbum_Click);
            // 
            // MenuItemAddFolder
            // 
            resources.ApplyResources(this.MenuItemAddFolder, "MenuItemAddFolder");
            this.MenuItemAddFolder.Name = "MenuItemAddFolder";
            this.MenuItemAddFolder.Click += new System.EventHandler(this.MenuItemAddFolder_Click);
            // 
            // MenuItemWatchDir
            // 
            resources.ApplyResources(this.MenuItemWatchDir, "MenuItemWatchDir");
            this.MenuItemWatchDir.Name = "MenuItemWatchDir";
            this.MenuItemWatchDir.Click += new System.EventHandler(this.MenuItemWatchDir_Click);
            // 
            // MenuItemReload
            // 
            resources.ApplyResources(this.MenuItemReload, "MenuItemReload");
            this.MenuItemReload.Name = "MenuItemReload";
            this.MenuItemReload.Click += new System.EventHandler(this.MenuItemReload_Click);
            // 
            // labelAlbumTitle
            // 
            resources.ApplyResources(this.labelAlbumTitle, "labelAlbumTitle");
            this.labelAlbumTitle.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.labelAlbumTitle.Name = "labelAlbumTitle";
            // 
            // tableLayoutPanel
            // 
            resources.ApplyResources(this.tableLayoutPanel, "tableLayoutPanel");
            this.tableLayoutPanel.Controls.Add(this.labelAlbumTitle, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.albumListView, 0, 1);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            // 
            // albumListView
            // 
            resources.ApplyResources(this.albumListView, "albumListView");
            this.albumListView.AllowDrop = true;
            this.albumListView.BackColor = System.Drawing.SystemColors.Desktop;
            this.albumListView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.albumListView.ContextMenuStrip = this.albumsContextMenu;
            this.albumListView.Cursor = System.Windows.Forms.Cursors.Hand;
            this.albumListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.albumListView.LargeImageList = this.coverImageList;
            this.albumListView.MultiSelect = false;
            this.albumListView.Name = "albumListView";
            this.albumListView.ShowGroups = false;
            this.albumListView.ShowItemToolTips = true;
            this.albumListView.TileSize = new System.Drawing.Size(200, 200);
            this.albumListView.UseCompatibleStateImageBehavior = false;
            this.albumListView.View = System.Windows.Forms.View.Tile;
            this.albumListView.DragDrop += new System.Windows.Forms.DragEventHandler(this.albumListView_DragDrop);
            this.albumListView.DragEnter += new System.Windows.Forms.DragEventHandler(this.albumListView_DragEnter);
            this.albumListView.KeyUp += new System.Windows.Forms.KeyEventHandler(this.albumListView_KeyUp);
            this.albumListView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.albumListView_MouseClick);
            // 
            // VisualAudioPlayer
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Desktop;
            this.Controls.Add(this.tableLayoutPanel);
            this.Name = "VisualAudioPlayer";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.VisualAudioPlayer_FormClosing);
            this.Shown += new System.EventHandler(this.VisualAudioPlayer_Shown);
            this.albumsContextMenu.ResumeLayout(false);
            this.tableLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ContextMenuStrip albumsContextMenu;
        private System.Windows.Forms.ImageList coverImageList;
        private System.Windows.Forms.ToolStripMenuItem MenuItemEditPlaylist;
        private System.Windows.Forms.ToolStripMenuItem MenuItemChooseCover;
        private System.Windows.Forms.ToolStripMenuItem MenuItemOpenFolder;
        private System.Windows.Forms.ToolStripMenuItem MenuItemDeleteAlbum;
        private System.Windows.Forms.ToolStripMenuItem MenuItemAddFolder;
        private System.Windows.Forms.ToolStripMenuItem MenuItemWatchDir;
        private ToolStripMenuItem MenuItemReload;
        public Label labelAlbumTitle;
        private TableLayoutPanel tableLayoutPanel;
        private ListViewNF albumListView;
    }
}

