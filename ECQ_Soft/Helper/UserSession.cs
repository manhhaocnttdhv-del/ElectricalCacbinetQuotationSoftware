using System;
using System.Collections.Generic;
using System.Linq;

namespace ECQ_Soft.Helper
{
    public static class UserSession
    {
        public static int UserId { get; set; }
        public static string Username { get; set; }
        public static string FullName { get; set; }
        public static int RoleId { get; set; }
        public static string RoleCode { get; set; } // "ADMIN", "MANAGER", "SALES", ...
        
        // Cung cấp thuộc tính Role kiểu cũ để tương thích ngược với các file hiện tại
        public static string Role 
        { 
            get => RoleCode?.ToLower(); 
            set => RoleCode = value?.ToUpper(); 
        }
        
        public static int? DepartmentId { get; set; }
        
        // Danh sách mã quyền hạn của user
        public static List<string> Permissions { get; set; } = new List<string>();

        public static bool HasPermission(string permissionCode)
        {
            if (Permissions == null) return false;
            // Admin mặc định có tất cả quyền
            if (string.Equals(RoleCode, "ADMIN", StringComparison.OrdinalIgnoreCase)) return true;
            return Permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
        }

        public static void Clear()
        {
            UserId = 0;
            Username = null;
            FullName = null;
            RoleId = 0;
            RoleCode = null;
            DepartmentId = null;
            Permissions.Clear();
        }
    }
}
