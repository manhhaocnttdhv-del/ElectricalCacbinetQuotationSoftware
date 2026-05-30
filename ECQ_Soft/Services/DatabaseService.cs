using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using ECQ_Soft.Model;
using ECQ_Soft.Services.Database;

namespace ECQ_Soft.Services
{
    /// <summary>
    /// Facade tổng hợp các API truy cập SQL Server mà phần còn lại của app đang dùng.
    /// Logic chi tiết tách sang các class riêng trong namespace
    /// <see cref="ECQ_Soft.Services.Database"/>:
    ///   - <see cref="DbHelpers"/>: connection + execute helper
    ///   - <see cref="EcqBuildConfigSchema"/>: SQL DDL cho ECQ_BuildConfig*
    ///   - <see cref="BuildConfigRepository"/>: CRUD cho cấu hình
    ///   - <see cref="UserPermissionRepository"/>: đọc permission user
    /// </summary>
    public static class DatabaseService
    {
        // ── Connection / generic execute ───────────────────────────────

        public static SqlConnection GetConnection() => DbHelpers.OpenConnection();

        public static DataTable ExecuteQuery(string sql, SqlParameter[] parameters = null)
            => DbHelpers.ExecuteQuery(sql, parameters);

        public static int ExecuteNonQuery(string sql, SqlParameter[] parameters = null)
            => DbHelpers.ExecuteNonQuery(sql, parameters);

        // ── Schema ─────────────────────────────────────────────────────

        public static void EnsureEcqBuildConfigTables() => BuildConfigRepository.EnsureTables();

        public static void InitializeDatabase()
        {
            try
            {
                using (DbHelpers.OpenConnection()) { }
                BuildConfigRepository.EnsureTables();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Loi ket noi SQL Server: " + ex.Message);
            }
        }

        // ── Build config CRUD ──────────────────────────────────────────

        /// <inheritdoc cref="BuildConfigRepository.SaveBuildConfigFromProducts"/>
        public static int SaveBuildConfigFromProducts(
            string configName,
            string googleSheetName,
            string googleSpreadsheetId,
            IEnumerable<Products> products,
            bool overwriteMode)
            => BuildConfigRepository.SaveBuildConfigFromProducts(
                configName, googleSheetName, googleSpreadsheetId, products, overwriteMode);

        /// <inheritdoc cref="BuildConfigRepository.SaveBuildConfigFromConfigItems"/>
        public static int SaveBuildConfigFromConfigItems(
            string configName,
            string googleSheetName,
            string googleSpreadsheetId,
            IEnumerable<ConfigProductItem> configItems,
            IEnumerable<Products> sourceProducts = null)
            => BuildConfigRepository.SaveBuildConfigFromConfigItems(
                configName, googleSheetName, googleSpreadsheetId, configItems, sourceProducts);

        /// <inheritdoc cref="BuildConfigRepository.Delete"/>
        public static int DeleteBuildConfig(int configId) => BuildConfigRepository.Delete(configId);

        /// <inheritdoc cref="BuildConfigRepository.GetAll"/>
        public static List<BuildConfigPackage> GetAllBuildConfigPackages(string configType = "BUILD_PACKAGE")
            => BuildConfigRepository.GetAll(configType);

        // ── Permissions ────────────────────────────────────────────────

        public static List<string> GetUserPermissions(int roleId)
            => UserPermissionRepository.Get(roleId);
    }
}
