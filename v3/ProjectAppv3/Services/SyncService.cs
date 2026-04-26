using ProjectApp.Models;
using Microsoft.Maui.Networking;
using System.Collections.Generic;

namespace ProjectApp.Services
{
    /// <summary>
    /// Điều phối sync giữa API và SQLite local.
    /// Online  → lấy từ API → ghi vào SQLite.
    /// Offline → đọc SQLite cache; nếu cache rỗng → chạy EnsureLocalSeedAsync().
    /// </summary>
    public class SyncService
    {
        private readonly ApiService      _api;
        private readonly DatabaseService _db;

        public event Action<string>? OnStatusChanged;

        public SyncService(ApiService api, DatabaseService db)
        {
            _api = api;
            _db  = db;
        }

        public async Task SyncAllAsync()
        {
            // Luôn đảm bảo có dữ liệu mặc định trong local DB trước
            await EnsureLocalSeedAsync();

            var hasNet = Connectivity.NetworkAccess == NetworkAccess.Internet;

            if (hasNet)
            {
                OnStatusChanged?.Invoke("Đang tải dữ liệu mới nhất...");
                await SyncRestaurantsAsync();
                await SyncToursAsync();
                await SyncLanguagesAsync();
                OnStatusChanged?.Invoke("Đồng bộ hoàn tất");
            }
            else
            {
                OnStatusChanged?.Invoke("Offline — dùng dữ liệu đã lưu");
            }
        }

        private async Task SyncRestaurantsAsync()
        {
            var data = await _api.GetRestaurantsAsync();
            if (data.Count > 0) await _db.SyncRestaurantsAsync(data);
        }

        private async Task SyncToursAsync()
        {
            var data = await _api.GetToursAsync();
            if (data.Count > 0) await _db.SyncToursAsync(data);
        }

        private async Task SyncLanguagesAsync()
        {
            var data = await _api.GetLanguagesAsync();
            if (data.Count > 0) await _db.SyncLanguagesAsync(data);
        }

