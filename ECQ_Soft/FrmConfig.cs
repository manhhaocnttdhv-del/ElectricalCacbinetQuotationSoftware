using ECQ_Soft.Model;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using ECQ_Soft.Helper;
using System.Windows.Forms;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.ComponentModel;
using Excel = Microsoft.Office.Interop.Excel;
using ECQ_Soft.Helpers;

namespace ECQ_Soft
{
    public partial class FrmConfig : UserControl
    {
        // ══════════════════════════════════════════════════════════════════
        // FIELDS – Biến trạng thái của form
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Cờ tránh vòng lặp khi cập nhật comboBox2 ↔ comboBox1 lẫn nhau.</summary>
        private bool isUpdatingComboBoxes = false;

        /// <summary>Kết nối tới Google Sheets API (khởi tạo một lần, dùng lại).</summary>
        private SheetsService _sheetsService;

        /// <summary>ID của Google Spreadsheet chứa toàn bộ dữ liệu.</summary>
        string spreadsheetId = "10gNCH_pG4LmkQ1g109H1WEM4nwBk4UBff_IDHar0Hd8";

        /// <summary>Tên sheet chứa danh sách sản phẩm (bảng master).</summary>
        string sheetName = "Products_Table";

        /// <summary>Tên sheet hiện tại đang làm việc (ví dụ: "Config_Tab1"). Null = chưa chọn.</summary>
        string configSheetName = null;

        /// <summary>Trả về đường dẫn file cache JSON cho một key dữ liệu cụ thể.</summary>
        private string GetCachePath(string key) => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"cache_{key}_{configSheetName ?? "global"}.json");

        /// <summary>Trả về SheetsService để FrmMain/modal dùng chung.</summary>
        public SheetsService GetSheetsService()
        {
            if (_sheetsService == null) InitGoogleSheetsService();
            return _sheetsService;
        }

        /// <summary>Trả về Spreadsheet ID hiện tại.</summary>
        public string GetSpreadsheetId() => spreadsheetId;

        /// <summary>Trả về tên sheet cấu hình hiện tại.</summary>
        public string GetConfigSheetName() => configSheetName;

        /// <summary>
        /// Cập nhật tên sheet cấu hình và reload lại dữ liệu cấu hình.
        /// Được gọi sau khi người dùng chọn/tạo tab từ modal FrmSheetSelector.
        /// </summary>
        public async Task SetConfigSheet(string newConfigSheetName)
        {
            if (string.IsNullOrEmpty(newConfigSheetName)) return;
            configSheetName = newConfigSheetName;
            lblCurrentTab.Text = $"Tab: [{configSheetName}]";

            // Xoá trắng 2 DataGridView (dataGridView1 và dgvParentProducts) khi đổi tab
            childProducts.Clear();
            configProducts.Clear();
            currentEditingConfigName = null;
            UpdateConfigGrid(); // Làm mới hiển thị trên giao diện dgvParentProducts

            // Nạp từ cache ngay lập tức để người dùng thấy danh sách cấu hình TỨC THÌ
            var cachedNames = LoadFromCache<List<string>>("config_names");
            if (cachedNames != null)
            {
                lstSavedConfigs.ClearItems();
                foreach (var n in cachedNames) lstSavedConfigs.AddItem(n);
            }

            await LoadDataAsync();
        }

        /// <summary>Cây danh mục sản phẩm (phân cấp, dùng cho bộ lọc).</summary>
        private List<CategoryItem> categoryTree = new List<CategoryItem>();

        /// <summary>Toàn bộ sản phẩm nạp từ Google Sheets (sheet Products_Table).</summary>
        private List<Products> allProducts = new List<Products>();


        /// <summary>Danh sách sản phẩm đang được thêm vào cấu hình báo giá hiện tại.</summary>
        private List<ConfigProductItem> configProducts = new List<ConfigProductItem>();

        /// <summary>Danh sách quan hệ sản phẩm chính – sản phẩm con (relation PR).</summary>
        private List<RelationItem> productRelations = new List<RelationItem>();

        /// <summary>Danh sách sản phẩm con (bên phải) đang được chọn, binding với dataGridView1.</summary>
        private BindingList<Products> childProducts = new BindingList<Products>();

        /// <summary>Toàn bộ cấu hình đã lưu trên Google Sheets (dùng để merge khi nạp nhiều cấu hình).</summary>
        private List<ConfigProductItem> allSavedConfigs = new List<ConfigProductItem>();

        /// <summary>Ghi nhớ sheet và gói vừa search để auto-fill khi nhấn Lưu.</summary>
        private string lastSearchedSheet = null;
        private string lastSearchedPkg = null;

        /// <summary>Tên cấu hình đang được chỉnh sửa (null = đang tạo mới).</summary>
        private string currentEditingConfigName = null;

        private CheckBox chkSelectAllAllProducts = new CheckBox();
        private CheckBox chkSelectAllChildProducts = new CheckBox();

        // ── Màu tuỳ chỉnh per-cell (được chọn qua color picker chuột phải) ──
        // Key = (rowIndex, colIndex) của DataGridView; Value = màu được chọn
        private Dictionary<(ConfigProductItem item, int c), Color> _cellBgColors = new Dictionary<(ConfigProductItem, int), Color>(); // màu nền
        private Dictionary<(ConfigProductItem item, int c), Color> _cellFgColors = new Dictionary<(ConfigProductItem, int), Color>(); // màu chữ
        private Dictionary<(ConfigProductItem item, int c), Font> _cellFonts = new Dictionary<(ConfigProductItem, int), Font>(); // font chữ (đậm, nghiêng, kích thước)
        private HashSet<string> _collapsedGroups = new HashSet<string>(); // Lưu trạng thái thu gọn của các nhóm (Header)

        /// <summary>Lưu vị trí ô được right-click (để hiển thị context menu đúng ô).</summary>
        private int _rightClickedRow = -1;
        private int _rightClickedCol = -1;

        /// <summary>Danh sách hiển thị trong dgvParentProducts (bao gồm cả 3 dòng TỔNG/VAT/THÀNH TIỀN).</summary>
        private List<ConfigProductItem> _displayList = new List<ConfigProductItem>();

        private Form _popupQuoteForm = null;

        // ══════════════════════════════════════════════════════════════════
        // KHỞI TẠO FORM
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Constructor: đăng ký tất cả event handlers và thiết lập context menu màu ô.
        /// </summary>
        public FrmConfig()
        {
            InitializeComponent();
            dgvParentProducts.CellValueChanged += DgvParentProducts_CellValueChanged;
            dgvParentProducts.CurrentCellDirtyStateChanged += DgvParentProducts_CurrentCellDirtyStateChanged;

            dataGridView1.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;

            dataGridView1.DataBindingComplete += Grid_DataBindingComplete;
            dgvParentProducts.DataBindingComplete += DgvParentProducts_DataBindingComplete;
            dgvParentProducts.CellFormatting += DgvParentProducts_CellFormatting;

            // Handle DataError to suppress technical dialogs
            dataGridView1.DataError += Grid_DataError;
            dgvParentProducts.DataError += Grid_DataError;

            // Multi-row toggle logic
            dataGridView1.CellContentClick += Grid_CellContentClick;
            dataGridView1.KeyDown += Grid_KeyDown;

            // Khi chọn dòng trong dataGridView1 (childProducts)
            dataGridView1.SelectionChanged += DataGridView1_SelectionChanged;
            // Vẽ rich text (highlight đỏ) cho dòng Vỏ tủ điện trong dataGridView1
            dataGridView1.CellPainting += DataGridView1_CabinetCellPainting;
            dgvParentProducts.CellPainting += DgvParentProducts_CabinetCellPainting;
            dgvParentProducts.RowPostPaint += DgvParentProducts_RowPostPaint;
            dgvParentProducts.CellMouseClick += DgvParentProducts_CellMouseClick;

            SetupHeaderCheckBox(dataGridView1, chkSelectAllChildProducts, "IsSelected");

            dataGridView1.DataSource = childProducts;

            // Click ra ngoài DataGridView → xóa bôi đen (selection)
            this.Click += (s, e) => dgvParentProducts.ClearSelection();
            this.MouseDown += (s, e) => dgvParentProducts.ClearSelection();
            // Khi focus rời khỏi DataGridView (ví dụ click vào button, textbox khác) → xóa selection
            dgvParentProducts.Leave += (s, e) => dgvParentProducts.ClearSelection();

            // ── Context menu chuột phải cho ô trong danh sách cấu hình ──
            var ctxCell = new ContextMenuStrip();

            var miSetBg = new System.Windows.Forms.ToolStripMenuItem("🎨  Màu nền ô");
            miSetBg.Click += (s, e) =>
            {
                using (var picker = new ECQ_Soft.Helper.ColorPickerPopup())
                {
                    if (picker.ShowDialog() == DialogResult.OK && picker.SelectedColor.HasValue)
                    {
                        foreach (DataGridViewCell cell in dgvParentProducts.SelectedCells)
                        {
                            if (cell.RowIndex >= 0 && cell.ColumnIndex >= 0)
                            {
                                var item = (ConfigProductItem)dgvParentProducts.Rows[cell.RowIndex].DataBoundItem;
                                _cellBgColors[(item, cell.ColumnIndex)] = picker.SelectedColor.Value;
                            }
                        }
                        dgvParentProducts.Refresh();
                    }
                }
            };

            var miSetFg = new System.Windows.Forms.ToolStripMenuItem("✏️  Màu chữ ô");
            miSetFg.Click += (s, e) =>
            {
                using (var picker = new ECQ_Soft.Helper.ColorPickerPopup())
                {
                    if (picker.ShowDialog() == DialogResult.OK && picker.SelectedColor.HasValue)
                    {
                        foreach (DataGridViewCell cell in dgvParentProducts.SelectedCells)
                        {
                            if (cell.RowIndex >= 0 && cell.ColumnIndex >= 0)
                            {
                                var item = (ConfigProductItem)dgvParentProducts.Rows[cell.RowIndex].DataBoundItem;
                                _cellFgColors[(item, cell.ColumnIndex)] = picker.SelectedColor.Value;
                            }
                        }
                        dgvParentProducts.Refresh();
                    }
                }
            };



            var miEditCell = new System.Windows.Forms.ToolStripMenuItem("📝  Chỉnh sửa nội dung");
            miEditCell.Click += (s, e) =>
            {
                if (_rightClickedRow >= 0 && _rightClickedCol >= 0)
                {
                    var cell = dgvParentProducts.Rows[_rightClickedRow].Cells[_rightClickedCol];
                    string currentValue = cell.Value?.ToString() ?? "";

                    using (Form frmEdit = new Form())
                    {
                        frmEdit.Text = "Chỉnh sửa nội dung ô";
                        frmEdit.Size = new Size(400, 250);
                        frmEdit.StartPosition = FormStartPosition.CenterParent;
                        frmEdit.FormBorderStyle = FormBorderStyle.FixedDialog;
                        frmEdit.MaximizeBox = false;
                        frmEdit.MinimizeBox = false;
                        frmEdit.Font = new Font("Segoe UI", 9f);

                        TextBox txtInput = new TextBox();
                        txtInput.Multiline = true;
                        txtInput.ScrollBars = ScrollBars.Vertical;
                        txtInput.Text = currentValue;
                        txtInput.Location = new Point(15, 15);
                        txtInput.Size = new Size(355, 130);
                        frmEdit.Controls.Add(txtInput);

                        Button btnOK = new Button();
                        btnOK.Text = "Xác nhận";
                        btnOK.DialogResult = DialogResult.OK;
                        btnOK.Location = new Point(190, 165);
                        btnOK.Size = new Size(85, 30);
                        btnOK.BackColor = Color.FromArgb(0, 120, 215);
                        btnOK.ForeColor = Color.White;
                        btnOK.FlatStyle = FlatStyle.Flat;
                        btnOK.FlatAppearance.BorderSize = 0;
                        frmEdit.Controls.Add(btnOK);

                        Button btnCancel = new Button();
                        btnCancel.Text = "Hủy";
                        btnCancel.DialogResult = DialogResult.Cancel;
                        btnCancel.Location = new Point(285, 165);
                        btnCancel.Size = new Size(85, 30);
                        btnCancel.BackColor = Color.FromArgb(200, 200, 200);
                        btnCancel.FlatStyle = FlatStyle.Flat;
                        btnCancel.FlatAppearance.BorderSize = 0;
                        frmEdit.Controls.Add(btnCancel);

                        frmEdit.AcceptButton = btnOK;
                        frmEdit.CancelButton = btnCancel;

                        if (frmEdit.ShowDialog() == DialogResult.OK)
                        {
                            var item = dgvParentProducts.Rows[_rightClickedRow].DataBoundItem as ECQ_Soft.Model.ConfigProductItem;
                            if (item != null)
                            {
                                string colName = dgvParentProducts.Columns[_rightClickedCol].DataPropertyName;
                                if (string.IsNullOrEmpty(colName)) colName = dgvParentProducts.Columns[_rightClickedCol].Name;

                                try
                                {
                                    var prop = typeof(ECQ_Soft.Model.ConfigProductItem).GetProperty(colName);
                                    bool wasReadOnly = cell.ReadOnly;
                                    cell.ReadOnly = false;

                                    if (prop != null && prop.CanWrite)
                                    {
                                        if (prop.PropertyType == typeof(string))
                                            cell.Value = txtInput.Text;
                                        else if (prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?))
                                        {
                                            if (decimal.TryParse(txtInput.Text, out decimal d)) cell.Value = d;
                                        }
                                        else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                                        {
                                            if (int.TryParse(txtInput.Text, out int i)) cell.Value = i;
                                        }
                                        else
                                        {
                                            cell.Value = txtInput.Text;
                                        }
                                    }
                                    else
                                    {
                                        cell.Value = txtInput.Text;
                                    }

                                    cell.ReadOnly = wasReadOnly;
                                    dgvParentProducts.RefreshEdit();
                                    dgvParentProducts.InvalidateRow(_rightClickedRow);
                                }
                                catch
                                {
                                    bool wasReadOnly = cell.ReadOnly;
                                    cell.ReadOnly = false;
                                    cell.Value = txtInput.Text;
                                    cell.ReadOnly = wasReadOnly;
                                }
                            }
                            else
                            {
                                cell.Value = txtInput.Text;
                            }
                        }
                    }
                }
            };

            var miClearColor = new System.Windows.Forms.ToolStripMenuItem("✖  Xoá màu và định dạng Font (các ô đang chọn)");
            miClearColor.Click += (s, e) =>
            {
                string[] sheetColOrder = { "STT", "TenHang", "MaHang", "XuatXu", "DonVi", "SoLuong", "DonGiaVND", "ThanhTienVND", "GhiChu", "GiaNhap", "ThanhTien", "LoiNhuan", "BangGia" };
                foreach (DataGridViewCell cell in dgvParentProducts.SelectedCells)
                {
                    if (cell.RowIndex >= 0 && cell.ColumnIndex >= 0)
                    {
                        var item = (ConfigProductItem)dgvParentProducts.Rows[cell.RowIndex].DataBoundItem;
                        var key = (item, cell.ColumnIndex);
                        _cellBgColors.Remove(key);
                        _cellFgColors.Remove(key);
                        _cellFonts.Remove(key);

                        if (item.SheetRowIndex >= 0)
                        {
                            string colName = dgvParentProducts.Columns[cell.ColumnIndex].Name;
                            int sheetColIdx = Array.IndexOf(sheetColOrder, colName);
                            if (sheetColIdx >= 0)
                            {
                                var sheetKey = (item.SheetRowIndex, sheetColIdx);
                                _sheetBgColors.Remove(sheetKey);
                                _sheetFgColors.Remove(sheetKey);
                            }
                        }
                    }
                }
                dgvParentProducts.Refresh();
            };

            var miFont = new System.Windows.Forms.ToolStripMenuItem("🅰️  Định dạng Font chữ (Đậm, Nghiêng, Cỡ chữ...)");
            miFont.Click += (s, e) =>
            {
                using (var fd = new FontDialog())
                {
                    if (dgvParentProducts.SelectedCells.Count > 0)
                    {
                        var firstCell = dgvParentProducts.SelectedCells[0];
                        var item = (ConfigProductItem)dgvParentProducts.Rows[firstCell.RowIndex].DataBoundItem;
                        var key = (item, firstCell.ColumnIndex);
                        var currentFont = firstCell.Style.Font ?? dgvParentProducts.Font;
                        if (_cellFonts.TryGetValue(key, out Font customFont))
                            currentFont = customFont;
                        fd.Font = currentFont;
                    }

                    if (fd.ShowDialog() == DialogResult.OK)
                    {
                        foreach (DataGridViewCell cell in dgvParentProducts.SelectedCells)
                        {
                            if (cell.RowIndex >= 0 && cell.ColumnIndex >= 0)
                            {
                                var item = (ConfigProductItem)dgvParentProducts.Rows[cell.RowIndex].DataBoundItem;
                                var key = (item, cell.ColumnIndex);
                                _cellFonts[key] = fd.Font;
                            }
                        }
                        dgvParentProducts.Refresh();
                    }
                }
            };

            ctxCell.Items.Add(miEditCell);
            ctxCell.Items.Add(new ToolStripSeparator());
            ctxCell.Items.Add(miFont);
            ctxCell.Items.Add(miSetBg);
            ctxCell.Items.Add(miSetFg);
            ctxCell.Items.Add(new ToolStripSeparator());
            ctxCell.Items.Add(miClearColor);

            // ── Xóa dòng ──
            var miDeleteRow = new ToolStripMenuItem("🗑  Xóa dòng này");
            miDeleteRow.ForeColor = Color.Red;
            miDeleteRow.Font = new Font("Times New Roman", 9.5F, FontStyle.Bold);
            miDeleteRow.Click += (s, e) =>
            {
                if (_rightClickedRow < 0 || _displayList == null || _rightClickedRow >= _displayList.Count)
                    return;

                var item = _displayList[_rightClickedRow];
                if (item.IsSummary) return; // Không xóa dòng tổng

                var confirm = MessageBox.Show(
                    $"Xóa dòng: \"{item.TenHang}\"?",
                    "Xác nhận xóa",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirm == DialogResult.Yes)
                {
                    configProducts.Remove(item);

                    for (int i = 0; i < configProducts.Count; i++)
                        configProducts[i].STT = (i + 1).ToString();

                    UpdateHeaderSum();
                    UpdateConfigGrid();
                }
            };
            ctxCell.Items.Add(new ToolStripSeparator());
            ctxCell.Items.Add(miDeleteRow);

            dgvParentProducts.ContextMenuStrip = ctxCell;
            dgvParentProducts.CellMouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    // Chỉ ghi nhận ô right-click để làm màu khởi tạo cho dialog
                    // KHÔNG thay đổi CurrentCell để giữ nguyên selection nhiều ô
                    _rightClickedRow = e.RowIndex;
                    _rightClickedCol = e.ColumnIndex;

                    // Ẩn "Xóa dòng" nếu là dòng Summary (TỔNG CỘNG, VAT, THÀNH TIỀN)
                    bool isSummaryRow = false;
                    if (_displayList != null && e.RowIndex < _displayList.Count)
                        isSummaryRow = _displayList[e.RowIndex].IsSummary;

