using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PEUtility
{
    public partial class MainWindow : Form
    {
        private Executable _executable;
        private const int NumRecentFiles = 10;
        private List<ListViewItem> _exportItems;

        public MainWindow(string argument)
        {
            InitializeComponent();

            DragEnter += MainWindow_DragEnter;
            DragDrop += MainWindow_DragDrop;

            importSearchBox.TextChanged += importSearchBox_TextChanged;
            exportSearchBox.TextChanged += exportSearchBox_TextChanged;

            importsList.MouseUp += importsList_MouseUp;
            exportsList.MouseUp += exportsList_MouseUp;

            ReadRecentFiles();
            if (argument != null)
            {
                OpenFile(argument);
            }
        }

        private void ReadRecentFiles()
        {
            var key = Registry.CurrentUser.CreateSubKey("Software\\WinDisasm");
            int i;
            for (i = 1; i <= NumRecentFiles; i++)
            {
                var value = key.GetValue("Recent" + i) as string;
                if (value != null)
                {
                    var item = new ToolStripMenuItem(value, null, ItemClick, value);
                    recentToolStripMenuItem.DropDownItems.Add(item);
                }
            }
            key.Close();

            recentToolStripMenuItem.Enabled = recentToolStripMenuItem.HasDropDownItems;
        }

        void ItemClick(object sender, EventArgs e)
        {
            OpenFile((sender as ToolStripItem).Name);
        }

        private void StoreRecentFile()
        {
            // Move current file to front
            int index = recentToolStripMenuItem.DropDownItems.IndexOfKey(_executable.Filename);
            ToolStripItem item;
            if (index != -1)
            {
                item = recentToolStripMenuItem.DropDownItems[index];
                recentToolStripMenuItem.DropDownItems.RemoveAt(index);
            }
            else
            {
                item = new ToolStripMenuItem(_executable.Filename, null, ItemClick, _executable.Filename);
            }
            recentToolStripMenuItem.DropDownItems.Insert(0, item);

            // Prune recent list
            while (recentToolStripMenuItem.DropDownItems.Count > NumRecentFiles)
            {
                recentToolStripMenuItem.DropDownItems.RemoveAt(NumRecentFiles);
            }

            // Rewrite registry entries
            using (var key = Registry.CurrentUser.CreateSubKey("Software\\WinDisasm"))
            {
                int i;
                for (i = 1; i <= NumRecentFiles; i++)
                {
                    key.DeleteValue("Recent" + i, false);
                }
                i = 1;
                foreach (var recent in recentToolStripMenuItem.DropDownItems)
                {
                    key.SetValue("Recent" + i, recent);
                    i++;
                }
            }
            
            recentToolStripMenuItem.Enabled = true;
        }

        void importSearchBox_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(importSearchBox.Text))
            {
                ShowAllImports();
                return;
            }

            importsList.Nodes.Clear();
            foreach (var importEntry in _executable.ImportEntries)
            {
                var node = importsList.Nodes.Add(importEntry.Name);
                bool hasMatch = false;
                foreach (var entry in importEntry.Entries)
                {
                    if (entry.IndexOf(importSearchBox.Text, StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        node.Nodes.Add(entry);
                        hasMatch = true;
                    }
                }
                if (hasMatch)
                {
                    node.ForeColor = Color.Black;
                    node.Expand();
                }
                else
                {
                    node.ForeColor = Color.Gray;
                }
            }
        }

        void exportSearchBox_TextChanged(object sender, EventArgs args)
        {
            string searchText = exportSearchBox.Text;
            exportsList.Clear();
            var entries = _exportItems
                .Where(e => e.Text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) != -1);
            exportsList.Items.AddRange(entries.ToArray());
        }

        void MainWindow_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            OpenFile(files[0]);
        }

        void MainWindow_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Move;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "All Executables (*.exe, *.dll)|*.exe;*.dll|EXE files (*.exe)|*.exe|DLL files (*.dll)|*.dll|All files (*.*)|*.*";
            dialog.ValidateNames = false;
            var result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                OpenFile(dialog.FileName);
            }
        }

        private void ShowAllImports()
        {
            importsList.Nodes.Clear();
            foreach (var importEntry in _executable.ImportEntries)
            {
                var node = importsList.Nodes.Add(importEntry.Name);
                foreach (var entry in importEntry.Entries)
                {
                    node.Nodes.Add(entry);
                }
            }
        }

        private void OpenFile(string filename)
        {
            if (_executable != null)
                _executable.Close();

            var newExecutable = new Executable(filename);
            if (!newExecutable.IsValid)
                return;

            _executable = newExecutable;
            Text = Path.GetFileName(filename) + " - PE Disassembler";

            StoreRecentFile();

            // Info
            typeValueLabel.Text = _executable.Type;

            // Imports
            ShowAllImports();
            importSearchBox.Enabled = true;

            // Exports
            exportsList.Clear();
            _exportItems = _executable.ExportEntries.Select(entry => {
                return new ListViewItem(entry.Name)
                {
                    Tag = entry
                };
                }).ToList();
            exportsList.Items.AddRange(_exportItems.ToArray());
            exportSearchBox.Enabled = true;
        }

        void importsList_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                importsList.SelectedNode = importsList.GetNodeAt(e.X, e.Y);

                if (importsList.SelectedNode != null)
                {
                    treeContextMenu.Tag = importsList.SelectedNode;
                    treeContextMenu.Show(importsList, e.Location);
                }
            }
        }

        void exportsList_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (exportsList.FocusedItem != null)
                {
                    treeContextMenu.Tag = exportsList.FocusedItem;
                    treeContextMenu.Show(exportsList, e.Location);

                    //var exportEntry = exportsList.FocusedItem.Tag as ExportEntry;
                    //treeContextMenu.Items.Add($"{exportEntry.Address:X}");
                }
            }
        }

        private void copyNameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeContextMenu.Tag is ListViewItem)
            {
                Clipboard.SetText((treeContextMenu.Tag as ListViewItem).Text);
            }
            else if (treeContextMenu.Tag is TreeNode)
            {
                Clipboard.SetText((treeContextMenu.Tag as TreeNode).Text);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_executable != null)
                _executable.Close();

            Close();
        }

        private void copyFunctionAddressMenuItem_Click(object sender, EventArgs e)
        {
            if (treeContextMenu.Tag is ListViewItem)
            {
                var listItem = treeContextMenu.Tag as ListViewItem;
                var exportEntry = listItem.Tag as ExportEntry;
                Clipboard.SetText($"{exportEntry.Address:X}");
            }
        }
    }
}
