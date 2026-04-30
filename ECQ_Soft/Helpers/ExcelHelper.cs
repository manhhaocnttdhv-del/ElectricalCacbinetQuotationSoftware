using System;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;

namespace ECQ_Soft.Helpers
{
    /// <summary>
    /// Helper xuất DataGridView ra file Excel.
    /// </summary>
    public static class ExcelHelper
    {
        public static void ExportToExcel(DataGridView dgv)
        {
            try
            {
                Excel.Application excelApp = new Excel.Application();
                excelApp.Visible       = true;
                excelApp.DisplayAlerts = false;

                Excel.Workbook  workbook  = excelApp.Workbooks.Add(Type.Missing);
                Excel.Worksheet worksheet = workbook.ActiveSheet;
                worksheet.Name = "ExportData";

                // ── 1. Header ────────────────────────────────────────────────
                for (int i = 0; i < dgv.Columns.Count; i++)
                    worksheet.Cells[1, i + 1] = dgv.Columns[i].HeaderText;

                Excel.Range headerRange = worksheet.Range[
                    worksheet.Cells[1, 1],
                    worksheet.Cells[1, dgv.Columns.Count]];
                headerRange.Font.Bold           = true;
                headerRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                headerRange.VerticalAlignment   = Excel.XlVAlign.xlVAlignCenter;

                // ── 2. Dữ liệu ──────────────────────────────────────────────
                for (int i = 0; i < dgv.Rows.Count; i++)
                    for (int j = 0; j < dgv.Columns.Count; j++)
                        if (dgv.Rows[i].Cells[j].Value != null)
                            worksheet.Cells[i + 2, j + 1] = dgv.Rows[i].Cells[j].Value.ToString();

                // ── 3. Format bảng ───────────────────────────────────────────
                Excel.Range used = worksheet.Range[
                    worksheet.Cells[1, 1],
                    worksheet.Cells[dgv.Rows.Count + 1, dgv.Columns.Count]];
                used.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
                used.Columns.AutoFit();
                used.Rows.AutoFit();
                used.WrapText = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi xuất Excel: " + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void ExportWithTemplate(DataGridView dgv, Model.ExportInfo info, string templatePath, string savePath)
        {
            Excel.Application excelApp = null;
            Excel.Workbook workbook = null;
            try
            {
                // Copy file template to destination (for Excel) or temp file (for PDF)
                string tempFilePath = savePath;
                if (info.Format == "PDF")
                {
                    tempFilePath = System.IO.Path.GetTempFileName() + ".xlsx";
                }
                
                System.IO.File.Copy(templatePath, tempFilePath, true);

                excelApp = new Excel.Application();
                excelApp.Visible = false;
                excelApp.DisplayAlerts = false;

                workbook = excelApp.Workbooks.Open(tempFilePath);
                Excel.Worksheet worksheet = workbook.Sheets["BG"]; // Or Sheets[1]

                // Update Header info
                worksheet.Cells[9, 1] = "Kính gửi: " + info.KinhGui;
                worksheet.Cells[10, 1] = "Địa chỉ: " + info.DiaChi;
                worksheet.Cells[11, 1] = "Người nhận: " + info.NguoiNhan;
                worksheet.Cells[12, 1] = "Mã số thuế/ Tax code: " + info.MaSoThue;
                worksheet.Cells[13, 1] = "Nội dung báo giá: " + info.NoiDung;
                worksheet.Cells[10, 6] = "Ngày báo giá : " + DateTime.Now.ToString("dd/MM/yyyy");

                // Clear old sample data (Rows 18 to 565 in template)
                Excel.Range deleteRange = worksheet.Range["18:565"];
                deleteRange.Delete(Excel.XlDeleteShiftDirection.xlShiftUp);

                // Now data starts at 17, and "Tổng Tiền" is at 18.
                int N = dgv.Rows.Count;
                int dataN = N;
                // Exclude summary rows from being exported as data rows
                int summaryCount = 0;
                for (int i = N - 1; i >= 0; i--)
                {
                    var item = dgv.Rows[i].DataBoundItem as ECQ_Soft.Model.ConfigProductItem;
                    if (item != null && item.IsSummary)
                        summaryCount++;
                    else
                        break;
                }
                dataN = N - summaryCount;

                if (dataN == 0)
                {
                    worksheet.Range["A17:I17"].ClearContents();
                }
                else
                {
                    // Insert dataN-1 rows before 18 to accommodate the grid data
                    if (dataN > 1)
                    {
                        Excel.Range insertRange = worksheet.Range[$"18:{18 + dataN - 2}"];
                        insertRange.Insert(Excel.XlInsertShiftDirection.xlShiftDown, Excel.XlInsertFormatOrigin.xlFormatFromLeftOrAbove);

                        // Copy formatting from Row 17
                        Excel.Range row17 = worksheet.Range["17:17"];
                        row17.Copy();
                        worksheet.Range[$"18:{18 + dataN - 2}"].PasteSpecial(Excel.XlPasteType.xlPasteFormats);
                    }

                    int headerCount = 0; // Đếm số dòng IsHeader để đánh STT 1, 2, 3...
                    // Fill data
                    for (int i = 0; i < dataN; i++)
                    {
                        int r = 17 + i;
                        DataGridViewRow dRow = dgv.Rows[i];

                        // DgvParentProducts structure:
                        // STT, TenHang, MaHang, XuatXu, DonVi, SoLuong, DonGia, ThanhTien
                        var itemCheck = dRow.DataBoundItem as ECQ_Soft.Model.ConfigProductItem;
                        bool isPinned = itemCheck != null && ECQ_Soft.Model.ConfigProductItem.IsPinned(itemCheck.TenHang);
                        bool isHeaderRow = itemCheck != null && itemCheck.IsHeader;

                        // STT: chỉ hiển thị cho dòng IsHeader, đánh số tự động 1, 2, 3...
                        if (isHeaderRow) headerCount++;
                        worksheet.Cells[r, 1] = isHeaderRow ? headerCount.ToString() : "";
                        worksheet.Cells[r, 2] = dRow.Cells["TenHang"]?.Value?.ToString() ?? "";

                        if (isPinned)
                        {
                            // Dòng Pinned: chỉ ẩn STT (đã xử lý ở trên), dữ liệu còn lại hiển thị bình thường
                            worksheet.Cells[r, 3] = dRow.Cells["MaHang"]?.Value?.ToString() ?? "";
                            worksheet.Cells[r, 4] = dRow.Cells["XuatXu"]?.Value?.ToString() ?? "";
                            worksheet.Cells[r, 5] = dRow.Cells["DonVi"]?.Value?.ToString() ?? "";
                            worksheet.Cells[r, 6] = dRow.Cells["SoLuong"]?.Value?.ToString() ?? "";
                            worksheet.Cells[r, 7] = dRow.Cells["DonGiaVND"]?.Value?.ToString() ?? "";
                            worksheet.Cells[r, 8] = dRow.Cells["ThanhTienVND"]?.Value?.ToString() ?? "";
                            worksheet.Cells[r, 9] = dRow.Cells["GhiChu"]?.Value?.ToString() ?? "";
                        }
                        else
                        {
                            worksheet.Cells[r, 3] = dRow.Cells["MaHang"]?.Value?.ToString() ?? "";
                            worksheet.Cells[r, 4] = dRow.Cells["XuatXu"]?.Value?.ToString() ?? "";
                            worksheet.Cells[r, 5] = dRow.Cells["DonVi"]?.Value?.ToString() ?? "";
                            worksheet.Cells[r, 6] = dRow.Cells["SoLuong"]?.Value?.ToString() ?? "";
                            worksheet.Cells[r, 7] = dRow.Cells["DonGiaVND"]?.Value?.ToString() ?? "";
                            worksheet.Cells[r, 8] = dRow.Cells["ThanhTienVND"]?.Value?.ToString() ?? "";
                            worksheet.Cells[r, 9] = dRow.Cells["GhiChu"]?.Value?.ToString() ?? "";
                        }

                        // Format row based on DataGridView logic
                        System.Drawing.Color rowBg = System.Drawing.Color.Empty;
                        System.Drawing.Color rowFg = System.Drawing.Color.Black;
                        bool isBold = false;

                        var item = dRow.DataBoundItem as ECQ_Soft.Model.ConfigProductItem;
                        if (item != null)
                        {
                            if (item.IsSummary)
                            {
                                rowBg = System.Drawing.Color.Yellow;
                                isBold = true;
                            }
                            else if (item.IsHeader)
                            {
                                rowBg = System.Drawing.Color.LightGreen;
                                isBold = true;
                            }
                            else if (ECQ_Soft.Model.ConfigProductItem.IsPinned(item.TenHang))
                            {
                                // Không tô màu đặc biệt, để nền mặc định
                            }
                        }

                        // Apply to Excel row
                        Excel.Range rowRange = worksheet.Range[worksheet.Cells[r, 1], worksheet.Cells[r, 9]];
                        
                        if (rowBg != System.Drawing.Color.Empty)
                        {
                            rowRange.Interior.Color = System.Drawing.ColorTranslator.ToOle(rowBg);
                        }
                        else
                        {
                            rowRange.Interior.ColorIndex = Excel.XlColorIndex.xlColorIndexNone;
                        }

                        rowRange.Font.Color = System.Drawing.ColorTranslator.ToOle(rowFg);
                        rowRange.Font.Bold = isBold;
                        rowRange.WrapText = true;

                        // Apply specific column colors for normal rows (DonGia, ThanhTien)
                        if (item != null && !item.IsSummary && !item.IsHeader && !ECQ_Soft.Model.ConfigProductItem.IsPinned(item.TenHang))
                        {
                            // Col 7 = DonGia, Col 8 = ThanhTien -> Cyan
                            worksheet.Cells[r, 7].Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Cyan);
                            worksheet.Cells[r, 8].Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Cyan);
                        }

                        // Dòng Pinned: KHÔNG gộp ô, để hiển thị đầy đủ tất cả các cột với border bình thường
                    }
                }

                // Tự động điều chỉnh chiều cao của tất cả các dòng dữ liệu để đảm bảo text không bị che
                if (dataN > 0)
                {
                    Excel.Range dataRows = worksheet.Range[$"17:{17 + dataN - 1}"];
                    dataRows.Rows.AutoFit();
                }

                // Update Sum Formula at Total Row
                int totalRow = 17 + (dataN > 0 ? dataN : 1);
                int lastDataRow = 17 + Math.Max(0, dataN - 1);
                // Divide by 2 because column H contains both Header sums and Item values.
                worksheet.Cells[totalRow, 8].Formula = $"=SUM(H17:H{lastDataRow})/2";
                // VAT
                worksheet.Cells[totalRow + 1, 8].Formula = $"=H{totalRow}*8%";
                // Total
                worksheet.Cells[totalRow + 2, 8].Formula = $"=H{totalRow}+H{totalRow + 1}";

                if (info.Format == "PDF")
                {
                    // Export to PDF
                    workbook.ExportAsFixedFormat(Excel.XlFixedFormatType.xlTypePDF, savePath);
                    workbook.Close(false);
                    
                    // Clean up temp file
                    try { System.IO.File.Delete(tempFilePath); } catch { }
                    
                    MessageBox.Show("Xuất PDF thành công!\n" + savePath, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // Save Excel
                    workbook.Save();
                    workbook.Close(true);
                    MessageBox.Show("Xuất Excel thành công!\n" + savePath, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                if (workbook != null) workbook.Close(false);
                MessageBox.Show("Lỗi khi xuất file: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (excelApp != null)
                {
                    excelApp.Quit();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
                }
            }
        }
    }
}
