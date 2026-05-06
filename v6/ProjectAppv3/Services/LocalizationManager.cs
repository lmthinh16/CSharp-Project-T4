using System.ComponentModel;

namespace ProjectApp.Services
{
    public sealed class LocalizationManager : INotifyPropertyChanged
    {
        public static readonly LocalizationManager Instance = new();

        private string _lang = "vi";

        private static readonly Dictionary<string, Dictionary<string, string>> _all = new()
        {
            ["vi"] = new()
            {
                // Tabs
                ["tab_home"]    = "Trang chủ",
                ["tab_map"]     = "Bản đồ",
                ["tab_tours"]   = "Tours",
                ["tab_profile"] = "Cá nhân",
                // MainPage
                ["main_title"] = "Ẩm Thực Vĩnh Khánh",
                ["main_list"]  = "Danh sách hàng quán",
                // MapPage
                ["map_navigate"]   = "🗺 Dẫn đường",
                ["map_audio"]      = "🔉 Audio",
                ["map_directions"] = "🗺 Đường đi",
                ["map_detail"]     = "ℹ Chi tiết",
                ["map_err_perm"]   = "Bạn chưa cấp quyền vị trí",
                // ToursPage
                ["tours_title"] = "🗺️ Các Tour",
                ["tours_empty"] = "Chưa có tour nào",
                // RestaurantDetailPage
                ["detail_desc"]       = "📄 Mô tả",
                ["detail_audio"]      = "🎵 Thuyết minh",
                ["detail_map"]        = "🗺️ Bản đồ",
                ["detail_fav_add"]    = "❤️ Thêm",
                ["detail_fav_saved"]  = "❤️ Đã lưu",
                ["detail_book"]       = "🍽️ Đặt bàn",
                // ProfilePage
                ["profile_title"]           = "👤 Cá Nhân",
                ["profile_visited"]         = "Đã ghé",
                ["profile_tours"]           = "Tours",
                ["profile_favorites"]       = "Yêu thích",
                ["profile_visit_history"]   = "📜 Lịch sử ghé thăm",
                ["profile_booking_history"] = "📜 Lịch sử đặt bàn",
                ["profile_fav_places"]      = "⭐ Quán yêu thích",
                ["profile_analytics"]       = "📊 Thống kê hành trình",
                ["profile_settings"]        = "⚙️ Cài đặt",
                ["profile_logout_title"]    = "Đăng xuất",
                ["profile_logout_msg"]      = "Bạn có chắc muốn đăng xuất?",
                ["profile_logout_confirm"]  = "Đăng xuất",
                ["profile_logout_cancel"]   = "Hủy",
                // LanguageSelectionPage
                ["lang_title"]    = "Chọn ngôn ngữ",
                ["lang_subtitle"] = "Select your language",
                ["lang_continue"] = "Tiếp tục",
                ["lang_save"]     = "Lưu",
                // Common
                ["err_title"] = "Lỗi",
                ["ok"]        = "OK",
            },
            ["en"] = new()
            {
                ["tab_home"]    = "Home",
                ["tab_map"]     = "Map",
                ["tab_tours"]   = "Tours",
                ["tab_profile"] = "Profile",
                ["main_title"] = "Vinh Khanh Cuisine",
                ["main_list"]  = "Restaurants",
                ["map_navigate"]   = "🗺 Navigate",
                ["map_audio"]      = "🔉 Audio",
                ["map_directions"] = "🗺 Directions",
                ["map_detail"]     = "ℹ Details",
                ["map_err_perm"]   = "Location permission denied",
                ["tours_title"] = "🗺️ Tours",
                ["tours_empty"] = "No tours available",
                ["detail_desc"]       = "📄 Description",
                ["detail_audio"]      = "🎵 Audio Guide",
                ["detail_map"]        = "🗺️ Map",
                ["detail_fav_add"]    = "❤️ Save",
                ["detail_fav_saved"]  = "❤️ Saved",
                ["detail_book"]       = "🍽️ Reserve",
                ["profile_title"]           = "👤 Profile",
                ["profile_visited"]         = "Visited",
                ["profile_tours"]           = "Tours",
                ["profile_favorites"]       = "Favorites",
                ["profile_visit_history"]   = "📜 Visit History",
                ["profile_booking_history"] = "📜 Booking History",
                ["profile_fav_places"]      = "⭐ Favorite Places",
                ["profile_analytics"]       = "📊 Journey Stats",
                ["profile_settings"]        = "⚙️ Settings",
                ["profile_logout_title"]    = "Logout",
                ["profile_logout_msg"]      = "Are you sure you want to logout?",
                ["profile_logout_confirm"]  = "Logout",
                ["profile_logout_cancel"]   = "Cancel",
                ["lang_title"]    = "Select Language",
                ["lang_subtitle"] = "Chọn ngôn ngữ của bạn",
                ["lang_continue"] = "Continue",
                ["lang_save"]     = "Save",
                ["err_title"] = "Error",
                ["ok"]        = "OK",
            },
            ["zh"] = new()
            {
                ["tab_home"]    = "首页",
                ["tab_map"]     = "地图",
                ["tab_tours"]   = "游览",
                ["tab_profile"] = "个人",
                ["main_title"] = "永庆美食",
                ["main_list"]  = "餐厅列表",
                ["map_navigate"]   = "🗺 导航",
                ["map_audio"]      = "🔉 音频",
                ["map_directions"] = "🗺 路线",
                ["map_detail"]     = "ℹ 详情",
                ["map_err_perm"]   = "未授予位置权限",
                ["tours_title"] = "🗺️ 游览",
                ["tours_empty"] = "暂无游览线路",
                ["detail_desc"]       = "📄 描述",
                ["detail_audio"]      = "🎵 语音导览",
                ["detail_map"]        = "🗺️ 地图",
                ["detail_fav_add"]    = "❤️ 收藏",
                ["detail_fav_saved"]  = "❤️ 已收藏",
                ["detail_book"]       = "🍽️ 预订",
                ["profile_title"]           = "👤 个人",
                ["profile_visited"]         = "已访问",
                ["profile_tours"]           = "游览",
                ["profile_favorites"]       = "收藏",
                ["profile_visit_history"]   = "📜 访问历史",
                ["profile_booking_history"] = "📜 预订历史",
                ["profile_fav_places"]      = "⭐ 收藏餐厅",
                ["profile_analytics"]       = "📊 行程统计",
                ["profile_settings"]        = "⚙️ 设置",
                ["profile_logout_title"]    = "退出登录",
                ["profile_logout_msg"]      = "确定要退出登录吗？",
                ["profile_logout_confirm"]  = "退出",
                ["profile_logout_cancel"]   = "取消",
                ["lang_title"]    = "选择语言",
                ["lang_subtitle"] = "Select your language",
                ["lang_continue"] = "继续",
                ["lang_save"]     = "保存",
                ["err_title"] = "错误",
                ["ok"]        = "确定",
            },
            ["ja"] = new()
            {
                ["tab_home"]    = "ホーム",
                ["tab_map"]     = "地図",
                ["tab_tours"]   = "ツアー",
                ["tab_profile"] = "プロフィール",
                ["main_title"] = "ヴィンカイン料理",
                ["main_list"]  = "レストラン一覧",
                ["map_navigate"]   = "🗺 ナビ",
                ["map_audio"]      = "🔉 音声",
                ["map_directions"] = "🗺 経路",
                ["map_detail"]     = "ℹ 詳細",
                ["map_err_perm"]   = "位置情報の許可がありません",
                ["tours_title"] = "🗺️ ツアー",
                ["tours_empty"] = "ツアーがありません",
                ["detail_desc"]       = "📄 説明",
                ["detail_audio"]      = "🎵 音声ガイド",
                ["detail_map"]        = "🗺️ 地図",
                ["detail_fav_add"]    = "❤️ 保存",
                ["detail_fav_saved"]  = "❤️ 保存済み",
                ["detail_book"]       = "🍽️ 予約",
                ["profile_title"]           = "👤 プロフィール",
                ["profile_visited"]         = "訪問済み",
                ["profile_tours"]           = "ツアー",
                ["profile_favorites"]       = "お気に入り",
                ["profile_visit_history"]   = "📜 訪問履歴",
                ["profile_booking_history"] = "📜 予約履歴",
                ["profile_fav_places"]      = "⭐ お気に入り",
                ["profile_analytics"]       = "📊 旅の統計",
                ["profile_settings"]        = "⚙️ 設定",
                ["profile_logout_title"]    = "ログアウト",
                ["profile_logout_msg"]      = "ログアウトしますか？",
                ["profile_logout_confirm"]  = "ログアウト",
                ["profile_logout_cancel"]   = "キャンセル",
                ["lang_title"]    = "言語を選択",
                ["lang_subtitle"] = "Select your language",
                ["lang_continue"] = "続ける",
                ["lang_save"]     = "保存",
                ["err_title"] = "エラー",
                ["ok"]        = "OK",
            },
            ["ko"] = new()
            {
                ["tab_home"]    = "홈",
                ["tab_map"]     = "지도",
                ["tab_tours"]   = "투어",
                ["tab_profile"] = "프로필",
                ["main_title"] = "빈카인 요리",
                ["main_list"]  = "레스토랑 목록",
                ["map_navigate"]   = "🗺 내비게이션",
                ["map_audio"]      = "🔉 오디오",
                ["map_directions"] = "🗺 경로",
                ["map_detail"]     = "ℹ 상세",
                ["map_err_perm"]   = "위치 권한이 없습니다",
                ["tours_title"] = "🗺️ 투어",
                ["tours_empty"] = "투어가 없습니다",
                ["detail_desc"]       = "📄 설명",
                ["detail_audio"]      = "🎵 오디오 가이드",
                ["detail_map"]        = "🗺️ 지도",
                ["detail_fav_add"]    = "❤️ 저장",
                ["detail_fav_saved"]  = "❤️ 저장됨",
                ["detail_book"]       = "🍽️ 예약",
                ["profile_title"]           = "👤 프로필",
                ["profile_visited"]         = "방문함",
                ["profile_tours"]           = "투어",
                ["profile_favorites"]       = "즐겨찾기",
                ["profile_visit_history"]   = "📜 방문 기록",
                ["profile_booking_history"] = "📜 예약 기록",
                ["profile_fav_places"]      = "⭐ 즐겨찾는 곳",
                ["profile_analytics"]       = "📊 여행 통계",
                ["profile_settings"]        = "⚙️ 설정",
                ["profile_logout_title"]    = "로그아웃",
                ["profile_logout_msg"]      = "로그아웃 하시겠습니까?",
                ["profile_logout_confirm"]  = "로그아웃",
                ["profile_logout_cancel"]   = "취소",
                ["lang_title"]    = "언어 선택",
                ["lang_subtitle"] = "Select your language",
                ["lang_continue"] = "계속",
                ["lang_save"]     = "저장",
                ["err_title"] = "오류",
                ["ok"]        = "확인",
            },
        };

        public string this[string key]
        {
            get
            {
                if (_all.TryGetValue(_lang, out var dict) && dict.TryGetValue(key, out var val))
                    return val;
                if (_all["vi"].TryGetValue(key, out val))
                    return val;
                return key;
            }
        }

        public void SetLanguage(string lang)
        {
            _lang = _all.ContainsKey(lang) ? lang : "vi";
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
