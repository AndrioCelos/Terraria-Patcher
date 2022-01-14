namespace TerrariaPatcher;

partial class MainForm {
	/// <summary>
	/// Required designer variable.
	/// </summary>
	private System.ComponentModel.IContainer components = null;

	/// <summary>
	/// Clean up any resources being used.
	/// </summary>
	/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
	protected override void Dispose(bool disposing) {
		if (disposing && (components != null)) {
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	#region Windows Form Designer generated code

	/// <summary>
	/// Required method for Designer support - do not modify
	/// the contents of this method with the code editor.
	/// </summary>
	private void InitializeComponent() {
			this.patchList = new System.Windows.Forms.CheckedListBox();
			this.selectAllBox = new System.Windows.Forms.CheckBox();
			this.patchButton = new System.Windows.Forms.Button();
			this.panel1 = new System.Windows.Forms.Panel();
			this.statusLabel = new System.Windows.Forms.Label();
			this.progressBar = new System.Windows.Forms.ProgressBar();
			this.runButton = new System.Windows.Forms.Button();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.patchDescriptionBox = new System.Windows.Forms.TextBox();
			this.patchVersionBox = new System.Windows.Forms.TextBox();
			this.patchNameBox = new System.Windows.Forms.TextBox();
			this.backgroundWorker = new System.ComponentModel.BackgroundWorker();
			this.panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.SuspendLayout();
			// 
			// patchList
			// 
			this.patchList.Dock = System.Windows.Forms.DockStyle.Fill;
			this.patchList.FormattingEnabled = true;
			this.patchList.IntegralHeight = false;
			this.patchList.Location = new System.Drawing.Point(0, 0);
			this.patchList.Margin = new System.Windows.Forms.Padding(4);
			this.patchList.Name = "patchList";
			this.patchList.Size = new System.Drawing.Size(239, 198);
			this.patchList.Sorted = true;
			this.patchList.TabIndex = 0;
			this.patchList.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.patchList_ItemCheck);
			this.patchList.SelectedIndexChanged += new System.EventHandler(this.patchList_SelectedIndexChanged);
			this.patchList.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.patchList_Format);
			// 
			// selectAllBox
			// 
			this.selectAllBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.selectAllBox.AutoSize = true;
			this.selectAllBox.Location = new System.Drawing.Point(4, 8);
			this.selectAllBox.Margin = new System.Windows.Forms.Padding(4);
			this.selectAllBox.Name = "selectAllBox";
			this.selectAllBox.Size = new System.Drawing.Size(78, 21);
			this.selectAllBox.TabIndex = 1;
			this.selectAllBox.Text = "Select all";
			this.selectAllBox.UseVisualStyleBackColor = true;
			this.selectAllBox.CheckedChanged += new System.EventHandler(this.selectAllBox_CheckedChanged);
			// 
			// patchButton
			// 
			this.patchButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.patchButton.Location = new System.Drawing.Point(370, 2);
			this.patchButton.Margin = new System.Windows.Forms.Padding(4);
			this.patchButton.Name = "patchButton";
			this.patchButton.Size = new System.Drawing.Size(80, 30);
			this.patchButton.TabIndex = 2;
			this.patchButton.Text = "&Patch";
			this.patchButton.UseVisualStyleBackColor = true;
			this.patchButton.Click += new System.EventHandler(this.patchButton_Click);
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.statusLabel);
			this.panel1.Controls.Add(this.progressBar);
			this.panel1.Controls.Add(this.runButton);
			this.panel1.Controls.Add(this.patchButton);
			this.panel1.Controls.Add(this.selectAllBox);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panel1.Location = new System.Drawing.Point(0, 198);
			this.panel1.Margin = new System.Windows.Forms.Padding(4);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(454, 79);
			this.panel1.TabIndex = 3;
			// 
			// statusLabel
			// 
			this.statusLabel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.statusLabel.AutoSize = true;
			this.statusLabel.Location = new System.Drawing.Point(3, 33);
			this.statusLabel.Name = "statusLabel";
			this.statusLabel.Size = new System.Drawing.Size(43, 17);
			this.statusLabel.TabIndex = 3;
			this.statusLabel.Text = "label1";
			this.statusLabel.Visible = false;
			// 
			// progressBar
			// 
			this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.progressBar.Location = new System.Drawing.Point(6, 53);
			this.progressBar.Name = "progressBar";
			this.progressBar.Size = new System.Drawing.Size(445, 23);
			this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
			this.progressBar.TabIndex = 4;
			// 
			// runButton
			// 
			this.runButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.runButton.Location = new System.Drawing.Point(282, 2);
			this.runButton.Margin = new System.Windows.Forms.Padding(4);
			this.runButton.Name = "runButton";
			this.runButton.Size = new System.Drawing.Size(80, 30);
			this.runButton.TabIndex = 2;
			this.runButton.Text = "&Run";
			this.runButton.UseVisualStyleBackColor = true;
			this.runButton.Click += new System.EventHandler(this.runButton_Click);
			// 
			// splitContainer1
			// 
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.Location = new System.Drawing.Point(0, 0);
			this.splitContainer1.Margin = new System.Windows.Forms.Padding(4);
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.patchList);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.patchDescriptionBox);
			this.splitContainer1.Panel2.Controls.Add(this.patchVersionBox);
			this.splitContainer1.Panel2.Controls.Add(this.patchNameBox);
			this.splitContainer1.Size = new System.Drawing.Size(454, 198);
			this.splitContainer1.SplitterDistance = 239;
			this.splitContainer1.SplitterWidth = 5;
			this.splitContainer1.TabIndex = 4;
			// 
			// patchDescriptionBox
			// 
			this.patchDescriptionBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.patchDescriptionBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.patchDescriptionBox.Location = new System.Drawing.Point(3, 46);
			this.patchDescriptionBox.Multiline = true;
			this.patchDescriptionBox.Name = "patchDescriptionBox";
			this.patchDescriptionBox.ReadOnly = true;
			this.patchDescriptionBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.patchDescriptionBox.Size = new System.Drawing.Size(194, 145);
			this.patchDescriptionBox.TabIndex = 1;
			this.patchDescriptionBox.Text = "Patch description";
			// 
			// patchVersionBox
			// 
			this.patchVersionBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.patchVersionBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.patchVersionBox.Location = new System.Drawing.Point(3, 27);
			this.patchVersionBox.Multiline = true;
			this.patchVersionBox.Name = "patchVersionBox";
			this.patchVersionBox.ReadOnly = true;
			this.patchVersionBox.Size = new System.Drawing.Size(194, 22);
			this.patchVersionBox.TabIndex = 1;
			this.patchVersionBox.Text = "Patch version";
			// 
			// patchNameBox
			// 
			this.patchNameBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.patchNameBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.patchNameBox.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.patchNameBox.Location = new System.Drawing.Point(3, 3);
			this.patchNameBox.Name = "patchNameBox";
			this.patchNameBox.ReadOnly = true;
			this.patchNameBox.Size = new System.Drawing.Size(194, 18);
			this.patchNameBox.TabIndex = 1;
			this.patchNameBox.Text = "Patch name";
			// 
			// backgroundWorker
			// 
			this.backgroundWorker.WorkerReportsProgress = true;
			this.backgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker_DoWork);
			this.backgroundWorker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker_ProgressChanged);
			this.backgroundWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker_RunWorkerCompleted);
			// 
			// MainForm
			// 
			this.AcceptButton = this.patchButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(454, 277);
			this.Controls.Add(this.splitContainer1);
			this.Controls.Add(this.panel1);
			this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "MainForm";
			this.Text = "Terraria Patcher";
			this.Load += new System.EventHandler(this.MainForm_Load);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			this.splitContainer1.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.ResumeLayout(false);

	}

	#endregion

	private System.Windows.Forms.CheckedListBox patchList;
	private System.Windows.Forms.CheckBox selectAllBox;
	private System.Windows.Forms.Button patchButton;
	private System.Windows.Forms.Panel panel1;
	private System.Windows.Forms.SplitContainer splitContainer1;
	private System.Windows.Forms.TextBox patchDescriptionBox;
	private System.Windows.Forms.TextBox patchNameBox;
	private System.Windows.Forms.Label statusLabel;
	private System.Windows.Forms.ProgressBar progressBar;
	private System.ComponentModel.BackgroundWorker backgroundWorker;
	private System.Windows.Forms.TextBox patchVersionBox;
	private System.Windows.Forms.Button runButton;
}
