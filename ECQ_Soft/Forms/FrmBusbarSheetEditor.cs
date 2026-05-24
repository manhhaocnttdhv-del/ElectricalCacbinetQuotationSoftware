using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using ECQ_Soft.Services;
using Color = System.Drawing.Color;
using Padding = System.Windows.Forms.Padding;

namespace ECQ_Soft
{
    /// <summary>
    /// Modal hiển thị toàn bộ nội dung sheet "Tính toán đồng thanh cái" dưới dạng RAW (3 bảng).
    /// Cho phép user chỉnh sửa trực tiếp và lưu ngược về Google Sheets.
    /// Hỗ trợ hiển thị công thức gốc (FORMULA) và formula bar giống Excel.
    /// Cột FILE: button Add File → upload nhiều file lên Google Drive (folder "file vnecco") → ghi link vào cell.
    /// </summary>
    public class FrmBusbarSheetEditor : Form
    {
        private const string SHEET_NAME = "Tính toán đồng thanh cái";
        private const string RANGE = "Tính toán đồng thanh cái!A1:AU";

        private readonly SheetsService _service;
        private readonly string _spreadsheetId;

        private DataGridView _grid;
        private Button _btnSave;
        private Button _btnReload;
        private Button _btnClose;
        private Label _lblStatus;
        private Label _lblCellRef;
        private TextBox _txtFormula;
        private ToolStrip _toolbar;
        private Panel _formulaBar;
        private CheckBox _chkShowFormulas;

        private IList<IList<object>> _formulaData;
        private IList<IList<object>> _valueData;

        // Lưu danh sách index các cột có header "FILE" để hiển thị button Add File
        private HashSet<int> _fileColumnIndices = new HashSet<int>();
        // Dòng header chứa text "FILE" (dùng để skip không vẽ button trên header row)
        private int _fileHeaderRow = -1;

        public FrmBusbarSheetEditor(SheetsService service, string spreadsheetId)
        {
            _service = service;
            _spreadsheetId = spreadsheetId;
            InitBusbarUi();
            this.Load += async (s, e) => await LoadBusbarSheetAsync();
        }

        private void InitBusbarUi()
        {
            this.Text = "Sheet: Tính toán đồng thanh cái — chỉnh sửa & lưu";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(1280, 720);
            this.MinimumSize = new Size(900, 500);
            this.ShowIcon = false;

            _toolbar = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden, Padding = new Padding(8, 4, 8, 4), BackColor = Color.FromArgb(245, 247, 250) };

            _btnReload = new Button { Text = "🔄 Tải lại", Width = 110, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = Color.White, Font = new Font("Segoe UI", 9.5f) };
            _btnReload.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
            _btnReload.Click += async (s, e) => await LoadBusbarSheetAsync();

            _btnSave = new Button { Text = "💾 Lưu & Cập nhật", Width = 160, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += async (s, e) => await SaveAndReloadAsync();

            _btnClose = new Button { Text = "Đóng", Width = 90, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = Color.White, Font = new Font("Segoe UI", 9.5f) };
            _btnClose.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
            _btnClose.Click += (s, e) => this.Close();

            _chkShowFormulas = new CheckBox { Text = "Hiện công thức", Font = new Font("Segoe UI", 9f), AutoSize = true, Checked = false };
            _chkShowFormulas.CheckedChanged += (s, e) => ToggleFormulaView();

            _lblStatus = new Label { AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Italic), ForeColor = Color.FromArgb(80, 80, 80), Padding = new Padding(12, 8, 0, 0), Text = "Đang khởi tạo..." };

            _toolbar.Items.Add(new ToolStripControlHost(_btnReload));
            _toolbar.Items.Add(new ToolStripSeparator());
            _toolbar.Items.Add(new ToolStripControlHost(_btnSave));
            _toolbar.Items.Add(new ToolStripSeparator());
            _toolbar.Items.Add(new ToolStripControlHost(_chkShowFormulas));
            _toolbar.Items.Add(new ToolStripSeparator());
            _toolbar.Items.Add(new ToolStripControlHost(_btnClose));
            _toolbar.Items.Add(new ToolStripControlHost(_lblStatus));

