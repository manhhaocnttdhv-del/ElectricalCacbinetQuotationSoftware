# Kế hoạch Phân quyền Hệ thống (Role-Based Access Control - RBAC)

Hệ thống phân quyền được xây dựng dựa trên thông tin phiên làm việc trong `UserSession` (lấy từ cơ sở dữ liệu MySQL sau khi đăng nhập thành công). Dữ liệu Báo giá vẫn lưu trên Google Sheets, nhưng quyền xem/ghi sẽ được kiểm soát ở Client dựa trên vai trò của người dùng.

---

## 1. Bảng phân quyền chức năng (Permission Matrix)

| Tab chức năng | Chức năng chi tiết | Sales (Nhân viên) | Manager (Quản lý) | Admin (Quản trị) |
| :--- | :--- | :---: | :---: | :---: |
| **Tab 1: Vỏ tủ & Thang máng** | Xem đơn giá, khối lượng | Có | Có | Có |
| | Thêm đơn hàng của bản thân | Có | Có | Có |
| | Xem/Sửa/Xóa đơn hàng | Chỉ của mình | Của mình & nhân viên trong phòng | Tất cả |
| | Xuất Excel đơn hàng | Có | Có | Có |
| **Tab 2: Liên kết sản phẩm** | Xem cây danh mục, liên kết | Có | Có | Có |
| | Tìm kiếm sản phẩm | Có | Có | Có |
| | Thêm/Xóa liên kết Cha-Con | **Không** | **Không** | Có |
| | Lưu quan hệ vào MySQL | **Không** | **Không** | Có |
| **Tab 3: Báo giá & Tính toán** | Đổi Sheet/Tab (`btnChangeSheet`) | **Không** | **Không** | Có |
| | Cấu hình & đóng gói sản phẩm | Có | Có | Có |
| | Thêm sản phẩm vào Báo giá | Có | Có | Có |
| | Xem dữ liệu bảng báo giá | Chỉ của mình | Của mình & nhân viên trong phòng | Tất cả |
| | Lưu báo giá (`btn_baogia`, `button5`)| Chỉ của mình | Của mình & nhân viên trong phòng | Tất cả |
| | Xóa tất cả báo giá | **Không** | **Không** | Có |
| | Xuất Excel báo giá | Có | Có | Có |

---

## 2. Giải pháp kỹ thuật & Hướng dẫn Cài đặt C#

### 2.1. Cập nhật Sheet Báo giá trên Google Sheets
Để lọc được báo giá của từng người dùng, mỗi dòng báo giá trên Google Sheets cần có cột **`NguoiTao`** (chứa `UserSession.Username`).
* Khi lưu báo giá từ `FrmQuotation` hoặc `FrmConfig` xuống Google Sheets, hệ thống sẽ tự động ghi đè giá trị `UserSession.Username` vào cột này.

---

### 2.2. Triển khai code phân quyền cho Tab 1: Vỏ tủ & Thang máng (`FrmQuotation.cs`)

Trong `FrmQuotation.cs`, khi tải danh sách đơn hàng từ Google Sheets lên `dgvRecord`, ta thực hiện lọc theo UserSession:

```csharp
private void ApplyPermissions()
{
    // Mở/khóa các control nhập liệu nếu cần
}

// Hàm lọc dữ liệu hiển thị trên DataGridView
private List<Record> FilterRecords(List<Record> allRecords)
{
    if (UserSession.HasPermission("quotation:view_all"))
    {
        return allRecords; // Xem toàn bộ
    }
    
    if (UserSession.HasPermission("quotation:view_dept"))
    {
        // Xem của mình và nhân viên cùng phòng ban
        var allowedUsernames = DatabaseService.GetUsernamesInSameDepartment(UserSession.DepartmentId ?? 0);
        allowedUsernames.Add(UserSession.Username);
        
        return allRecords.Where(r => allowedUsernames.Contains(r.Creator, StringComparer.OrdinalIgnoreCase)).ToList();
    }
    
    // Chỉ xem của chính mình
    return allRecords.Where(r => string.Equals(r.Creator, UserSession.Username, StringComparison.OrdinalIgnoreCase)).ToList();
}
```

---

### 2.3. Triển khai code phân quyền cho Tab 2: Liên kết sản phẩm (`FrmRelation.cs`)