                    miDeleteRow.Visible = !isSummaryRow;
                }
            };
        }

        // ══════════════════════════════════════════════════════════════════

        private void DgvParentProducts_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _displayList.Count) return;
            var item = _displayList[e.RowIndex];
            
            if (item.IsHeader)
            {
                bool isCollapsed = _collapsedGroups.Contains(item.TenHang);
                string symbol = isCollapsed ? "+" : "-";

                Rectangle headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, dgvParentProducts.RowHeadersWidth, e.RowBounds.Height);
                
                int boxSize = 14;
                int x = headerBounds.Left + (headerBounds.Width - boxSize) / 2;
                int y = headerBounds.Top + (headerBounds.Height - boxSize) / 2;
                Rectangle boxRect = new Rectangle(x, y, boxSize, boxSize);

                e.Graphics.FillRectangle(Brushes.White, boxRect);
                e.Graphics.DrawRectangle(Pens.Black, boxRect);

                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                
                using (Font f = new Font("Consolas", 10, FontStyle.Bold))
                {
                    e.Graphics.DrawString(symbol, f, Brushes.Black, boxRect, sf);
                }
            }
        }

        private void DgvParentProducts_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex == -1 && e.RowIndex >= 0 && e.RowIndex < _displayList.Count)
            {
                var item = _displayList[e.RowIndex];

                if (item.IsHeader)
                {
                    if (_collapsedGroups.Contains(item.TenHang))
                        _collapsedGroups.Remove(item.TenHang);
                    else
                        _collapsedGroups.Add(item.TenHang);

                    dgvParentProducts.DataSource = null; // Bắt buộc DataGridView reset lại hiển thị thay vì chỉ gán lại List
                    UpdateConfigGrid();
                }
            }
        }

        // EVENT HANDLERS – DataGridView
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Sau khi binding xong, áp dụng style cho dgvParentProducts (danh sách cấu hình).</summary>
        private void DgvParentProducts_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            FormatConfigGrid(dgvParentProducts);
            AdjustDgvParentProductsRowHeights();
        }

        /// <summary>Sau khi binding xong ở grid sản phẩm (trái/phải), áp dụng style chung.</summary>
        private void Grid_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv != null)
            {
                FormatDataGridView(dgv);
                dgv.ClearSelection(); // Tránh auto-select dòng đầu tiên sau khi gán DataSource
            }
        }

        /// <summary>
        /// Commit ngay khi ô đang sửa thay đổi giá trị (tránh phải nhấn Enter thủ công).
        /// Cần thiết để checkbox IsSelected hoạt động mượt mà.
        /// </summary>
        private void Grid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv != null && dgv.IsCurrentCellDirty)
                dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        // ══════════════════════════════════════════════════════════════════
        // KẾT NỐI GOOGLE SHEETS
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Khởi tạo _sheetsService từ file config.json (Service Account credentials).
        /// Chỉ gọi một lần; các lần sau dùng lại instance đã có.
        /// </summary>
        private void InitGoogleSheetsService()
        {
            try
            {
                GoogleCredential credential;

                using (var stream = new FileStream("config.json", FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleCredential.FromStream(stream)
                        .CreateScoped(SheetsService.Scope.Spreadsheets);
                }


                _sheetsService = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "GSheetConfig",
                });
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show("Không tìm thấy file 'credentials.json'.\n\n" + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ex)
            {
                MessageBox.Show("Lỗi khi đọc file credentials.\n\n" + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Google.GoogleApiException ex)
            {
                MessageBox.Show("Lỗi xác thực với Google API.\n\n" + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi không xác định khi kết nối Google Sheets.\n\n" + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnChangeSheet_Click(object sender, EventArgs e)
        {
            if (_sheetsService == null) InitGoogleSheetsService();

            string selectedSheet = null;
            bool cancelled = false;

            using (var selector = new FrmSheetSelector(spreadsheetId, _sheetsService))
            {
                var result = selector.ShowDialog(this);
                if (result == DialogResult.OK && !string.IsNullOrEmpty(selector.SelectedSheetName))
                    selectedSheet = selector.SelectedSheetName;
                else
                    cancelled = true;
            }

            if (!cancelled)
            {
                await SetConfigSheet(selectedSheet);
            }
        }

        private void FrmConfig_Load(object sender, EventArgs e)
        {
            // Không gọi LoadDataAsync() ở đây vì FrmMain đã gọi trước đó.
            
            btn_baogia.Click += btn_baogia_Click;
            
            btnOpenSearchModal.Click += (s, ev) => OpenProductSearch(toConfigurationArea: true);
            btnOpenSearchModalForQuote.Click += (s, ev) => OpenProductSearch(toConfigurationArea: false);

            SetupProductManagementUI();
            button4.Click += BtnRemoveParent_Click;
            button3.Click += Button3_Click;
            button5.Click += Button5_Click;
            button6.Click += Button6_Click;
            button7.Click += Button7_Click;

            comboBox1.SelectedValueChanged -= ComboBox1_SelectedValueChanged;
            comboBox1.SelectedValueChanged += ComboBox1_SelectedValueChanged;

            lstSavedConfigs.Confirmed -= Button6_Click;
            lstSavedConfigs.Confirmed += Button6_Click;

            // Vẽ icon play (tam giác) lên button8
            SetPlayIcon(button8);

            // Gọi ngay để cột ▲▼ xuất hiện dù grid chưa có data
            EnsureMoveColumns(dgvParentProducts);

            // Thêm cột ▲▼ vào dataGridView1 (panel phải)
            EnsureMoveColumns(dataGridView1);

            // Xử lý click vào cột ▲▼ ngay trong grid (dùng CellMouseClick để biết vị trí chuột)
            dgvParentProducts.CellMouseClick += DgvParentProducts_MoveButtonCellClick;
            dataGridView1.CellMouseClick     += DataGridView1_MoveButtonCellClick;

            // Load danh sách Donggoi_ vào comboBox1
            _ = LoadDonggoiSheetsToComboAsync();
        }


        /// <summary>
        /// Tạo icon play (tam giác) vẽ bằng GDI+ và gán lên button.
        /// </summary>
        private static void SetPlayIcon(System.Windows.Forms.Button btn)
        {
            int w = btn.Width > 0 ? btn.Width : 32;
            int h = btn.Height > 0 ? btn.Height : 32;

            var bmp = new System.Drawing.Bitmap(w, h);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.Clear(System.Drawing.Color.Transparent);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Màu tam giác: xanh đậm (#1a3a5c)
                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(26, 58, 92)))
                {
                    // Tam giác play: căn giữa button, hơi lệch phải 1px cho cân thị giác
                    int margin = 8;
                    var triangle = new System.Drawing.Point[]
                    {
                        new System.Drawing.Point(margin + 1,          margin),
                        new System.Drawing.Point(w - margin + 1,      h / 2),
                        new System.Drawing.Point(margin + 1,          h - margin),
                    };
                    g.FillPolygon(brush, triangle);
                }
            }

            btn.Image = bmp;
            btn.ImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            btn.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            btn.Text = "";
        }

        /// <summary>
        /// Vẽ mũi tên ▲ hoặc ▼ lên button bằng GDI+.
        /// </summary>
        private static void SetArrowIcon(System.Windows.Forms.Button btn, bool isUp)
        {
            int w = btn.Width > 0 ? btn.Width : 22;
            int h = btn.Height > 0 ? btn.Height : 30;

            var bmp = new System.Drawing.Bitmap(w, h);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.Clear(System.Drawing.Color.Transparent);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(26, 58, 92)))
                {
                    int mx = w / 2;        // midX
                    int my = h / 2;        // midY
                    int half = w / 2 - 3;  // half-width of arrow
                    int tip = 6;           // height of arrow head

                    System.Drawing.Point[] arrow;
                    if (isUp)
                    {
                        arrow = new System.Drawing.Point[]
                        {
                            new System.Drawing.Point(mx,          my - tip),  // đỉnh
                            new System.Drawing.Point(mx + half,   my + tip),  // phải dưới
                            new System.Drawing.Point(mx - half,   my + tip),  // trái dưới
                        };
                    }
                    else
                    {
                        arrow = new System.Drawing.Point[]
                        {
                            new System.Drawing.Point(mx,          my + tip),  // đỉnh
                            new System.Drawing.Point(mx + half,   my - tip),  // phải trên
                            new System.Drawing.Point(mx - half,   my - tip),  // trái trên
                        };
                    }
                    g.FillPolygon(brush, arrow);
                }
            }

            btn.Image = bmp;
            btn.ImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            btn.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            btn.Text = "";
        }

        /// <summary>
        /// Cập nhật trạng thái enabled của 2 button lên/xuống dựa vào dòng đang chọn.
        /// </summary>
        private void UpdateMoveButtonsState()
        {
            int idx = GetSelectedConfigIndex();
            // Không chọn dòng nào, hoặc chọn dòng summary → disable cả 2
            bool valid = idx >= 0 && idx < configProducts.Count && !configProducts[idx].IsSummary;
        }

        /// <summary>
        /// Lấy index trong configProducts tương ứng với dòng đang được chọn trên dgvParentProducts.
        /// Trả về -1 nếu không có dòng nào được chọn hoặc dòng là summary.
        /// </summary>
        private int GetSelectedConfigIndex()
        {
            if (dgvParentProducts.SelectedRows.Count == 0) return -1;
            int displayIdx = dgvParentProducts.SelectedRows[0].Index;
            if (displayIdx < 0 || displayIdx >= _displayList.Count) return -1;
            var item = _displayList[displayIdx];
            if (item.IsSummary) return -1;
            return configProducts.IndexOf(item);
        }

        /// <summary>
        /// Di chuyển dòng trong configProducts lên (-1) hoặc xuống (+1).
        /// displayRowIndex: index dòng trên _displayList (từ CellClick). -1 = dùng dòng đang select.
        /// Dòng Header kéo theo toàn bộ nhóm sản phẩm bên dưới nó.
        /// </summary>
        private void MoveConfigRow(int direction, int displayRowIndex = -1)
        {
            // Lấy configIndex từ displayRowIndex (inline click) hoặc selection (toolbar)
            int idx;
            if (displayRowIndex >= 0)
            {
                if (displayRowIndex >= _displayList.Count) return;
                var clickedItem = _displayList[displayRowIndex];
                if (clickedItem.IsSummary) return;
                idx = configProducts.IndexOf(clickedItem);
            }
            else
            {
                idx = GetSelectedConfigIndex();
            }
            if (idx < 0) return;

            var item = configProducts[idx];

            // Chặn di chuyển nếu là dòng Pinned (Vỏ tủ, Đồng, Phụ kiện, Nhân công)
            if (ConfigProductItem.IsPinned(item.TenHang))
            {
                return;
            }

            if (item.IsHeader)
            {
                // Xác định nhóm (header + các sản phẩm bên dưới cho đến header kế tiếp)
                int groupEnd = idx + 1;
                while (groupEnd < configProducts.Count && !configProducts[groupEnd].IsHeader)
                    groupEnd++;
                // groupEnd là index KHÔNG thuộc nhóm (header kế hoặc hết list)

                int groupSize = groupEnd - idx;

                if (direction == -1) // lên
                {
                    if (idx == 0) return; // đã đầu
                    // Tìm header nhóm phía trên
                    int prevGroupStart = idx - 1;
                    while (prevGroupStart > 0 && !configProducts[prevGroupStart].IsHeader)
                        prevGroupStart--;

                    var group = configProducts.GetRange(idx, groupSize);
                    configProducts.RemoveRange(idx, groupSize);
                    configProducts.InsertRange(prevGroupStart, group);
                    idx = prevGroupStart; // vị trí mới của header
                }
                else // xuống
                {
                    if (groupEnd >= configProducts.Count) return; // đã cuối
                    // Tìm end của nhóm kế tiếp
                    int nextGroupEnd = groupEnd + 1;
                    while (nextGroupEnd < configProducts.Count && !configProducts[nextGroupEnd].IsHeader)
                        nextGroupEnd++;
                    int nextGroupSize = nextGroupEnd - groupEnd;

                    var nextGroup = configProducts.GetRange(groupEnd, nextGroupSize);
                    configProducts.RemoveRange(groupEnd, nextGroupSize);
                    configProducts.InsertRange(idx, nextGroup);
                    idx = idx + nextGroupSize; // vị trí mới của header đã di chuyển
                }
            }
            else
            {
                // Dòng sản phẩm thường: chỉ swap với dòng liền kề (không vượt qua header)
                int newIdx = idx + direction;
                if (newIdx < 0 || newIdx >= configProducts.Count) return;
                if (configProducts[newIdx].IsHeader) return; // không nhảy qua header

                var tmp = configProducts[idx];
                configProducts[idx] = configProducts[newIdx];
                configProducts[newIdx] = tmp;
                idx = newIdx;
            }

            // Cập nhật STT
            for (int i = 0; i < configProducts.Count; i++)
                configProducts[i].STT = (i + 1).ToString();

            UpdateHeaderSum();
            UpdateConfigGrid();

            // Giữ nguyên selection vào dòng vừa di chuyển
            int newDisplayIdx = _displayList.IndexOf(configProducts[idx]);
            if (newDisplayIdx >= 0 && newDisplayIdx < dgvParentProducts.Rows.Count)
            {
                dgvParentProducts.ClearSelection();
                dgvParentProducts.Rows[newDisplayIdx].Selected = true;
                dgvParentProducts.FirstDisplayedScrollingRowIndex = Math.Max(0, newDisplayIdx - 2);
            }

            UpdateMoveButtonsState();
        }

        /// <summary>
        /// Xử lý click vào cột ColMove: nửa trên ▲ = lên, nửa dưới ▼ = xuống.
        /// </summary>
        private void DgvParentProducts_MoveButtonCellClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (!dgvParentProducts.Columns.Contains("ColMove")) return;
            if (e.ColumnIndex != dgvParentProducts.Columns["ColMove"].Index) return;

            // Xác định click vào nửa trên hay nửa dưới của ô
            Rectangle cellBounds = dgvParentProducts.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            bool isUp = e.Y < cellBounds.Height / 2;

            MoveConfigRow(isUp ? -1 : +1, e.RowIndex);
        }

        /// <summary>
        /// Di chuyển item trong childProducts (dataGridView1) lên hoặc xuống.
        /// </summary>
        private void DataGridView1_MoveButtonCellClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (!dataGridView1.Columns.Contains("ColMove")) return;
            if (e.ColumnIndex != dataGridView1.Columns["ColMove"].Index) return;

            Rectangle cellBounds = dataGridView1.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            bool isUp = e.Y < cellBounds.Height / 2;
            int direction = isUp ? -1 : +1;

            int idx = e.RowIndex;
            int newIdx = idx + direction;
            if (newIdx < 0 || newIdx >= childProducts.Count) return;

            // Swap trong childProducts (BindingList)
            var currentItem = childProducts[idx];
            var targetItem = childProducts[newIdx];

            // Chặn di chuyển nếu một trong hai là dòng Pinned
            if (ConfigProductItem.IsPinned(currentItem.Name) || ConfigProductItem.IsPinned(targetItem.Name))
            {
                return;
            }

            childProducts[idx]    = targetItem;
            childProducts[newIdx] = currentItem;

            // Giữ selection vào dòng vừa di chuyển
            dataGridView1.ClearSelection();
            if (newIdx >= 0 && newIdx < dataGridView1.Rows.Count)
            {
                dataGridView1.Rows[newIdx].Selected = true;
                dataGridView1.FirstDisplayedScrollingRowIndex = Math.Max(0, newIdx - 2);
            }
        }

        private void Button6_Click(object sender, EventArgs e)
        {
            // Tìm vị trí của các Header được chọn
            var checkedItems = lstSavedConfigs.CheckedItems.Cast<string>().ToList();
            if (checkedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng tích chọn ít nhất một cấu hình để tải!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // configProducts.Clear(); // Bỏ Clear để thực hiện cộng dồn (Merge) theo yêu cầu
            int totalAdded = 0;

            foreach (string selectedHeaderName in checkedItems)
            {
                // Tránh nạp trùng cấu hình đã có trong bảng hiện tại
                if (configProducts.Any(p => p.IsHeader && string.Equals(p.TenHang?.Trim(), selectedHeaderName, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                int headerIndex = allSavedConfigs.FindIndex(c =>
                    c.IsHeader &&
                    string.Equals(c.TenHang?.Trim(), selectedHeaderName, StringComparison.OrdinalIgnoreCase));

                if (headerIndex >= 0)
                {
                    // Thêm dòng Header của cấu hình này (Clone để độc lập STT)
                    configProducts.Add(allSavedConfigs[headerIndex].Clone());

                    // Thêm các sản phẩm thuộc cấu hình này
                    for (int i = headerIndex + 1; i < allSavedConfigs.Count; i++)
                    {
                        if (allSavedConfigs[i].IsHeader) break;
                        configProducts.Add(allSavedConfigs[i].Clone());
                        totalAdded++;
                    }
                }
            }


            // Cập nhật lại STT cho toàn bộ danh sách mới
            for (int i = 0; i < configProducts.Count; i++)
            {
                configProducts[i].STT = (i + 1).ToString();
            }

            UpdateHeaderSum();
            UpdateConfigGrid();

            // Reset editing state since we may have multiple configs now
            currentEditingConfigName = checkedItems.Count == 1 ? checkedItems[0] : null;
            button5.Text = checkedItems.Count == 1 ? "Cập nhật" : "Lưu";

            MessageBox.Show($"Đã nạp {totalAdded} sản phẩm từ {checkedItems.Count} cấu hình được chọn!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


        // ══════════════════════════════════════════════════════════════════
        // NẠP DỮ LIỆU (LOAD DATA)
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Nạp toàn bộ dữ liệu: trước nạp từ cache (hiện ngay), sau đó nạp song song từ mạng.
        /// Bao gồm: danh sách cấu hình, sản phẩm, quan hệ PR, cấu hình đã lưu.
        /// </summary>
        public async Task LoadDataAsync()
        {
            if (_sheetsService == null) InitGoogleSheetsService();

            // 1. NẠP TOÀN BỘ TỪ CACHE (HIỆN LÊN TỨC THÌ)
            // Nạp Tên Cấu Hình
            var cachedConfigNames = LoadFromCache<List<string>>("config_names");
            if (cachedConfigNames != null)
            {
                lstSavedConfigs.ClearItems();
                foreach (var n in cachedConfigNames) lstSavedConfigs.AddItem(n);
            }
            // Nạp Sản Phẩm
            var cachedProducts = LoadFromCache<List<Products>>("all_products");
            if (cachedProducts != null)
            {
                allProducts.Clear();
                allProducts.AddRange(cachedProducts);
                UpdateFiltersFromProducts(allProducts); // Cập nhật Hãng & Danh mục từ cache
            }
            // Nạp Quan hệ
            var cachedRelations = LoadFromCache<List<RelationItem>>("product_relations");
            if (cachedRelations != null)
            {
                productRelations.Clear();
                productRelations.AddRange(cachedRelations);
                UpdateProductRelationCombo();
            }

            try
            {
                // 2. NẠP SONG SONG TỪ GOOGLE SHEETS (GIẢM DELAY)
                var configNamesTask = FetchConfigNamesAsync();
                var productsTask = FetchAllProductsAsync();
                var relationsTask = FetchProductRelationsAsync();
                var savedConfigsFullTask = FetchSavedConfigsFullDataAsync();

                await Task.WhenAll(configNamesTask, productsTask, relationsTask, savedConfigsFullTask);

                // Sau khi nạp xong mạng, dữ liệu sẽ được cập nhật và lưu vào cache trong từng hàm con
                UpdateHeaderSum();
                UpdateConfigGrid();
                // dataGridView1.DataSource is already bound to childProducts in constructor
            }
            catch (Exception ex)
            {
                // Lỗi mạng không quan trọng vì đã có Cache hiển thị
                Console.WriteLine($"LoadDataAsync network error: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy danh sách tên cấu hình đã lưu (header dòng IsHeader=1) từ Google Sheets.
        /// Cập nhật lstSavedConfigs và lưu vào cache.
        /// </summary>
        private async Task FetchConfigNamesAsync()
        {
            if (string.IsNullOrEmpty(configSheetName)) return;
            try
            {
                // Lấy cột A→F để có SoLuong (cột F, index 5) xác định đây là header
                var response = await _sheetsService.Spreadsheets.Values.Get(spreadsheetId, $"{configSheetName}!A2:F").ExecuteAsync();
                if (response.Values != null)
                {
                    var freshNames = response.Values
                        .Where(r => 
                        {
                            if (r.Count <= 4 || r[4]?.ToString()?.Trim() != "TỦ") return false;
                            bool hasSl = r.Count > 5 && !string.IsNullOrWhiteSpace(r[5]?.ToString());
                            string maHang = r.Count > 2 ? r[2]?.ToString()?.Trim() : "";
                            return !hasSl && string.IsNullOrEmpty(maHang)
                                && !string.IsNullOrEmpty(r[1]?.ToString())
                                && !r[1].ToString().StartsWith("--");
                        })
                        .Select(r => r[1].ToString())
                        .Distinct().ToList();

                    if (freshNames.Count > 0)
                    {
                        var currentNames = lstSavedConfigs.Items.Cast<object>().Select(x => x.ToString()).ToList();
                        if (!freshNames.SequenceEqual(currentNames))
                        {
                            this.Invoke((MethodInvoker)delegate {
                                lstSavedConfigs.ClearItems();
                                foreach (var name in freshNames) lstSavedConfigs.AddItem(name);
                            });
                            SaveToCache("config_names", freshNames);
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Lấy toàn bộ sản phẩm từ sheet Products_Table (cột A→K).
        /// Sau khi nạp: cập nhật dữ liệu.
        /// </summary>
        private async Task FetchAllProductsAsync()
        {
            try
            {
                var response = await _sheetsService.Spreadsheets.Values.Get(spreadsheetId, $"{sheetName}!A2:R").ExecuteAsync();
                if (response.Values != null && response.Values.Count > 0)
                {
                    var newProducts = new List<Products>();
                    for (int i = 0; i < response.Values.Count; i++)
                    {
                        var row = response.Values[i];
                        if (row.Count < 2) continue;
                        newProducts.Add(new Products
                        {
                            Id = (row.Count > 0 && int.TryParse(row[0]?.ToString(), out int id)) ? id : i + 1,
                            Name = row.Count > 1 ? row[1]?.ToString() : "",
                            Model = row.Count > 2 ? row[2]?.ToString() : "",
                            SKU = row.Count > 3 ? row[3]?.ToString() : "",
                            Price = row.Count > 4 ? row[4]?.ToString() : "0",
                            PriceCost = row.Count > 5 ? row[5]?.ToString() : "0",
                            Weight = row.Count > 6 ? row[6]?.ToString() : "0",
                            Width = row.Count > 7 ? row[7]?.ToString() : "0",
                            Height = row.Count > 8 ? row[8]?.ToString() : "0",
                            Length = row.Count > 9 ? row[9]?.ToString() : "0",
                            Category = row.Count > 10 ? row[10]?.ToString() : "",
                            Type = row.Count > 11 ? row[11]?.ToString() : "",
                            HÃNG = row.Count > 12 ? row[12]?.ToString() : "",
                            TrangThai = row.Count > 13 ? row[13]?.ToString() : "",
                            Pole = row.Count > 14 ? row[14]?.ToString() : "",
                            Ir = row.Count > 15 ? row[15]?.ToString() : "",
                            Icu = row.Count > 16 ? row[16]?.ToString() : "",
                            PriceList = row.Count > 17 ? row[17]?.ToString() : ""
                        });
                    }
                    allProducts.Clear();
                    allProducts.AddRange(newProducts);
                    this.Invoke((MethodInvoker)delegate {
                        UpdateFiltersFromProducts(allProducts);
                    });
                    SaveToCache("all_products", allProducts);
                }
            }
            catch { }
        }

        /// <summary>
        /// Xây dựng cây danh mục và danh sách hãng từ dữ liệu sản phẩm.
        /// Được gọi sau khi nạp sản phẩm để cập nhật các bộ lọc.
        /// </summary>
        private void UpdateFiltersFromProducts(List<Products> products)
        {
            var rawCategories = products.Select(p => p.Category).Where(c => !string.IsNullOrEmpty(c)).ToList();

            // ── CategoryTreeDropdown mới (đa cấp đệ quy) ──
            var treeNodes = CategoryParser.ParseToTreeNodes(rawCategories);

            // Giữ lại categoryTree cũ để tương thích các chỗ khác vẫn còn dùng
            categoryTree = CategoryParser.ParseToTree(rawCategories);
            categoryTree.Insert(0, new CategoryItem { DisplayText = "-- Tất cả danh mục --", FullPath = "" });
        }

        /// <summary>
        /// Lấy danh sách quan hệ sản phẩm chính – con từ sheet Products_Relatation.
        /// Cập nhật productRelations và comboBox2/comboBox1.
        /// </summary>
        private async Task FetchProductRelationsAsync()
        {
            try
            {
                var response = await _sheetsService.Spreadsheets.Values.Get(spreadsheetId, "Products_Relatation!A2:E").ExecuteAsync();
                if (response.Values != null)
                {
                    var newRelations = new List<RelationItem>();
                    foreach (var row in response.Values)
                    {
                        if (row.Count < 3) continue;
                        int.TryParse(row[1]?.ToString(), out int mainId);
                        int.TryParse(row[2]?.ToString(), out int childId);
                        newRelations.Add(new RelationItem { ID_Product_Main = mainId, ID_Product_Child = childId, Category_PR = row.Count > 3 ? row[3]?.ToString() : "" });
                    }
                    productRelations.Clear();
                    productRelations.AddRange(newRelations);
                    this.Invoke((MethodInvoker)delegate { UpdateProductRelationCombo(); });
                    SaveToCache("product_relations", productRelations);
                }
            }
            catch { }
        }

        /// <summary>
        /// Cập nhật comboBox2 (Sản phẩm chính) và comboBox1 (Danh mục PR)
        /// dựa trên dữ liệu quan hệ hiện tại.
        /// </summary>
        private void UpdateProductRelationCombo()
        {
            // CHỈ lấy ID_Product_Main (Sản phẩm chính) để tránh hiển thị linh kiện con
            var mainProductIds = productRelations.Select(r => r.ID_Product_Main).Distinct().ToList();
            var mainProductsDisplay = allProducts
                .Where(p => mainProductIds.Contains(p.Id))
                .Select(p => new { Id = p.Id, Name = p.Name })
                .OrderBy(p => p.Name).ToList();

            mainProductsDisplay.Insert(0, new { Id = 0, Name = "-- Chọn sản phẩm --" });

            var catPRs = productRelations.Select(r => r.Category_PR).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
            catPRs.Insert(0, "-- Tất cả danh mục --");
            // comboBox1 đã được dùng để hiển thị Donggoi_ sheets — không ghi đè tại đây
        }

        /// <summary>
        /// Load tất cả Donggoi_ sheets từ Google Sheets vào comboBox1,
        /// hiển thị format: "Donggoi_1 - tên cấu hình 1, tên cấu hình 2".
        /// </summary>
        private async Task LoadDonggoiSheetsToComboAsync()
        {
            try
            {
                if (_sheetsService == null) InitGoogleSheetsService();

                var spreadsheet = await _sheetsService.Spreadsheets.Get(spreadsheetId).ExecuteAsync();
                var donggoiSheetNames = spreadsheet.Sheets
                    .Select(s => s.Properties.Title)
                    .Where(t => t.StartsWith("Donggoi_"))
                    .OrderBy(t => t)
                    .ToList();

                var displayItems = new List<string> { "-- Chọn cấu hình đóng gói --" };

                foreach (var sName in donggoiSheetNames)
                {
                    try
                    {
                        var resp = await _sheetsService.Spreadsheets.Values
                            .Get(spreadsheetId, $"{sName}!A2:B100").ExecuteAsync();
                        var rows = resp.Values;
                        var groupNames = new List<string>();
                        if (rows != null)
                        {
                            foreach (var row in rows)
                            {
                                string col0 = row.Count > 0 ? row[0]?.ToString() ?? "" : "";
                                string col1 = row.Count > 1 ? row[1]?.ToString() ?? "" : "";
                                if (!string.IsNullOrEmpty(col0) && string.IsNullOrEmpty(col1))
                                    groupNames.Add(col0);
                            }
                        }

                        if (groupNames.Count > 0)
                        {
                            foreach (var gn in groupNames)
                                displayItems.Add($"{sName} - {gn}");
                        }
                        else
                        {
                            displayItems.Add(sName);
                        }
                    }
                    catch { displayItems.Add(sName); }
                }

                if (InvokeRequired)
                    Invoke(new Action(() => RefreshComboBox1(displayItems)));
                else
                    RefreshComboBox1(displayItems);
            }
            catch { /* Không crash nếu chưa connect được Sheets */ }
        }

        private void RefreshComboBox1(List<string> items)
        {
            comboBox1.SelectedValueChanged -= ComboBox1_SelectedValueChanged;
            comboBox1.DataSource = items;
            comboBox1.SelectedIndex = 0;
            comboBox1.SelectedValueChanged += ComboBox1_SelectedValueChanged;
        }

        // ── Màu đọc từ Google Sheet (dùng để khôi phục màu khi load cấu hình) ──
        // Key = (sheetRowIndex 0-based, sheetColIndex 0-based); Value = màu tương ứng
        private Dictionary<(int r, int c), Color> _sheetBgColors = new Dictionary<(int, int), Color>(); // màu nền
        private Dictionary<(int r, int c), Color> _sheetFgColors = new Dictionary<(int, int), Color>(); // màu chữ

        /// <summary>
        /// Lấy toàn bộ dữ liệu cấu hình đã lưu (bao gồm allSavedConfigs)
        /// và sau đó đọc cả thông tin màu sắc từng ô của sheet.
        /// </summary>
        private async Task FetchSavedConfigsFullDataAsync()
        {
            if (string.IsNullOrEmpty(configSheetName)) return;
            try
            {
                var response = await _sheetsService.Spreadsheets.Values.Get(spreadsheetId, $"{configSheetName}!A2:M").ExecuteAsync();
                if (response.Values != null)
                {
                    var newSavedItems = new List<ConfigProductItem>();
                    for (int i = 0; i < response.Values.Count; i++)
                    {
                        var row = response.Values[i];
                        if (row.Count < 2) continue;
                        string tenHang = row[1]?.ToString()?.Trim() ?? "";
                        if (tenHang.StartsWith("TỔNG CỘNG") || tenHang.StartsWith("THUẾ VAT") || tenHang.StartsWith("THÀNH TIỀN")) continue;

                        Func<string, decimal> parseCurrency = (s) => {
                            if (string.IsNullOrEmpty(s)) return 0;
                            string clean = s.Replace(".", "").Replace(",", "").Replace("₫", "").Trim();
                            decimal.TryParse(clean, out decimal res);
                            return res;
                        };

                        newSavedItems.Add(new ConfigProductItem
                        {
                            STT = ((row.Count > 0 && int.TryParse(row[0]?.ToString(), out int stt)) ? stt : i + 1).ToString(),
                            TenHang = tenHang,
                            MaHang = row.Count > 2 ? row[2]?.ToString() : "",
                            XuatXu = row.Count > 3 ? row[3]?.ToString() : "",
                            DonVi = row.Count > 4 ? row[4]?.ToString() : "",
                            SoLuong = (row.Count > 5 && int.TryParse(row[5]?.ToString(), out int sl)) ? sl : 0,
                            DonGiaVND = parseCurrency(row.Count > 6 ? row[6]?.ToString() : "0"),
                            ThanhTienVND = parseCurrency(row.Count > 7 ? row[7]?.ToString() : "0"),
                            GhiChu = row.Count > 8 ? row[8]?.ToString() : "",
                            GiaNhap = parseCurrency(row.Count > 9 ? row[9]?.ToString() : "0"),
                            ThanhTien = parseCurrency(row.Count > 10 ? row[10]?.ToString() : "0"),
                            LoiNhuan = parseCurrency(row.Count > 11 ? row[11]?.ToString() : "0"),
                            BangGia = parseCurrency(row.Count > 12 ? row[12]?.ToString() : "0"),
                            IsHeader = (row.Count > 4 && row[4]?.ToString()?.Trim() == "TỦ") 
                                       && !(row.Count > 5 && !string.IsNullOrWhiteSpace(row[5]?.ToString())) 
                                       && string.IsNullOrEmpty(row.Count > 2 ? row[2]?.ToString()?.Trim() : ""),
                            SheetRowIndex = i // Lưu vị trí dòng trên sheet (0-based, tương ứng row 2+)
                        });
                    }
                    allSavedConfigs.Clear();
                    allSavedConfigs.AddRange(newSavedItems);
                }

                // Đọc formatting (màu nền, màu chữ) từ Google Sheet
                await FetchSheetFormattingAsync();
            }
            catch { }
        }

        /// <summary>
        /// Đọc màu nền và màu chữ của từng ô trong config sheet.
        /// Kết quả lưu vào _sheetBgColors và _sheetFgColors để áp dụng lên DGV.
        /// </summary>
        private async Task FetchSheetFormattingAsync()
        {
            try
            {
                var getRequest = _sheetsService.Spreadsheets.Get(spreadsheetId);
                getRequest.Ranges = new[] { $"{configSheetName}!A2:L1000" };
                getRequest.IncludeGridData = true;
                var spreadsheet = await getRequest.ExecuteAsync();

                _sheetBgColors.Clear();
                _sheetFgColors.Clear();

                var sheet = spreadsheet.Sheets?.FirstOrDefault();
                if (sheet?.Data == null || sheet.Data.Count == 0) return;

                var gridData = sheet.Data[0];
                if (gridData.RowData == null) return;

                for (int r = 0; r < gridData.RowData.Count; r++)
                {
                    var rowData = gridData.RowData[r];
                    if (rowData.Values == null) continue;

                    for (int c = 0; c < rowData.Values.Count && c < 12; c++)
                    {
                        var cell = rowData.Values[c];
                        if (cell?.UserEnteredFormat == null) continue;

                        // Màu nền
                        var bg = cell.UserEnteredFormat.BackgroundColor;
                        if (bg != null)
                        {
                            int red = (int)((bg.Red ?? 1f) * 255);
                            int green = (int)((bg.Green ?? 1f) * 255);
                            int blue = (int)((bg.Blue ?? 1f) * 255);
                            // Bỏ qua trắng (mặc định)
                            if (!(red >= 250 && green >= 250 && blue >= 250))
                                _sheetBgColors[(r, c)] = Color.FromArgb(red, green, blue);
                        }

                        // Màu chữ
                        var fg = cell.UserEnteredFormat.TextFormat?.ForegroundColor;
                        if (fg != null)
                        {
                            int red = (int)((fg.Red ?? 0f) * 255);
                            int green = (int)((fg.Green ?? 0f) * 255);
                            int blue = (int)((fg.Blue ?? 0f) * 255);
                            // Bỏ qua đen (mặc định)
                            if (!(red <= 5 && green <= 5 && blue <= 5))
                                _sheetFgColors[(r, c)] = Color.FromArgb(red, green, blue);
                        }
                    }
                }
            }
            catch { }
        }

        // ══════════════════════════════════════════════════════════════════
        // ĐỊNH DẠNG GIAO DIỆN (FORMAT / STYLE)
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Định dạng grid sản phẩm (dataGridView1):
        /// ẩn cột không cần thiết, đặt header text, chỉ cho phép sửa checkbox IsSelected.
        /// </summary>
        private void FormatDataGridView(DataGridView dgv)
        {
            if (dgv == null || dgv.IsDisposed || dgv.Columns == null || dgv.Columns.Count == 0) return;

            try
            {
                dgv.EnableHeadersVisualStyles = false;
                dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.Yellow,
                    ForeColor = Color.FromArgb(31, 73, 125),
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                };
                dgv.ColumnHeadersHeight = 40;
                dgv.RowHeadersVisible = false;
                dgv.BackgroundColor = Color.White;
                dgv.BorderStyle = BorderStyle.FixedSingle;
                dgv.GridColor = Color.LightGray;

                var cols = dgv.Columns.Cast<DataGridViewColumn>().ToList();

                foreach (var col in cols)
                {
                    if (col == null || col.DataGridView == null) continue;
                    string colName = col.Name;

                    // 1. Hide unwanted columns
                    if (colName == "Weight" || colName == "Length" || colName == "Width" || colName == "Height" || colName == "PriceList" || colName == "SheetRowIndex")
                    {
                        col.Visible = false;
                        continue;
                    }

                    if (colName == "ColMove")
                    {
                        col.Visible = true;
                        col.DisplayIndex = 0;
                        col.HeaderText = "";
                        continue;
                    }

                    col.Visible = true;

                    // 2. Set headers and format
                    if (colName == "Id")
                    {
                        col.HeaderText = "STT";
                        col.DisplayIndex = 1;
                        col.FillWeight = 30; // Giảm cực nhỏ vì chỉ có số STT
                    }
                    else if (colName == "Name")
                    {
                        col.HeaderText = "Tên sản phẩm";
                        col.DisplayIndex = 2;
                        col.FillWeight = 320; // Tăng cực lớn để hiện trọn vẹn tên sp
                        // Bật WrapMode để multiline (Vỏ tủ điện...) hiển thị đúng
                        col.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                    }
                    else if (colName == "Model")
                    {
                        col.HeaderText = "Model";
                        col.DisplayIndex = 3;
                        col.FillWeight = 80; // Tối ưu lại
                    }
                    else if (colName == "SKU")
                    {
                        col.HeaderText = "Mã SKU";
                        col.DisplayIndex = 4;
                        col.FillWeight = 80; // Tối ưu lại
                    }
                    else if (colName == "Price")
                    {
                        col.HeaderText = "Giá bán";
                        col.DefaultCellStyle.Format = "N0";
                        col.DisplayIndex = 5;
                        col.FillWeight = 75; // Tối ưu lại
                    }
                    else if (colName == "PriceCost")
                    {
                        col.HeaderText = "Giá nhập";
                        col.DefaultCellStyle.Format = "N0";
                        col.DisplayIndex = 6;
                        col.FillWeight = 75; // Tối ưu lại
                    }
                    else if (colName == "Category")
                    {
                        col.HeaderText = "Danh mục";
                        col.DisplayIndex = 7;
                        col.FillWeight = 90; // Tối ưu lại
                    }
                    else if (colName == "Type")
                    {
                        col.HeaderText = "Type";
                        col.DisplayIndex = 8;
                        col.FillWeight = 70; // Tối ưu lại
                    }
                    else if (colName == "HÃNG")
                    {
                        col.HeaderText = "Hãng";
                        col.DisplayIndex = 9;
                        col.FillWeight = 55; // Tối ưu lại
                    }
                    else if (colName == "SoLuong")
                    {
                        if (dgv == dataGridView1)
                        {
                            col.HeaderText = "Số lượng";
                            col.ReadOnly = false;
                            col.DisplayIndex = 10;
                            col.FillWeight = 55; // Giảm nhẹ để dồn chỗ cho Name
                            col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                            col.DefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                            col.DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
                            col.DefaultCellStyle.ForeColor = Color.DarkGreen;
                        }
                        else
                        {
                            col.Visible = false;
                        }
                    }
                    else if (colName == "IsSelected")
                    {
                        if (dgv == dataGridView1)
                        {
                            col.HeaderText = "";
                            col.ReadOnly = false;
                            col.DisplayIndex = 11;
                            col.FillWeight = 40;
                        }
                        else
                        {
                            col.Visible = false;
                        }
                    }
                    else
                    {
                        col.Visible = false;
                    }

                    if (colName != "IsSelected" && colName != "ColMove" && colName != "Price" && colName != "PriceCost" && colName != "SoLuong")
                    {
                        col.ReadOnly = true;
                    }
                    else if (colName == "Price" || colName == "PriceCost" || colName == "SoLuong")
                    {
                        col.ReadOnly = false;
                    }
                }

                dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgv.MultiSelect = true;
            }
            catch (Exception) { }
        }
        // ══════════════════════════════════════════════════════════════════
        // BỘ LỌC QUAN HỆ (COMBO SẢN PHẨM CHÍNH – DANH MỤC PR)
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Khi đổi sản phẩm chính (comboBox2), lọc lại danh sách Danh mục PR (comboBox1)
        /// và cập nhật grid sản phẩm con (dataGridView1) tương ứng.
        /// </summary>
        private void ComboBox2_SelectedValueChanged(object sender, EventArgs e)
        {
            if (isUpdatingComboBoxes) return;

            int selectedId = 0;

            isUpdatingComboBoxes = true;
            try
            {
                if (selectedId > 0)
                {
                    var catPRs = productRelations
                        .Where(r => r.ID_Product_Main == selectedId)
                        .Select(r => r.Category_PR)
                        .Where(c => !string.IsNullOrEmpty(c))
                        .Distinct()
                        .ToList();

                    catPRs.Insert(0, "-- Tất cả danh mục --");
                    string currentCat = comboBox1.SelectedItem?.ToString();

                    comboBox1.DataSource = catPRs;

                    if (catPRs.Contains(currentCat))
                        comboBox1.SelectedItem = currentCat;
                }
                else
                {
                    var catPRs = productRelations.Select(r => r.Category_PR).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
                    catPRs.Insert(0, "-- Tất cả danh mục --");
                    string currentCat = comboBox1.SelectedItem?.ToString();

                    comboBox1.DataSource = catPRs;

                    if (catPRs.Contains(currentCat))
                        comboBox1.SelectedItem = currentCat;
                }
            }
            finally
            {
                isUpdatingComboBoxes = false;
            }
        }

        /// <summary>
        /// Khi đổi Danh mục PR (comboBox1), lọc lại danh sách sản phẩm chính (comboBox2)
        /// cho phù hợp và cập nhật grid sản phẩm con.
        /// </summary>
        private void ComboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            if (isUpdatingComboBoxes) return;

            isUpdatingComboBoxes = true;
            try
            {
                string selectedCatPR = comboBox1.SelectedItem?.ToString();

                var relationProductIds = productRelations.Select(r => r.ID_Product_Main)
                    .Distinct()
                    .ToList();

                if (!string.IsNullOrEmpty(selectedCatPR) && selectedCatPR != "-- Tất cả danh mục --")
                {
                    relationProductIds = productRelations
                        .Where(r => string.Equals(r.Category_PR?.Trim(), selectedCatPR.Trim(), StringComparison.OrdinalIgnoreCase))
                        .Select(r => r.ID_Product_Main)
                        .Distinct()
                        .ToList();
                }

                var relationProductsDisplay = allProducts
                    .Where(p => relationProductIds.Contains(p.Id))
                    .Select(p => new { Id = p.Id, Name = p.Name })
                    .OrderBy(p => p.Name)
                    .ToList();
                relationProductsDisplay.Insert(0, new { Id = 0, Name = "-- Chọn sản phẩm --" });
            }
            finally
            {
                isUpdatingComboBoxes = false;
            }
        }

        private async void BtnAddFromRelation_Click(object sender, EventArgs e)
        {
            string selectedPkg = comboBox1.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedPkg) || selectedPkg == "-- Chọn cấu hình đóng gói --") return;

            try
            {
                this.Cursor = Cursors.WaitCursor;

                // Tách Tên sheet và Tên cấu hình (ví dụ: "Donggoi_1 - tủ điện" -> sName="Donggoi_1", pkgName="tủ điện")
                string sName = selectedPkg;
                string pkgName = "";
                int splitIdx = selectedPkg.IndexOf(" - ");
                if (splitIdx > 0)
                {
                    sName = selectedPkg.Substring(0, splitIdx);
                    pkgName = selectedPkg.Substring(splitIdx + 3);
                }

                if (_sheetsService == null) InitGoogleSheetsService();

                // Đọc dữ liệu từ Sheet
                var resp = await _sheetsService.Spreadsheets.Values.Get(spreadsheetId, $"{sName}!A2:I2000").ExecuteAsync();
                var rows = resp.Values ?? new List<IList<object>>();

                var foundProducts = new List<Products>();
                bool inTargetGroup = false;

                foreach (var row in rows)
                {
                    if (row.Count == 0) continue;
                    string col0 = row[0]?.ToString() ?? "";
                    string col1 = row.Count > 1 ? row[1]?.ToString() ?? "" : "";

                    bool isGroupHeader = !string.IsNullOrEmpty(col0) && string.IsNullOrEmpty(col1);

                    if (isGroupHeader)
                    {
                        // Nếu đúng nhóm cần tìm -> bật flag, nếu sang nhóm khác -> tắt flag (thoát)
                        if (string.Equals(col0.Trim(), pkgName.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            inTargetGroup = true;
                            continue;
                        }
                        else if (inTargetGroup)
                        {
                            break; // Đã đọc xong nhóm target
                        }
                    }
                    else if (inTargetGroup)
                    {
                        // Là dòng sản phẩm của nhóm cần tìm
                        int id = 0; int.TryParse(col0, out id);
                        string ten = col1;
                        string model = row.Count > 2 ? row[2]?.ToString() ?? "" : "";
                        string sku = row.Count > 3 ? row[3]?.ToString() ?? "" : "";
                        string price = row.Count > 4 ? row[4]?.ToString() ?? "0" : "0";
                        string cost = row.Count > 5 ? row[5]?.ToString() ?? "0" : "0";
                        string cat = row.Count > 6 ? row[6]?.ToString() ?? "" : "";
                        string hang = row.Count > 7 ? row[7]?.ToString() ?? "" : "";
                        int soLuong = 1; int.TryParse(row.Count > 8 ? row[8]?.ToString() : "1", out soLuong);
                        if (soLuong <= 0) soLuong = 1;

                        // Cố gắng map ID gốc nếu có trong allProducts, nếu không thì tạo mới
                        // CLONE để không mutate allProducts gốc
                        Products existing = null;
                        if (!string.IsNullOrWhiteSpace(sku) || id > 0)
                        {
                            existing = allProducts.FirstOrDefault(p =>
                                (!string.IsNullOrWhiteSpace(sku) && p.SKU == sku) ||
                                (id > 0 && p.Id == id));
                        }

                        if (existing != null)
                        {
                            // Clone để tránh sửa object gốc trong allProducts
                            foundProducts.Add(new Products
                            {
                                Id = existing.Id, Name = existing.Name, Model = existing.Model,
                                SKU = existing.SKU, Price = existing.Price, PriceCost = existing.PriceCost,
                                Category = existing.Category, HÃNG = existing.HÃNG,
                                Type = existing.Type, PriceList = existing.PriceList,
                                SoLuong = soLuong, IsSelected = false
                            });
                        }
                        else
                        {
                            foundProducts.Add(new Products
                            {
                                Id = id, Name = ten, Model = model, SKU = sku,
                                Price = price, PriceCost = cost, Category = cat, HÃNG = hang,
                                SoLuong = soLuong, IsSelected = false
                            });
                        }
                    }
                }

                if (foundProducts.Count > 0)
                {
                    lastSearchedSheet = sName;
                    lastSearchedPkg = pkgName;

                    childProducts.Clear();
                    foreach (var p in foundProducts) childProducts.Add(p);

                    UpdateConfigGrid(); // Update the dataGridView1
                    AdjustDataGridView1RowHeights(); // Điều chỉnh chiều cao dòng multiline
                    MessageBox.Show($"Đã nạp {foundProducts.Count} sản phẩm từ gói \"{pkgName}\"!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Không tìm thấy sản phẩm nào trong gói này.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải dữ liệu gói: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private async void Button3_Click(object sender, EventArgs e)
        {
            // Nếu không có sản phẩm nào trong childProducts, hỏi user xác nhận
            bool hasProducts = childProducts.Any();
            if (!hasProducts)
            {
                var confirm = MessageBox.Show(
                    "Danh sách sản phẩm chưa có gì.\nBạn vẫn muốn mở form đóng gói?",
                    "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes) return;
            }

            try
            {
                this.Cursor = Cursors.WaitCursor;
                if (_sheetsService == null) InitGoogleSheetsService();

                // Lấy danh sách Sheet hiện tại bắt đầu bằng Donggoi_
                var spreadsheet = await _sheetsService.Spreadsheets.Get(spreadsheetId).ExecuteAsync();
                var donggoiSheetNames = spreadsheet.Sheets
                    .Select(s => s.Properties.Title)
                    .Where(t => t.StartsWith("Donggoi_"))
                    .ToList();

                // Với mỗi Donggoi_ sheet, đọc các dòng header nhóm (cột A có giá trị, cột B rỗng)
                // để hiển thị format: "Donggoi_1 - nhóm A", "Donggoi_1 - nhóm B"
                var sheetDisplayMap = new Dictionary<string, string>(); // key=displayLabel, value=sheetName
                sheetDisplayMap["-- Tạo sheet mới --"] = "";

                foreach (var sName in donggoiSheetNames)
                {
                    try
                    {
                        var resp = await _sheetsService.Spreadsheets.Values
                            .Get(spreadsheetId, $"{sName}!A2:B100").ExecuteAsync();
                        var rows = resp.Values;
                        var groupNames = new List<string>();
                        if (rows != null)
                        {
                            foreach (var row in rows)
                            {
                                string col0 = row.Count > 0 ? row[0]?.ToString() ?? "" : "";
                                string col1 = row.Count > 1 ? row[1]?.ToString() ?? "" : "";
                                if (!string.IsNullOrEmpty(col0) && string.IsNullOrEmpty(col1))
                                    groupNames.Add(col0);
                            }
                        }

                        if (groupNames.Any())
                        {
                            foreach (var gn in groupNames)
                                sheetDisplayMap[$"{sName} - {gn}"] = sName;
                        }
                        else
                        {
                            sheetDisplayMap[sName] = sName;
                        }
                    }
                    catch { sheetDisplayMap[sName] = sName; }
                }

                this.Cursor = Cursors.Default;

                // Chuyển childProducts sang ConfigProductItem để hiển thị trong modal preview
                var childItemsForModal = childProducts.Select(p =>
                {
                    decimal price = 0; decimal.TryParse(p.Price?.Replace(".", "").Replace(",", ""), out price);
                    decimal priceCost = 0; decimal.TryParse(p.PriceCost?.Replace(".", "").Replace(",", ""), out priceCost);
                    if (priceCost <= 0) priceCost = price;
                    int sl = p.SoLuong > 0 ? p.SoLuong : 1;
                    return new ConfigProductItem
                    {
                        TenHang      = p.Name,
                        MaHang       = p.SKU,
                        XuatXu       = p.HÃNG ?? "",
                        DonVi        = ConfigProductItem.IsPinned(p.Name) ? GetPinnedDonVi(p.Name) : "Cái",
                        SoLuong      = sl,
                        DonGiaVND    = price,
                        ThanhTienVND = price * sl,
                        GiaNhap      = priceCost,
                        ThanhTien    = priceCost * sl,
                        LoiNhuan     = (price - priceCost) * sl,
                        BangGia      = 0,
                        GhiChu       = "",
                        IsHeader     = false
                    };
                }).ToList();

                // Xác định item mặc định để select trong modal
                string defaultDisplay = null;
                if (!string.IsNullOrEmpty(lastSearchedSheet) && !string.IsNullOrEmpty(lastSearchedPkg))
                    defaultDisplay = $"{lastSearchedSheet} - {lastSearchedPkg}";

                // Mở Modal lưu đóng gói với sheetDisplayMap và các giá trị mặc định
                using (var frm = new FrmSavePackage(childItemsForModal, sheetDisplayMap, defaultDisplay, lastSearchedPkg))
                {
                    if (frm.ShowDialog() == DialogResult.OK)
                    {
                        this.Cursor = Cursors.WaitCursor;
                        bool saved = await SaveConfigToSpecificSheetAsync(frm.SheetName, frm.ConfigName, frm.IsOverwrite);
                        this.Cursor = Cursors.Default;

                        if (saved)
                        {
                            // Cập nhật lại list Donggoi_ ở comboBox1
                            _ = LoadDonggoiSheetsToComboAsync();

                            MessageBox.Show($"Đóng gói \"{frm.ConfigName}\" vào Sheet \"{frm.SheetName}\" thành công!",
                                "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void Button5_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(configSheetName))
            {
                MessageBox.Show("Vui lòng chọn hoặc tạo tab báo giá trước khi lưu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (configProducts.Count == 0)
            {
                MessageBox.Show("Danh sách báo giá đang trống!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                this.Cursor = Cursors.WaitCursor;
                bool saved = await SaveCurrentQuotationToSheetAsync();
                this.Cursor = Cursors.Default;

                if (saved)
                {
                    await FetchConfigNamesAsync();
                    await FetchSavedConfigsFullDataAsync();
                    MessageBox.Show($"Đã lưu báo giá vào tab '{configSheetName}' thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show($"Lỗi khi lưu báo giá: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task<bool> SaveCurrentQuotationToSheetAsync()
        {
            if (_sheetsService == null) InitGoogleSheetsService();

            // 1. Đọc dữ liệu hiện có từ sheet để gộp
            var response = await _sheetsService.Spreadsheets.Values.Get(spreadsheetId, $"{configSheetName}!A2:M").ExecuteAsync();
            var existingGroups = new List<(string name, List<ConfigProductItem> items)>();
            
            if (response.Values != null)
            {
                string currentGroupName = null;
                var currentGroupItems = new List<ConfigProductItem>();

                for (int i = 0; i < response.Values.Count; i++)
                {
                    var row = response.Values[i];
                    if (row.Count < 2) continue;
                    
                    string tenHang = row[1]?.ToString()?.Trim() ?? "";
                    if (tenHang.StartsWith("TỔNG CỘNG") || tenHang.StartsWith("THUẾ VAT") || tenHang.StartsWith("THÀNH TIỀN") || string.IsNullOrEmpty(tenHang)) 
                        continue;

                    bool isHeader = false;
                    if (row.Count > 4 && row[4]?.ToString()?.Trim() == "TỦ")
                    {
                        bool hasSl = row.Count > 5 && !string.IsNullOrWhiteSpace(row[5]?.ToString());
                        string maHang = row.Count > 2 ? row[2]?.ToString()?.Trim() : "";
                        if (!hasSl && string.IsNullOrEmpty(maHang))
                        {
                            isHeader = true;
                        }
                    }

                    Func<string, decimal> parseCurrency = (s) => {
                        if (string.IsNullOrEmpty(s)) return 0;
                        string clean = s.Replace(".", "").Replace(",", "").Replace("₫", "").Trim();
                        decimal.TryParse(clean, out decimal res);
                        return res;
                    };

                    var item = new ConfigProductItem
                    {
                        STT = row.Count > 0 ? row[0]?.ToString() : "",
                        TenHang = tenHang,
                        MaHang = row.Count > 2 ? row[2]?.ToString() : "",
                        XuatXu = row.Count > 3 ? row[3]?.ToString() : "",
                        DonVi = row.Count > 4 ? row[4]?.ToString() : "",
                        SoLuong = (row.Count > 5 && int.TryParse(row[5]?.ToString(), out int sl)) ? sl : 0,
                        DonGiaVND = parseCurrency(row.Count > 6 ? row[6]?.ToString() : "0"),
                        ThanhTienVND = parseCurrency(row.Count > 7 ? row[7]?.ToString() : "0"),
                        GhiChu = row.Count > 8 ? row[8]?.ToString() : "",
                        GiaNhap = parseCurrency(row.Count > 9 ? row[9]?.ToString() : "0"),
                        ThanhTien = parseCurrency(row.Count > 10 ? row[10]?.ToString() : "0"),
                        LoiNhuan = parseCurrency(row.Count > 11 ? row[11]?.ToString() : "0"),
                            BangGia = parseCurrency(row.Count > 12 ? row[12]?.ToString() : "0"),
                        IsHeader = isHeader
                    };

                    if (isHeader)
                    {
                        if (currentGroupName != null) existingGroups.Add((currentGroupName, currentGroupItems));
                        currentGroupName = tenHang;
                        currentGroupItems = new List<ConfigProductItem>();
                    }
                    else
                    {
                        currentGroupItems.Add(item);
                    }
                }
                if (currentGroupName != null) existingGroups.Add((currentGroupName, currentGroupItems));
            }

            // 2. Parse configProducts hiện tại vào các nhóm để gộp
            var sessionGroups = new List<(string name, List<ConfigProductItem> items)>();
            string sessionCurGroupName = null;
            var sessionCurGroupItems = new List<ConfigProductItem>();

            foreach (var item in configProducts)
            {
                if (item.IsHeader)
                {
                    if (sessionCurGroupName != null) sessionGroups.Add((sessionCurGroupName, sessionCurGroupItems));
                    sessionCurGroupName = item.TenHang;
                    sessionCurGroupItems = new List<ConfigProductItem>();
                }
                else if (!item.IsSummary)
                {
                    sessionCurGroupItems.Add(item);
                }
            }
            if (sessionCurGroupName != null) sessionGroups.Add((sessionCurGroupName, sessionCurGroupItems));

            // 3. Gộp Session vào Existing
            foreach (var sGroup in sessionGroups)
            {
                var matchIdx = existingGroups.FindIndex(g => string.Equals(g.name?.Trim(), sGroup.name?.Trim(), StringComparison.OrdinalIgnoreCase));
                if (matchIdx >= 0)
                {
                    // Ghi đè (Overwrite) toàn bộ danh sách sản phẩm của nhóm cũ bằng danh sách từ giao diện
                    existingGroups[matchIdx] = (sGroup.name, new List<ConfigProductItem>(sGroup.items));
                }
                else
                {
                    // Nhóm mới hoàn toàn -> Thêm vào cuối
                    existingGroups.Add((sGroup.name, new List<ConfigProductItem>(sGroup.items)));
                }
            }

            // 4. Flatten toàn bộ và tính toán lại totals
            var finalItems = new List<ConfigProductItem>();
            foreach (var g in existingGroups)
            {
                // Thêm Header
                var header = new ConfigProductItem
                {
                    TenHang = g.name,
                    XuatXu = "VNECCO",
                    DonVi = "TỦ",
                    SoLuong = 1,
                    IsHeader = true
                };
                
                // Tính tổng cho header từ các items con
                header.DonGiaVND = g.items.Sum(p => p.DonGiaVND * p.SoLuong);
                header.ThanhTienVND = g.items.Sum(p => p.ThanhTienVND);
                header.GiaNhap = g.items.Sum(p => p.GiaNhap * p.SoLuong);
                header.ThanhTien = g.items.Sum(p => p.ThanhTien);
                header.LoiNhuan = g.items.Sum(p => p.LoiNhuan);
                header.BangGia = g.items.Sum(p => p.BangGia);

                finalItems.Add(header);
                finalItems.AddRange(g.items);
            }

            // Tính tổng Toàn Sheet
            decimal tongCongGiaNhap = finalItems.Where(p => !p.IsHeader).Sum(p => p.ThanhTien);
            decimal tongCongThanhTien = finalItems.Where(p => !p.IsHeader).Sum(p => p.ThanhTienVND);
            decimal vatRate = 0.08m;
            decimal vatGiaNhap = tongCongGiaNhap * vatRate;
            decimal vatThanhTien = tongCongThanhTien * vatRate;

            finalItems.Add(new ConfigProductItem { TenHang = "TỔNG CỘNG (Giá chưa bao gồm VAT)", ThanhTienVND = tongCongThanhTien, ThanhTien = tongCongThanhTien - tongCongGiaNhap, GiaNhap = tongCongThanhTien - tongCongGiaNhap, LoiNhuan = tongCongThanhTien - tongCongGiaNhap, IsSummary = true });
            finalItems.Add(new ConfigProductItem { TenHang = "THUẾ VAT 8%", ThanhTienVND = vatThanhTien, ThanhTien = vatGiaNhap, IsSummary = true });
            finalItems.Add(new ConfigProductItem { TenHang = "THÀNH TIỀN", DonGiaVND = tongCongThanhTien + vatThanhTien, ThanhTienVND = tongCongThanhTien + vatThanhTien, ThanhTien = tongCongGiaNhap + vatGiaNhap, GiaNhap = tongCongGiaNhap + vatGiaNhap, IsSummary = true });

            // Build Rows to Save
            var allRows = new List<IList<object>>();
            var headerRowIndices = new List<int>();
            var summaryRowIndices = new List<int>();

            for (int i = 0; i < finalItems.Count; i++)
            {
                var item = finalItems[i];
                item.STT = (i + 1).ToString(); // Đánh lại STT toàn bộ

                var rowFields = new List<object>
                {
                    item.IsSummary ? "" : (item.STT ?? ""),
                    item.TenHang ?? "",
                    item.MaHang ?? "",
                    item.XuatXu ?? "",
                    item.DonVi ?? "",
                    item.IsHeader || item.IsSummary ? "" : (object)item.SoLuong,
                    item.DonGiaVND,
                    item.ThanhTienVND,
                    item.GhiChu ?? "",
                    item.GiaNhap,
                    item.ThanhTien,
                    item.LoiNhuan,
                    item.BangGia
                };

                allRows.Add(rowFields);
                if (item.IsHeader) headerRowIndices.Add(i);
                if (item.IsSummary) summaryRowIndices.Add(i);
            }

            // Thêm dòng Header vào đầu danh sách
            var headerNames = new List<object> { "STT", "Tên hàng", "Mã hàng", "Xuất xứ", "Đơn vị", "Số lượng", "Đơn giá (VNĐ)", "Thành tiền (VNĐ)", "Ghi chú", "Giá Nhập", "Thành Tiền", "Lợi nhuận", "Bảng Giá" };
            allRows.Insert(0, headerNames);

            // 5. Ghi dữ liệu
            await _sheetsService.Spreadsheets.Values.Clear(
                new Google.Apis.Sheets.v4.Data.ClearValuesRequest(), spreadsheetId, $"{configSheetName}!A1:M2000").ExecuteAsync();

            if (allRows.Count > 0)
            {
                var valueRange = new Google.Apis.Sheets.v4.Data.ValueRange { Values = allRows };
                var updateReq = _sheetsService.Spreadsheets.Values.Update(valueRange, spreadsheetId, $"{configSheetName}!A1");
                updateReq.ValueInputOption = Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                await updateReq.ExecuteAsync();
            }

            // 6. Formatting
            await ApplyQuotationFormattingAsync(configSheetName, headerRowIndices, summaryRowIndices, allRows, finalItems);

            return true;
        }

        private async Task ApplyQuotationFormattingAsync(string sheetName, List<int> headerRowIndices, List<int> summaryRowIndices, List<IList<object>> allDataRows = null, List<ConfigProductItem> finalItems = null)
        {
            var spreadsheet = await _sheetsService.Spreadsheets.Get(spreadsheetId).ExecuteAsync();
            var sheet = spreadsheet.Sheets.FirstOrDefault(s => s.Properties.Title == sheetName);
            if (sheet == null) return;
            int sheetId = sheet.Properties.SheetId.Value;

            var requests = new List<Google.Apis.Sheets.v4.Data.Request>();
            int totalRows = allDataRows != null && allDataRows.Count > 0 ? allDataRows.Count : 2000;

            // Xoá các nhóm cũ nếu có (để tránh lỗi khi ghi đè)
            if (sheet.RowGroups != null)
            {
                foreach (var group in sheet.RowGroups)
                {
                    requests.Add(new Google.Apis.Sheets.v4.Data.Request
                    {
                        DeleteDimensionGroup = new Google.Apis.Sheets.v4.Data.DeleteDimensionGroupRequest
                        {
                            Range = new Google.Apis.Sheets.v4.Data.DimensionRange
                            {
                                SheetId = sheetId,
                                Dimension = "ROWS",
                                StartIndex = group.Range.StartIndex,
                                EndIndex = group.Range.EndIndex
                            }
                        }
                    });
                }
            }

            // Thêm các nhóm mới cho mỗi Header
            if (finalItems != null && headerRowIndices != null)
            {
                for (int i = 0; i < headerRowIndices.Count; i++)
                {
                    int startIdx = headerRowIndices[i] + 1; // Dòng đầu tiên sau Header
                    int endIdx;
                    
                    if (i + 1 < headerRowIndices.Count)
                    {
                        endIdx = headerRowIndices[i + 1] - 1; // Dòng cuối cùng trước Header tiếp theo
                    }
                    else
                    {
                        endIdx = (summaryRowIndices != null && summaryRowIndices.Count > 0 ? summaryRowIndices[0] : finalItems.Count) - 1;
                    }

                    if (endIdx >= startIdx)
                    {
                        requests.Add(new Google.Apis.Sheets.v4.Data.Request
                        {
                            AddDimensionGroup = new Google.Apis.Sheets.v4.Data.AddDimensionGroupRequest
                            {
                                Range = new Google.Apis.Sheets.v4.Data.DimensionRange
                                {
                                    SheetId = sheetId,
                                    Dimension = "ROWS",
                                    StartIndex = startIdx + 1, // +1 vì row 0 trên Sheet là Tiêu đề cột
                                    EndIndex = endIdx + 2     // +1 vì row 0, +1 vì EndIndex là exclusive (không bao gồm)
                                }
                            }
                        });
                    }
                }
            }

            // Thiết lập độ rộng các cột
            Action<int, int> setColWidth = (colIdx, width) =>
            {
                requests.Add(new Google.Apis.Sheets.v4.Data.Request
                {
                    UpdateDimensionProperties = new Google.Apis.Sheets.v4.Data.UpdateDimensionPropertiesRequest
                    {
                        Range = new Google.Apis.Sheets.v4.Data.DimensionRange { SheetId = sheetId, Dimension = "COLUMNS", StartIndex = colIdx, EndIndex = colIdx + 1 },
                        Properties = new Google.Apis.Sheets.v4.Data.DimensionProperties { PixelSize = width },
                        Fields = "pixelSize"
                    }
                });
            };
            setColWidth(0, 45);  // STT
            setColWidth(1, 300); // Tên hàng
            setColWidth(2, 100); // Mã hàng
            setColWidth(3, 80);  // Xuất xứ
            setColWidth(4, 55);  // Đơn vị
            setColWidth(5, 60);  // Số lượng
            setColWidth(6, 110); // Đơn giá
            setColWidth(7, 120); // Thành tiền
            setColWidth(8, 80);  // Ghi chú
            setColWidth(9, 110); // Giá nhập
            setColWidth(10, 120); // Thành tiền nhập
            setColWidth(11, 100); // Lợi nhuận
            setColWidth(12, 80);  // Bảng giá

            // Thêm viền (Borders) cho toàn bộ vùng dữ liệu
            requests.Add(new Google.Apis.Sheets.v4.Data.Request
            {
                UpdateBorders = new Google.Apis.Sheets.v4.Data.UpdateBordersRequest
                {
                    Range = new Google.Apis.Sheets.v4.Data.GridRange { SheetId = sheetId, StartRowIndex = 0, EndRowIndex = totalRows, StartColumnIndex = 0, EndColumnIndex = 13 },
                    Top = new Google.Apis.Sheets.v4.Data.Border { Style = "SOLID", Color = new Google.Apis.Sheets.v4.Data.Color { Red = 0, Green = 0, Blue = 0 } },
                    Bottom = new Google.Apis.Sheets.v4.Data.Border { Style = "SOLID", Color = new Google.Apis.Sheets.v4.Data.Color { Red = 0, Green = 0, Blue = 0 } },
                    Left = new Google.Apis.Sheets.v4.Data.Border { Style = "SOLID", Color = new Google.Apis.Sheets.v4.Data.Color { Red = 0, Green = 0, Blue = 0 } },
                    Right = new Google.Apis.Sheets.v4.Data.Border { Style = "SOLID", Color = new Google.Apis.Sheets.v4.Data.Color { Red = 0, Green = 0, Blue = 0 } },
                    InnerHorizontal = new Google.Apis.Sheets.v4.Data.Border { Style = "SOLID", Color = new Google.Apis.Sheets.v4.Data.Color { Red = 0, Green = 0, Blue = 0 } },
                    InnerVertical = new Google.Apis.Sheets.v4.Data.Border { Style = "SOLID", Color = new Google.Apis.Sheets.v4.Data.Color { Red = 0, Green = 0, Blue = 0 } }
                }
            });

            // Format Header Row (Row 1)
            requests.Add(new Google.Apis.Sheets.v4.Data.Request
            {
                RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                {
                    Range = new Google.Apis.Sheets.v4.Data.GridRange { SheetId = sheetId, StartRowIndex = 0, EndRowIndex = 1, StartColumnIndex = 0, EndColumnIndex = 9 },
                    Cell = new Google.Apis.Sheets.v4.Data.CellData { UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat { BackgroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 1f, Green = 1f, Blue = 0f }, TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat { Bold = true, ForegroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 0.12f, Green = 0.286f, Blue = 0.49f } }, HorizontalAlignment = "CENTER", VerticalAlignment = "MIDDLE" } },
                    Fields = "userEnteredFormat(backgroundColor,textFormat,horizontalAlignment,verticalAlignment)"
                }
            });

            // Reset format vùng dữ liệu
            requests.Add(new Google.Apis.Sheets.v4.Data.Request
            {
                RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                {
                    Range = new Google.Apis.Sheets.v4.Data.GridRange { SheetId = sheetId, StartRowIndex = 1, EndRowIndex = 2000, StartColumnIndex = 0, EndColumnIndex = 13 },
                    Cell = new Google.Apis.Sheets.v4.Data.CellData { UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat { BackgroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 1, Green = 1, Blue = 1 }, TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat { Bold = false } } },
                    Fields = "userEnteredFormat(backgroundColor,textFormat)"
                }
            });

            // Bật WRAP cho cột B (Tên sản phẩm) để hiển thị đúng multiline (Vỏ tủ điện...)
            requests.Add(new Google.Apis.Sheets.v4.Data.Request
            {
                RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                {
                    Range = new Google.Apis.Sheets.v4.Data.GridRange { SheetId = sheetId, StartRowIndex = 1, EndRowIndex = totalRows, StartColumnIndex = 1, EndColumnIndex = 2 },
                    Cell = new Google.Apis.Sheets.v4.Data.CellData { UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat { WrapStrategy = "WRAP", VerticalAlignment = "TOP" } },
                    Fields = "userEnteredFormat(wrapStrategy,verticalAlignment)"
                }
            });

            // Căn giữa cho STT(0), Mã hàng(2), Xuất xứ(3), Đơn vị(4), Số lượng(5)
            foreach (int c in new[] { 0, 2, 3, 4, 5 })
            {
                requests.Add(new Google.Apis.Sheets.v4.Data.Request
                {
                    RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                    {
                        Range = new Google.Apis.Sheets.v4.Data.GridRange { SheetId = sheetId, StartRowIndex = 1, EndRowIndex = totalRows, StartColumnIndex = c, EndColumnIndex = c + 1 },
                        Cell = new Google.Apis.Sheets.v4.Data.CellData { UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat { HorizontalAlignment = "CENTER", VerticalAlignment = "MIDDLE" } },
                        Fields = "userEnteredFormat(horizontalAlignment,verticalAlignment)"
                    }
                });
            }

            // Căn phải cho Đơn giá(6), Thành tiền(7), Giá Nhập(9), Thành Tiền Nhập(10), Lợi nhuận(11)
            foreach (int c in new[] { 6, 7, 9, 10, 11 })
            {
                requests.Add(new Google.Apis.Sheets.v4.Data.Request
                {
                    RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                    {
                        Range = new Google.Apis.Sheets.v4.Data.GridRange { SheetId = sheetId, StartRowIndex = 1, EndRowIndex = totalRows, StartColumnIndex = c, EndColumnIndex = c + 1 },
                        Cell = new Google.Apis.Sheets.v4.Data.CellData { UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat { HorizontalAlignment = "RIGHT", VerticalAlignment = "MIDDLE" } },
                        Fields = "userEnteredFormat(horizontalAlignment,verticalAlignment)"
                    }
                });
            }

            // Format số cho các cột giá: #,##0 (có dấu phân cách hàng nghìn)
            foreach (int c in new[] { 6, 7, 9, 10, 11, 12 })
            {
                requests.Add(new Google.Apis.Sheets.v4.Data.Request
                {
                    RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                    {
                        Range = new Google.Apis.Sheets.v4.Data.GridRange { SheetId = sheetId, StartRowIndex = 1, EndRowIndex = totalRows, StartColumnIndex = c, EndColumnIndex = c + 1 },
                        Cell = new Google.Apis.Sheets.v4.Data.CellData
                        {
                            UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat
                            {
                                NumberFormat = new Google.Apis.Sheets.v4.Data.NumberFormat
                                {
                                    Type = "NUMBER",
                                    Pattern = "#,##0"
                                }
                            }
                        },
                        Fields = "userEnteredFormat(numberFormat)"
                    }
                });
            }

            // Helper function để format các cột giá (J, K, L, M)
            Action<int> formatPriceColumns = (sheetRowIdx) =>
            {
                // Cột J, K (9..11): Giá Nhập, Thành Tiền -> Cyan
                requests.Add(new Google.Apis.Sheets.v4.Data.Request
                {
                    RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                    {
                        Range = new Google.Apis.Sheets.v4.Data.GridRange { SheetId = sheetId, StartRowIndex = sheetRowIdx, EndRowIndex = sheetRowIdx + 1, StartColumnIndex = 9, EndColumnIndex = 11 },
                        Cell = new Google.Apis.Sheets.v4.Data.CellData { UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat { BackgroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 0f, Green = 1f, Blue = 1f }, TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat { Bold = true, ForegroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 0f, Green = 0f, Blue = 0f } }, HorizontalAlignment = "CENTER", VerticalAlignment = "MIDDLE" } },
                        Fields = "userEnteredFormat(backgroundColor,textFormat,horizontalAlignment,verticalAlignment)"
                    }
                });
                // Cột L (11..12): Lợi nhuận -> Yellow, Red Text
                requests.Add(new Google.Apis.Sheets.v4.Data.Request
                {
                    RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                    {
                        Range = new Google.Apis.Sheets.v4.Data.GridRange { SheetId = sheetId, StartRowIndex = sheetRowIdx, EndRowIndex = sheetRowIdx + 1, StartColumnIndex = 11, EndColumnIndex = 12 },
                        Cell = new Google.Apis.Sheets.v4.Data.CellData { UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat { BackgroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 1f, Green = 1f, Blue = 0f }, TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat { Bold = true, ForegroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 1f, Green = 0f, Blue = 0f } }, HorizontalAlignment = "CENTER", VerticalAlignment = "MIDDLE" } },
                        Fields = "userEnteredFormat(backgroundColor,textFormat,horizontalAlignment,verticalAlignment)"
                    }
                });
                // Cột M (12..13): Bảng Giá -> CornflowerBlue (Light Blue)
                requests.Add(new Google.Apis.Sheets.v4.Data.Request
                {
                    RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                    {
                        Range = new Google.Apis.Sheets.v4.Data.GridRange { SheetId = sheetId, StartRowIndex = sheetRowIdx, EndRowIndex = sheetRowIdx + 1, StartColumnIndex = 12, EndColumnIndex = 13 },
                        Cell = new Google.Apis.Sheets.v4.Data.CellData { UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat { BackgroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 0.39f, Green = 0.58f, Blue = 0.93f }, TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat { Bold = true, ForegroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 0f, Green = 0f, Blue = 0f } }, HorizontalAlignment = "CENTER", VerticalAlignment = "MIDDLE" } },
                        Fields = "userEnteredFormat(backgroundColor,textFormat,horizontalAlignment,verticalAlignment)"
                    }
                });
            };

            // Format cho dòng Header (Row 1)
            formatPriceColumns(0);

            // Format Group Headers (Xanh lá)
            foreach (int hi in headerRowIndices)
            {
                int sheetRowIdx = hi + 1; // Row 1-based, +1 for headers
                requests.Add(new Google.Apis.Sheets.v4.Data.Request
                {
                    RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                    {
                        Range = new Google.Apis.Sheets.v4.Data.GridRange { SheetId = sheetId, StartRowIndex = sheetRowIdx, EndRowIndex = sheetRowIdx + 1, StartColumnIndex = 0, EndColumnIndex = 13 },
                        Cell = new Google.Apis.Sheets.v4.Data.CellData { UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat { BackgroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 144f / 255f, Green = 238f / 255f, Blue = 144f / 255f }, TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat { Bold = true } } },
                        Fields = "userEnteredFormat(backgroundColor,textFormat)"
                    }
                });
            }

            // Format Summary Rows (Vàng)
            foreach (int si in summaryRowIndices)
            {
                int sheetRowIdx = si + 1;
                requests.Add(new Google.Apis.Sheets.v4.Data.Request
                {
                    RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                    {
                        Range = new Google.Apis.Sheets.v4.Data.GridRange { SheetId = sheetId, StartRowIndex = sheetRowIdx, EndRowIndex = sheetRowIdx + 1, StartColumnIndex = 0, EndColumnIndex = 9 },
                        Cell = new Google.Apis.Sheets.v4.Data.CellData { UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat { BackgroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 1f, Green = 1f, Blue = 0f }, TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat { Bold = true } } },
                        Fields = "userEnteredFormat(backgroundColor,textFormat)"
                    }
                });
                formatPriceColumns(sheetRowIdx);
            }

            // TextFormatRuns: tô màu đỏ từ khóa quan trọng trong dòng "Vỏ tủ điện"
            if (allDataRows != null)
            {
                for (int ri = 0; ri < allDataRows.Count; ri++)
                {
                    string cellText = allDataRows[ri].Count > 1 ? allDataRows[ri][1]?.ToString() ?? "" : "";
                    if (!cellText.StartsWith("Vỏ tủ điện")) continue;

                    int sheetRowIdx2 = ri; // ri maps directly to 0-based StartRowIndex
                    var runs = BuildRichTextRuns(cellText);
                    if (runs == null || runs.Count == 0) continue;

                    requests.Add(new Google.Apis.Sheets.v4.Data.Request
                    {
                        UpdateCells = new Google.Apis.Sheets.v4.Data.UpdateCellsRequest
                        {
                            Range = new Google.Apis.Sheets.v4.Data.GridRange
                            {
                                SheetId = sheetId,
                                StartRowIndex = sheetRowIdx2,
                                EndRowIndex = sheetRowIdx2 + 1,
                                StartColumnIndex = 1, // cột B
                                EndColumnIndex = 2
                            },
                            Rows = new List<Google.Apis.Sheets.v4.Data.RowData>
                            {
                                new Google.Apis.Sheets.v4.Data.RowData
                                {
                                    Values = new List<Google.Apis.Sheets.v4.Data.CellData>
                                    {
                                        new Google.Apis.Sheets.v4.Data.CellData
                                        {
                                            UserEnteredValue = new Google.Apis.Sheets.v4.Data.ExtendedValue
                                            {
                                                StringValue = cellText
                                            },
                                            TextFormatRuns = runs
                                        }
                                    }
                                }
                            },
                            Fields = "userEnteredValue,textFormatRuns"
                        }
                    });
                }
            }

            // ── Đồng bộ màu và font custom lên Google Sheets ──
            string[] sheetColOrder = { "STT", "TenHang", "MaHang", "XuatXu", "DonVi", "SoLuong", "DonGiaVND", "ThanhTienVND", "GhiChu", "GiaNhap", "ThanhTien", "LoiNhuan", "BangGia" };

            var sheetToDgvColMap = new Dictionary<int, int>();
            for (int i = 0; i < dgvParentProducts.Columns.Count; i++)
            {
                string colName = dgvParentProducts.Columns[i].Name;
                int sheetIdx = Array.IndexOf(sheetColOrder, colName);
                if (sheetIdx >= 0) sheetToDgvColMap[sheetIdx] = i;
            }

            if (finalItems != null)
            {
                for (int finalIdx = 0; finalIdx < finalItems.Count; finalIdx++)
                {
                    var item = finalItems[finalIdx];
                    if (item.IsHeader || item.IsSummary) continue;

                    int sheetRow = finalIdx + 1; // +1 vì row 0 là dòng tên cột (STT, Tên hàng...)

                    for (int sheetColIdx = 0; sheetColIdx < 13; sheetColIdx++)
                    {
                        bool hasBg = false, hasFg = false, hasFont = false;
                        Color bg = Color.White, fg = Color.Black;
                        Font cFont = null;

                        // 1. Lấy màu gốc từ Google Sheets (nếu item này từng có trên Sheet)
                        if (item.SheetRowIndex >= 0)
                        {
                            var sheetKey = (item.SheetRowIndex, sheetColIdx);
                            if (_sheetBgColors.TryGetValue(sheetKey, out Color sBg)) { bg = sBg; hasBg = true; }
                            if (_sheetFgColors.TryGetValue(sheetKey, out Color sFg)) { fg = sFg; hasFg = true; }
                        }

                        // 2. Ghi đè bằng màu được chọn trong session hiện tại (nếu có)
                        if (sheetToDgvColMap.TryGetValue(sheetColIdx, out int dgvColIdx))
                        {
                            var cellKey = (item, dgvColIdx);
                            if (_cellBgColors.TryGetValue(cellKey, out Color cBg)) { bg = cBg; hasBg = true; }
                            if (_cellFgColors.TryGetValue(cellKey, out Color cFg)) { fg = cFg; hasFg = true; }
                            if (_cellFonts.TryGetValue(cellKey, out Font f)) { cFont = f; hasFont = true; }
                        }

                        // Nếu không có gì tuỳ chỉnh thì bỏ qua (nó sẽ dùng format mặc định đã reset)
                        if (!hasBg && !hasFg && !hasFont) continue;

                        var cellFormat = new Google.Apis.Sheets.v4.Data.CellFormat();
                        var fieldsList = new List<string>();

                        if (hasBg)
                        {
                            cellFormat.BackgroundColor = new Google.Apis.Sheets.v4.Data.Color
                            {
                                Red = bg.R / 255f, Green = bg.G / 255f, Blue = bg.B / 255f
                            };
                            fieldsList.Add("backgroundColor");
                        }

                        if (hasFg || hasFont)
                        {
                            var textFormat = new Google.Apis.Sheets.v4.Data.TextFormat();
                            if (hasFg)
                            {
                                textFormat.ForegroundColor = new Google.Apis.Sheets.v4.Data.Color
                                {
                                    Red = fg.R / 255f, Green = fg.G / 255f, Blue = fg.B / 255f
                                };
                                fieldsList.Add("textFormat.foregroundColor");
                            }
                            if (hasFont)
                            {
                                textFormat.Bold = cFont.Bold;
                                textFormat.Italic = cFont.Italic;
                                textFormat.Strikethrough = cFont.Strikeout;
                                textFormat.Underline = cFont.Underline;
                                textFormat.FontSize = (int)cFont.Size;
                                
                                fieldsList.Add("textFormat.bold");
                                fieldsList.Add("textFormat.italic");
                                fieldsList.Add("textFormat.strikethrough");
                                fieldsList.Add("textFormat.underline");
                                fieldsList.Add("textFormat.fontSize");
                            }
                            cellFormat.TextFormat = textFormat;
                        }

                        if (fieldsList.Count > 0)
                        {
                            string fieldsString = "userEnteredFormat(" + string.Join(",", fieldsList) + ")";
                            requests.Add(new Google.Apis.Sheets.v4.Data.Request
                            {
                                RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                                {
                                    Range = new Google.Apis.Sheets.v4.Data.GridRange
                                    {
                                        SheetId = sheetId,
                                        StartRowIndex = sheetRow,
                                        EndRowIndex = sheetRow + 1,
                                        StartColumnIndex = sheetColIdx,
                                        EndColumnIndex = sheetColIdx + 1
                                    },
                                    Cell = new Google.Apis.Sheets.v4.Data.CellData { UserEnteredFormat = cellFormat },
                                    Fields = fieldsString
                                }
                            });
                        }
                    }
                }
            }

            if (requests.Count > 0)
            {
                await _sheetsService.Spreadsheets.BatchUpdate(new Google.Apis.Sheets.v4.Data.BatchUpdateSpreadsheetRequest { Requests = requests }, spreadsheetId).ExecuteAsync();
            }
        }

        /// <summary>Trả về đơn vị tính phù hợp cho từng loại Pinned item.</summary>
        private static string GetPinnedDonVi(string tenHang)
        {
            if (string.IsNullOrEmpty(tenHang)) return "Cái";
            if (tenHang.StartsWith("Vỏ tủ", StringComparison.OrdinalIgnoreCase))           return "TỦ";
            if (tenHang.StartsWith("Hệ thống đồng thanh", StringComparison.OrdinalIgnoreCase)) return "Hệ";
            if (tenHang.StartsWith("Phụ kiện", StringComparison.OrdinalIgnoreCase))         return "Lô";
            if (tenHang.StartsWith("Nhân công", StringComparison.OrdinalIgnoreCase))        return "Cái";
            return "Cái";
        }

        private ConfigProductItem CreateConfigItem(Products product, decimal price, decimal priceCost)
        {
            string donVi = ConfigProductItem.IsPinned(product.Name)
                ? GetPinnedDonVi(product.Name)
                : "Cái";
            return new ConfigProductItem
            {
                TenHang = product.Name,
                MaHang = product.SKU,
                XuatXu = product.HÃNG,
                DonVi = donVi,
                SoLuong = product.SoLuong > 0 ? product.SoLuong : 1,
                DonGiaVND = price,
                ThanhTienVND = price * (product.SoLuong > 0 ? product.SoLuong : 1),
                GhiChu = "",
                GiaNhap = priceCost,
                ThanhTien = priceCost * (product.SoLuong > 0 ? product.SoLuong : 1),
                LoiNhuan = (price - priceCost) * (product.SoLuong > 0 ? product.SoLuong : 1),
                BangGia = 0,
                IsHeader = false
            };
        }

        private async Task<bool> SaveConfigToSpecificSheetAsync(string targetSheet, string pkgName, bool overwrite)
        {
            if (_sheetsService == null) InitGoogleSheetsService();

            // 1. Đảm bảo Sheet tồn tại
            var spreadsheet = await _sheetsService.Spreadsheets.Get(spreadsheetId).ExecuteAsync();
            var sheetMeta = spreadsheet.Sheets.FirstOrDefault(s => s.Properties.Title == targetSheet);
            if (sheetMeta == null)
            {
                var addReq = new Google.Apis.Sheets.v4.Data.Request
                {
                    AddSheet = new Google.Apis.Sheets.v4.Data.AddSheetRequest
                    {
                        Properties = new Google.Apis.Sheets.v4.Data.SheetProperties { Title = targetSheet }
                    }
                };
                await _sheetsService.Spreadsheets.BatchUpdate(
                    new Google.Apis.Sheets.v4.Data.BatchUpdateSpreadsheetRequest
                    {
                        Requests = new List<Google.Apis.Sheets.v4.Data.Request> { addReq }
                    }, spreadsheetId).ExecuteAsync();
            }

            // 2. Đọc dữ liệu hiện có (row 2 trở xuống) để parse các nhóm cũ
            var readResp = await _sheetsService.Spreadsheets.Values
                .Get(spreadsheetId, $"{targetSheet}!A2:H2000").ExecuteAsync();
            var existingRows = readResp.Values ?? new List<IList<object>>();

            // Parse nhóm cũ: dòng header nhóm = dòng có cột B rỗng và cột A có giá trị
            // (vì dòng header nhóm = [pkgName, "", "", "", "", "", "", ""])
            var groups = new List<(string name, List<IList<object>> rows)>();
            string curName = null;
            var curRows = new List<IList<object>>();

            foreach (var row in existingRows)
            {
                if (row.Count == 0) continue;
                string col0 = row[0]?.ToString() ?? "";
                string col1 = row.Count > 1 ? row[1]?.ToString() ?? "" : "";

                bool isGroupHeader = !string.IsNullOrEmpty(col0) && string.IsNullOrEmpty(col1);

                if (isGroupHeader)
                {
                    if (curName != null) groups.Add((curName, new List<IList<object>>(curRows)));
                    curName = col0;
                    curRows = new List<IList<object>>();
                }
                else if (curName != null)
                {
                    curRows.Add(row);
                }
            }
            if (curName != null) groups.Add((curName, new List<IList<object>>(curRows)));

            // 3. Build danh sách sản phẩm mới từ childProducts
            var newProductRows = childProducts.Select(p => (IList<object>)new List<object>
            {
                p.Id, p.Name ?? "", p.Model ?? "", p.SKU ?? "",
                p.Price ?? "0", p.PriceCost ?? "0",
                p.Category ?? "", p.HÃNG ?? "", p.SoLuong
            }).ToList();

            // Xóa và ghi lại đến cột I
            await _sheetsService.Spreadsheets.Values.Clear(
                new Google.Apis.Sheets.v4.Data.ClearValuesRequest(), spreadsheetId, $"{targetSheet}!A2:I2000").ExecuteAsync();

            // 4. Gộp nhóm: overwrite hoặc append
            int existIdx = groups.FindIndex(g =>
                string.Equals(g.name?.Trim(), pkgName?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (existIdx >= 0)
            {
                if (!overwrite)
                {
                    MessageBox.Show($"Tên cấu hình \"{pkgName}\" đã tồn tại trong Sheet \"{targetSheet}\".\n\nVui lòng tích chọn 'Ghi đè cấu hình cũ' hoặc nhập tên khác.", 
                        "Trùng tên cấu hình", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                groups[existIdx] = (pkgName, newProductRows);
            }
            else
            {
                groups.Add((pkgName, newProductRows));
            }

            // 5. Flatten thành danh sách rows để ghi
            var allRows = new List<IList<object>>();
            var groupHeaderRowIndices = new List<int>(); // index 0-based trong allRows

            foreach (var g in groups)
            {
                groupHeaderRowIndices.Add(allRows.Count);
                // Dòng header nhóm: chỉ cột A = pkgName, còn lại rỗng
                allRows.Add(new List<object> { g.name, "", "", "", "", "", "", "" });
                allRows.AddRange(g.rows);
            }

            // 6. Ghi header cột vào row 1
            var colHeaderRange = new Google.Apis.Sheets.v4.Data.ValueRange
            {
                Values = new List<IList<object>>
                {
                    new List<object> { "ID", "Tên sản phẩm", "Model", "Mã SKU", "Giá bán", "Giá nhập", "Danh mục", "Hãng", "Số lượng" }
                }
            };
            var writeHeader = _sheetsService.Spreadsheets.Values.Update(colHeaderRange, spreadsheetId, $"{targetSheet}!A1");
            writeHeader.ValueInputOption = Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            await writeHeader.ExecuteAsync();

            // 7. Ghi dữ liệu mới từ row 2
            if (allRows.Count > 0)
            {
                var valueRange = new Google.Apis.Sheets.v4.Data.ValueRange { Values = allRows };
                var updateReq = _sheetsService.Spreadsheets.Values.Update(valueRange, spreadsheetId, $"{targetSheet}!A2");
                updateReq.ValueInputOption = Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                await updateReq.ExecuteAsync();
            }

            // 8. Áp dụng màu sắc
            await ApplySheetFormattingAsync(targetSheet, groupHeaderRowIndices, allRows.Count, allRows);
            return true;
        }

        private async Task ApplySheetFormattingAsync(string sheetName, List<int> headerRowIndices, int totalDataRows, List<IList<object>> allDataRows = null)
        {
            var spreadsheet = await _sheetsService.Spreadsheets.Get(spreadsheetId).ExecuteAsync();
            var sheet = spreadsheet.Sheets.FirstOrDefault(s => s.Properties.Title == sheetName);
            if (sheet == null) return;
            int sheetId = sheet.Properties.SheetId.Value;

            const int NUM_COLS = 8;
            var requests = new List<Google.Apis.Sheets.v4.Data.Request>();

            // Reset toàn bộ format vùng dữ liệu (trắng, không bold)
            requests.Add(new Google.Apis.Sheets.v4.Data.Request
            {
                RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                {
                    Range = new Google.Apis.Sheets.v4.Data.GridRange
                    {
                        SheetId = sheetId, StartRowIndex = 0, EndRowIndex = 2000,
                        StartColumnIndex = 0, EndColumnIndex = NUM_COLS
                    },
                    Cell = new Google.Apis.Sheets.v4.Data.CellData
                    {
                        UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat
                        {
                            BackgroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 1, Green = 1, Blue = 1 },
                            TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat { Bold = false }
                        }
                    },
                    Fields = "userEnteredFormat(backgroundColor,textFormat)"
                }
            });

            // Tô màu Header cột (row 0): nền vàng nhạt, chữ xanh đậm, bold
            requests.Add(new Google.Apis.Sheets.v4.Data.Request
            {
                RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                {
                    Range = new Google.Apis.Sheets.v4.Data.GridRange
                    {
                        SheetId = sheetId, StartRowIndex = 0, EndRowIndex = 1,
                        StartColumnIndex = 0, EndColumnIndex = NUM_COLS
                    },
                    Cell = new Google.Apis.Sheets.v4.Data.CellData
                    {
                        UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat
                        {
                            BackgroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 1.0f, Green = 0.922f, Blue = 0.612f },
                            TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat
                            {
                                Bold = true,
                                ForegroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 0.122f, Green = 0.286f, Blue = 0.490f }
                            },
                            HorizontalAlignment = "CENTER", VerticalAlignment = "MIDDLE"
                        }
                    },
                    Fields = "userEnteredFormat(backgroundColor,textFormat,horizontalAlignment,verticalAlignment)"
                }
            });

            // Tô màu xanh (#0070C0, chữ trắng, bold) cho mỗi dòng header nhóm cấu hình
            foreach (int hi in headerRowIndices)
            {
                int sheetRowIdx = hi + 1; // +1 vì row 0 là header cột
                requests.Add(new Google.Apis.Sheets.v4.Data.Request
                {
                    RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                    {
                        Range = new Google.Apis.Sheets.v4.Data.GridRange
                        {
                            SheetId = sheetId,
                            StartRowIndex = sheetRowIdx, EndRowIndex = sheetRowIdx + 1,
                            StartColumnIndex = 0, EndColumnIndex = NUM_COLS
                        },
                        Cell = new Google.Apis.Sheets.v4.Data.CellData
                        {
                            UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat
                            {
                                BackgroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 146f / 255f, Green = 208f / 255f, Blue = 80f / 255f },
                                TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat
                                {
                                    Bold = true,
                                    ForegroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 1f, Green = 1f, Blue = 1f }
                                },
                                HorizontalAlignment = "LEFT", VerticalAlignment = "MIDDLE"
                            }
                        },
                        Fields = "userEnteredFormat(backgroundColor,textFormat,horizontalAlignment,verticalAlignment)"
                    }
                });
            }

            // ── Bật WRAP cho cột B (Tên sản phẩm) để hiển thị đúng multiline (Vỏ tủ điện...) ──
            requests.Add(new Google.Apis.Sheets.v4.Data.Request
            {
                RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                {
                    Range = new Google.Apis.Sheets.v4.Data.GridRange
                    {
                        SheetId = sheetId,
                        StartRowIndex = 1,             // từ row 2 (bỏ qua header cột)
                        EndRowIndex = 2000,
                        StartColumnIndex = 1,          // cột B
                        EndColumnIndex = 2             // chỉ cột B
                    },
                    Cell = new Google.Apis.Sheets.v4.Data.CellData
                    {
                        UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat
                        {
                            WrapStrategy = "WRAP",
                            VerticalAlignment = "TOP"
                        }
                    },
                    Fields = "userEnteredFormat(wrapStrategy,verticalAlignment)"
                }
            });

            // ── TextFormatRuns: tô màu đỏ từ khóa quan trọng trong dòng "Vỏ tủ điện" ──
            if (allDataRows != null)
            {
                for (int ri = 0; ri < allDataRows.Count; ri++)
                {
                    string cellText = allDataRows[ri].Count > 1 ? allDataRows[ri][1]?.ToString() ?? "" : "";
                    if (!cellText.StartsWith("Vỏ tủ điện")) continue;

                    int sheetRowIdx2 = ri + 1; // +1 vì row 0 là header cột
                    var runs = BuildRichTextRuns(cellText);
                    if (runs == null || runs.Count == 0) continue;

                    // Gộp ô (Merge cells) từ cột B đến cột H để text đè lên các cột khác
                    requests.Add(new Google.Apis.Sheets.v4.Data.Request
                    {
                        MergeCells = new Google.Apis.Sheets.v4.Data.MergeCellsRequest
                        {
                            Range = new Google.Apis.Sheets.v4.Data.GridRange
                            {
                                SheetId = sheetId,
                                StartRowIndex = sheetRowIdx2,
                                EndRowIndex = sheetRowIdx2 + 1,
                                StartColumnIndex = 1, // cột B
                                EndColumnIndex = NUM_COLS // đến hết
                            },
                            MergeType = "MERGE_ROWS"
                        }
                    });

                    requests.Add(new Google.Apis.Sheets.v4.Data.Request
                    {
                        UpdateCells = new Google.Apis.Sheets.v4.Data.UpdateCellsRequest
                        {
                            Range = new Google.Apis.Sheets.v4.Data.GridRange
                            {
                                SheetId = sheetId,
                                StartRowIndex = sheetRowIdx2,
                                EndRowIndex = sheetRowIdx2 + 1,
                                StartColumnIndex = 1, // cột B
                                EndColumnIndex = 2
                            },
                            Rows = new List<Google.Apis.Sheets.v4.Data.RowData>
                            {
                                new Google.Apis.Sheets.v4.Data.RowData
                                {
                                    Values = new List<Google.Apis.Sheets.v4.Data.CellData>
                                    {
                                        new Google.Apis.Sheets.v4.Data.CellData
                                        {
                                            UserEnteredValue = new Google.Apis.Sheets.v4.Data.ExtendedValue
                                            {
                                                StringValue = cellText
                                            },
                                            TextFormatRuns = runs
                                        }
                                    }
                                }
                            },
                            Fields = "userEnteredValue,textFormatRuns"
                        }
                    });
                }
            }

            if (requests.Count > 0)
            {
                var batchUpdate = new Google.Apis.Sheets.v4.Data.BatchUpdateSpreadsheetRequest { Requests = requests };
                await _sheetsService.Spreadsheets.BatchUpdate(batchUpdate, spreadsheetId).ExecuteAsync();
            }
        }

        /// <summary>
        /// Tạo danh sách TextFormatRun để tô màu đỏ + bold các từ khóa quan trọng
        /// trong nội dung ô "Vỏ tủ điện" trên Google Sheets.
        /// </summary>
        private static IList<Google.Apis.Sheets.v4.Data.TextFormatRun> BuildRichTextRuns(string text)
        {
            var patterns = new[]
            {
                "trong nhà", "ngoài trời",
                @"\d+\s*lớp cánh",
                @"H\d+xW\d+xD\d+mm",
                @"\d+(?:\.\d+)?mm",
                "sơn sần", "sơn bóng",
                @"RAL\s*\d+[^\s,\n]*",
                @"có tô màu\s+\S+",
            };

            var allMatches = new List<(int start, int len)>();
            foreach (var pat in patterns)
            {
                var rx = new System.Text.RegularExpressions.Regex(
                    pat, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                foreach (System.Text.RegularExpressions.Match m in rx.Matches(text))
                    allMatches.Add((m.Index, m.Length));
            }
            allMatches.Sort((a, b) => a.start != b.start ? a.start.CompareTo(b.start) : b.len.CompareTo(a.len));

            var clean = new List<(int start, int len)>();
            int covered = 0;
            foreach (var m in allMatches)
                if (m.start >= covered) { clean.Add(m); covered = m.start + m.len; }

            if (clean.Count == 0) return new List<Google.Apis.Sheets.v4.Data.TextFormatRun>();

            var normalFmt = new Google.Apis.Sheets.v4.Data.TextFormat
            {
                ForegroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 0.118f, Green = 0.118f, Blue = 0.118f },
                Bold = false
            };
            var redFmt = new Google.Apis.Sheets.v4.Data.TextFormat
            {
                ForegroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 1f, Green = 0f, Blue = 0f },
                Bold = true
            };

            var runs = new List<Google.Apis.Sheets.v4.Data.TextFormatRun>();
            // Bắt đầu bằng format bình thường từ ký tự 0
            runs.Add(new Google.Apis.Sheets.v4.Data.TextFormatRun { StartIndex = 0, Format = normalFmt });

            foreach (var m in clean)
            {
                // Tô đỏ từ khóa
                runs.Add(new Google.Apis.Sheets.v4.Data.TextFormatRun { StartIndex = m.start, Format = redFmt });
                // Trả về normal sau từ khóa (nếu không phải cuối text)
                int endIdx = m.start + m.len;
                if (endIdx < text.Length)
                    runs.Add(new Google.Apis.Sheets.v4.Data.TextFormatRun { StartIndex = endIdx, Format = normalFmt });
            }

            return runs;
        }



        private void OpenProductSearch(bool toConfigurationArea = true)
        {
            if (allProducts == null || allProducts.Count == 0)
            {
                MessageBox.Show("Danh sách sản phẩm đang trống. Vui lòng nhấn Cập nhật để tải dữ liệu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!toConfigurationArea)
            {
                // ═══ MỞ POPUP FULLSCREEN: Search + Bảng Báo Giá ═══
                OpenFullscreenQuotationPopup();
                return;
            }

            // ═══ Luồng cũ: chỉ mở FrmProductSearch cho khu vực CẤU HÌNH ═══
            var frm = new FrmProductSearch(allProducts, isForQuote: false);
            
            btnOpenSearchModal.Enabled = false;

            frm.OnProductsSelected += (selectedList) =>
            {
                foreach (var p in selectedList)
                {
                    var existing = childProducts.FirstOrDefault(cp => cp.SKU == p.SKU);
                    if (existing != null)
                        existing.SoLuong += p.SoLuong;
                    else
                        childProducts.Add(p);
                }
                AdjustDataGridView1RowHeights();
                dataGridView1.Refresh();
            };

            frm.FormClosed += (s, ev) => { btnOpenSearchModal.Enabled = true; };
            frm.ShowDialog(this);
        }

        /// <summary>
        /// Mở 2 cửa sổ riêng biệt:
        ///   1) Popup fullscreen chứa groupBox2 gốc (BẢNG BÁO GIÁ/ DỰ TOÁN) — reparent từ FrmConfig
        ///   2) FrmProductSearch — popup tìm kiếm sản phẩm riêng
        /// Tắt 1 trong 2 → tắt cả 2.
        /// </summary>
        private void OpenFullscreenQuotationPopup()
        {
            btnOpenSearchModalForQuote.Enabled = false;
            btnOpenSearchModal.Enabled = false;

            // ════════════════════════════════════════════════════════════
            // POPUP 1: Bảng Báo Giá / Dự Toán (FULLSCREEN)
            //   → Reparent groupBox2 từ splitMain.Panel2 vào popup
            // ════════════════════════════════════════════════════════════
            var popupQuote = new Form
            {
                Text = "BẢNG BÁO GIÁ / DỰ TOÁN",
                WindowState = FormWindowState.Maximized,
                StartPosition = FormStartPosition.CenterScreen,
                Font = new Font("Times New Roman", 9F),
                BackColor = Color.White
            };

            // Lưu parent gốc để trả lại khi đóng
            var originalParent = groupBox2.Parent;

            // Di chuyển groupBox2 sang popup
            originalParent.Controls.Remove(groupBox2);
            groupBox2.Dock = DockStyle.Fill;
            popupQuote.Controls.Add(groupBox2);

            // ════════════════════════════════════════════════════════════
            // POPUP 2: Tìm kiếm sản phẩm (cửa sổ riêng)
            // ════════════════════════════════════════════════════════════
            var frmSearch = new FrmProductSearch(allProducts, isForQuote: true);
            frmSearch.StartPosition = FormStartPosition.CenterScreen;

            frmSearch.OnProductsSelected += (selectedList) =>
            {
                AddSelectedProductsToConfig(selectedList);
            };

            frmSearch.OnHeaderAdded += (stt, name) =>
            {
                currentEditingConfigName = null;
                button5.Text = "Lưu";
                configProducts.Add(new ConfigProductItem
                {
                    STT = stt,
                    TenHang = name,
                    MaHang = "",
                    XuatXu = "VNECCO",
                    DonVi = "TỦ",
                    SoLuong = 1,
                    DonGiaVND = 0,
                    ThanhTienVND = 0,
                    GhiChu = "",
                    GiaNhap = 0,
                    ThanhTien = 0,
                    BangGia = 0,
                    IsHeader = true
                });

                for (int i = 0; i < configProducts.Count; i++)
                    configProducts[i].STT = (i + 1).ToString();

                UpdateHeaderSum();
                UpdateConfigGrid();
            };

            frmSearch.OnAdvancedConfigRequested += () =>
            {
                btnAdvancedConfigForQuotation_Click(null, null);
            };

            // ════════════════════════════════════════════════════════════
            // Tắt 1 → tắt cả 2
            // ════════════════════════════════════════════════════════════
            bool isClosing = false;

            popupQuote.FormClosed += (s, ev) =>
            {
                // Trả groupBox2 về vị trí gốc
                popupQuote.Controls.Remove(groupBox2);
                groupBox2.Dock = DockStyle.Fill;
                originalParent.Controls.Add(groupBox2);

                btnOpenSearchModalForQuote.Enabled = true;
                btnOpenSearchModal.Enabled = true;
                UpdateHeaderSum();
                UpdateConfigGrid();

                // Tắt popup search nếu còn mở
                if (!isClosing && !frmSearch.IsDisposed)
                {
                    isClosing = true;
                    frmSearch.Close();
                }
            };

            frmSearch.FormClosed += (s, ev) =>
            {
                // Tắt popup quote nếu còn mở
                if (!isClosing && !popupQuote.IsDisposed)
                {
                    isClosing = true;
                    popupQuote.Close();
                }
            };

            // ════════════════════════════════════════════════════════════
            // Mở cả 2 popup (modeless — cả 2 đều tương tác tự do)
            // ════════════════════════════════════════════════════════════
            // Disable form cha để user không thao tác FrmConfig khi popup đang mở
            var parentForm = this.FindForm();
            if (parentForm != null) parentForm.Enabled = false;

            popupQuote.FormClosed += (s2, ev2) =>
            {
                if (parentForm != null) parentForm.Enabled = true;
                parentForm?.Activate();
            };

            // Mở popup quote modeless (fullscreen)
            popupQuote.Show();

            // Mở popup search modeless (cửa sổ nhỏ, tự do di chuyển)
            frmSearch.Show();
            frmSearch.BringToFront();
        }

        private void AddSelectedProductsToConfig(List<Products> selectedItems)
        {
            if (selectedItems.Count == 0) return;

            // Tự động thêm dòng Header nếu danh sách đang rỗng 
            if (configProducts.Count == 0 || !configProducts.Any(p => p.IsHeader))
            {
                string headerName = "Sản phẩm từ tìm kiếm";
                button5.Text = "Lưu";
                currentEditingConfigName = null;
                configProducts.Add(new ConfigProductItem
                {
                    STT = "1",
                    TenHang = headerName,
                    DonVi = "TỦ",
                    SoLuong = 1,
                    IsHeader = true,
                    XuatXu = "VNECCO"
                });
            }

            foreach (var product in selectedItems)
            {
                var existing = configProducts.FirstOrDefault(p => p.MaHang == product.SKU && !p.IsHeader);
                if (existing != null)
                {
                    existing.SoLuong += product.SoLuong;
                    existing.ThanhTienVND = existing.SoLuong * existing.DonGiaVND;
                    existing.ThanhTien = existing.SoLuong * existing.GiaNhap;
                    existing.LoiNhuan = existing.ThanhTienVND - existing.ThanhTien;
                }
                else
                {
                    decimal price = 0;
                    decimal.TryParse(product.Price?.Replace(".", "").Replace(",", ""), out price);
                    decimal priceCost = 0;
                    decimal.TryParse(product.PriceCost?.Replace(".", "").Replace(",", ""), out priceCost);

                    var newItem = new ConfigProductItem
                    {
                        TenHang = product.Name,
                        MaHang = product.SKU,
                        XuatXu = product.HÃNG,
                        DonVi = ConfigProductItem.IsPinned(product.Name) ? GetPinnedDonVi(product.Name) : "Cái",
                        SoLuong = product.SoLuong,
                        DonGiaVND = price,
                        ThanhTienVND = price * product.SoLuong,
                        GiaNhap = priceCost > 0 ? priceCost : price,
                        ThanhTien = (priceCost > 0 ? priceCost : price) * product.SoLuong,
                        LoiNhuan = (price - (priceCost > 0 ? priceCost : price)) * product.SoLuong,
                        IsHeader = false
                    };

                    // TÌM VỊ TRÍ CHÈN: Trước các dòng Phụ kiện/Đồng/Nhân công
                    int insertIdx = configProducts.Count;
                    for (int i = 0; i < configProducts.Count; i++)
                    {
                        if (configProducts[i].IsHeader) continue;
                        string name = configProducts[i].TenHang ?? "";
                        if (name.Contains("đồng thanh cái") || name.Contains("Phụ kiện") || name.Contains("Nhân công"))
                        {
                            insertIdx = i;
                            break;
                        }
                    }
                    configProducts.Insert(insertIdx, newItem);
                }
            }

            // Cập nhật lại STT toàn bộ
            for (int i = 0; i < configProducts.Count; i++)
                configProducts[i].STT = (i + 1).ToString();

            UpdateHeaderSum();
            UpdateConfigGrid();
        }


        private void UpdateHeaderSum()
        {
            // Cập nhật tổng cho TỪ́NG header - mỗi nhóm tính riêng
            for (int i = 0; i < configProducts.Count; i++)
            {
                if (!configProducts[i].IsHeader) continue;

                // Phạm vi nhóm: từ i+1 đến header kế tiếp (hoặc cuối list)
                int groupEnd = i + 1;
                while (groupEnd < configProducts.Count && !configProducts[groupEnd].IsHeader)
                    groupEnd++;

                var groupItems = configProducts
                    .Skip(i + 1).Take(groupEnd - i - 1)
                    .Where(p => !p.IsHeader && !p.IsSummary)
                    .ToList();

                configProducts[i].DonGiaVND = groupItems.Sum(p => p.DonGiaVND * p.SoLuong);
                configProducts[i].ThanhTienVND = groupItems.Sum(p => p.ThanhTienVND);
                configProducts[i].GiaNhap = groupItems.Sum(p => p.GiaNhap * p.SoLuong);
                configProducts[i].ThanhTien = groupItems.Sum(p => p.ThanhTien);
                configProducts[i].LoiNhuan = groupItems.Sum(p => p.LoiNhuan);
                configProducts[i].BangGia = groupItems.Sum(p => p.BangGia);
            }
        }


        private void DgvParentProducts_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvParentProducts.IsCurrentCellDirty)
            {
                dgvParentProducts.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void DgvParentProducts_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                var columnName = dgvParentProducts.Columns[e.ColumnIndex].Name;
                if (columnName == "SoLuong" || columnName == "DonGiaVND" || columnName == "GiaNhap")
                {
                    var item = dgvParentProducts.Rows[e.RowIndex].DataBoundItem as ConfigProductItem;
                    if (item != null && !item.IsHeader)
                    {
                        item.ThanhTienVND = item.SoLuong * item.DonGiaVND;
                        item.ThanhTien = item.SoLuong * item.GiaNhap;
                        item.LoiNhuan = item.ThanhTienVND - item.ThanhTien;
                        item.BangGia = 0;

                        UpdateHeaderSum();
                        dgvParentProducts.Refresh(); // Gọi refresh thay vì InvalidateRow để có thể update row Header
                    }
                }
            }
        }

        private void BtnRemoveParent_Click(object sender, EventArgs e)
        {
            if (configProducts.Count > 0)
            {
                if (MessageBox.Show("Bạn có chắc chắn muốn xóa toàn bộ danh sách báo giá?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    configProducts.Clear();
                    UpdateHeaderSum();
                    UpdateConfigGrid();
                }
            }
        }

        private void UpdateConfigGrid()
        {
            // Tạo bản sao cho DataSource để không ảnh hưởng configProducts gốc
            var baseList = configProducts.Where(p => !p.IsSummary).ToList();
            _displayList = new List<ConfigProductItem>();
            bool isCurrentGroupCollapsed = false;

            foreach (var p in baseList)
            {
                if (p.IsHeader)
                {
                    isCurrentGroupCollapsed = _collapsedGroups.Contains(p.TenHang);
                    _displayList.Add(p);
                }
                else
                {
                    if (!isCurrentGroupCollapsed)
                    {
                        _displayList.Add(p);
                    }
                }
            }

            if (_displayList.Count > 0)
            {
                // Tính tổng (chỉ tính các dòng không phải header, DỰA TRÊN BASELIST để không bị sai khi thu gọn)
                decimal tongCongGiaNhap = baseList.Where(p => !p.IsHeader).Sum(p => p.ThanhTien);
                decimal tongCongThanhTien = baseList.Where(p => !p.IsHeader).Sum(p => p.ThanhTienVND);
                decimal tongCongLoiNhuan = baseList.Where(p => !p.IsHeader).Sum(p => p.LoiNhuan);
                decimal tongCongBangGia = baseList.Where(p => !p.IsHeader).Sum(p => p.BangGia);
                decimal vatRate = 0.08m;
                decimal vatGiaNhap = tongCongGiaNhap * vatRate;
                decimal vatThanhTien = tongCongThanhTien * vatRate;

                _displayList.Add(new ConfigProductItem
                {
                    STT = "",
                    TenHang = "TỔNG CỘNG (Giá chưa bao gồm VAT)",
                    DonGiaVND = 0,
                    ThanhTienVND = tongCongThanhTien,
                    GiaNhap = tongCongThanhTien - tongCongGiaNhap,
                    ThanhTien = tongCongGiaNhap,
                    LoiNhuan = tongCongThanhTien - tongCongGiaNhap,
                    BangGia = 0,
                    IsSummary = true
                });
                _displayList.Add(new ConfigProductItem
                {
                    STT = "",
                    TenHang = "THUẾ VAT 8%",
                    DonGiaVND = 0,
                    ThanhTienVND = vatThanhTien,
                    GiaNhap = 0,
                    ThanhTien = vatGiaNhap,
                    IsSummary = true
                });
                _displayList.Add(new ConfigProductItem
                {
                    STT = "",
                    TenHang = "THÀNH TIỀN",
                    DonGiaVND = tongCongThanhTien + vatThanhTien,
                    ThanhTienVND = tongCongThanhTien + vatThanhTien,
                    GiaNhap = tongCongGiaNhap + vatGiaNhap,
                    ThanhTien = tongCongGiaNhap + vatGiaNhap,
                    BangGia = 0,
                    IsSummary = true
                });
            }

            dgvParentProducts.DataSource = _displayList;

            // Đảm bảo cột ▲▼ luôn tồn tại sau khi DataSource được reset
            EnsureMoveColumns(dgvParentProducts);

            // ═══ Set style trực tiếp cho từng dòng ═══
            for (int i = 0; i < _displayList.Count; i++)
            {
                var row = dgvParentProducts.Rows[i];
                var item = _displayList[i];

                if (item.IsSummary)
                {
                    // Nền VÀNG, Bold (giống bảng báo giá)
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 255, 0);
                    row.DefaultCellStyle.ForeColor = Color.Black;
                    row.DefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

                    // Số tiền → bôi đậm, màu đen (mặc định của row). Riêng Giá Nhập của TỔNG CỘNG thì màu đỏ.
                    foreach (var colName in new[] { "DonGiaVND", "ThanhTienVND", "GiaNhap", "ThanhTien", "LoiNhuan", "BangGia" })
                    {
                        if (dgvParentProducts.Columns.Contains(colName))
                        {
                            if (colName == "GiaNhap" && item.TenHang.StartsWith("TỔNG CỘNG"))
                            {
                                row.Cells[colName].Style.ForeColor = Color.Red;
                            }
                            else
                            {
                                row.Cells[colName].Style.ForeColor = Color.Black;
                            }
                            row.Cells[colName].Style.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                        }
                    }
                }
                else if (item.IsHeader)
                {
                    row.DefaultCellStyle.BackColor = Color.LightGreen;
                    row.DefaultCellStyle.ForeColor = Color.Black;
                    row.DefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                }
            }

        }
        private void FormatConfigGrid(DataGridView dgv)
        {
            if (dgv == null || dgv.IsDisposed || dgv.Columns == null || dgv.Columns.Count == 0) return;

            try
            {
                // Thêm cột ▲▼ trước tiên
                EnsureMoveColumns(dgv);

                if (dgv.Columns.Contains("STT")) dgv.Columns["STT"].HeaderText = "STT";
                if (dgv.Columns.Contains("TenHang")) dgv.Columns["TenHang"].HeaderText = "Tên hàng";
                if (dgv.Columns.Contains("MaHang")) dgv.Columns["MaHang"].HeaderText = "Mã hàng";
                if (dgv.Columns.Contains("XuatXu")) dgv.Columns["XuatXu"].HeaderText = "Xuất xứ";
                if (dgv.Columns.Contains("DonVi")) dgv.Columns["DonVi"].HeaderText = "Đơn vị";
                if (dgv.Columns.Contains("SoLuong")) dgv.Columns["SoLuong"].HeaderText = "Số lượng";

                if (dgv.Columns.Contains("DonGiaVND"))
                {
                    dgv.Columns["DonGiaVND"].HeaderText = "Đơn giá (VNĐ)";
                    dgv.Columns["DonGiaVND"].DefaultCellStyle.Format = "N0";
                }
                if (dgv.Columns.Contains("ThanhTienVND"))
                {
                    dgv.Columns["ThanhTienVND"].HeaderText = "Thành tiền (VNĐ)";
                    dgv.Columns["ThanhTienVND"].DefaultCellStyle.Format = "N0";
                }
                if (dgv.Columns.Contains("GhiChu")) dgv.Columns["GhiChu"].HeaderText = "Ghi chú";
                if (dgv.Columns.Contains("GiaNhap"))
                {
                    dgv.Columns["GiaNhap"].HeaderText = "Giá Nhập";
                    dgv.Columns["GiaNhap"].DefaultCellStyle.Format = "N0";
                    dgv.Columns["GiaNhap"].Visible = true;
                }
                if (dgv.Columns.Contains("TienDo")) 
                {
                    dgv.Columns["TienDo"].HeaderText = "Tiến độ";
                    dgv.Columns["TienDo"].Visible = true;
                }
                if (dgv.Columns.Contains("ThanhTien"))
                {
                    dgv.Columns["ThanhTien"].HeaderText = "Thành Tiền";
                    dgv.Columns["ThanhTien"].DefaultCellStyle.Format = "N0";
                    dgv.Columns["ThanhTien"].Visible = true;
                }
                if (dgv.Columns.Contains("LoiNhuan"))
                {
                    dgv.Columns["LoiNhuan"].HeaderText = "Lợi nhuận";
                    dgv.Columns["LoiNhuan"].DefaultCellStyle.Format = "N0";
                    dgv.Columns["LoiNhuan"].Visible = true;
                }
                if (dgv.Columns.Contains("BangGia"))
                {
                    dgv.Columns["BangGia"].HeaderText = "Bảng Giá";
                    dgv.Columns["BangGia"].DefaultCellStyle.Format = "N0";
                    dgv.Columns["BangGia"].Visible = true;
                }

                if (dgv.Columns.Contains("IsHeader")) dgv.Columns["IsHeader"].Visible = false;
                if (dgv.Columns.Contains("IsSummary")) dgv.Columns["IsSummary"].Visible = false;
                if (dgv.Columns.Contains("SheetRowIndex")) dgv.Columns["SheetRowIndex"].Visible = false;

                // ── Kiểu dáng tổng thể ───────────────────────────────────
                dgv.BackgroundColor = Color.White;
                dgv.GridColor = Color.FromArgb(189, 215, 238);
                dgv.BorderStyle = BorderStyle.FixedSingle;
                dgv.CellBorderStyle = DataGridViewCellBorderStyle.Single; // Viền đầy đủ 4 cạnh
                dgv.RowHeadersVisible = false;
                dgv.EnableHeadersVisualStyles = false;
                dgv.AllowUserToAddRows = false; // Không tạo dòng trống cuối
                dgv.ColumnHeadersHeight = 36;
                dgv.RowTemplate.Height = 36;

                // Dòng dữ liệu: nền trắng, chữ đen
                dgv.DefaultCellStyle.BackColor = Color.White;
                dgv.DefaultCellStyle.ForeColor = Color.Black;
                dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 112, 192);
                dgv.DefaultCellStyle.SelectionForeColor = Color.White;
                dgv.DefaultCellStyle.Font = new Font("Segoe UI", 8.5f);
                dgv.DefaultCellStyle.Padding = new Padding(2, 1, 2, 1);

                // Header cột chính: nền vàng, chữ xanh đậm, bold, căn giữa
                var yellowHeader = new DataGridViewCellStyle
                {
                    BackColor = Color.Yellow,
                    ForeColor = Color.FromArgb(31, 73, 125),
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    WrapMode = DataGridViewTriState.True
                };
                dgv.ColumnHeadersDefaultCellStyle = yellowHeader;

                // Cột Giá Nhập, Thành Tiền: Cyan
                var cyanHeader = new DataGridViewCellStyle(yellowHeader)
                {
                    BackColor = Color.Cyan,
                    ForeColor = Color.Black
                };
                foreach (var colName in new[] { "GiaNhap", "ThanhTien" })
                {
                    if (dgv.Columns.Contains(colName))
                        dgv.Columns[colName].HeaderCell.Style = cyanHeader;
                }

                // Cột Lợi nhuận: Yellow (giữ nguyên nền vàng, đổi chữ đỏ)
                var loiNhuanHeader = new DataGridViewCellStyle(yellowHeader)
                {
                    ForeColor = Color.Red
                };
                if (dgv.Columns.Contains("LoiNhuan"))
                    dgv.Columns["LoiNhuan"].HeaderCell.Style = loiNhuanHeader;

                // Cột Bảng Giá: Light Blue
                var lightBlueHeader = new DataGridViewCellStyle(yellowHeader)
                {
                    BackColor = Color.CornflowerBlue,
                    ForeColor = Color.Black
                };
                if (dgv.Columns.Contains("BangGia"))
                    dgv.Columns["BangGia"].HeaderCell.Style = lightBlueHeader;

                dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgv.SelectionMode = DataGridViewSelectionMode.CellSelect;
                dgv.MultiSelect = true;

                // Button move column: không dùng AutoFill
                if (dgv.Columns.Contains("ColMove"))
                    dgv.Columns["ColMove"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

                // FillWeight: tỉ lệ chiều rộng cột
                if (dgv.Columns.Contains("STT")) dgv.Columns["STT"].FillWeight = 25;
                if (dgv.Columns.Contains("TenHang")) dgv.Columns["TenHang"].FillWeight = 200;
                if (dgv.Columns.Contains("MaHang")) dgv.Columns["MaHang"].FillWeight = 80;
                if (dgv.Columns.Contains("XuatXu")) dgv.Columns["XuatXu"].FillWeight = 50;
                if (dgv.Columns.Contains("DonVi")) dgv.Columns["DonVi"].FillWeight = 40;
                if (dgv.Columns.Contains("SoLuong")) dgv.Columns["SoLuong"].FillWeight = 40;
                if (dgv.Columns.Contains("DonGiaVND")) dgv.Columns["DonGiaVND"].FillWeight = 90;
                if (dgv.Columns.Contains("ThanhTienVND")) dgv.Columns["ThanhTienVND"].FillWeight = 90;
                if (dgv.Columns.Contains("GhiChu")) dgv.Columns["GhiChu"].FillWeight = 70;
                if (dgv.Columns.Contains("GiaNhap")) dgv.Columns["GiaNhap"].FillWeight = 90;
                if (dgv.Columns.Contains("ThanhTien")) dgv.Columns["ThanhTien"].FillWeight = 90;
                if (dgv.Columns.Contains("LoiNhuan")) dgv.Columns["LoiNhuan"].FillWeight = 90;
                if (dgv.Columns.Contains("BangGia")) dgv.Columns["BangGia"].FillWeight = 90;

                // Căn giữa cột STT, Xuất xứ, Đơn vị, Số lượng
                foreach (var colName in new[] { "STT", "XuatXu", "DonVi", "SoLuong" })
                {
                    if (dgv.Columns.Contains(colName))
                        dgv.Columns[colName].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                // Căn phải cột số tiền
                foreach (var colName in new[] { "DonGiaVND", "ThanhTienVND", "GiaNhap", "ThanhTien", "LoiNhuan", "BangGia" })
                {
                    if (dgv.Columns.Contains(colName))
                        dgv.Columns[colName].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }

                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    // ColMove là custom cell — không set ReadOnly để nhận CellMouseClick
                    if (col.Name == "ColMove") { col.ReadOnly = false; continue; }
                    if (col.Name != "SoLuong" && col.Name != "GhiChu" && col.Name != "TenHang" && col.Name != "DonGiaVND" && col.Name != "GiaNhap")
                        col.ReadOnly = true;
                    else
                        col.ReadOnly = false;
                }
            }
            catch (Exception) { /* Ignore lifecycle exceptions */ }
        }

        /// <summary>
        /// Thêm 1 cột MoveButton (▲▼ trong 1 ô) vào dgvParentProducts nếu chưa có.
        /// </summary>
        private void EnsureMoveColumns(DataGridView dgv)
        {
            if (dgv.Columns.Contains("ColMove")) return;

            var col = new ECQ_Soft.Helper.MoveButtonColumn
            {
                Name         = "ColMove",
                HeaderText   = "",
                Width        = 28,
                MinimumWidth = 28,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                ReadOnly     = false,
                DisplayIndex = 0,
                Resizable    = DataGridViewTriState.False,
                SortMode     = DataGridViewColumnSortMode.NotSortable,
            };
            dgv.Columns.Insert(0, col);
        }


        private void DgvParentProducts_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_displayList == null || e.RowIndex < 0 || e.RowIndex >= _displayList.Count) return;

            var item = _displayList[e.RowIndex];
            string colName = dgvParentProducts.Columns[e.ColumnIndex].Name;

            // Bỏ qua cột custom ▲▼ (không phải data column)
            if (colName == "ColMove") return;

            // ── Override cột STT: số La Mã cho header, 1→n cho dòng con ──
            if (colName == "STT")
            {
                if (item.IsSummary)
                {
                    e.Value = "";
                    e.FormattingApplied = true;
                }
                else if (item.IsHeader)
                {
                    int headerOrder = _displayList.Take(e.RowIndex + 1).Count(x => x.IsHeader);
                    e.Value = ToRoman(headerOrder);
                    e.FormattingApplied = true;
                }
                else
                {
                    int childIndex = 0;
                    for (int i = e.RowIndex - 1; i >= 0; i--)
                    {
                        if (_displayList[i].IsHeader) break;
                        if (!_displayList[i].IsSummary) childIndex++;
                    }
                    e.Value = (childIndex + 1).ToString();
                    e.FormattingApplied = true;
                }
            }

            // HIỂN THỊ DẤU "-" CHO CÁC Ô TRỐNG (MÃ HÀNG, XUẤT XỨ, ĐƠN VỊ, TIẾN ĐỘ)
            if (!item.IsSummary && !item.IsHeader)
            {
                var dashCols = new[] { "MaHang", "XuatXu", "DonVi", "TienDo" };
                if (Array.IndexOf(dashCols, colName) >= 0)
                {
                    if (e.Value == null || string.IsNullOrWhiteSpace(e.Value.ToString()))
                    {
                        e.Value = "-";
                        e.FormattingApplied = true;
                    }
                }
            }

            if (item.IsSummary)
            {
                e.CellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);

                // Ẩn giá trị 0 ở các cột không liên quan (giống gộp ô trong Excel)
                // Đơn giá (VNĐ) cũng được ẩn đi ở tất cả các dòng tổng kết
                var hiddenCols = new[] { "STT", "MaHang", "XuatXu", "DonVi", "SoLuong", "GhiChu", "DonGiaVND" };
                if (Array.IndexOf(hiddenCols, colName) >= 0)
                {
                    e.Value = "";
                    e.FormattingApplied = true;
                }

                // Ẩn Giá Nhập và Bảng Giá cho các dòng THUẾ VAT và THÀNH TIỀN
                if ((colName == "LoiNhuan" || colName == "BangGia" || colName == "GiaNhap") && (item.TenHang.StartsWith("THUẾ VAT") || item.TenHang == "THÀNH TIỀN"))
                {
                    e.Value = "";
                    e.FormattingApplied = true;
                }

                // Định dạng màu chữ cho cấu hình hiển thị như trong Excel mẫu
                var numberCols = new[] { "DonGiaVND", "ThanhTienVND", "GiaNhap", "ThanhTien", "LoiNhuan", "BangGia" };
                if (Array.IndexOf(numberCols, colName) >= 0)
                {
                    if (colName == "GiaNhap" || colName == "ThanhTien")
                    {
                        e.CellStyle.BackColor = Color.Cyan;
                        e.CellStyle.ForeColor = Color.Black;
                    }
                    else if (colName == "LoiNhuan")
                    {
                        e.CellStyle.BackColor = Color.Yellow;
                        e.CellStyle.ForeColor = Color.Red;
                    }
                    else if (colName == "BangGia")
                    {
                        e.CellStyle.BackColor = Color.CornflowerBlue;
                        e.CellStyle.ForeColor = Color.Black;
                    }
                    else
                    {
                        e.CellStyle.BackColor = Color.Yellow; // IsSummary có nền vàng
                        e.CellStyle.ForeColor = Color.Black;
                    }
                }
                else
                {
                    e.CellStyle.BackColor = Color.Yellow;
                    e.CellStyle.ForeColor = Color.Black;
                }
            }
            else if (item.IsHeader)
            {
                // Dòng header nhóm
                if (colName == "GiaNhap" || colName == "ThanhTien")
                {
                    e.CellStyle.BackColor = Color.Cyan;
                    e.CellStyle.ForeColor = Color.Black;
                    e.CellStyle.SelectionBackColor = Color.DeepSkyBlue;
                    e.CellStyle.SelectionForeColor = Color.Black;
                }
                else if (colName == "LoiNhuan")
                {
                    e.CellStyle.BackColor = Color.Yellow;
                    e.CellStyle.ForeColor = Color.Red;
                    e.CellStyle.SelectionBackColor = Color.Gold;
                    e.CellStyle.SelectionForeColor = Color.Red;
                }
                else if (colName == "BangGia")
                {
                    e.CellStyle.BackColor = Color.CornflowerBlue;
                    e.CellStyle.ForeColor = Color.Black;
                    e.CellStyle.SelectionBackColor = Color.RoyalBlue;
                    e.CellStyle.SelectionForeColor = Color.White;
                }
                else
                {
                    e.CellStyle.BackColor = Color.LightGreen;
                    e.CellStyle.ForeColor = Color.Black;
                    e.CellStyle.SelectionBackColor = Color.LimeGreen;
                    e.CellStyle.SelectionForeColor = Color.Black;
                }
                e.CellStyle.Font = new Font(dgvParentProducts.Font, FontStyle.Bold);
            }

            // Áp dụng màu tuỳ chỉnh per-cell từ Google Sheet (nếu có lưu SheetRowIndex)
            if (item.SheetRowIndex >= 0)
            {
                // Mapping: Tên cột DGV -> Index cột trên Sheet (0-12)
                string[] sheetColOrder = { "STT", "TenHang", "MaHang", "XuatXu", "DonVi", "SoLuong",
                                         "DonGiaVND", "ThanhTienVND", "GhiChu", "GiaNhap", "ThanhTien", "LoiNhuan", "BangGia" };
                int sheetColIdx = Array.IndexOf(sheetColOrder, colName);

                if (sheetColIdx >= 0)
                {
                    var sheetKey = (item.SheetRowIndex, sheetColIdx);
                    if (_sheetBgColors.TryGetValue(sheetKey, out Color sBg))
                        e.CellStyle.BackColor = sBg;
                    if (_sheetFgColors.TryGetValue(sheetKey, out Color sFg))
                        e.CellStyle.ForeColor = sFg;
                }
            }

            // Áp dụng màu tuỳ chỉnh per-cell được chọn trực tiếp trong session này (Ghi đè màu gốc từ Sheet)
            if (e.RowIndex >= 0 && e.RowIndex < _displayList.Count)
            {
                var rowItem = _displayList[e.RowIndex];
                var key = (rowItem, e.ColumnIndex);
                if (_cellBgColors.TryGetValue(key, out Color bg))
                    e.CellStyle.BackColor = bg;
                if (_cellFgColors.TryGetValue(key, out Color fg))
                    e.CellStyle.ForeColor = fg;
                if (_cellFonts.TryGetValue(key, out Font cFont))
                    e.CellStyle.Font = cFont;
            }
        }

        /// <summary>Chuyển số nguyên dương sang chữ số La Mã (I, II, III, IV...)</summary>
        private static string ToRoman(int number)
        {
            if (number <= 0) return "";
            var map = new (int val, string sym)[]
            {
                (1000,"M"),(900,"CM"),(500,"D"),(400,"CD"),
                (100,"C"),(90,"XC"),(50,"L"),(40,"XL"),
                (10,"X"),(9,"IX"),(5,"V"),(4,"IV"),(1,"I")
            };
            var result = new System.Text.StringBuilder();
            foreach (var (val, sym) in map)
                while (number >= val) { result.Append(sym); number -= val; }
            return result.ToString();
        }

        private void UpdateGridSelector(DataGridView dgv, List<Products> source)
        {
            dgv.DataSource = source.ToList();
            // FormatDataGridView will be called by DataBindingComplete
        }

        
        private void SetupProductManagementUI()
        {
            // Empty method as product management UI was removed from FrmConfig
        }


        private async void btnAdvancedConfigForQuote_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmAdvancedConfig())
            {
                await frm.LoadDataAsync(_sheetsService, spreadsheetId);
                if (frm.IsCanceled) return;
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    AddAdvancedConfigResult("CẤU HÌNH TỪ GỢI Ý CHUYÊN SÂU", frm.SelectedAdvancedItems);
                }
            }
        }

        private async void button9_Click(object sender, EventArgs e)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                await LoadDataAsync();
                this.Cursor = Cursors.Default;
                MessageBox.Show("Đã cập nhật dữ liệu từ Google Sheets!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show($"Lỗi khi cập nhật dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveToCache<T>(string key, T data)
        {
            try
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                File.WriteAllText(GetCachePath(key), json);
            }
            catch { }
        }

        private T LoadFromCache<T>(string key)
        {
            try
            {
                string path = GetCachePath(key);
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
                }
            }
            catch { }
            return default;
        }

        private void Grid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            // Suppress technical dialogs silently
            e.ThrowException = false;
        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv != null && e.KeyCode == Keys.Space)
            {
                ToggleSelectedRows(dgv);
                e.Handled = true;
            }
        }

        private void Grid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv != null && dgv.Columns[e.ColumnIndex].Name == "IsSelected")
            {
                dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);

                // If multiple rows are selected, sync their checkbox with the clicked one
                if (dgv.SelectedRows.Count > 1)
                {
                    bool newValue = (bool)dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                    foreach (DataGridViewRow row in dgv.SelectedRows)
                    {
                        var item = row.DataBoundItem as Products;
                        if (item != null) item.IsSelected = newValue;
                    }
                    dgv.Refresh();
                }
            }
        }

        private void ToggleSelectedRows(DataGridView dgv)
        {
            if (dgv.SelectedRows.Count > 0)
            {
                // Find current state from first selected row
                var firstItem = dgv.SelectedRows[0].DataBoundItem as Products;
                if (firstItem != null)
                {
                    bool newValue = !firstItem.IsSelected;
                    foreach (DataGridViewRow row in dgv.SelectedRows)
                    {
                        var item = row.DataBoundItem as Products;
                        if (item != null) item.IsSelected = newValue;
                    }
                    dgv.Refresh();
                }
            }
        }

        private void Button7_Click(object sender, EventArgs e)
        {
            childProducts.Clear();
            chkSelectAllChildProducts.Checked = false;
        }

        private void btn_baogia_Click(object sender, EventArgs e)
        {
            // Lấy TẤT CẢ sản phẩm từ bảng trên (tránh trùng lặp)
            var allItems = childProducts.ToList();
            if (allItems.Count == 0) return;

            // Lấy tên nhóm từ ComboBox hiện tại, hoặc gán chi tiết tên mặc định
            string catPR = comboBox1.SelectedItem?.ToString();
            bool hasCatPR = !string.IsNullOrEmpty(catPR) && catPR != "-- Tất cả danh mục --" && catPR != "-- Chọn cấu hình đóng gói --";
            string headerName = hasCatPR ? catPR : "Gói sản phẩm báo giá";

            // Tìm xem header này đã tồn tại trong danh sách dgvParentProducts chưa
            int headerIdx = configProducts.FindIndex(p =>
                p.IsHeader && string.Equals(p.TenHang?.Trim(), headerName?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (headerIdx < 0)
            {
                // Chưa có header -> Thêm mới dòng header màu xanh
                configProducts.Add(new ConfigProductItem
                {
                    STT = (configProducts.Count + 1).ToString(),
                    TenHang = headerName,
                    MaHang = "",
                    XuatXu = "VNECCO",
                    DonVi = "TỦ",
                    SoLuong = 1,
                    DonGiaVND = 0,
                    ThanhTienVND = 0,
                    GhiChu = "",
                    GiaNhap = 0,
                    ThanhTien = 0,
                    BangGia = 0,
                    IsHeader = true
                });

                // Thêm các sản phẩm con xuống dưới header
                foreach (var product in allItems)
                {
                    // Logic cũ: if (!configProducts.Any(x => x.MaHang == product.SKU))
                    // Sửa lại: Nếu SKU trống -> Kiểm tra theo Tên. Nếu có SKU -> Kiểm tra theo SKU.
                    bool isDuplicate = false;
                    if (!string.IsNullOrEmpty(product.SKU))
                        isDuplicate = configProducts.Any(x => x.MaHang == product.SKU);
                    else
                        isDuplicate = configProducts.Any(x => string.IsNullOrEmpty(x.MaHang) && x.TenHang == product.Name);

                    if (!isDuplicate)
                    {
                        decimal price = 0; decimal.TryParse(product.Price?.Replace(".", "").Replace(",", ""), out price);
                        decimal priceCost = 0; decimal.TryParse(product.PriceCost?.Replace(".", "").Replace(",", ""), out priceCost);
                        configProducts.Add(CreateConfigItem(product, price, priceCost));
                    }
                }
            }
            else
            {
                // Đã có header -> Tìm phạm vi của nhóm này
                int groupEndIdx = headerIdx + 1;
                while (groupEndIdx < configProducts.Count && !configProducts[groupEndIdx].IsHeader)
                {
                    groupEndIdx++;
                }

                int insertIdx = groupEndIdx; // Thêm dòng mới vào cuối nhóm

                foreach (var product in allItems)
                {
                    // Chỉ tìm trong group hiện tại (từ headerIdx + 1 đến groupEndIdx - 1)
                    ConfigProductItem existingItem = null;
                    for (int k = headerIdx + 1; k < groupEndIdx; k++)
                    {
                        var p = configProducts[k];
                        if (!string.IsNullOrEmpty(product.SKU))
                        {
                            if (string.Equals(p.MaHang, product.SKU, StringComparison.OrdinalIgnoreCase))
                            {
                                existingItem = p;
                                break;
                            }
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(p.MaHang) && string.Equals(p.TenHang, product.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                existingItem = p;
                                break;
                            }
                        }
                    }

                    decimal price = 0; decimal.TryParse(product.Price?.Replace(".", "").Replace(",", ""), out price);
                    decimal priceCost = 0; decimal.TryParse(product.PriceCost?.Replace(".", "").Replace(",", ""), out priceCost);

                    if (existingItem != null)
                    {
                        // UPDATE dòng đã có
                        existingItem.DonGiaVND = price;
                        existingItem.GiaNhap = priceCost;
                        existingItem.SoLuong = product.SoLuong > 0 ? product.SoLuong : 1;
                        existingItem.ThanhTienVND = price * existingItem.SoLuong;
                        existingItem.ThanhTien = priceCost * existingItem.SoLuong;
                        existingItem.LoiNhuan = (price - priceCost) * existingItem.SoLuong;
                        existingItem.DonVi = ConfigProductItem.IsPinned(product.Name) ? GetPinnedDonVi(product.Name) : "Cái";
                    }
                    else
                    {
                        // THÊM dòng mới vào cuối nhóm
                        configProducts.Insert(insertIdx++, CreateConfigItem(product, price, priceCost));
                    }
                }
            }

            // Đánh lại số thứ tự (STT) cho toàn bộ bảng
            for (int i = 0; i < configProducts.Count; i++)
                configProducts[i].STT = (i + 1).ToString();

            // Tính tổng nhóm và refresh form
            UpdateHeaderSum();
            UpdateConfigGrid();

            // Nhảy xuống dòng cuối cùng trên grid để user dễ nhìn
            if (dgvParentProducts.Rows.Count > 0)
            {
                dgvParentProducts.FirstDisplayedScrollingRowIndex = dgvParentProducts.Rows.Count - 1;
            }

            // Tự động bật popup báo giá lên cho user thấy
            // btnPopOutQuote_Click(null, null);
        }


        private void BtnExportExcel_Click(object sender, EventArgs e)
        {
            if (configProducts.Count == 0)
            {
                MessageBox.Show("Danh s\u00e1ch c\u1ea5u h\u00ecnh \u0111ang tr\u1ed1ng, kh\u00f4ng c\u00f3 d\u1eef li\u1ec7u \u0111\u1ec3 xu\u1ea5t!",
                    "Th\u00f4ng b\u00e1o", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ExportConfigToExcel(dgvParentProducts, configProducts);
        }

        private void ExportConfigToExcel(DataGridView dgv, List<ConfigProductItem> items)
        {
            try
            {
                Excel.Application excelApp = new Excel.Application();
                excelApp.Visible = true;
                excelApp.DisplayAlerts = false;

                Excel.Workbook workbook = excelApp.Workbooks.Add(Type.Missing);
                Excel.Worksheet ws = workbook.ActiveSheet;
                ws.Name = "Danh Sach Cau Hinh";

                // Thu thập các cột hiện thị
                var visibleCols = new List<DataGridViewColumn>();
                foreach (DataGridViewColumn col in dgv.Columns)
                    if (col.Visible) visibleCols.Add(col);

                // ── 1. Header cột ──────────────────────────────────────────────
                // Lấy màu header từ DGV (ColumnHeadersDefaultCellStyle.BackColor)
                Color dgvHeaderBg = dgv.ColumnHeadersDefaultCellStyle.BackColor;
                Color dgvHeaderFg = dgv.ColumnHeadersDefaultCellStyle.ForeColor;

                for (int c = 0; c < visibleCols.Count; c++)
                {
                    Excel.Range hCell = (Excel.Range)ws.Cells[1, c + 1];
                    hCell.Value2 = visibleCols[c].HeaderText;

                    // Lấy màu header riêng của từng cột (ví dụ cột giá màu xanh)
                    Color colHdrBg = visibleCols[c].HeaderCell.Style.BackColor != Color.Empty
                                     ? visibleCols[c].HeaderCell.Style.BackColor : dgvHeaderBg;
                    Color colHdrFg = visibleCols[c].HeaderCell.Style.ForeColor != Color.Empty
                                     ? visibleCols[c].HeaderCell.Style.ForeColor : dgvHeaderFg;

                    hCell.Interior.Color = ColorTranslator.ToOle(colHdrBg);
                    hCell.Font.Color = ColorTranslator.ToOle(colHdrFg);
                    hCell.Font.Bold = true;
                    hCell.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                    hCell.VerticalAlignment = Excel.XlVAlign.xlVAlignCenter;
                }

                // ── 2. Dữ liệu + màu nền/chữ theo logic DGV ──────────────────
                // ** Không đọc từ dgvRow.DefaultCellStyle vì CellFormatting là dynamic,
                //    màu không được lưu vào Style. Phải áp dụng cùng quy tắc với UpdateConfigGrid. **
                var moneyCols = new[] { "DonGiaVND", "ThanhTienVND", "GiaNhap", "ThanhTien", "LoiNhuan", "BangGia" };
                var hiddenSumCols = new[] { "STT", "MaHang", "XuatXu", "DonVi", "SoLuong", "GhiChu" };

                for (int r = 0; r < _displayList.Count; r++)
                {
                    var item = _displayList[r];

                    // --- Quy tắc màu dòng (giống UpdateConfigGrid + CellFormatting) ---
                    Color rowBg;
                    Color rowFg;
                    bool rowBold;

                    if (item.IsSummary)
                    {
                        rowBg = Color.Yellow;         // Dòng tổng: nền vàng
                        rowFg = Color.Black;
                        rowBold = true;
                    }
                    else if (item.IsHeader)
                    {
                        rowBg = Color.LightGreen;     // Dòng header nhóm: xanh lá
                        rowFg = Color.Black;
                        rowBold = true;
                    }
                    else
                    {
                        rowBg = Color.White;          // Dòng thường: trắng
                        rowFg = Color.Black;
                        rowBold = false;
                    }

                    // --- Ghi từng ô: đọc giá trị trực tiếp từ item, không qua DGV cell
                    for (int c = 0; c < visibleCols.Count; c++)
                    {
                        Excel.Range xCell = (Excel.Range)ws.Cells[r + 2, c + 1];
                        string colNm = visibleCols[c].Name;
                        int dgvColIdx = visibleCols[c].Index;

                        // ── Giá trị: đọc thẳng từ item ──
                        if (item.IsSummary && Array.IndexOf(hiddenSumCols, colNm) >= 0)
                        {
                            xCell.Value2 = ""; // ẩn cột không liên quan ở dòng tổng
                        }
                        else
                        {
                            object val = null;
                            switch (colNm)
                            {
                                case "STT": val = item.STT; break;
                                case "TenHang": val = item.TenHang; break;
                                case "MaHang": val = item.MaHang; break;
                                case "XuatXu": val = item.XuatXu; break;
                                case "DonVi": val = item.DonVi; break;
                                case "SoLuong": val = item.SoLuong; break;
                                case "DonGiaVND": val = item.DonGiaVND; break;
                                case "ThanhTienVND": val = item.ThanhTienVND; break;
                                case "GhiChu": val = item.GhiChu; break;
                                case "GiaNhap": val = item.GiaNhap; break;
                                case "ThanhTien": val = item.ThanhTien; break;
                                case "LoiNhuan": val = item.LoiNhuan; break;
                                case "BangGia": val = item.BangGia; break;
                            }
                            if (val is decimal decVal)
                                xCell.Value2 = (double)decVal;
                            else if (val != null)
                                xCell.Value2 = val.ToString();
                        }

                        // ── Màu nền: per-cell picker > sheet color > màu dòng mặc định ──
                        // _sheetBgColors: màu load từ Google Sheet (nguồn chính của per-cell color)
                        // _cellBgColors:  màu set qua color picker trong session hiện tại (ghi đè)
                        string[] sheetColOrd = { "STT","TenHang","MaHang","XuatXu","DonVi","SoLuong",
                                                  "DonGiaVND","ThanhTienVND","GhiChu","GiaNhap","ThanhTien","LoiNhuan","BangGia" };
                        int sheetC = Array.IndexOf(sheetColOrd, colNm);
                        var sheetKeyBg = (item.SheetRowIndex, sheetC);

                        Color cellBg = rowBg;
                        Color cellFg = rowFg;
                        
                        // Màu mặc định cho dòng Header và Summary của các cột giá
                        if (item.IsHeader || item.IsSummary)
                        {
                            if (colNm == "GiaNhap" || colNm == "ThanhTien")
                            {
                                cellBg = Color.Cyan;
                                cellFg = Color.Black;
                            }
                            else if (colNm == "LoiNhuan")
                            {
                                cellBg = Color.Yellow;
                                cellFg = Color.Red;
                            }
                            else if (colNm == "BangGia")
                            {
                                cellBg = Color.CornflowerBlue;
                                cellFg = Color.Black;
                            }
                        }
                        
                        if (sheetC >= 0 && item.SheetRowIndex >= 0 && _sheetBgColors.TryGetValue(sheetKeyBg, out Color sheetBg))
                            cellBg = sheetBg;                                   // màu từ Google Sheet
                        if (_cellBgColors.TryGetValue((item, dgvColIdx), out Color customBg))
                            cellBg = customBg;                                  // picker ghi đè
                        xCell.Interior.Color = ColorTranslator.ToOle(cellBg);

                        // ── Màu chữ: sheet color > picker ghi đè ──
                        var sheetKeyFg = (item.SheetRowIndex, sheetC);
                        if (sheetC >= 0 && item.SheetRowIndex >= 0 && _sheetFgColors.TryGetValue(sheetKeyFg, out Color sheetFg))
                            cellFg = sheetFg;
                        if (_cellFgColors.TryGetValue((item, dgvColIdx), out Color customFg))
                            cellFg = customFg;
                        xCell.Font.Color = ColorTranslator.ToOle(cellFg);
                        xCell.Font.Bold = rowBold;

                        // ── Căn chỉnh ──
                        if (Array.IndexOf(new[] { "STT", "XuatXu", "DonVi", "SoLuong" }, colNm) >= 0)
                            xCell.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                        else if (Array.IndexOf(moneyCols, colNm) >= 0)
                            xCell.HorizontalAlignment = Excel.XlHAlign.xlHAlignRight;
                        else
                            xCell.HorizontalAlignment = Excel.XlHAlign.xlHAlignLeft;

                        // ── Định dạng số tiền ──
                        if (Array.IndexOf(moneyCols, colNm) >= 0)
                            xCell.NumberFormat = "#,##0";

                        // ── Định dạng Rich Text cho Vỏ tủ điện ──
                        if (colNm == "TenHang" && item.TenHang != null && item.TenHang.StartsWith("Vỏ tủ điện"))
                        {
                            xCell.WrapText = true;
                            string text = item.TenHang;

                            var patterns = new[]
                            {
                                "trong nhà", "ngoài trời",
                                @"\d+\s*lớp cánh",
                                @"H\d+xW\d+xD\d+mm",
                                @"\d+(?:\.\d+)?mm",
                                "sơn sần", "sơn bóng",
                                @"RAL\s*\d+[^\s,\n]*",
                                @"có tô màu\s+\S+",
                            };

                            var allMatches = new List<(int start, int len)>();
                            foreach (var pat in patterns)
                            {
                                var rx = new System.Text.RegularExpressions.Regex(pat, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                foreach (System.Text.RegularExpressions.Match m in rx.Matches(text))
                                    allMatches.Add((m.Index, m.Length));
                            }
                            allMatches.Sort((a, b) => a.start != b.start ? a.start.CompareTo(b.start) : b.len.CompareTo(a.len));

                            var clean = new List<(int start, int len)>();
                            int covered = 0;
                            foreach (var m in allMatches)
                                if (m.start >= covered) { clean.Add(m); covered = m.start + m.len; }

                            foreach (var m in clean)
                            {
                                // Excel Characters are 1-indexed
                                xCell.Characters[m.start + 1, m.len].Font.Color = ColorTranslator.ToOle(Color.Red);
                                xCell.Characters[m.start + 1, m.len].Font.Bold = true;
                            }
                        }
                    }
                }

                // ── 3. Viền bảng + chiều cao hàng ──
                Excel.Range used = ws.Range[ws.Cells[1, 1], ws.Cells[_displayList.Count + 1, visibleCols.Count]];
                used.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
                used.Borders.Weight = Excel.XlBorderWeight.xlThin;
                // used.WrapText = false; // Bỏ đi để không đè lên xCell.WrapText = true ở trên
                used.VerticalAlignment = Excel.XlVAlign.xlVAlignCenter;

                // Header cột cao 30pt, dữ liệu 15pt (giống DGV)
                ws.Rows[1].RowHeight = 30;
                for (int r2 = 2; r2 <= _displayList.Count + 1; r2++)
                {
                    Excel.Range rowRange = (Excel.Range)ws.Rows[r2];
                    var item = _displayList[r2 - 2]; // r2 bắt đầu từ 2 tương ứng với index 0
                    if (item.TenHang != null && item.TenHang.StartsWith("Vỏ tủ điện"))
                    {
                        int lineCount = item.TenHang.Split('\n').Length;
                        rowRange.RowHeight = Math.Max(15, lineCount * 15);
                    }
                    else
                    {
                        rowRange.RowHeight = 15;
                    }
                }

                // ── 4. Độ rộng cột tuỳ chỉnh ──────────────────────────────
                for (int c = 0; c < visibleCols.Count; c++)
                {
                    Excel.Range excelCol = (Excel.Range)ws.Columns[c + 1];
                    switch (visibleCols[c].Name)
                    {
                        case "STT": excelCol.ColumnWidth = 5; break;
                        case "TenHang": excelCol.ColumnWidth = 40; break;
                        case "MaHang": excelCol.ColumnWidth = 14; break;
                        case "XuatXu": excelCol.ColumnWidth = 10; break;
                        case "DonVi": excelCol.ColumnWidth = 8; break;
                        case "SoLuong": excelCol.ColumnWidth = 8; break;
                        case "GhiChu": excelCol.ColumnWidth = 20; break;
                        case "DonGiaVND":
                        case "ThanhTienVND":
                        case "GiaNhap":
                        case "ThanhTien":
                        case "LoiNhuan":
                        case "BangGia": excelCol.ColumnWidth = 16; break;
                        default: excelCol.ColumnWidth = 12; break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi xuất Excel: " + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetupHeaderCheckBox(DataGridView dgv, CheckBox chk, string columnName)
        {
            chk.Size = new Size(15, 15);
            chk.BackColor = Color.White;
            dgv.Controls.Add(chk);

            chk.CheckedChanged += (s, e) =>
            {
                dgv.EndEdit();
                if (dgv.DataSource is IEnumerable<Products> source)
                {
                    foreach (var item in source) item.IsSelected = chk.Checked;
                    dgv.Refresh();
                }
            };

            dgv.DataBindingComplete += (s, e) => PositionHeaderCheckBox(dgv, chk, columnName);
            dgv.ColumnWidthChanged += (s, e) => PositionHeaderCheckBox(dgv, chk, columnName);
            dgv.Scroll += (s, e) => PositionHeaderCheckBox(dgv, chk, columnName);
            dgv.Resize += (s, e) => PositionHeaderCheckBox(dgv, chk, columnName);
        }

        private void PositionHeaderCheckBox(DataGridView dgv, CheckBox chk, string columnName)
        {
            if (!dgv.Columns.Contains(columnName)) return;
            Rectangle rect = dgv.GetCellDisplayRectangle(dgv.Columns[columnName].Index, -1, true);
            chk.Location = new Point(rect.X + (rect.Width - chk.Width) / 2, rect.Y + (rect.Height - chk.Height) / 2);
        }

        private void DataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            // Method kept for potential future use or event binding compatibility
        }



        /// <summary>
        /// CellPainting cho dataGridView1: vẽ rich text (highlight đỏ) cho dòng "Vỏ tủ điện".
        /// Tương tự DrawRichCabinetCell trong FrmAdvancedConfig.
        /// </summary>
        private void DataGridView1_CabinetCellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (!dataGridView1.Columns.Contains("Name")) return;
            if (e.ColumnIndex != dataGridView1.Columns["Name"].Index) return;

            string text = e.Value?.ToString() ?? "";
            if (!text.StartsWith("Vỏ tủ điện")) return; // Chỉ xử lý dòng vỏ tủ

            bool isSelected = e.State.HasFlag(DataGridViewElementStates.Selected);

            // Vẽ nền + border mặc định
            e.PaintBackground(e.CellBounds, isSelected);
            e.Paint(e.CellBounds, DataGridViewPaintParts.Border);

            // Vẽ rich text highlight
            DrawRichCabinetCellForConfig(e.Graphics, e.CellBounds, text, dataGridView1.Font, isSelected);

            e.Handled = true;
        }

        /// <summary>
        /// Vẽ từng dòng text trong ô "Vỏ tủ điện" với highlight màu đỏ cho từ khóa quan trọng.
        /// Logic tương tự DrawRichCabinetCell trong FrmAdvancedConfig.
        /// </summary>
        private static void DrawRichCabinetCellForConfig(System.Drawing.Graphics g, Rectangle bounds, string text, Font baseFont, bool isSelected)
        {
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Color colNormal    = isSelected ? Color.White : Color.FromArgb(30, 30, 30);
            Color colHighlight = Color.Red;

            Font fntBold   = new Font(baseFont ?? new Font("Segoe UI", 9f), FontStyle.Bold);
            Font fntNormal = baseFont ?? new Font("Segoe UI", 9f);

            // Các pattern cần tô đỏ (giống FrmAdvancedConfig)
            var patterns = new[]
            {
                "trong nhà", "ngoài trời",
                @"\d+\s*lớp cánh",
                @"H\d+xW\d+xD\d+mm",
                @"\d+(?:\.\d+)?mm",
                "sơn sần", "sơn bóng",
                @"RAL\s*\d+[^\s,\n]*",
                @"có tô màu\s+\S+",
            };

            string[] lines = text.Split('\n');
            int lineH  = fntNormal.Height + 3;
            int topPad = Math.Max(3, (bounds.Height - lines.Length * lineH) / 2);
            int curY   = bounds.Top + topPad;

            foreach (string rawLine in lines)
            {
                string line = rawLine.TrimEnd('\r');
                int curX = bounds.Left + 4;

                // Thu thập tất cả vị trí keyword
                var allMatches = new List<(int start, int len)>();
                foreach (var pat in patterns)
                {
                    var rx = new System.Text.RegularExpressions.Regex(
                        pat, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    foreach (System.Text.RegularExpressions.Match m in rx.Matches(line))
                        allMatches.Add((m.Index, m.Length));
                }
                allMatches.Sort((a, b) => a.start != b.start ? a.start.CompareTo(b.start) : b.len.CompareTo(a.len));

                // Loại overlap
                var clean = new List<(int start, int len)>();
                int covered = 0;
                foreach (var m in allMatches)
                    if (m.start >= covered) { clean.Add(m); covered = m.start + m.len; }

                // Vẽ từng đoạn
                int p = 0;
                foreach (var m in clean)
                {
                    if (m.start > p)
                    {
                        string normal = line.Substring(p, m.start - p);
                        using (var br = new SolidBrush(colNormal))
                            g.DrawString(normal, fntNormal, br, curX, curY);
                        curX += (int)g.MeasureString(normal, fntNormal).Width - 2;
                    }
                    string hi = line.Substring(m.start, m.len);
                    using (var br = new SolidBrush(colHighlight))
                        g.DrawString(hi, fntBold, br, curX, curY);
                    curX += (int)g.MeasureString(hi, fntBold).Width - 2;
                    p = m.start + m.len;
                }
                if (p < line.Length)
                {
                    string tail = line.Substring(p);
                    using (var br = new SolidBrush(colNormal))
                        g.DrawString(tail, fntNormal, br, curX, curY);
                }
                curY += lineH;
            }
            fntBold.Dispose();
        }

        /// <summary>
        /// Tự động điều chỉnh chiều cao các dòng "Vỏ tủ điện" (multiline) trong dataGridView1.
        /// Gọi sau khi childProducts được cập nhật từ FrmAdvancedConfig.
        /// </summary>
        private void AdjustDataGridView1RowHeights()
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                var prod = row.DataBoundItem as Products;
                if (prod == null) continue;
                string name = prod.Name ?? "";
                if (name.StartsWith("Vỏ tủ điện"))
                {
                    int lineCount = name.Split('\n').Length;
                    int fontH = dataGridView1.Font?.Height ?? 15;
                    int needed = lineCount * (fontH + 3) + 10;
                    row.Height = Math.Max(36, needed);
                }
            }
            dataGridView1.Invalidate();
        }

        private void DgvParentProducts_CabinetCellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (!dgvParentProducts.Columns.Contains("TenHang")) return;
            if (e.ColumnIndex != dgvParentProducts.Columns["TenHang"].Index) return;

            string text = e.Value?.ToString() ?? "";
            if (!text.StartsWith("Vỏ tủ điện")) return; // Chỉ xử lý dòng vỏ tủ

            bool isSelected = e.State.HasFlag(DataGridViewElementStates.Selected);

            // Vẽ nền + border mặc định
            e.PaintBackground(e.CellBounds, isSelected);
            e.Paint(e.CellBounds, DataGridViewPaintParts.Border);

            // Vẽ rich text highlight
            DrawRichCabinetCellForConfig(e.Graphics, e.CellBounds, text, dgvParentProducts.Font, isSelected);

            e.Handled = true;
        }

        private void AdjustDgvParentProductsRowHeights()
        {
            foreach (DataGridViewRow row in dgvParentProducts.Rows)
            {
                var item = row.DataBoundItem as ConfigProductItem;
                if (item == null) continue;
                string name = item.TenHang ?? "";
                if (name.StartsWith("Vỏ tủ điện"))
                {
                    int lineCount = name.Split('\n').Length;
                    int fontH = dgvParentProducts.Font?.Height ?? 15;
                    int needed = lineCount * (fontH + 3) + 10;
                    row.Height = Math.Max(36, needed);
                }
            }
            dgvParentProducts.Invalidate();
        }

        private void DataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0 && dataGridView1.Columns[e.ColumnIndex].Name == "Id")
            {
                e.Value = (e.RowIndex + 1).ToString();
                e.FormattingApplied = true;
            }
        }

        private void AddAdvancedConfigResult(string headerName, List<AdvancedConfigResultItem> items)
        {
            if (items == null || items.Count == 0) return;

            // 1. Thêm dòng Header (nếu có tên)
            if (!string.IsNullOrEmpty(headerName))
            {
                configProducts.Add(new ConfigProductItem
                {
                    TenHang = headerName,
                    MaHang = "",
                    XuatXu = "VNECCO",
                    DonVi = "TỦ",
                    SoLuong = 1,
                    DonGiaVND = 0,
                    ThanhTienVND = 0,
                    GiaNhap = 0,
                    ThanhTien = 0,
                    BangGia = 0,
                    IsHeader = true
                });
            }

            // 2. Thêm từng dòng sản phẩm từ AdvancedConfig
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.TenCauHinh)) continue;

                // Ưu tiên dùng ReferenceProduct (sản phẩm đã khớp trong FrmAdvancedConfig)
                var matched = item.ReferenceProduct;

                // Nếu không có ReferenceProduct thì thử tìm lại theo tên
                if (matched == null)
                {
                    matched = allProducts.FirstOrDefault(p =>
                        string.Equals(p.Name?.Trim(), item.TenCauHinh, StringComparison.OrdinalIgnoreCase));
                }

                decimal price = item.DonGia;
                decimal priceCost = 0;

                if (matched != null)
                {
                    if (price == 0)
                        decimal.TryParse(matched.Price?.Replace(".", "").Replace(",", ""), out price);
                    decimal.TryParse(matched.PriceCost?.Replace(".", "").Replace(",", ""), out priceCost);
                }

                configProducts.Add(new ConfigProductItem
                {
                    TenHang = item.TenCauHinh,
                    MaHang  = matched?.SKU ?? "",
                    XuatXu  = matched?.HÃNG ?? "",
                    DonVi   = "Cái",
                    SoLuong = item.SoLuong > 0 ? item.SoLuong : 1,
                    DonGiaVND   = price,
                    ThanhTienVND = price * (item.SoLuong > 0 ? item.SoLuong : 1),
                    GhiChu  = item.ThuocTinh ?? "",
                    GiaNhap  = priceCost > 0 ? priceCost : price,
                    ThanhTien = (priceCost > 0 ? priceCost : price) * (item.SoLuong > 0 ? item.SoLuong : 1),
                    BangGia  = price - (priceCost > 0 ? priceCost : price),
                    IsHeader = false
                });
            }

            // 3. Cập nhật lại STT toàn bộ
            for (int i = 0; i < configProducts.Count; i++)
                configProducts[i].STT = (i + 1).ToString();

            UpdateHeaderSum();
            UpdateConfigGrid();

            button5.Text = "Lưu";
            currentEditingConfigName = null;
        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private async System.Threading.Tasks.Task<List<ExportInfo>> FetchOldCustomersAsync()
        {
            var list = new List<ExportInfo>();
            try
            {
                if (_sheetsService == null) InitGoogleSheetsService();
                var readResp = await _sheetsService.Spreadsheets.Values.Get(spreadsheetId, "'Khach hang'!A2:F1000").ExecuteAsync();
                if (readResp.Values != null)
                {
                    // Lấy từ dưới lên trên để lấy khách hàng gần nhất
                    for (int i = readResp.Values.Count - 1; i >= 0; i--)
                    {
                        var row = readResp.Values[i];
                        if (row.Count == 0) continue;
                        string kg = row.Count > 0 ? row[0]?.ToString() : "";
                        if (string.IsNullOrWhiteSpace(kg)) continue;

                        if (!list.Any(x => x.KinhGui == kg))
                        {
                            list.Add(new ExportInfo
                            {
                                KinhGui = kg,
                                DiaChi = row.Count > 1 ? row[1]?.ToString() : "",
                                NguoiNhan = row.Count > 2 ? row[2]?.ToString() : "",
                                MaSoThue = row.Count > 3 ? row[3]?.ToString() : "",
                                NoiDung = row.Count > 4 ? row[4]?.ToString() : ""
                            });
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        private async void btnExportFile_Click(object sender, EventArgs e)
        {
            if (dgvParentProducts.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(configSheetName))
            {
                MessageBox.Show("Vui lòng chọn hoặc tạo tab báo giá (Google Sheets) trước khi xuất file!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                this.Cursor = Cursors.WaitCursor;
                // Gọi hàm lưu dữ liệu lên Google Sheets trước khi xuất
                bool saved = await SaveCurrentQuotationToSheetAsync();
                this.Cursor = Cursors.Default;
                
                if (!saved) return; // Nếu lưu lỗi thì dừng lại, không xuất nữa
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show($"Lỗi khi tự động lưu báo giá trước khi xuất: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var oldCustomers = await FetchOldCustomersAsync();

            using (FrmExportInfo frm = new FrmExportInfo(oldCustomers))
            {
                if (frm.ShowDialog(this) == DialogResult.OK)
                {
                    string templateFileName = "VNECCO BG_Tủ Điện, TBA và Hệ thống Cơ điện.xlsx";
                    string templatePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FILE", templateFileName);

                    // Nếu không tìm thấy ở thư mục hiện tại, thử tìm ở thư mục cha (hữu ích khi debug)
                    if (!System.IO.File.Exists(templatePath))
                    {
                        string projectPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\FILE", templateFileName));
                        if (System.IO.File.Exists(projectPath)) templatePath = projectPath;
                    }
                    
                    // Thử thêm 1 cấp nữa nếu vẫn không thấy
                    if (!System.IO.File.Exists(templatePath))
                    {
                        string rootPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\FILE", templateFileName));
                        if (System.IO.File.Exists(rootPath)) templatePath = rootPath;
                    }

                    if (!System.IO.File.Exists(templatePath))
                    {
                        MessageBox.Show("Không tìm thấy file mẫu báo giá!\nĐường dẫn đã thử: " + templatePath, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    SaveFileDialog sfd = new SaveFileDialog();
                    sfd.Title = "Chọn nơi lưu file";
                    if (frm.ExportData.Format == "Excel")
                    {
                        sfd.Filter = "Excel Files|*.xlsx";
                        sfd.DefaultExt = "xlsx";
                    }
                    else
                    {
                        sfd.Filter = "PDF Files|*.pdf";
                        sfd.DefaultExt = "pdf";
                    }

                    sfd.FileName = "BaoGia_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        ExcelHelper.ExportWithTemplate(dgvParentProducts, frm.ExportData, templatePath, sfd.FileName);

                        // Mở file vừa xuất bằng ứng dụng mặc định
                        if (System.IO.File.Exists(sfd.FileName))
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = sfd.FileName,
                                UseShellExecute = true
                            });
                        }

                        // Lưu thông tin khách hàng lên sheet "Khach hang"
                        try
                        {
                            string targetSheetName = "Khach hang";

                            if (_sheetsService == null) InitGoogleSheetsService();

                            // Kiểm tra xem sheet đã tồn tại chưa
                            var spreadsheet = await _sheetsService.Spreadsheets.Get(spreadsheetId).ExecuteAsync();
                            bool sheetExists = spreadsheet.Sheets.Any(s => s.Properties.Title == targetSheetName);

                            if (!sheetExists)
                            {
                                // Tạo sheet mới
                                var addSheetRequest = new Google.Apis.Sheets.v4.Data.Request
                                {
                                    AddSheet = new Google.Apis.Sheets.v4.Data.AddSheetRequest
                                    {
                                        Properties = new Google.Apis.Sheets.v4.Data.SheetProperties
                                        {
                                            Title = targetSheetName
                                        }
                                    }
                                };
                                var batchUpdateRequest = new Google.Apis.Sheets.v4.Data.BatchUpdateSpreadsheetRequest
                                {
                                    Requests = new List<Google.Apis.Sheets.v4.Data.Request> { addSheetRequest }
                                };
                                await _sheetsService.Spreadsheets.BatchUpdate(batchUpdateRequest, spreadsheetId).ExecuteAsync();

                                // Thêm dòng tiêu đề
                                var headerRangeObj = new Google.Apis.Sheets.v4.Data.ValueRange();
                                headerRangeObj.Values = new List<IList<object>> {
                                    new List<object> { "Kính gửi", "Địa chỉ", "Người nhận", "Mã số thuế", "Nội dung báo giá", "Tên cấu hình" }
                                };
                                var appendHeaderReq = _sheetsService.Spreadsheets.Values.Append(headerRangeObj, spreadsheetId, $"'{targetSheetName}'!A1:F1");
                                appendHeaderReq.ValueInputOption = Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                                await appendHeaderReq.ExecuteAsync();
                            }

                            string range = $"'{targetSheetName}'!A:F";
                            var valueRange = new Google.Apis.Sheets.v4.Data.ValueRange();
                            valueRange.Values = new List<IList<object>> {
                                new List<object> {
                                    frm.ExportData.KinhGui ?? "",
                                    frm.ExportData.DiaChi ?? "",
                                    frm.ExportData.NguoiNhan ?? "",
                                    frm.ExportData.MaSoThue ?? "",
                                    frm.ExportData.NoiDung ?? "",
                                    configSheetName ?? "Mặc định"
                                }
                            };

                            var appendRequest = _sheetsService.Spreadsheets.Values.Append(valueRange, spreadsheetId, range);
                            appendRequest.ValueInputOption = Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                            await appendRequest.ExecuteAsync();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Đã xuất file thành công nhưng không thể lưu thông tin lên sheet 'Khach hang': " + ex.Message, "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
        }

        private void btnPopOutQuote_Click(object sender, EventArgs e)
        {
            if (_popupQuoteForm != null && !_popupQuoteForm.IsDisposed)
            {
                _popupQuoteForm.BringToFront();
                return;
            }

            _popupQuoteForm = new Form();
            _popupQuoteForm.Text = "BẢNG BÁO GIÁ VÀ DỰ TOÁN";
            _popupQuoteForm.Size = new Size(1300, 700);
            _popupQuoteForm.StartPosition = FormStartPosition.CenterScreen;
            _popupQuoteForm.Icon = this.ParentForm?.Icon;
            
            _popupQuoteForm.Controls.Add(groupBox2);
            groupBox2.Dock = DockStyle.Fill;
            
            _popupQuoteForm.FormClosing += (s, ev) => {
                if (!this.IsDisposed)
                {
                    splitMain.Panel2.Controls.Add(groupBox2);
                }
            };
            
            splitMain.Panel2Collapsed = true;
            _popupQuoteForm.Show();
        }

        private async void btnAdvancedConfigForQuotation_Click(object sender, EventArgs e)
        {
            if (_sheetsService == null) InitGoogleSheetsService();

            using (var frm = new FrmAdvancedConfig())
            {
                // Tự động Maximized và load data
                await frm.LoadDataAsync(_sheetsService, spreadsheetId);

                if (frm.IsCanceled) return;

                if (frm.ShowDialog(this) == DialogResult.OK)
                {
                    var results = frm.SelectedAdvancedItems;
                    if (results == null || results.Count == 0) return;

                    // Thêm Header cho nhóm cấu hình nâng cao này
                    string headerName = !string.IsNullOrEmpty(frm.SelectedHeader) ? frm.SelectedHeader : "Cấu hình nâng cao";

                    // Thêm dòng header (Màu xanh)
                    configProducts.Add(new ConfigProductItem
                    {
                        STT = (configProducts.Count + 1).ToString(),
                        TenHang = headerName,
                        IsHeader = true,
                        XuatXu = "VNECCO",
                        DonVi = "TỦ",
                        SoLuong = 1,
                        DonGiaVND = 0,
                        ThanhTienVND = 0,
                        GiaNhap = 0
                    });

                    // Thêm các sản phẩm kết quả
                    foreach (var res in results)
                    {
                        ConfigProductItem configItem;
                        if (res.ReferenceProduct != null)
                        {
                            decimal price = res.DonGia;
                            // Thử lấy giá nhập từ ReferenceProduct nếu có
                            decimal priceCost = 0;
                            if (!string.IsNullOrEmpty(res.ReferenceProduct.PriceCost))
                            {
                                decimal.TryParse(res.ReferenceProduct.PriceCost.Replace(".", "").Replace(",", ""), out priceCost);
                            }
                            if (priceCost <= 0) priceCost = price; // Fallback

                            configItem = CreateConfigItem(res.ReferenceProduct, price, priceCost);
                            configItem.TenHang = res.TenCauHinh;
                        }
                        else
                        {
                            // Nếu không có product gốc (ví dụ Vỏ tủ tự tính), tạo item chay
                            configItem = new ConfigProductItem
                            {
                                TenHang = res.TenCauHinh,
                                XuatXu = "VNECCO",
                                DonVi = "Cái",
                                DonGiaVND = res.DonGia,
                                GiaNhap = res.DonGia, // Tạm thời để bằng giá bán
                                ThanhTienVND = res.DonGia * res.SoLuong
                            };
                        }

                        configItem.SoLuong = res.SoLuong;
                        configItem.GhiChu = res.ThuocTinh;
                        configItem.STT = (configProducts.Count + 1).ToString();
                        
                        configProducts.Add(configItem);
                    }

                    UpdateHeaderSum();
                    UpdateConfigGrid();
                    
                    MessageBox.Show($"Đã thêm {results.Count} hạng mục từ cấu hình nâng cao.", 
                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        private Products CloneProduct(Products p)
        {
            if (p == null) return null;
            return new Products
            {
                Id = p.Id,
                SheetRowIndex = p.SheetRowIndex,
                Name = p.Name,
                Model = p.Model,
                SKU = p.SKU,
                Price = p.Price,
                PriceCost = p.PriceCost,
                Weight = p.Weight,
                Length = p.Length,
                Width = p.Width,
                Height = p.Height,
                Category = p.Category,
                Type = p.Type,
                HÃNG = p.HÃNG,
                TrangThai = p.TrangThai,
                Pole = p.Pole,
                Ir = p.Ir,
                Icu = p.Icu,
                PriceList = p.PriceList,
                SoLuong = p.SoLuong,
                ExtraAttributes = new Dictionary<string, string>(p.ExtraAttributes)
            };
        }

        private async void btnAdvancedConfigBuild_Click(object sender, EventArgs e)
        {
            if (_sheetsService == null) InitGoogleSheetsService();

            using (var frm = new FrmAdvancedConfig())
            {
                // Tải data
                await frm.LoadDataAsync(_sheetsService, spreadsheetId);
                if (frm.IsCanceled) return;

                if (frm.ShowDialog(this) == DialogResult.OK)
                {
                    var results = frm.SelectedAdvancedItems;
                    if (results == null || results.Count == 0) return;

                    foreach (var res in results)
                    {
                        if (res.ReferenceProduct != null)
                        {
                            // Clone sản phẩm gốc để đưa vào danh sách xây dựng
                            var prod = CloneProduct(res.ReferenceProduct);
                            prod.Name = res.TenCauHinh;
                            prod.SoLuong = res.SoLuong;
                            
                            // Nếu chưa có trong danh sách thì thêm vào
                            if (!childProducts.Any(p => p.Id == prod.Id && p.Name == prod.Name))
                            {
                                childProducts.Add(prod);
                            }
                        }
                        else
                        {
                            // Trường hợp không có product gốc (VD: Vỏ tủ tự tính)
                            var prod = new Products
                            {
                                Name = res.TenCauHinh,
                                Model = res.ThuocTinh,
                                SKU = "CUSTOM",
                                Price = res.DonGia.ToString(),
                                SoLuong = res.SoLuong,
                                HÃNG = "VNECCO"
                            };
                            prod.ExtraAttributes["DonVi"] = "Cái";
                            childProducts.Add(prod);
                        }
                    }

                    MessageBox.Show($"Đã thêm {results.Count} hạng mục vào danh sách Xây dựng cấu hình.", 
                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}