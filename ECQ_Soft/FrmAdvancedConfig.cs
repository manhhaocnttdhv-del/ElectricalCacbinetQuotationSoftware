using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using ECQ_Soft.Model;

namespace ECQ_Soft
{
    public partial class FrmAdvancedConfig : Form
    {
        private List<HierarchyNode> _rootNodes = new List<HierarchyNode>();
        private SheetsService _service;
        private string _spreadsheetId;
        
        public string SelectedHeader { get; private set; }
        public List<string> SelectedComponents { get; private set; } = new List<string>();

        public FrmAdvancedConfig()
        {
            InitializeComponent();
            SetupEvents();
        }

        public async Task LoadDataAsync(SheetsService service, string spreadsheetId)
        {
            _service = service;
            _spreadsheetId = spreadsheetId;
            
            try
            {
                var response = await _service.Spreadsheets.Values.Get(_spreadsheetId, "Workflow!A2:F").ExecuteAsync();
                var values = response.Values;
                if (values == null || values.Count <= 1) return;

                BuildTreeFromRows(values);
                LoadInitialLevel();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu Workflow: " + ex.Message);
            }
        }

        private void BuildTreeFromRows(IList<IList<object>> rows)
        {
            _rootNodes.Clear();
            var allNodes = new Dictionary<string, HierarchyNode>();
            var dataRows = rows.Skip(1).ToList();

            foreach (var row in dataRows)
            {
                string stt = row.Count > 0 ? row[0]?.ToString()?.Trim() : "";
                string name = row.Count > 1 ? row[1]?.ToString()?.Trim() : "";
                if (!string.IsNullOrEmpty(stt) && !string.IsNullOrEmpty(name))
                    allNodes[stt] = new HierarchyNode(name);
            }

            foreach (var row in dataRows)
            {
                string stt = row.Count > 0 ? row[0]?.ToString()?.Trim() : "";
                string name = row.Count > 1 ? row[1]?.ToString()?.Trim() : "";
                string idMeRaw = row.Count > 3 ? row[3]?.ToString()?.Trim() : "0";
                string[] idMes = idMeRaw.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (idMes.Length == 0) idMes = new[] { "0" };

                if (!string.IsNullOrEmpty(stt) && !string.IsNullOrEmpty(name))
                {
                    var node = allNodes[stt];
                    foreach (var idMe in idMes) {
                        if (idMe == "0") { if (!_rootNodes.Contains(node)) _rootNodes.Add(node); }
                        else if (allNodes.ContainsKey(idMe)) {
                            var parent = allNodes[idMe];
                            if (!parent.Children.Contains(node)) parent.Children.Add(node);
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(name) && string.IsNullOrEmpty(stt))
                {
                    foreach (var idMe in idMes) {
                        if (allNodes.ContainsKey(idMe)) allNodes[idMe].Components.Add(name);
                    }
                }
                string compColE = row.Count > 4 ? row[4]?.ToString()?.Trim() : "";
                if (!string.IsNullOrEmpty(compColE) && !compColE.StartsWith("=")) {
                    foreach (var idMe in idMes) {
                        if (allNodes.ContainsKey(idMe)) {
                            if (!allNodes[idMe].Components.Contains(compColE)) allNodes[idMe].Components.Add(compColE);
                        }
                    }
                }
            }
        }

        private void SetupEvents()
        {
            btnApply.Click += (s, e) => {
                // Lấy node từ combo cuối cùng có dữ liệu
                var panels = pnlStepsContainer.Controls.OfType<Panel>().Where(p => p.Tag is int).OrderBy(p => (int)p.Tag).ToList();
                HierarchyNode finalNode = null;
                foreach (var p in panels)
                {
                    var cbo = p.Controls.OfType<ComboBox>().FirstOrDefault();
                    if (cbo != null && cbo.SelectedItem is HierarchyNode node)
                    {
                        finalNode = node;
                    }
                }

                if (finalNode != null)
                {
                    SelectedHeader = finalNode.Name;
                    SelectedComponents = new List<string>(finalNode.Components);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            };
            btnCancel.Click += (s, e) => this.Close();
        }

        private void LoadInitialLevel()
        {
            pnlStepsContainer.Controls.Clear();
            AddStep(_rootNodes, 0);
        }

        private void AddStep(List<HierarchyNode> nodes, int level)
        {
            if (nodes == null || nodes.Count == 0) return;

            // Nếu không phải level 0, thêm dấu mũi tên
            if (level > 0)
            {
                Label lblArrow = new Label();
                lblArrow.Text = "➔";
                lblArrow.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
                lblArrow.ForeColor = System.Drawing.Color.Gray;
                lblArrow.AutoSize = true;
                lblArrow.Tag = level; // Tag level để dễ xóa
                lblArrow.Margin = new System.Windows.Forms.Padding(10, 80, 10, 0);
                pnlStepsContainer.Controls.Add(lblArrow);
            }

            // Group Panel cho mỗi bước
            Panel pnlStep = new Panel();
            pnlStep.Size = new Size(250, 200);
            pnlStep.Tag = level;
            pnlStep.Margin = new System.Windows.Forms.Padding(5, 50, 5, 0);

            // Label
            Label lbl = new Label();
            lbl.Text = $"Level {level} ({(level == 0 ? "Bộ phận Mẹ" : "Chọn tiếp theo")})";
            lbl.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lbl.AutoSize = true;
            lbl.Location = new Point(0, 0);
            pnlStep.Controls.Add(lbl);

            // Combobox
            ComboBox cbo = new ComboBox();
            cbo.DropDownStyle = ComboBoxStyle.DropDownList;
            cbo.Width = 240;
            cbo.Location = new Point(0, 30);
            cbo.DataSource = new List<HierarchyNode>(nodes);
            cbo.DisplayMember = "Name";
            cbo.SelectedIndex = -1;

            cbo.SelectedIndexChanged += (s, e) => {
                // Xóa các bước phía sau
                RemoveStepsFromLevel(level + 1);
                
                if (cbo.SelectedItem is HierarchyNode selectedNode)
                {
                    if (selectedNode.Children.Count > 0)
                    {
                        AddStep(selectedNode.Children, level + 1);
                    }
                }
                UpdateApplyButton();
            };

            pnlStep.Controls.Add(cbo);
            pnlStepsContainer.Controls.Add(pnlStep);
        }

        private void RemoveStepsFromLevel(int level)
        {
            var controlsToRemove = pnlStepsContainer.Controls.Cast<Control>()
                                    .Where(c => (c.Tag is int l && l >= level))
                                    .ToList();
            foreach (var c in controlsToRemove)
            {
                pnlStepsContainer.Controls.Remove(c);
                c.Dispose();
            }
        }

        private void UpdateApplyButton()
        {
            var combos = pnlStepsContainer.Controls.OfType<Panel>()
                         .SelectMany(p => p.Controls.OfType<ComboBox>())
                         .ToList();
            btnApply.Enabled = combos.Any(c => c.SelectedIndex >= 0);
        }
    }
}