Chức năng tạo liên kết sản phẩm là chức năng quản trị danh mục hệ thống, chỉ cho phép User có quyền `relation:edit` (ví dụ Admin hoặc Kỹ thuật viên được cấp quyền) sửa đổi. Nhân viên khác chỉ được xem (Read-only).

```csharp
private void ApplyPermissions()
{
    bool canEdit = UserSession.HasPermission("relation:edit");
    
    // Khóa/Ẩn các nút chỉnh sửa nếu không có quyền
    btnAddParent.Enabled = canEdit;
    btnAddChild.Enabled = canEdit;
    btnSaveRelation.Enabled = canEdit;
    btnRemoveParent.Enabled = canEdit;
    btnRemoveChild.Enabled = canEdit;
    
    // Đổi màu các nút bị khóa để người dùng dễ nhận biết
    if (!canEdit)
    {
        btnAddParent.BackColor = Color.Gray;
        btnAddChild.BackColor = Color.Gray;
        btnSaveRelation.BackColor = Color.Gray;
        btnRemoveParent.BackColor = Color.Gray;
        btnRemoveChild.BackColor = Color.Gray;
    }
}
```

Gọi hàm `ApplyPermissions()` trong sự kiện `Load` hoặc ngay sau khi khởi tạo form.

---

### 2.4. Triển khai code phân quyền cho Tab 3: Báo giá & Tính toán (`FrmConfig.cs`)

Tab 3 quản lý báo giá trực tiếp trên Google Sheets. Phân quyền ở đây rất quan trọng để tránh nhân viên ghi đè hoặc xóa nhầm báo giá của nhau.

```csharp
private void ApplyPermissions()
{
    bool canChangeSheet = UserSession.HasPermission("config:change_sheet");
    bool canDeleteAll = UserSession.HasPermission("quotation:delete_all");
    
    // Chỉ những tài khoản được cấp quyền tương ứng mới thực hiện được các hành động này
    btnChangeSheet.Visible = canChangeSheet;
    button4.Enabled = canDeleteAll; // Nút Xóa tất cả báo giá
    button7.Enabled = canDeleteAll; // Nút Xóa tất cả cấu hình
    
    if (!canDeleteAll)
    {
        button4.BackColor = Color.Gray;
        button7.BackColor = Color.Gray;
    }
}

// Lọc danh sách báo giá khi hiển thị lên dgvParentProducts
private List<QuotationRow> FilterQuotationRows(List<QuotationRow> allRows)
{
    if (UserSession.HasPermission("quotation:view_all")) return allRows;
    
    if (UserSession.HasPermission("quotation:view_dept"))
    {
        var allowedUsernames = DatabaseService.GetUsernamesInSameDepartment(UserSession.DepartmentId ?? 0);
        allowedUsernames.Add(UserSession.Username);
        
        return allRows.Where(r => allowedUsernames.Contains(r.NguoiTao, StringComparer.OrdinalIgnoreCase)).ToList();
    }
    
    // Mặc định xem của chính mình
    return allRows.Where(r => string.Equals(r.NguoiTao, UserSession.Username, StringComparison.OrdinalIgnoreCase)).ToList();
}
```

---

## 3. Các hàm bổ trợ MySQL trong `DatabaseService.cs`

Để hỗ trợ lọc dữ liệu theo phòng ban cho các Quản lý (Manager), thêm hàm này vào [DatabaseService.cs](file:///e:/VNECCO/ElectricalCacbinetQuotationSoftware/ECQ_Soft/Services/DatabaseService.cs):

```csharp
public static List<string> GetUsernamesInSameDepartment(int departmentId)
{
    var usernames = new List<string>();
    string sql = "SELECT username FROM users WHERE department_id = @deptId AND status = 'active'";
    
    var parameters = new MySqlParameter[] {
        new MySqlParameter("@deptId", departmentId)
    };
    
    try
    {
        DataTable dt = ExecuteQuery(sql, parameters);
        foreach (DataRow row in dt.Rows)
        {
            usernames.Add(row["username"].ToString());
        }
    }
    catch (Exception ex)
    {
        // Log lỗi hoặc xử lý ngoại lệ
        System.Diagnostics.Debug.WriteLine("Lỗi truy vấn phòng ban: " + ex.Message);
    }
    
    return usernames;
}
```
