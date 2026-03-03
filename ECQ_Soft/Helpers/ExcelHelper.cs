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
    }
}
