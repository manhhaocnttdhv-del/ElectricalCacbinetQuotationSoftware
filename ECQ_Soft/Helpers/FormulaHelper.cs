using System;
using System.Data;
using System.Globalization;
using System.Windows.Forms;

namespace ECQ_Soft.Helpers
{
    /// <summary>
    /// Helper tính công thức khối lượng / diện tích vật liệu.
    /// Ký hiệu biến: a=H (chiều cao), b=W (chiều rộng), c=D (chiều sâu), d=T (độ dày).
    /// </summary>
    public static class FormulaHelper
    {
        public static float EvaluateFormula(string formula, int H, int W, int D, float T)
        {
            try
            {
                if (formula == null)
                {
                    MessageBox.Show("Hãy chọn loại tủ điện",
                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return 0;
                }

                var ci = CultureInfo.InvariantCulture;
                formula = formula
                    .Replace("a", H.ToString(ci))
                    .Replace("b", W.ToString(ci))
                    .Replace("c", D.ToString(ci))
                    .Replace("d", T.ToString(ci));

                var result = new DataTable().Compute(formula, "");
                return Convert.ToSingle(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tính công thức: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0;
            }
        }
    }
}
