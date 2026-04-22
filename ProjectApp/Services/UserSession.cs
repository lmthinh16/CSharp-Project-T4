namespace ProjectApp.Services
{
    /// <summary>
    /// Singleton quản lý phiên đăng nhập.
    /// Persist qua Preferences — không mất khi tắt app.
    /// Hỗ trợ 3 chế độ: Guest, User, Owner/Admin.
    /// </summary>
    public class UserSession
    {
        private static UserSession? _instance;
        public static UserSession Current => _instance ??= new UserSession();

        // Preferences keys
        private const string K_LoggedIn  = "session_logged_in";
        private const string K_IsGuest   = "session_is_guest";
        private const string K_UserId    = "session_user_id";
        private const string K_Username  = "session_username";
        private const string K_FullName  = "session_full_name";
        private const string K_Role      = "session_role";
        private const string K_Lang      = "session_language";

        // ── Properties ───────────────────────────────────────────

        public bool IsLoggedIn
        {
            get => Preferences.Get(K_LoggedIn, false);
            private set => Preferences.Set(K_LoggedIn, value);
        }

        public bool IsGuest
        {
            get => Preferences.Get(K_IsGuest, false);
            private set => Preferences.Set(K_IsGuest, value);
        }

        public int UserId
        {
            get => Preferences.Get(K_UserId, 0);
            private set => Preferences.Set(K_UserId, value);
        }

        public string Username
        {
            get => Preferences.Get(K_Username, string.Empty);
            private set => Preferences.Set(K_Username, value);
        }

        public string FullName
        {
            get => Preferences.Get(K_FullName, "Du khách");
            private set => Preferences.Set(K_FullName, value);
        }

        public string Role
        {
            get => Preferences.Get(K_Role, "user");
            private set => Preferences.Set(K_Role, value);
        }

        /// Ngôn ngữ hiện tại ("vi", "en", "zh", ...)
        public string Language
        {
            get => Preferences.Get(K_Lang, "vi");
            set => Preferences.Set(K_Lang, value);
        }

        // ── Computed ──────────────────────────────────────────────

        /// User thực sự (không phải guest)
        public bool IsAuthenticatedUser => IsLoggedIn && !IsGuest;

        public bool IsAdmin => IsAuthenticatedUser && Role == "admin";
        public bool IsOwner => IsAuthenticatedUser && (Role == "owner" || Role == "admin");

        /// Tên hiển thị: FullName nếu đăng nhập, "Du khách" nếu guest
        public string DisplayName => IsGuest ? "Du khách" : FullName;

        /// Avatar chữ cái đầu
        public string AvatarInitial => IsGuest ? "G"
            : (FullName.Length > 0 ? FullName[0].ToString().ToUpper() : "U");

        // ── Actions ───────────────────────────────────────────────

        /// Gọi sau khi API trả về login thành công
        public void LoginAsUser(int id, string username, string fullName, string role)
        {
            IsLoggedIn = true;
            IsGuest    = false;
            UserId     = id;
            Username   = username;
            FullName   = fullName;
            Role       = role;
            System.Diagnostics.Debug.WriteLine($"[Session] Login: {fullName} ({role})");
        }

        /// Guest mode — không cần tài khoản
        public void LoginAsGuest()
        {
            IsLoggedIn = true;
            IsGuest    = true;
            UserId     = 0;
            Username   = "guest";
            FullName   = "Du khách";
            Role       = "user";
            System.Diagnostics.Debug.WriteLine("[Session] Login as guest");
        }

        /// Đăng xuất — xóa toàn bộ session
        public void Logout()
        {
            Preferences.Remove(K_LoggedIn);
            Preferences.Remove(K_IsGuest);
            Preferences.Remove(K_UserId);
            Preferences.Remove(K_Username);
            Preferences.Remove(K_FullName);
            Preferences.Remove(K_Role);
            // Giữ lại Language preference
            System.Diagnostics.Debug.WriteLine("[Session] Logged out");
        }
    }
}