        // ── Seed đầy đủ khi lần đầu chạy offline ────────────────
        private async Task EnsureLocalSeedAsync()
        {
            var existingRestaurants = await _db.GetRestaurantsAsync();
            if (existingRestaurants.Count > 0) return;

            // ── Restaurants (giữ nguyên data gốc từ App.xaml.cs) ──
            var restaurants = new List<Restaurant>
            {
                new()
                {
                    Name        = "Ốc Oanh",
                    Description = "Ốc nướng tiêu, ốc hấp sả đặc sản",
                    Category    = "Quán ăn",
                    Latitude    = 10.760825909365554,
                    Longitude   = 106.70331368648232,
                    Address     = "Vĩnh Khánh, Phường 8, Quận 4",
                    ImageUrl    = "oc_oanh.jpg",
                    Rating      = 4.6,
                    OpenHours   = "16:00 - 23:00",
                    Radius      = 50,
                    TtsScript   = "Ốc Oanh nổi tiếng với ốc nướng tiêu và ốc hấp sả đặc sản. Đây là một trong những quán ốc lâu năm nhất tại Vĩnh Khánh, Quận 4.",
                    TtsScriptEn = "Oc Oanh is famous for pepper-grilled snails and lemongrass-steamed snails. This is one of the oldest snail restaurants in Vinh Khanh, District 4.",
                    TtsScriptZh = "Oc Oanh以其胡椒烤蜗牛和香茅蒸蜗牛而闻名。这是荣康第四郡最古老的蜗牛餐厅之一。",
                },
                new()
                {
                    Name        = "Ốc Sáu Nở",
                    Description = "Ốc tươi ngon đa dạng",
                    Category    = "Quán ăn",
                    Latitude    = 10.761090779311022,
                    Longitude   = 106.70289908345818,
                    Address     = "Vĩnh Khánh, Phường 8, Quận 4",
                    ImageUrl    = "oc_sau_no.jpg",
                    Rating      = 4.4,
                    OpenHours   = "15:00 - 23:00",
                    Radius      = 50,
                    TtsScript   = "Ốc Sáu Nở chuyên cung cấp các loại ốc tươi ngon đa dạng, phục vụ theo phong cách dân dã đặc trưng của Vĩnh Khánh.",
                    TtsScriptEn = "Oc Sau No specializes in a wide variety of fresh snails, served in the rustic style characteristic of Vinh Khanh.",
                    TtsScriptZh = "Oc Sau No专门提供各种新鲜美味的蜗牛，以荣康特色的朴实风格服务。",
                },
                new()
                {
                    Name        = "Ốc Thảo",
                    Description = "Ốc các loại chế biến đa dạng",
                    Category    = "Quán ăn",
                    Latitude    = 10.761758951046252,
                    Longitude   = 106.70235823553499,
                    Address     = "Vĩnh Khánh, Phường 8, Quận 4",
                    ImageUrl    = "oc_thao.jpg",
                    Rating      = 4.2,
                    OpenHours   = "16:00 - 22:30",
                    Radius      = 50,
                    TtsScript   = "Ốc Thảo phục vụ nhiều loại ốc được chế biến theo nhiều cách khác nhau, từ nướng, hấp đến xào.",
                    TtsScriptEn = "Oc Thao serves many types of snails prepared in various ways, from grilled and steamed to stir-fried.",
                    TtsScriptZh = "Oc Thao提供多种烹饪方式的蜗牛，包括烤、蒸和炒。",
                },
                new()
                {
                    Name        = "Lãng Quán",
                    Description = "Quán ăn phong cách trẻ trung",
                    Category    = "Quán ăn",
                    Latitude    = 10.761281731910726,
                    Longitude   = 106.70537328006456,
                    Address     = "Vĩnh Khánh, Phường 8, Quận 4",
                    ImageUrl    = "lang.jpg",
                    Rating      = 4.5,
                    OpenHours   = "10:00 - 22:00",
                    Radius      = 50,
                    TtsScript   = "Lãng Quán là quán ăn mang phong cách trẻ trung, năng động với nhiều món ăn đặc sắc và không gian thoải mái.",
                    TtsScriptEn = "Lang Quan is a youthful and dynamic restaurant with distinctive dishes and a relaxed atmosphere.",
                    TtsScriptZh = "浪馆是一家充满活力的餐厅，提供独特的菜肴和轻松的氛围。",
                },
                new()
                {
                    Name        = "Ớt Xiêm Quán",
                    Description = "Đồ ăn cay nồng đậm đà",
                    Category    = "Quán ăn",
                    Latitude    = 10.761345472033696,
                    Longitude   = 106.70569016657214,
                    Address     = "Vĩnh Khánh, Phường 8, Quận 4",
                    ImageUrl    = "ot_xiem.jpg",
                    Rating      = 4.4,
                    OpenHours   = "11:00 - 21:00",
                    Radius      = 50,
                    TtsScript   = "Ớt Xiêm Quán nổi bật với những món ăn cay nồng đậm đà, đặc biệt là các món có ớt xiêm xanh đặc trưng.",
                    TtsScriptEn = "Ot Xiem Quan stands out for its bold and spicy dishes, especially those featuring the distinctive green bird's eye chili.",
                    TtsScriptZh = "辣椒馆以其浓郁辛辣的菜肴著称，尤其是使用特色绿色朝天椒的菜肴。",
                },
                new()
                {
                    Name        = "Bún Cá Châu Đốc - Dì Tư",
                    Description = "Bún cá Châu Đốc chính gốc",
                    Category    = "Quán ăn",
                    Latitude    = 10.761060311531145,
                    Longitude   = 106.70668201075144,
                    Address     = "Vĩnh Khánh, Phường 8, Quận 4",
                    ImageUrl    = "bun_ca.jpg",
                    Rating      = 4.6,
                    OpenHours   = "06:00 - 14:00",
                    Radius      = 50,
                    TtsScript   = "Bún Cá Châu Đốc của Dì Tư mang đến hương vị chính gốc của món bún cá nổi tiếng từ miền Tây Nam Bộ.",
                    TtsScriptEn = "Di Tu's Chau Doc Fish Noodle Soup brings authentic flavors of the famous fish noodle dish from the Mekong Delta region.",
                    TtsScriptZh = "四婶的朱笃鱼粉带来了来自湄公河三角洲地区著名鱼粉的正宗口味。",
                },
                new()
                {
                    Name        = "Chilli Lẩu Nướng Quán",
                    Description = "Lẩu nướng buffet giá sinh viên",
                    Category    = "Quán ăn",
                    Latitude    = 10.760840551806485,
                    Longitude   = 106.70405082000606,
                    Address     = "Vĩnh Khánh, Phường 8, Quận 4",
                    ImageUrl    = "chili.jpg",
                    Rating      = 4.3,
                    OpenHours   = "10:00 - 23:00",
                    Radius      = 50,
                    TtsScript   = "Chilli Lẩu Nướng Quán phục vụ buffet lẩu nướng với mức giá phù hợp sinh viên, không gian rộng rãi và vui vẻ.",
                    TtsScriptEn = "Chilli Hot Pot and Grill offers a hotpot and grill buffet at student-friendly prices in a spacious and lively setting.",
                    TtsScriptZh = "辣椒火锅烧烤店以适合学生的价格提供火锅烧烤自助餐，空间宽敞热闹。",
                },
                new()
                {
                    Name        = "Thế Giới Bò",
                    Description = "Các món bò đa dạng chất lượng",
                    Category    = "Quán ăn",
                    Latitude    = 10.764267370582093,
                    Longitude   = 106.70118183588556,
                    Address     = "Vĩnh Khánh, Phường 8, Quận 4",
                    ImageUrl    = "tgbo.jpg",
                    Rating      = 4.5,
                    OpenHours   = "10:00 - 22:00",
                    Radius      = 50,
                    TtsScript   = "Thế Giới Bò cung cấp đa dạng các món từ thịt bò chất lượng cao, từ bò nướng, bò lúc lắc đến các món lẩu bò.",
                    TtsScriptEn = "Beef World offers a wide variety of high-quality beef dishes, from grilled beef and shaking beef to beef hotpot.",
                    TtsScriptZh = "牛肉世界提供各种优质牛肉菜肴，从烤牛肉、牛肉粒到牛肉火锅。",
                },
                new()
                {
                    Name        = "Cơm Cháy Kho Quẹt",
                    Description = "Cơm cháy giòn rụm kho quẹt đậm đà",
                    Category    = "Quán ăn",
                    Latitude    = 10.760625291110975,
                    Longitude   = 106.70371667475501,
                    Address     = "Vĩnh Khánh, Phường 8, Quận 4",
                    ImageUrl    = "com_chay_kho_quet.jpg",
                    Rating      = 4.4,
                    OpenHours   = "10:00 - 21:00",
                    Radius      = 50,
                    TtsScript   = "Cơm Cháy Kho Quẹt nổi tiếng với cơm cháy giòn tan ăn kèm kho quẹt đậm đà, món ăn mang hương vị miền quê đặc trưng.",
                    TtsScriptEn = "Crispy Rice with Braised Pork Sauce is famous for its crispy burnt rice served with rich braised sauce, a dish with a distinctive rustic flavor.",
                    TtsScriptZh = "锅巴沾酱以其酥脆的锅巴配上浓郁的红烧酱著称，是一道具有独特乡土风味的菜肴。",
                },
                new()
                {
                    Name        = "Bò Lá Lốt Cô Út",
                    Description = "Bò lá lốt nướng thơm ngon",
                    Category    = "Quán ăn",
                    Latitude    = 10.761278781423528,
                    Longitude   = 106.70529381362458,
                    Address     = "Vĩnh Khánh, Phường 8, Quận 4",
                    ImageUrl    = "bo_la_lot.jpg",
                    Rating      = 4.7,
                    OpenHours   = "15:00 - 23:00",
                    Radius      = 50,
                    TtsScript   = "Bò Lá Lốt Cô Út nổi tiếng với món bò lá lốt nướng thơm ngon, được chế biến theo công thức gia truyền độc đáo.",
                    TtsScriptEn = "Co Ut's Betel Leaf Beef is famous for its fragrant grilled beef wrapped in betel leaves, prepared according to a unique family recipe.",
                    TtsScriptZh = "小姑姑叶包牛肉以其香气四溢的叶包烤牛肉著称，按照独特的家传秘方制作。",
                },
                new()
                {
                    Name        = "Bún Thịt Nướng Cô Nga",
                    Description = "Bún thịt nướng đặc biệt",
                    Category    = "Quán ăn",
                    Latitude    = 10.760883450920542,
                    Longitude   = 106.70674182239293,
                    Address     = "Vĩnh Khánh, Phường 8, Quận 4",
                    ImageUrl    = "bun_thit_nuong.jpg",
                    Rating      = 4.5,
                    OpenHours   = "06:00 - 20:00",
                    Radius      = 50,
                    TtsScript   = "Bún Thịt Nướng Cô Nga là địa chỉ quen thuộc với món bún thịt nướng đặc biệt, thịt nướng thơm lừng ăn kèm rau sống và nước mắm chua ngọt.",
                    TtsScriptEn = "Co Nga's Grilled Pork Vermicelli is a familiar address for the special vermicelli with grilled pork, fragrant grilled meat served with fresh vegetables and sweet-sour fish sauce.",
                    TtsScriptZh = "阿娥烤肉米粉是烤肉米粉的老字号，香气扑鼻的烤肉配上新鲜蔬菜和酸甜鱼露。",
                },
            };

            foreach (var r in restaurants)
            {
                await _db.SaveRestaurantAsync(r);
                // Sau SaveRestaurantAsync, r.Id đã được SQLite cập nhật về Id thật

                // Kiểm tra đã có AudioGuide cho nhà hàng này chưa
                var existingAudios = await _db.GetAudioGuidesAsync(r.Id);
                if (existingAudios.Count > 0) continue; // Đã có, bỏ qua

                // Tạo AudioGuide offline dựa trên TtsScript có sẵn trong Restaurant
                if (!string.IsNullOrEmpty(r.TtsScript))
                {
                    await _db.SaveAudioGuideAsync(new AudioGuide
                    {
                        RestaurantId  = r.Id,
                        Title         = $"🎙️ Giới thiệu: {r.Name}",
                        LanguageCode  = "vi-VN",
                        TextContent   = r.TtsScript,
                        FilePath      = "", // Offline mode: không có file, sẽ đọc TTS
                        IsGeneratedByTTS = true,
                        DurationSeconds = 60,
                        SortOrder     = 0
                    });
                }

                if (!string.IsNullOrEmpty(r.TtsScriptEn))
                {
                    await _db.SaveAudioGuideAsync(new AudioGuide
                    {
                        RestaurantId  = r.Id,
                        Title         = $"🎙️ English: {r.Name}",
                        LanguageCode  = "en-US",
                        TextContent   = r.TtsScriptEn,
                        FilePath      = "",
                        IsGeneratedByTTS = true,
                        DurationSeconds = 65,
                        SortOrder     = 1
                    });
                }
            }

            // ── Tours (giữ nguyên từ MainPage.xaml.cs gốc) ─────────
            var tours = new List<Tour>
            {
                new()
                {
                    Id          = 1,
                    Name        = "Tour Ốc",
                    NameEn      = "Snail Tour",
                    Emoji       = "🦪",
                    Description = "3 quán ốc ngon nổi tiếng",
                    DescEn      = "3 famous snail restaurants",
                    Duration    = "45 phút",
                    Rating      = 4.4,
                    IsActive    = true,
                    Pois        = "[1,2,3]",
                },
                new()
                {
                    Id          = 2,
                    Name        = "Đồ Nướng",
                    NameEn      = "Grill Tour",
                    Emoji       = "🔥",
                    Description = "Lẩu nướng, bò lá lốt",
                    DescEn      = "Hotpot and grilled beef",
                    Duration    = "60 phút",
                    Rating      = 4.5,
                    IsActive    = true,
                    Pois        = "[7,8,10]",
                },
                new()
                {
                    Id          = 3,
                    Name        = "Tour Ăn Vặt",
                    NameEn      = "Street Snack Tour",
                    Emoji       = "🍢",
                    Description = "Cơm cháy, bún thịt nướng",
                    DescEn      = "Crispy rice and grilled pork noodles",
                    Duration    = "40 phút",
                    Rating      = 4.3,
                    IsActive    = true,
                    Pois        = "[9,11]",
                },
            };

            foreach (var t in tours)
                await _db.SaveTourAsync(t);

            // ── Languages ─────────────────────────────────────────
            await _db.SyncLanguagesAsync(new List<AppLanguage>
            {
                new() { Code="vi", Name="Tiếng Việt", Flag="🇻🇳", IsDefault=true,  SortOrder=0 },
                new() { Code="en", Name="English",    Flag="🇺🇸", IsDefault=false, SortOrder=1 },
                new() { Code="zh", Name="中文",        Flag="🇨🇳", IsDefault=false, SortOrder=2 },
            });

            
        }
    }
}
