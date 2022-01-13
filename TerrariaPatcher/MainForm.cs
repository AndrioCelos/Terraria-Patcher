#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TerrariaPatcher;
public partial class MainForm : Form {
	private bool updatingSelectAll;

	public MainForm(IEnumerable<PatchSet> patchSets) {
		this.InitializeComponent();
		foreach (var patchSet in patchSets) {
			this.patchList.Items.Add(patchSet, true);
		}
	}

	private void patchList_Format(object sender, ListControlConvertEventArgs e) {
		if (e.ListItem is PatchSet patchSet) {
			e.Value = patchSet.Name ?? patchSet.GetType().Name;
		}
	}

	private void patchList_SelectedValueChanged(object sender, EventArgs e) {
	}

	private void patchList_SelectedIndexChanged(object sender, EventArgs e) {
		if (this.patchList.SelectedItem is PatchSet patchSet) {
			this.patchNameBox.Text = patchSet.Name ?? patchSet.Name;
			this.patchVersionBox.Text = patchSet.Version.ToString();
			this.patchDescriptionBox.Text = patchSet.Description ?? "";
		}
	}

	private void button1_Click(object sender, EventArgs e) {
		this.okButton.Enabled = false;
		this.progressBar.Value = 0;
		this.progressBar.Maximum = this.patchList.CheckedItems.Cast<PatchSet>().Sum(p => p.Patches.Count + 1);
		this.statusLabel.Visible = true;
		this.backgroundWorker.RunWorkerAsync(this.patchList.CheckedItems.Cast<PatchSet>());
	}

	private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
		var n = 0;
		this.backgroundWorker.ReportProgress(n, $"Preparing...");

		foreach (var patchSet in (IEnumerable<PatchSet>) e.Argument) {
			this.backgroundWorker.ReportProgress(n, $"Applying {patchSet.Name}...");
			patchSet.Apply((n2, s) => this.backgroundWorker.ReportProgress(n + 1 + n2, $"{patchSet.Name}/{s}"));
			n += patchSet.Patches.Count + 1;
		}
		foreach (var targetModule in Program.TargetModules) {
			this.backgroundWorker.ReportProgress(-1, $"Writing {targetModule}...");
			targetModule.Write();
		}
	}

	private void selectAllBox_CheckedChanged(object sender, EventArgs e) {
		if (this.updatingSelectAll) return;
		this.updatingSelectAll = true;
		if (this.selectAllBox.CheckState != CheckState.Indeterminate) {
			for (var i = 0; i < this.patchList.Items.Count; i++)
				this.patchList.SetItemChecked(i, this.selectAllBox.Checked);
		}
		this.updatingSelectAll = false;
	}

	private void patchList_ItemCheck(object sender, ItemCheckEventArgs e) {
		if (!this.okButton.Enabled) {
			e.NewValue = e.CurrentValue;
			return;
		}
		if (this.updatingSelectAll || e.Index < 0 || e.Index >= this.patchList.Items.Count) return;
		this.updatingSelectAll = true;
		try {
			if (e.NewValue == CheckState.Unchecked) {
				var patches = Enumerable.Range(0, this.patchList.Items.Count)
				.Where(i => i != e.Index && this.patchList.GetItemChecked(i) && (((PatchSet) this.patchList.Items[i]).Dependencies?.Contains(((PatchSet) this.patchList.Items[e.Index]).GetType()) ?? false))
				.Select(i => (i, ((PatchSet) this.patchList.Items[i]).Name));
				if (patches.Any()) {
					if (MessageBox.Show(this, $"Deselecting this patch will also deselect the following dependent patches.\n\n{string.Join("\n", patches.Select(p => p.Name))}", "Terraria Patcher", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK) {
						foreach (var en in patches) {
							this.patchList.SetItemCheckState(en.i, CheckState.Unchecked);
						}
					} else {
						e.NewValue = e.CurrentValue;
						return;
					}
				}
				for (var i = 0; i < this.patchList.Items.Count; i++) {
					if (i != e.Index && this.patchList.GetItemChecked(i)) {
						this.selectAllBox.CheckState = CheckState.Indeterminate;
						return;
					}
				}
				this.selectAllBox.CheckState = CheckState.Unchecked;
			} else {
				var dependencies = ((PatchSet) this.patchList.Items[e.Index]).Dependencies;
				if (dependencies is not null) {
					var patches = Enumerable.Range(0, this.patchList.Items.Count)
					.Where(i => !this.patchList.GetItemChecked(i) && dependencies.Contains(((PatchSet) this.patchList.Items[i]).GetType()))
					.Select(i => (i, ((PatchSet) this.patchList.Items[i]).Name));
					if (patches.Any()) {
						if (MessageBox.Show(this, $"Selecting this patch will also select the following dependency patches.\n\n{string.Join("\n", patches.Select(p => p.Name))}", "Terraria Patcher", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK) {
							foreach (var en in patches) {
								this.patchList.SetItemCheckState(en.i, CheckState.Checked);
							}
						} else {
							e.NewValue = e.CurrentValue;
							return;
						}
					}
				}
				for (var i = 0; i < this.patchList.Items.Count; i++) {
					if (i != e.Index && !this.patchList.GetItemChecked(i)) {
						this.selectAllBox.CheckState = CheckState.Indeterminate;
						return;
					}
				}
				this.selectAllBox.CheckState = CheckState.Checked;
			}
		} finally {
			this.updatingSelectAll = false;
		}
	}

	private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
		//this.okButton.Enabled = true;
		this.statusLabel.Text = "Complete";
		this.progressBar.Style = ProgressBarStyle.Continuous;
		this.progressBar.Value = this.progressBar.Maximum;
	}

	private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
		this.statusLabel.Text = e.UserState.ToString();
		if (e.ProgressPercentage < 0)
			this.progressBar.Style = ProgressBarStyle.Marquee;
		else {
			this.progressBar.Style = ProgressBarStyle.Continuous;
			this.progressBar.Value = e.ProgressPercentage;
		}
	}

	private void button1_Click_1(object sender, EventArgs e) {
		var path = Program.TargetModules.Select(t => t.OutputPath).First(p => Path.GetExtension(p) == ".exe");
		Process.Start(new ProcessStartInfo(path) { WorkingDirectory = Path.GetDirectoryName(path) });
	}
}
