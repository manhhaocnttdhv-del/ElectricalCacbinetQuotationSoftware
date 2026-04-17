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

        /// <summary>Quan hệ BASE từ childProducts (reset khi childProducts thay đổi).</summary>
        private HashSet<int> _baseRelatedIds = null;
        /// <summary>Quan hệ MỞ RỘNG — tích lũy khi user click vào sp trên dgvAllProducts (không reset khi click).</summary>
        private HashSet<int> _expandedRelatedIds = new HashSet<int>();
        private bool _isUpdatingSelection = false;
        /// <summary>Helper: sp có được phép chọn không (trong base hoặc expanded)?</summary>
        private bool IsRelatedProduct(int id) =>
            _baseRelatedIds == null || _baseRelatedIds.Contains(id) || _expandedRelatedIds.Contains(id);

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
        private Dictionary<(int r, int c), Color> _cellBgColors = new Dictionary<(int, int), Color>(); // màu nền
        private Dictionary<(int r, int c), Color> _cellFgColors = new Dictionary<(int, int), Color>(); // màu chữ

        /// <summary>Lưu vị trí ô được right-click (để hiển thị context menu đúng ô).</summary>
        private int _rightClickedRow = -1;
        private int _rightClickedCol = -1;

        /// <summary>Danh sách hiển thị trong dgvParentProducts (bao gồm cả 3 dòng TỔNG/VAT/THÀNH TIỀN).</summary>
        private List<ConfigProductItem> _displayList = new List<ConfigProductItem>();

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

            dgvAllProducts.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;
            dataGridView1.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;

            dgvAllProducts.DataBindingComplete += Grid_DataBindingComplete;
            dataGridView1.DataBindingComplete += Grid_DataBindingComplete;
            dgvParentProducts.DataBindingComplete += DgvParentProducts_DataBindingComplete;
            dgvParentProducts.CellFormatting += DgvParentProducts_CellFormatting;

            // Handle DataError to suppress technical dialogs
            dgvAllProducts.DataError += Grid_DataError;
            dataGridView1.DataError += Grid_DataError;
            dgvParentProducts.DataError += Grid_DataError;

            // Multi-row toggle logic
            dgvAllProducts.CellContentClick += Grid_CellContentClick;
            dataGridView1.CellContentClick += Grid_CellContentClick;
            dgvAllProducts.KeyDown += Grid_KeyDown;
            dataGridView1.KeyDown += Grid_KeyDown;

            // Khi chọn dòng trong dataGridView1 (childProducts) → gray-out sp không quan hệ trên dgvAllProducts
            dataGridView1.SelectionChanged += DataGridView1_SelectionChanged;
            // Gray-out + ngăn click dòng bị disable trên dgvAllProducts
            dgvAllProducts.CellFormatting += DgvAllProducts_CellFormatting;
            dgvAllProducts.CellMouseClick += DgvAllProducts_CellMouseClick;

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
                                _cellBgColors[(cell.RowIndex, cell.ColumnIndex)] = picker.SelectedColor.Value;
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
                                _cellFgColors[(cell.RowIndex, cell.ColumnIndex)] = picker.SelectedColor.Value;
                        }
                        dgvParentProducts.Refresh();
                    }
                }
            };

            var miClearColor = new System.Windows.Forms.ToolStripMenuItem("✖  Xoá màu ô (tất cả ô đang chọn)");
            miClearColor.Click += (s, e) =>
            {
                foreach (DataGridViewCell cell in dgvParentProducts.SelectedCells)
                {
                    var key = (cell.RowIndex, cell.ColumnIndex);
                    _cellBgColors.Remove(key);
                    _cellFgColors.Remove(key);
                }
                dgvParentProducts.Refresh();
            };

            ctxCell.Items.Add(miSetBg);
            ctxCell.Items.Add(miSetFg);
            ctxCell.Items.Add(new ToolStripSeparator());
            ctxCell.Items.Add(miClearColor);

            dgvParentProducts.ContextMenuStrip = ctxCell;
            dgvParentProducts.CellMouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    // Chỉ ghi nhận ô right-click để làm màu khởi tạo cho dialog
                    // KHÔNG thay đổi CurrentCell để giữ nguyên selection nhiều ô
                    _rightClickedRow = e.RowIndex;
                    _rightClickedCol = e.ColumnIndex;
                }
            };
        }

        // ══════════════════════════════════════════════════════════════════
        // EVENT HANDLERS – DataGridView
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Sau khi binding xong, áp dụng style cho dgvParentProducts (danh sách cấu hình).</summary>
        private void DgvParentProducts_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            FormatConfigGrid(dgvParentProducts);
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

        private void FrmConfig_Load(object sender, EventArgs e)
        {
            // Không gọi LoadDataAsync() ở đây vì FrmMain đã gọi trước đó.
            // Việc gọi lại sẽ gây race condition: khi tab lần đầu hiển thị,
            // sự kiện Load này kích hoạt và load lại từ configSheetName mặc định
            // ("Products_Config"), ghi đè tab mà người dùng đã chọn từ modal.

            button2.Click += Button2_Click;
            button1.Click += BtnAddParent_Click;
            btnSearch.Click += BtnAddFromRelation_Click;
            btn_baogia.Click += btn_baogia_Click;
            button4.Click += BtnRemoveParent_Click;
            button3.Click += Button3_Click;
            button5.Click += Button5_Click;
            button6.Click += Button6_Click;
            button7.Click += Button7_Click;
            button10.Click += BtnExportExcel_Click;
            //// Lấy sản phẩm từ các dòng đang được chọn trên grid (highlight xanh)
            // Đăng ký trong FrmConfig_Load hoặc Constructor
            dgvAllProducts.SelectionChanged += (s, _) =>
            {
                var selectedItems = dgvAllProducts.SelectedRows
                    .Cast<DataGridViewRow>()
                    .Select(r => r.DataBoundItem as Products)
                    .Where(p => p != null)
                    .ToList();

                bool hasSelection = selectedItems.Count > 0;
                button1.Enabled = hasSelection;
                button8.Enabled = hasSelection;
            };


            // button9 = "Cập nhật": reload toàn bộ dữ liệu từ Google Sheets
            button9.Click += async (s, ev) =>
            {
                button9.Enabled = false;
                button9.Text = "Đang tải...";
                try { await LoadDataAsync(); }
                finally { button9.Enabled = true; button9.Text = "Cập nhật"; }
            };

            // Nhấn Enter trong ô tìm kiếm = kích hoạt tìm kiếm
            textBox2.KeyDown += (s, ev) =>
            {
                if (ev.KeyCode == Keys.Enter) { Button2_Click(s, ev); ev.Handled = true; }
            };


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

        private void Button2_Click(object sender, EventArgs e)
        {
            string searchText = textBox2.Text.Trim().ToLower();
            string selectedCat = cboCategory?.SelectedFullPath ?? "";

            var filteredProducts = allProducts.AsEnumerable();

            // 1. Filter by Name / SKU / Model (Partial match)
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredProducts = filteredProducts.Where(p =>
                    (p.Name != null && p.Name.ToLower().Contains(searchText)) ||
                    (p.SKU != null && p.SKU.ToLower().Contains(searchText)) ||
                    (p.Model != null && p.Model.ToLower().Contains(searchText))
                );
            }

            // 2. Filter by Category (từ CategoryTreeDropdown)
            if (!string.IsNullOrEmpty(selectedCat))
            {
                // Match nếu category của sản phẩm bắt đầu bằng selectedCat (bao gồm node cha lẫn con)
                filteredProducts = filteredProducts.Where(p =>
                    p.Category != null &&
                    p.Category.TrimEnd(';').Trim().StartsWith(selectedCat, StringComparison.OrdinalIgnoreCase)
                );
            }

            dgvAllProducts.DataSource = filteredProducts.ToList();
            // FormatDataGridView will be called by DataBindingComplete
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
                dgvAllProducts.DataSource = allProducts.ToList();
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
                UpdateGridSelector(dgvAllProducts, allProducts);
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
                // Lấy cột A→E để có DonVi (cột E, index 4) xác định đây là header (DonVi="TỦ")
                var response = await _sheetsService.Spreadsheets.Values.Get(spreadsheetId, $"{configSheetName}!A2:E").ExecuteAsync();
                if (response.Values != null)
                {
                    var freshNames = response.Values
                        .Where(r => r.Count >= 5
                            && r[4]?.ToString()?.Trim() == "TỦ"
                            && !string.IsNullOrEmpty(r[1]?.ToString())
                            && !r[1].ToString().StartsWith("--"))
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
        /// Sau khi nạp: cập nhật bộ lọc hãng/danh mục và hiển thị lên dgvAllProducts.
        /// </summary>
        private async Task FetchAllProductsAsync()
        {
            try
            {
                var response = await _sheetsService.Spreadsheets.Values.Get(spreadsheetId, $"{sheetName}!A2:N").ExecuteAsync();
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
                            Length = row.Count > 7 ? row[7]?.ToString() : "0",
                            Width = row.Count > 8 ? row[8]?.ToString() : "0",
                            Height = row.Count > 9 ? row[9]?.ToString() : "0",
                            Category = row.Count > 10 ? row[10]?.ToString() : "",
                            HÃNG = row.Count > 11 ? row[11]?.ToString() : "",
                            PriceList = row.Count > 12 ? row[12]?.ToString() : "",
                            // TienDo = row.Count > 13 ? row[13]?.ToString() : ""
                        });
                    }
                    allProducts.Clear();
                    allProducts.AddRange(newProducts);
                    this.Invoke((MethodInvoker)delegate {
                        UpdateFiltersFromProducts(allProducts);
                        dgvAllProducts.DataSource = allProducts.ToList();
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
            if (cboCategory != null)
                cboCategory.LoadTree(treeNodes);

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
                var response = await _sheetsService.Spreadsheets.Values.Get(spreadsheetId, $"{configSheetName}!A2:L").ExecuteAsync();
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
                            BangGia = parseCurrency(row.Count > 11 ? row[11]?.ToString() : "0"),
                            IsHeader = row.Count > 4 && row[4]?.ToString()?.Trim() == "TỦ",
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
        /// Định dạng hai grid sản phẩm (dgvAllProducts & dataGridView1):
        /// ẩn cột không cần thiết, đặt header text, chỉ cho phép sửa checkbox IsSelected.
        /// </summary>
        private void FormatDataGridView(DataGridView dgv)
        {
            if (dgv == null || dgv.IsDisposed || dgv.Columns == null || dgv.Columns.Count == 0) return;

            try
            {
                dgv.ColumnHeadersVisible = true;
                dgv.RowHeadersVisible = false; // Ẩn cột Row Header xám ngoài cùng bên trái
                // Sử dụng mảng cố định để duyệt để tránh lỗi đồng bộ
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

                    // 2. Set headers and format
                    if (colName == "Id")
                    {
                        col.HeaderText = "ID";
                        col.Visible = true;
                    }
                    else if (colName == "Name") col.HeaderText = "Tên sản phẩm";
                    else if (colName == "Model") col.HeaderText = "Model";
                    else if (colName == "SKU") col.HeaderText = "Mã SKU";
                    else if (colName == "Price")
                    {
                        col.HeaderText = "Giá bán";
                        col.DefaultCellStyle.Format = "N0";
                    }
                    else if (colName == "PriceCost")
                    {
                        col.HeaderText = "Giá nhập";
                        col.DefaultCellStyle.Format = "N0";
                    }
                    else if (colName == "HÃNG") col.HeaderText = "Hãng";
                    else if (colName == "Category") col.HeaderText = "Danh mục";
                    else if (colName == "IsSelected")
                    {
                        // Chỉ hiện checkbox ở dataGridView1 (grid phải - Sản phẩm con/relation)
                        if (dgv == dataGridView1)
                        {
                            col.HeaderText = "Chọn";
                            try { col.ReadOnly = false; } catch { }
                            try { col.DisplayIndex = dgv.Columns.Count - 1; } catch { }
                        }
                        else
                        {
                            col.Visible = false;
                            continue;
                        }
                    }

                    // 3. Global ReadOnly
                    if (colName != "IsSelected")
                    {
                        try { col.ReadOnly = true; } catch { }
                    }
                }

                dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgv.MultiSelect = true;
            }
            catch (Exception)
            {
                // Silently ignore layout-related exceptions during binding
            }
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
                var resp = await _sheetsService.Spreadsheets.Values.Get(spreadsheetId, $"{sName}!A2:H2000").ExecuteAsync();
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

                        // Cố gắng map ID gốc nếu có trong allProducts, nếu không thì tạo mới
                        var existing = allProducts.FirstOrDefault(p => p.SKU == sku || (id > 0 && p.Id == id));
                        if (existing != null)
                        {
                            existing.IsSelected = false;
                            foundProducts.Add(existing);
                        }
                        else
                        {
                            foundProducts.Add(new Products
                            {
                                Id = id, Name = ten, Model = model, SKU = sku,
                                Price = price, PriceCost = cost, Category = cat, HÃNG = hang, IsSelected = false
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
                    RefreshAllProductsGrayOut();
                    UpdateConfigGrid(); // Update the dgvAllProducts and dataGridView1
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
                    return new ConfigProductItem
                    {
                        TenHang = p.Name, MaHang = p.SKU, SoLuong = 1,
                        DonGiaVND = price, GiaNhap = priceCost > 0 ? priceCost : price, IsHeader = false
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

        private void Button5_Click(object sender, EventArgs e)
        {
            var selectedItems = childProducts.Where(p => p.IsSelected).ToList();
            if (selectedItems.Count == 0) return;

            // Xác định tên nhóm (header) từ Danh mục PR
            string catPR = comboBox1.SelectedItem?.ToString();
            bool hasCatPR = !string.IsNullOrEmpty(catPR) && catPR != "-- Tất cả danh mục --";
            string headerName = hasCatPR ? catPR : "Sản phẩm liên quan";

            // Tìm vị trí header khớp tên trong danh sách cấu hình hiện tại
            int headerIdx = configProducts.FindIndex(p =>
                p.IsHeader && string.Equals(p.TenHang?.Trim(), headerName?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (headerIdx < 0)
            {
                // Nếu chưa có header này, tạo mới và thêm các sản phẩm con vào dưới
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

                foreach (var product in selectedItems)
                {
                    if (!configProducts.Any(x => x.MaHang == product.SKU))
                    {
                        decimal price = 0; decimal.TryParse(product.Price?.Replace(".", "").Replace(",", ""), out price);
                        decimal priceCost = 0; decimal.TryParse(product.PriceCost?.Replace(".", "").Replace(",", ""), out priceCost);
                        configProducts.Add(CreateConfigItem(product, price, priceCost));
                    }
                    product.IsSelected = false; // Bỏ chọn sau khi thêm
                }
            }
            else
            {
                // Nếu đã có header, tìm vị trí kết thúc của nhóm này (trước header tiếp theo hoặc cuối danh sách)
                int insertIdx = headerIdx + 1;
                while (insertIdx < configProducts.Count && !configProducts[insertIdx].IsHeader)
                {
                    insertIdx++;
                }

                foreach (var product in selectedItems)
                {
                    if (!configProducts.Any(x => x.MaHang == product.SKU))
                    {
                        decimal price = 0; decimal.TryParse(product.Price?.Replace(".", "").Replace(",", ""), out price);
                        decimal priceCost = 0; decimal.TryParse(product.PriceCost?.Replace(".", "").Replace(",", ""), out priceCost);
                        configProducts.Insert(insertIdx++, CreateConfigItem(product, price, priceCost));
                    }
                    product.IsSelected = false;
                }
            }

            // Cập nhật lại STT
            for (int i = 0; i < configProducts.Count; i++)
                configProducts[i].STT = (i + 1).ToString();

            UpdateHeaderSum();
            UpdateConfigGrid();
            dataGridView1.Refresh();
        }

        private ConfigProductItem CreateConfigItem(Products product, decimal price, decimal priceCost)
        {
            return new ConfigProductItem
            {
                TenHang = product.Name,
                MaHang = product.SKU,
                XuatXu = product.HÃNG,
                DonVi = "Cái",
                SoLuong = 1,
                DonGiaVND = price,
                ThanhTienVND = price,
                GhiChu = "",
                GiaNhap = priceCost,
                ThanhTien = priceCost,
                BangGia = price - priceCost,
                // TienDo = product.TienDo,
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
                p.Id, p.Name, p.Model, p.SKU,
                p.Price ?? "0", p.PriceCost ?? "0",
                p.Category, p.HÃNG
            }).ToList();

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
                    new List<object> { "ID", "Tên sản phẩm", "Model", "Mã SKU", "Giá bán", "Giá nhập", "Danh mục", "Hãng" }
                }
            };
            var writeHeader = _sheetsService.Spreadsheets.Values.Update(colHeaderRange, spreadsheetId, $"{targetSheet}!A1");
            writeHeader.ValueInputOption = Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            await writeHeader.ExecuteAsync();

            // 7. Xóa dữ liệu cũ và ghi mới từ row 2
            await _sheetsService.Spreadsheets.Values.Clear(
                new Google.Apis.Sheets.v4.Data.ClearValuesRequest(), spreadsheetId, $"{targetSheet}!A2:H2000").ExecuteAsync();

            if (allRows.Count > 0)
            {
                var valueRange = new Google.Apis.Sheets.v4.Data.ValueRange { Values = allRows };
                var updateReq = _sheetsService.Spreadsheets.Values.Update(valueRange, spreadsheetId, $"{targetSheet}!A2");
                updateReq.ValueInputOption = Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                await updateReq.ExecuteAsync();
            }

            // 8. Áp dụng màu sắc
            await ApplySheetFormattingAsync(targetSheet, groupHeaderRowIndices, allRows.Count);
            return true;
        }

        private async Task ApplySheetFormattingAsync(string sheetName, List<int> headerRowIndices, int totalDataRows)
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

            if (requests.Count > 0)
            {
                var batchUpdate = new Google.Apis.Sheets.v4.Data.BatchUpdateSpreadsheetRequest { Requests = requests };
                await _sheetsService.Spreadsheets.BatchUpdate(batchUpdate, spreadsheetId).ExecuteAsync();
            }
        }
        private void BtnAddParent_Click(object sender, EventArgs e)
        {
            // Lấy sản phẩm từ các dòng đang được chọn trên grid (highlight xanh)
            var selectedItems = dgvAllProducts.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => r.DataBoundItem as Products)
                .Where(p => p != null)
                .ToList();

            if (selectedItems.Count == 0) return;

            // Tự động thêm dòng Header nếu danh sách đang rỗng 
            if (configProducts.Count == 0 || !configProducts.Any(p => p.IsHeader))
            {
                // Lấy tên sản phẩm đầu tiên được chọn làm header
                string headerName = "Sản phẩm thêm vào";

                button5.Text = "Lưu";
                currentEditingConfigName = null;
                configProducts.Add(new ConfigProductItem
                {
                    STT = "1",
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
            }

            foreach (var product in selectedItems)
            {
                if (!configProducts.Any(p => p.MaHang == product.SKU))
                {
                    decimal price = 0;
                    decimal.TryParse(product.Price?.Replace(".", "").Replace(",", ""), out price);
                    decimal priceCost = 0;
                    decimal.TryParse(product.PriceCost?.Replace(".", "").Replace(",", ""), out priceCost);

                    configProducts.Add(new ConfigProductItem
                    {
                        STT = (configProducts.Count + 1).ToString(),
                        TenHang = product.Name,
                        MaHang = product.SKU,
                        XuatXu = product.HÃNG,
                        DonVi = "Cái",
                        SoLuong = 1,
                        DonGiaVND = price,
                        ThanhTienVND = price,
                        GhiChu = "",
                        GiaNhap = priceCost > 0 ? priceCost : price,
                        ThanhTien = priceCost > 0 ? priceCost : price,
                        BangGia = price - (priceCost > 0 ? priceCost : price),
                        IsHeader = false
                    });
                }
            }

            // Cập nhật lại STT toàn bộ
            for (int i = 0; i < configProducts.Count; i++)
                configProducts[i].STT = (i + 1).ToString();

            UpdateHeaderSum();
            UpdateConfigGrid();
            dgvAllProducts.ClearSelection();
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
                configProducts[i].BangGia = groupItems.Sum(p => p.BangGia);
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            BtnAddParent_Click(sender, e);
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
                if (columnName == "SoLuong")
                {
                    var item = dgvParentProducts.Rows[e.RowIndex].DataBoundItem as ConfigProductItem;
                    if (item != null && !item.IsHeader)
                    {
                        item.ThanhTienVND = item.SoLuong * item.DonGiaVND;
                        item.ThanhTien = item.SoLuong * item.GiaNhap;
                        item.BangGia = item.ThanhTienVND - item.ThanhTien;

                        UpdateHeaderSum();
                        dgvParentProducts.Refresh(); // Gọi refresh thay vì InvalidateRow để có thể update row Header
                    }
                }
            }
        }

        private void BtnRemoveParent_Click(object sender, EventArgs e)
        {
            if (dgvParentProducts.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dgvParentProducts.SelectedRows)
                {
                    var product = row.DataBoundItem as ConfigProductItem;
                    if (product != null)
                    {
                        // Không cho phép XÓA dòng Header bằng nút Delete này 
                        if (!product.IsHeader)
                        {
                            configProducts.RemoveAll(p => p.MaHang == product.MaHang && !p.IsHeader);
                        }
                    }
                }

                // Cập nhật lại STT sau khi xóa
                for (int i = 0; i < configProducts.Count; i++)
                {
                    configProducts[i].STT = (i + 1).ToString();
                }

                UpdateHeaderSum();
                UpdateConfigGrid();
            }
        }

        private void UpdateConfigGrid()
        {
            // Tạo bản sao cho DataSource để không ảnh hưởng configProducts gốc
            _displayList = configProducts.Where(p => !p.IsSummary).ToList();

            if (_displayList.Count > 0)
            {
                // Tính tổng (chỉ tính các dòng không phải header)
                decimal tongCongGiaNhap = _displayList.Where(p => !p.IsHeader).Sum(p => p.ThanhTien);
                decimal tongCongThanhTien = _displayList.Where(p => !p.IsHeader).Sum(p => p.ThanhTienVND);
                decimal tongCongBangGia = _displayList.Where(p => !p.IsHeader).Sum(p => p.BangGia);
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
                    BangGia = tongCongThanhTien - tongCongGiaNhap,
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
                    foreach (var colName in new[] { "DonGiaVND", "ThanhTienVND", "GiaNhap", "ThanhTien", "BangGia" })
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
                if (dgv.Columns.Contains("BangGia"))
                {
                    dgv.Columns["BangGia"].HeaderText = "Lợi nhuận";
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

                // Cột giá: header màu xanh dương, phân biệt với cột thông tin
                var blueHeader = new DataGridViewCellStyle(yellowHeader)
                {
                    BackColor = Color.FromArgb(0, 112, 192),
                    ForeColor = Color.White
                };
                foreach (var colName in new[] { "GiaNhap", "ThanhTien", "BangGia" })
                {
                    if (dgv.Columns.Contains(colName))
                        dgv.Columns[colName].HeaderCell.Style = blueHeader;
                }

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
                if (dgv.Columns.Contains("BangGia")) dgv.Columns["BangGia"].FillWeight = 90;

                // Căn giữa cột STT, Xuất xứ, Đơn vị, Số lượng
                foreach (var colName in new[] { "STT", "XuatXu", "DonVi", "SoLuong" })
                {
                    if (dgv.Columns.Contains(colName))
                        dgv.Columns[colName].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                // Căn phải cột số tiền
                foreach (var colName in new[] { "DonGiaVND", "ThanhTienVND", "GiaNhap", "ThanhTien", "BangGia" })
                {
                    if (dgv.Columns.Contains(colName))
                        dgv.Columns[colName].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }

                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    // ColMove là custom cell — không set ReadOnly để nhận CellMouseClick
                    if (col.Name == "ColMove") { col.ReadOnly = false; continue; }
                    if (col.Name != "SoLuong" && col.Name != "GhiChu" && col.Name != "TenHang")
                        col.ReadOnly = true;
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
                if ((colName == "BangGia" || colName == "GiaNhap") && (item.TenHang.StartsWith("THUẾ VAT") || item.TenHang == "THÀNH TIỀN"))
                {
                    e.Value = "";
                    e.FormattingApplied = true;
                }

                // Định dạng màu chữ cho cấu hình hiển thị như trong Excel mẫu
                var numberCols = new[] { "DonGiaVND", "ThanhTienVND", "GiaNhap", "ThanhTien", "BangGia" };
                if (Array.IndexOf(numberCols, colName) >= 0)
                {
                    // Chỉ hiển thị màu ĐỎ cho cột Giá Nhập của dòng TỔNG CỘNG
                    if (colName == "GiaNhap" && item.TenHang.StartsWith("TỔNG CỘNG"))
                    {
                        e.CellStyle.ForeColor = Color.Red;
                    }
                    else
                    {
                        e.CellStyle.ForeColor = Color.Black;
                    }
                }
            }
            else if (item.IsHeader)
            {
                // Dòng header nhóm: nền xanh lá
                e.CellStyle.BackColor = Color.LightGreen;
                e.CellStyle.ForeColor = Color.Black;
                e.CellStyle.SelectionBackColor = Color.LimeGreen;
                e.CellStyle.SelectionForeColor = Color.Black;
                e.CellStyle.Font = new Font(dgvParentProducts.Font, FontStyle.Bold);
            }

            // Áp dụng màu tuỳ chỉnh per-cell từ Google Sheet (nếu có lưu SheetRowIndex)
            if (item.SheetRowIndex >= 0)
            {
                // Mapping: Tên cột DGV -> Index cột trên Sheet (0-11)
                string[] sheetColOrder = { "STT", "TenHang", "MaHang", "XuatXu", "DonVi", "SoLuong",
                                         "DonGiaVND", "ThanhTienVND", "GhiChu", "GiaNhap", "ThanhTien", "BangGia" };
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
            var key = (e.RowIndex, e.ColumnIndex);
            if (_cellBgColors.TryGetValue(key, out Color bg))
                e.CellStyle.BackColor = bg;
            if (_cellFgColors.TryGetValue(key, out Color fg))
                e.CellStyle.ForeColor = fg;
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

            // Xóa hết → phục hồi dgvAllProducts về trạng thái bình thường
            RefreshAllProductsGrayOut();
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
                // Đã có header -> Thêm vào ngay dưới nhóm đó
                int insertIdx = headerIdx + 1;
                while (insertIdx < configProducts.Count && !configProducts[insertIdx].IsHeader)
                {
                    insertIdx++;
                }

                foreach (var product in allItems)
                {
                    bool isDuplicate = false;
                    if (!string.IsNullOrEmpty(product.SKU))
                        isDuplicate = configProducts.Any(x => x.MaHang == product.SKU);
                    else
                        isDuplicate = configProducts.Any(x => string.IsNullOrEmpty(x.MaHang) && x.TenHang == product.Name);

                    if (!isDuplicate)
                    {
                        decimal price = 0; decimal.TryParse(product.Price?.Replace(".", "").Replace(",", ""), out price);
                        decimal priceCost = 0; decimal.TryParse(product.PriceCost?.Replace(".", "").Replace(",", ""), out priceCost);
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
                var moneyCols = new[] { "DonGiaVND", "ThanhTienVND", "GiaNhap", "ThanhTien", "BangGia" };
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
                                                  "DonGiaVND","ThanhTienVND","GhiChu","GiaNhap","ThanhTien","BangGia" };
                        int sheetC = Array.IndexOf(sheetColOrd, colNm);
                        var sheetKeyBg = (item.SheetRowIndex, sheetC);

                        Color cellBg = rowBg;
                        if (sheetC >= 0 && item.SheetRowIndex >= 0 && _sheetBgColors.TryGetValue(sheetKeyBg, out Color sheetBg))
                            cellBg = sheetBg;                                   // màu từ Google Sheet
                        if (_cellBgColors.TryGetValue((r, dgvColIdx), out Color customBg))
                            cellBg = customBg;                                  // picker ghi đè
                        xCell.Interior.Color = ColorTranslator.ToOle(cellBg);

                        // ── Màu chữ: summary+tiền → đỏ; sheet color > picker ghi đè ──
                        Color cellFg = (item.IsSummary && Array.IndexOf(moneyCols, colNm) >= 0)
                                       ? Color.Red : rowFg;
                        var sheetKeyFg = (item.SheetRowIndex, sheetC);
                        if (sheetC >= 0 && item.SheetRowIndex >= 0 && _sheetFgColors.TryGetValue(sheetKeyFg, out Color sheetFg))
                            cellFg = sheetFg;
                        if (_cellFgColors.TryGetValue((r, dgvColIdx), out Color customFg))
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
                    }
                }

                // ── 3. Viền bảng + chiều cao hàng ──
                Excel.Range used = ws.Range[ws.Cells[1, 1], ws.Cells[_displayList.Count + 1, visibleCols.Count]];
                used.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
                used.Borders.Weight = Excel.XlBorderWeight.xlThin;
                used.WrapText = false;       // Không xuống dòng bên trong ô
                used.VerticalAlignment = Excel.XlVAlign.xlVAlignCenter;

                // Header cột cao 30pt, dữ liệu 15pt (giống DGV)
                ws.Rows[1].RowHeight = 30;
                for (int r2 = 2; r2 <= _displayList.Count + 1; r2++)
                    ((Excel.Range)ws.Rows[r2]).RowHeight = 15;

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

        private void button8_Click(object sender, EventArgs e)
        {
            // Lấy sản phẩm từ các dòng đang được chọn trên grid (highlight xanh)
            var selectedItems = dgvAllProducts.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => r.DataBoundItem as Products)
                .Where(p => p != null)
                .ToList();

            if (selectedItems.Count == 0) return;

            // ── Bỏ qua kiểm tra quan hệ bắt buộc khi chọn NHIỀU sản phẩm ──
            // Thêm thẳng tất cả sản phẩm được chọn vào nhóm cấu hình (tránh trùng)
            foreach (var product in selectedItems)
            {
                if (!childProducts.Any(p => p.Id == product.Id))
                    childProducts.Add(product);
            }

            // Cập nhật gray-out base từ toàn bộ childProducts
            RefreshAllProductsGrayOut();

            // Tìm SP có quan hệ với các SP vừa thêm, chưa có trong childProducts
            var relatedNotInList = new List<Products>();
            foreach (var p in selectedItems)
            {
                var relIds = productRelations
                    .Where(r => r.ID_Product_Main == p.Id || r.ID_Product_Child == p.Id)
                    .Select(r => r.ID_Product_Main == p.Id ? r.ID_Product_Child : r.ID_Product_Main);
                foreach (var relId in relIds)
                {
                    if (!childProducts.Any(c => c.Id == relId) &&
                        !relatedNotInList.Any(x => x.Id == relId))
                    {
                        var rel = allProducts.FirstOrDefault(x => x.Id == relId);
                        if (rel != null) relatedNotInList.Add(rel);
                    }
                }
            }

            // Nếu có SP quan hệ chưa trong danh sách → hỏi user
            if (relatedNotInList.Count > 0)
            {
                bool addToConfig = false;
                var chosen = ShowRelatedProductsDialog(selectedItems, relatedNotInList, out addToConfig);
                if (chosen != null && chosen.Count > 0)
                {
                    foreach (var p in chosen)
                    {
                        _expandedRelatedIds.Add(p.Id);
                        if (addToConfig && !childProducts.Any(c => c.Id == p.Id))
                        {
                            childProducts.Add(p);
                        }
                    }
                    dgvAllProducts.Refresh();
                    if (addToConfig)
                    {
                        RefreshAllProductsGrayOut();
                    }
                }
            }
        }

        /// <summary>
        /// Modal hiển thị danh sách SP có quan hệ với SP vừa thêm.
        /// User tick chọn SP nào muốn bỏ mờ (hiện rõ) trên dgvAllProducts.
        /// Trả về danh sách SP được tick, hoặc null nếu bấm Hủy.
        /// </summary>
        private List<Products> ShowRelatedProductsDialog(List<Products> sourceProducts, List<Products> relatedProducts, out bool addToConfig)
        {
            addToConfig = false;
            bool internalAddToConfig = false;
            using (var frm = new Form())
            {
                frm.Text = "Sản phẩm có quan hệ";
                frm.StartPosition = FormStartPosition.CenterParent;
                frm.FormBorderStyle = FormBorderStyle.Sizable;   // Cho phép resize
                frm.MaximizeBox = true;
                frm.MinimizeBox = false;
                frm.MinimumSize = new Size(700, 460);
                frm.Width = 960;
                frm.Height = 520;
                frm.Font = new Font("Segoe UI", 9.5f);

                string sourceNames = string.Join(", ", sourceProducts.Select(p => p.Name ?? p.Model ?? $"ID {p.Id}"));
                var lblTitle = new Label
                {
                    Text = $"Sản phẩm \"{sourceNames}\" có {relatedProducts.Count} sản phẩm quan hệ chưa có trong danh sách:",
                    AutoSize = false,
                    Size = new Size(frm.ClientSize.Width - 28, 42),
                    Location = new Point(14, 10),
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.DarkSlateBlue,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                frm.Controls.Add(lblTitle);

                var btnCheckAll   = new Button { Text = "✔ Chọn tất cả",    Size = new Size(120, 26), Location = new Point(14,  56), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f), Anchor = AnchorStyles.Top | AnchorStyles.Left };
                var btnUncheckAll = new Button { Text = "✖ Bỏ chọn tất cả", Size = new Size(130, 26), Location = new Point(142, 56), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f), Anchor = AnchorStyles.Top | AnchorStyles.Left };
                frm.Controls.Add(btnCheckAll);
                frm.Controls.Add(btnUncheckAll);

                // DataGridView chiếm toàn bộ không gian giữa — Anchor All để stretch
                var dgv = new DataGridView
                {
                    Location = new Point(14, 88),
                    Size = new Size(frm.ClientSize.Width - 28, frm.ClientSize.Height - 180),
                    ReadOnly = false,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    RowHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    BackgroundColor = Color.White,
                    Font = new Font("Segoe UI", 9f),
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
                };
                var chkCol = new DataGridViewCheckBoxColumn { HeaderText = "Hiển thị", Name = "colChk", Width = 72, AutoSizeMode = DataGridViewAutoSizeColumnMode.None };
                dgv.Columns.Add(chkCol);
                dgv.Columns.Add("colName", "Tên sản phẩm");
                dgv.Columns.Add("colModel", "Model");
                dgv.Columns.Add("colSKU", "Mã SKU");
                dgv.Columns["colModel"].Width = 130;
                dgv.Columns["colSKU"].Width = 130;

                foreach (var p in relatedProducts)
                    dgv.Rows.Add(true, p.Name, p.Model, p.SKU);

                frm.Controls.Add(dgv);
                btnCheckAll.Click   += (s, ev) => { foreach (DataGridViewRow r in dgv.Rows) r.Cells["colChk"].Value = true;  };
                btnUncheckAll.Click += (s, ev) => { foreach (DataGridViewRow r in dgv.Rows) r.Cells["colChk"].Value = false; };

                var lblHint = new Label
                {
                    Text = "Các sản phẩm được tick sẽ hiển thị rõ (bỏ mờ) trong danh sách bên trái để bạn tiếp tục chọn.",
                    AutoSize = false,
                    Size = new Size(frm.ClientSize.Width - 28, 20),
                    Location = new Point(14, frm.ClientSize.Height - 82),
                    ForeColor = Color.Gray,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
                };
                frm.Controls.Add(lblHint);

                var btnOK = new Button
                {
                    Text = "✔ Hiển thị đã chọn",
                    Size = new Size(160, 34),
                    Location = new Point(frm.ClientSize.Width - 278, frm.ClientSize.Height - 56),
                    DialogResult = DialogResult.OK,
                    BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Right
                };
                btnOK.FlatAppearance.BorderSize = 0;

                var btnCancel = new Button
                {
                    Text = "Bỏ qua",
                    Size = new Size(100, 34),
                    Location = new Point(frm.ClientSize.Width - 110, frm.ClientSize.Height - 56),
                    DialogResult = DialogResult.Cancel,
                    FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f),
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Right
                };

                var btnAddConfig = new Button
                {
                    Text = "➕ ADD VÀO ĐÓNG GÓI CẤU HÌNH",
                    Size = new Size(260, 34),
                    Location = new Point(14, frm.ClientSize.Height - 56),
                    BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Left
                };
                btnAddConfig.FlatAppearance.BorderSize = 0;
                btnAddConfig.Click += (s, ev) => 
                {
                    internalAddToConfig = true;
                    frm.DialogResult = DialogResult.OK;
                    frm.Close();
                };

                frm.Controls.Add(btnOK);
                frm.Controls.Add(btnCancel);
                frm.Controls.Add(btnAddConfig);
                frm.AcceptButton = btnOK;
                frm.CancelButton = btnCancel;

                if (frm.ShowDialog(this) != DialogResult.OK) return null;

                addToConfig = internalAddToConfig;

                var result = new List<Products>();
                for (int i = 0; i < dgv.Rows.Count; i++)
                {
                    bool chk = dgv.Rows[i].Cells["colChk"].Value as bool? ?? false;
                    if (chk) result.Add(relatedProducts[i]);
                }
                return result;
            }
        }

        /// <summary>
        /// Khi chọn dòng trong dataGridView1 (childProducts):
        /// - Có sản phẩm được chọn → tính relatedIds và gray-out sp không quan hệ trên dgvAllProducts
        /// - Không chọn dòng nào → phục hồi dgvAllProducts về bình thường
        /// </summary>
        private void DataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            // Khi selection bên phải thay đổi → reset expansion (user bắt đầu flow mới)
            if (_isUpdatingSelection) return;
            RefreshAllProductsGrayOut();
        }


        /// <summary>
        /// Ngăn user click trực tiếp vào dòng bị disable (gray) trên dgvAllProducts.
        /// </summary>
        private void DgvAllProducts_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (_baseRelatedIds == null || e.RowIndex < 0 || _isUpdatingSelection) return;

            var product = dgvAllProducts.Rows[e.RowIndex].DataBoundItem as Products;
            if (product == null) return;

            if (!IsRelatedProduct(product.Id))
            {
                _isUpdatingSelection = true;
                dgvAllProducts.Rows[e.RowIndex].Selected = false;
                _isUpdatingSelection = false;
            }
        }

        /// <summary>
        /// Tính lại _relatedProductIds từ toàn bộ childProducts hiện tại (union quan hệ của tất cả sp còn lại).
        /// Gọi sau mỗi lần xóa sp khỏi childProducts để cập nhật gray-out trên dgvAllProducts.
        /// - Còn sp trong childProducts có quan hệ → gray-out sp không liên quan
        /// - childProducts rỗng hoặc không sp nào có quan hệ → phục hồi all selectable
        /// </summary>
        private void RefreshAllProductsGrayOut()
        {
            // Tính base từ childProducts
            var baseIds = new HashSet<int>();
            foreach (var p in childProducts)
            {
                var ids = productRelations
                    .Where(r => r.ID_Product_Main == p.Id || r.ID_Product_Child == p.Id)
                    .Select(r => r.ID_Product_Main == p.Id ? r.ID_Product_Child : r.ID_Product_Main);
                foreach (var id in ids) baseIds.Add(id);
            }

            _baseRelatedIds = baseIds.Any() ? baseIds : null;
            _expandedRelatedIds.Clear(); // Reset expansion khi childProducts thay đổi
            dgvAllProducts.Refresh();
        }


        /// <summary>
        /// Gray-out các dòng không có quan hệ khi đang trong chế độ lọc (_relatedProductIds != null).
        /// Ngăn chọn: nếu user cố click vào dòng bị disable → deselect ngay.
        /// </summary>
        private void DgvAllProducts_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_baseRelatedIds == null || e.RowIndex < 0) return;

            var product = dgvAllProducts.Rows[e.RowIndex].DataBoundItem as Products;
            if (product == null) return;

            // Dòng bị ẩn nếu không thuộc base VÀ không thuộc expanded
            if (!IsRelatedProduct(product.Id))
            {
                e.CellStyle.BackColor = Color.FromArgb(235, 235, 235);
                e.CellStyle.ForeColor = Color.FromArgb(170, 170, 170);
                e.CellStyle.SelectionBackColor = Color.FromArgb(215, 215, 215);
                e.CellStyle.SelectionForeColor = Color.FromArgb(150, 150, 150);

                if (dgvAllProducts.Rows[e.RowIndex].Selected && !_isUpdatingSelection)
                {
                    _isUpdatingSelection = true;
                    dgvAllProducts.Rows[e.RowIndex].Selected = false;
                    _isUpdatingSelection = false;
                }
            }
        }

        private async void button11_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmAdvancedConfig())
            {
                await frm.LoadDataAsync(_sheetsService, spreadsheetId);

                if (frm.ShowDialog() == DialogResult.OK)
                {
                    int addedCount = 0;
                    foreach (var item in frm.SelectedAdvancedItems)
                    {
                        if (string.IsNullOrEmpty(item.TenCauHinh)) continue;

                        // Ưu tiên dùng ReferenceProduct đã khớp sẵn trong FrmAdvancedConfig
                        Products prod = item.ReferenceProduct;

                        // Nếu không có thì tạo object Products từ thông tin trong item
                        if (prod == null)
                        {
                            prod = allProducts.FirstOrDefault(p =>
                                string.Equals(p.Name?.Trim(), item.TenCauHinh, StringComparison.OrdinalIgnoreCase));
                        }

                        if (prod == null)
                        {
                            // Tạo mới Products chỉ từ tên + giá
                            prod = new Products
                            {
                                Id    = 0,
                                Name  = item.TenCauHinh,
                                Price = item.DonGia.ToString(),
                            };
                        }

                        // Tránh thêm trùng (kiểm tra theo tên)
                        if (childProducts.Any(p => string.Equals(p.Name, prod.Name, StringComparison.OrdinalIgnoreCase)))
                            continue;

                        childProducts.Add(prod);
                        addedCount++;
                    }

                    if (addedCount > 0)
                    {
                        RefreshAllProductsGrayOut();
                    }
                }
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
    }
}
