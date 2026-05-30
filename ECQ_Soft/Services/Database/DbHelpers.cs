using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace ECQ_Soft.Services.Database
{
    /// <summary>
    /// Helper hạ tầng cho các thao tác SQL Server: connection, execute, parameter,
    /// type conversion. Tất cả repository khác build trên đây.
    /// </summary>
    internal static class DbHelpers
    {
        private const string FallbackConnectionString =
            "Server=103.121.91.94,1433;Database=vnecco_erp;User ID=erp_vnecco;Password=@vnecco123;" +
            "Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=15;";

        public static string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["SqlServerVnecco"]?.ConnectionString
                ?? FallbackConnectionString;
        }

        public static SqlConnection OpenConnection()
        {
            var conn = new SqlConnection(GetConnectionString());
            conn.Open();
            return conn;
        }

        public static DataTable ExecuteQuery(string sql, SqlParameter[] parameters = null)
        {
            using (var conn = OpenConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
            }
        }

        public static int ExecuteNonQuery(string sql, SqlParameter[] parameters = null)
        {
            using (var conn = OpenConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteNonQuery();
            }
        }

        public static object ExecuteScalar(SqlConnection conn, SqlTransaction tran,
            string sql, SqlParameter[] parameters)
        {
            using (var cmd = new SqlCommand(sql, conn, tran))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteScalar();
            }
        }

        public static int ExecuteNonQuery(SqlConnection conn, SqlTransaction tran,
            string sql, SqlParameter[] parameters)
        {
            using (var cmd = new SqlCommand(sql, conn, tran))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteNonQuery();
            }
        }

        public static SqlParameter Param(string name, object value)
        {
            return new SqlParameter(name, value ?? DBNull.Value);
        }

        public static int ToInt(object v)
        {
            if (v == null || v == DBNull.Value) return 0;
            return Convert.ToInt32(v);
        }

        public static int? ToNullableInt(object v)
        {
            if (v == null || v == DBNull.Value) return null;
            return Convert.ToInt32(v);
        }
    }
}