            // Formula Bar
            _formulaBar = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = Color.FromArgb(250, 250, 250), BorderStyle = BorderStyle.FixedSingle };
            _lblCellRef = new Label { Text = "A1", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Width = 60, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Left, BackColor = Color.FromArgb(230, 230, 230) };
            var lblFx = new Label { Text = " fx ", Font = new Font("Segoe UI", 9f, FontStyle.Italic), Width = 28, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Left, ForeColor = Color.Gray };
            _txtFormula = new TextBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 9.5f), BorderStyle = BorderStyle.None };
            _txtFormula.KeyDown += TxtFormula_KeyDown;
            _formulaBar.Controls.Add(_txtFormula);
            _formulaBar.Controls.Add(lblFx);
            _formulaBar.Controls.Add(_lblCellRef);

            // Grid
            _grid = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = true, AllowUserToDeleteRows = true, RowHeadersVisible = true, MultiSelect = true, SelectionMode = DataGridViewSelectionMode.CellSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None, BackgroundColor = Color.White, Font = new Font("Segoe UI", 9f), BorderStyle = BorderStyle.None, EnableHeadersVisualStyles = false, ColumnHeadersHeight = 32 };
            _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(0, 90, 158), ForeColor = Color.White, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Alignment = DataGridViewContentAlignment.MiddleCenter };
            _grid.RowTemplate.Height = 24;
            typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(_grid, true, null);
            _grid.SelectionChanged += Grid_SelectionChanged;
            _grid.CellBeginEdit += Grid_CellBeginEdit;
            _grid.CellEndEdit += Grid_CellEndEdit;
            _grid.CellPainting += Grid_CellPainting;
            _grid.CellClick += Grid_CellClick;

            this.Controls.Add(_grid);
            this.Controls.Add(_formulaBar);
            this.Controls.Add(_toolbar);
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            if (_grid.CurrentCell == null) return;
            int row = _grid.CurrentCell.RowIndex;
            int col = _grid.CurrentCell.ColumnIndex;
            _lblCellRef.Text = $"{ColumnIndexToLetter(col)}{row + 1}";
            string formula = GetFormulaAt(row, col);
            _txtFormula.Text = formula ?? (_grid.CurrentCell.Value?.ToString() ?? "");
        }

        private void Grid_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            string formula = GetFormulaAt(e.RowIndex, e.ColumnIndex);
            if (formula != null && formula.StartsWith("="))
                _grid.CurrentCell.Value = formula;
        }

        private void Grid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            string newValue = _grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "";
            SetFormulaAt(e.RowIndex, e.ColumnIndex, newValue);
            if (newValue.StartsWith("="))
                _grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.ForeColor = Color.Blue;
            _txtFormula.Text = newValue;
        }

        private void TxtFormula_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && _grid.CurrentCell != null)
            {
                int row = _grid.CurrentCell.RowIndex;
                int col = _grid.CurrentCell.ColumnIndex;
                string newValue = _txtFormula.Text;
                _grid.CurrentCell.Value = newValue;
                SetFormulaAt(row, col, newValue);
                if (newValue.StartsWith("="))
                    _grid.Rows[row].Cells[col].Style.ForeColor = Color.Blue;
                else
                    _grid.Rows[row].Cells[col].Style.ForeColor = Color.Black;
                if (row + 1 < _grid.Rows.Count)
                    _grid.CurrentCell = _grid.Rows[row + 1].Cells[col];
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private string GetFormulaAt(int row, int col)
        {
            if (_formulaData == null || row >= _formulaData.Count) return null;
            var r = _formulaData[row];
            if (r == null || col >= r.Count) return null;
            return r[col]?.ToString();
        }

        private void SetFormulaAt(int row, int col, string value)
        {
            if (_formulaData == null) return;
            while (_formulaData.Count <= row) _formulaData.Add(new List<object>());
            var r = _formulaData[row];
            while (r.Count <= col) r.Add("");
            r[col] = value;
        }

        private string GetValueAt(int row, int col)
        {
            if (_valueData == null || row >= _valueData.Count) return null;
            var r = _valueData[row];
            if (r == null || col >= r.Count) return null;
            return r[col]?.ToString();
        }

        private void ToggleFormulaView()
        {
            if (_formulaData == null) return;
            _grid.SuspendLayout();
            for (int i = 0; i < _grid.Rows.Count; i++)
            {
                if (_grid.Rows[i].IsNewRow) continue;
                for (int c = 0; c < _grid.Columns.Count; c++)
                {
                    string formula = GetFormulaAt(i, c);
                    if (formula == null) continue;
                    if (_chkShowFormulas.Checked)
                    {
                        _grid.Rows[i].Cells[c].Value = formula;
                        if (formula.StartsWith("=")) _grid.Rows[i].Cells[c].Style.ForeColor = Color.Blue;
                    }
                    else
                    {
                        string val = GetValueAt(i, c);
                        _grid.Rows[i].Cells[c].Value = val ?? formula;
                        _grid.Rows[i].Cells[c].Style.ForeColor = Color.Black;
                    }
                }
            }
            _grid.ResumeLayout();
        }

        private async Task LoadBusbarSheetAsync()
        {
            try
            {
                _lblStatus.Text = "Đang tải dữ liệu từ Google Sheets...";
                _grid.Enabled = false;

                var formulaReq = _service.Spreadsheets.Values.Get(_spreadsheetId, RANGE);
                formulaReq.ValueRenderOption = SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum.FORMULA;
                var valueReq = _service.Spreadsheets.Values.Get(_spreadsheetId, RANGE);
                valueReq.ValueRenderOption = SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum.FORMATTEDVALUE;

                var formulaTask = formulaReq.ExecuteAsync();
                var valueTask = valueReq.ExecuteAsync();
                await Task.WhenAll(formulaTask, valueTask);

                _formulaData = formulaTask.Result.Values ?? new List<IList<object>>();
                _valueData = valueTask.Result.Values ?? new List<IList<object>>();
                BindBusbarGrid();
                _lblStatus.Text = $"Đã tải {_formulaData.Count} dòng. Bạn có thể chỉnh sửa trực tiếp và bấm Lưu.";
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Lỗi tải sheet.";
                MessageBox.Show("Không thể tải sheet 'Tính toán đồng thanh cái'.\nLỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { _grid.Enabled = true; }
        }

        private void BindBusbarGrid()
        {
            _grid.SuspendLayout();
            try
            {
                _grid.Columns.Clear();
                _grid.Rows.Clear();
                int maxCols = 0;
                foreach (var r in _formulaData) { if (r != null && r.Count > maxCols) maxCols = r.Count; }
                if (_valueData != null) foreach (var r in _valueData) { if (r != null && r.Count > maxCols) maxCols = r.Count; }
                if (maxCols == 0) maxCols = 10;

                for (int c = 0; c < maxCols; c++)
                    _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "col" + c, HeaderText = ColumnIndexToLetter(c), Width = 130, SortMode = DataGridViewColumnSortMode.NotSortable });

                int rowCount = Math.Max(_formulaData.Count, _valueData != null ? _valueData.Count : 0);
                for (int i = 0; i < rowCount; i++)
                {
                    var values = new object[maxCols];
                    for (int c = 0; c < maxCols; c++)
                    {
                        string val = GetValueAt(i, c);
                        string formula = GetFormulaAt(i, c);
                        values[c] = val ?? formula ?? "";
                    }
                    int idx = _grid.Rows.Add(values);
                    for (int c = 0; c < maxCols; c++)
                    {
                        string formula = GetFormulaAt(i, c);
                        if (formula != null && formula.StartsWith("="))
                            _grid.Rows[idx].Cells[c].Style.ForeColor = Color.FromArgb(0, 100, 0);
                    }
                }
                HighlightBusbarHeaders();
            }
            finally { _grid.ResumeLayout(); }
        }

        private void HighlightBusbarHeaders()
        {
            for (int i = 0; i < _grid.Rows.Count; i++)
            {
                string firstCell = _grid.Rows[i].Cells[0].Value?.ToString() ?? "";
                if (firstCell.StartsWith("Bảng", StringComparison.OrdinalIgnoreCase) || firstCell.IndexOf("(Bảng", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _grid.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(255, 247, 200);
                    _grid.Rows[i].DefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                }
            }
            // Detect cột FILE sau khi bind xong
            DetectFileColumns();
        }

        /// <summary>
        /// Quét grid để tìm các cột có cell chứa text "FILE" (header row trong sheet).
        /// </summary>
        private void DetectFileColumns()
        {
            _fileColumnIndices.Clear();
            _fileHeaderRow = -1;

            int scanRows = Math.Min(_grid.Rows.Count, 10);
            for (int i = 0; i < scanRows; i++)
            {
                if (_grid.Rows[i].IsNewRow) continue;
                for (int c = 0; c < _grid.Columns.Count; c++)
                {
                    string cellValue = _grid.Rows[i].Cells[c].Value?.ToString()?.Trim() ?? "";
                    if (cellValue.Equals("FILE", StringComparison.OrdinalIgnoreCase))
                    {
                        _fileColumnIndices.Add(c);
                        if (_fileHeaderRow < 0) _fileHeaderRow = i;
                    }
                }
            }
        }

        /// <summary>
        /// Vẽ button "📎 Add" vào các cell thuộc cột FILE (trừ header row).
        /// </summary>
        private void Grid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (!_fileColumnIndices.Contains(e.ColumnIndex)) return;
            if (e.RowIndex <= _fileHeaderRow) return;
            if (_grid.Rows[e.RowIndex].IsNewRow) return;

            e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);

            string cellText = e.Value?.ToString() ?? "";
            int btnWidth = 70;
            int padding = 4;

            // Vẽ nội dung text bên trái (nếu có link file)
            if (!string.IsNullOrEmpty(cellText))
            {
                var textRect = new Rectangle(
                    e.CellBounds.X + padding,
                    e.CellBounds.Y + 2,
                    e.CellBounds.Width - btnWidth - padding * 2,
                    e.CellBounds.Height - 4);

                TextRenderer.DrawText(e.Graphics, cellText, e.CellStyle.Font,
                    textRect, Color.Blue, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            // Vẽ button "📎 Add" bên phải
            var btnRect = new Rectangle(
                e.CellBounds.Right - btnWidth - padding,
                e.CellBounds.Y + 3,
                btnWidth - 2,
                e.CellBounds.Height - 7);

            using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                btnRect, Color.FromArgb(0, 120, 215), Color.FromArgb(0, 90, 180),
                System.Drawing.Drawing2D.LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(brush, btnRect);
            }

            using (var pen = new Pen(Color.FromArgb(0, 80, 160)))
            {
                e.Graphics.DrawRectangle(pen, btnRect);
            }

            string btnText = string.IsNullOrEmpty(cellText) ? "📎 Add" : "📎 Đổi";
            TextRenderer.DrawText(e.Graphics, btnText,
                new Font("Segoe UI", 8f, FontStyle.Bold), btnRect,
                Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            e.Handled = true;
        }

        /// <summary>
        /// Xử lý click vào button Add File trong cột FILE.
        /// Mở OpenFileDialog (Multiselect) → Upload lên Google Drive folder "file vnecco" → ghi link vào cell.
        /// </summary>
        private void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (!_fileColumnIndices.Contains(e.ColumnIndex)) return;
            if (e.RowIndex <= _fileHeaderRow) return;
            if (_grid.Rows[e.RowIndex].IsNewRow) return;

            // Kiểm tra click có nằm trong vùng button không
            var cellRect = _grid.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);
            var mousePos = _grid.PointToClient(Cursor.Position);
            int btnWidth = 70;
            int padding = 4;
            var btnRect = new Rectangle(
                cellRect.Right - btnWidth - padding,
                cellRect.Y + 3,
                btnWidth - 2,
                cellRect.Height - 7);

            if (!btnRect.Contains(mousePos)) return;

            // Mở dialog chọn nhiều file
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Chọn file đính kèm (có thể chọn nhiều file)";
                ofd.Filter = "Tất cả file (*.*)|*.*|PDF (*.pdf)|*.pdf|Hình ảnh (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|Excel (*.xlsx;*.xls)|*.xlsx;*.xls|Word (*.docx;*.doc)|*.docx;*.doc|AutoCAD (*.dwg;*.dxf)|*.dwg;*.dxf";
                ofd.FilterIndex = 1;
                ofd.Multiselect = true;

                if (ofd.ShowDialog() == DialogResult.OK && ofd.FileNames.Length > 0)
                {
                    _ = UploadFilesToDriveAsync(ofd.FileNames, e.RowIndex, e.ColumnIndex);
                }
            }
        }

        /// <summary>
        /// Upload nhiều file lên Google Drive (folder "file vnecco") và ghi link vào cell.
        /// </summary>
        private async Task UploadFilesToDriveAsync(string[] filePaths, int rowIndex, int colIndex)
        {
            try
            {
                _grid.Enabled = false;
                int totalFiles = filePaths.Length;
                _lblStatus.Text = $"Đang upload {totalFiles} file lên Google Drive (folder: file vnecco)...";

                // Upload vào folder "file vnecco" trên Drive
                var uploader = new GoogleDriveUploader();

                var links = new List<string>();
                for (int i = 0; i < filePaths.Length; i++)
                {
                    _lblStatus.Text = $"Đang upload ({i + 1}/{totalFiles}): {Path.GetFileName(filePaths[i])}...";
                    var result = await uploader.UploadFileAsync(filePaths[i]);
                    links.Add(result.WebViewLink);
                }

                // Ghi tất cả link vào cell, phân cách bằng dấu xuống dòng
                string cellValue = string.Join("\n", links);
                _grid.Rows[rowIndex].Cells[colIndex].Value = cellValue;
                SetFormulaAt(rowIndex, colIndex, cellValue);

                _grid.Rows[rowIndex].Cells[colIndex].Tag = filePaths;

                _lblStatus.Text = $"✅ Đã upload {totalFiles} file thành công lên Google Drive.";
                _grid.InvalidateCell(colIndex, rowIndex);

                MessageBox.Show(
                    $"Đã upload {totalFiles} file lên Google Drive (folder: file vnecco) thành công!\n\nLink đã được ghi vào ô {ColumnIndexToLetter(colIndex)}{rowIndex + 1}.\nBấm 'Lưu & Cập nhật' để đồng bộ lên Google Sheets.",
                    "Upload thành công",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "❌ Upload thất bại.";
                MessageBox.Show(
                    $"Lỗi khi upload file lên Google Drive:\n{ex.Message}",
                    "Lỗi Upload",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _grid.Enabled = true;
            }
        }

        private async Task SaveAndReloadAsync()
        {
            var confirm = MessageBox.Show("Lưu toàn bộ thay đổi lên Google Sheets?\nSau khi lưu, sheet sẽ tự tính toán lại các công thức.", "Xác nhận lưu", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;
            try
            {
                _lblStatus.Text = "Đang lưu...";
                _btnSave.Enabled = false;
                _btnReload.Enabled = false;

                var newValues = new List<IList<object>>();
                if (_formulaData != null)
                {
                    foreach (var r in _formulaData)
                    {
                        if (r == null) { newValues.Add(new List<object>()); continue; }
                        var rowValues = new List<object>(r);
                        while (rowValues.Count > 0 && string.IsNullOrEmpty(rowValues[rowValues.Count - 1]?.ToString())) rowValues.RemoveAt(rowValues.Count - 1);
                        newValues.Add(rowValues);
                    }
                }

                await _service.Spreadsheets.Values.Clear(new ClearValuesRequest(), _spreadsheetId, RANGE).ExecuteAsync();
                if (newValues.Count > 0)
                {
                    var body = new ValueRange { Values = newValues };
                    var update = _service.Spreadsheets.Values.Update(body, _spreadsheetId, SHEET_NAME + "!A1");
                    update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                    await update.ExecuteAsync();
                }

                _lblStatus.Text = "Đã lưu. Đang tải lại kết quả...";
                await LoadBusbarSheetAsync();
                MessageBox.Show("Đã lưu và cập nhật. Công thức đã được Google Sheets tính toán lại.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Lưu thất bại.";
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { _btnSave.Enabled = true; _btnReload.Enabled = true; }
        }

        private static string ColumnIndexToLetter(int index)
        {
            string letters = "";
            int n = index;
            do { letters = (char)('A' + (n % 26)) + letters; n = n / 26 - 1; } while (n >= 0);
            return letters;
        }
    }
}
