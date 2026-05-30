using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace ECQ_Soft.Services.Database
{
    /// <summary>
    /// Repository đọc permission của user từ bảng PermissionRecord_Role_Mapping.
    /// Có thêm bộ permission ECQ admin được tự động gán nếu user có "AccessAdminPanel".
    /// </summary>
    internal static class UserPermissionRepository
    {
        public static List<string> Get(int roleId)
        {
            var permissions = new List<string>();

            const string sql = @"
                SELECT pr.SystemName
                FROM [dbo].[PermissionRecord_Role_Mapping] prm
                INNER JOIN [dbo].[PermissionRecord] pr ON pr.Id = prm.PermissionRecord_Id
                WHERE prm.CustomerRole_Id = @roleId";

            try
            {
                DataTable dt = DbHelpers.ExecuteQuery(sql, new[]
                {
                    new SqlParameter("@roleId", roleId)
                });

                foreach (DataRow row in dt.Rows)
                {
                    permissions.Add(row["SystemName"].ToString());
                }

                if (permissions.Contains("AccessAdminPanel"))
                {
                    AddEcqAdminPermissions(permissions);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Loi tai quyen han SQL Server: " + ex.Message);
            }

            return permissions;
        }

        private static readonly string[] EcqAdminPermissions =
        {
            "quotation:view_all",
            "quotation:view_dept",
            "quotation:update_price",
            "quotation:add_record",
            "quotation:delete_record",
            "quotation:export_excel",
            "relation:view",
            "relation:edit",
            "relation:save",
            "quotation:add_product",
            "quotation:advanced_config",
            "quotation:save_to_quote",
            "quotation:clear_all",
            "config:pack_config",
            "config:view_all",
            "config:view_dept",
            "config:save_quote",
            "config:clear_all",
            "config:export_excel",
            "config:change_sheet",
            "config:load_config",
            "config:sync_sheet",
            "config:edit_formula",
            "quotation:advanced_config:update_name",
            "quotation:advanced_config:apply",
            "quotation:advanced_config:reload",
            "quotation:advanced_config:add_item",
            "quotation:advanced_config:delete_item",
            "user:manage",
            "role:manage",
            "system:view_logs"
        };

        private static void AddEcqAdminPermissions(List<string> permissions)
        {
            foreach (string permission in EcqAdminPermissions)
            {
                if (!permissions.Contains(permission))
                {
                    permissions.Add(permission);
                }
            }
        }
    }
}
