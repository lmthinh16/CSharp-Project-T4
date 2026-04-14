using ProjectApp.Models;
using ProjectApp.Services;

namespace ProjectApp
{
    public partial class App : Application
    {
        public static DatabaseService Database { get; private set; } = null!;
        public static AudioQueueService Audio { get; private set; } = null!;

        public App()
        {
            InitializeComponent();
            Database = new DatabaseService();
            Audio = new AudioQueueService();
            InitializeSampleData();
            MainPage = new AppShell();
        }

        private async void InitializeSampleData()
        {
            try
            {
                await Database.ClearRestaurantsAsync();
                await Database.ClearAudiosAsync();
                
                var restaurants = await Database.GetRestaurantsAsync();
                if (restaurants.Count == 0)
                {
                    var r1 = new Restaurant
                    {
                        Name = "Ốc Oanh",
                        Description = "Ốc nướng tiêu, ốc hấp sả đặc sản",
                        Lat = 10.760825909365554,
                        Lng = 106.70331368648232,
                        Address = "Vĩnh Khánh, Phường 8, Quận 4",
                        ImagePath = "oc_oanh.jpg",
                        Rating = 4.6,
                        OpenHours = "16:00 - 23:00",
                        Radius = 50.0,
                        Priority = 10
                    };
                    await Database.SaveRestaurantAsync(r1);

                    await Database.SaveAudioAsync(new Audio { RestaurantId = r1.Id, LanguageCode = "vi-VN", TextContent = "Chào mừng bạn đến với Ốc Oanh, nơi nổi tiếng với món ốc nướng tiêu và ốc hấp sả đặc sản." });
                    await Database.SaveAudioAsync(new Audio { RestaurantId = r1.Id, LanguageCode = "en-US", TextContent = "Welcome to Oc Oanh, famous for its pepper grilled snails and lemongrass steamed snails." });
                    await Database.SaveAudioAsync(new Audio { RestaurantId = r1.Id, LanguageCode = "zh-CN", TextContent = "欢迎来到蜗牛大王，这里以黑胡椒烤蜗牛和香茅蒸蜗牛闻名。" });

                    var r2 = new Restaurant
                    {
                        Name = "Ốc Sáu Nở",
                        Description = "Ốc tươi ngon đa dạng",
                        ImagePath = "oc_sau_no.jpg",
                        Lat = 10.761090779311022,
                        Lng = 106.70289908345818,
                        Address = "Vĩnh Khánh, Phường 8, Quận 4",
                        Rating = 4.4,
                        OpenHours = "15:00 - 23:00"
                    };
                    await Database.SaveRestaurantAsync(r2);

                    var r3 = new Restaurant
                    {
                        Name = "Ốc Thảo",
                        Description = "Ốc các loại chế biến đa dạng",
                        Lat = 10.761758951046252,
                        Lng = 106.70235823553499,
                        ImagePath = "oc_thao.jpg",
                        Address = "Vĩnh Khánh, Phường 8, Quận 4",
                        Rating = 4.2,
                        OpenHours = "16:00 - 22:30"
                    };
                    await Database.SaveRestaurantAsync(r3);
                    
                    await Database.SaveAudioAsync(new Audio { RestaurantId = r3.Id, LanguageCode = "vi-VN", TextContent = "Dành cho những tâm hồn ăn uống yêu thích sự tỉ mỉ, Ốc Thảo mang đến thực đơn ốc với phong cách chế biến cực kỳ đa dạng." });
                    await Database.SaveAudioAsync(new Audio { RestaurantId = r3.Id, LanguageCode = "en-US", TextContent = "For foodies who love meticulousness, Oc Thao offers a snail menu with extremely diverse cooking styles." });
                    await Database.SaveAudioAsync(new Audio { RestaurantId = r3.Id, LanguageCode = "zh-CN", TextContent = "对于喜欢细致的吃货来说，Oc Thao提供了极具多样烹饪风格的蜗牛菜单。" });

                    var r4 = new Restaurant
                    {
                        Name = "Lãng Quán",
                        Description = "Quán ăn phong cách trẻ trung",
                        Lat = 10.761281731910726,
                        Lng = 106.70537328006456,
                        ImagePath = "lang.jpg",
                        Address = "Vĩnh Khánh, Phường 8, Quận 4",
                        Rating = 4.5,
                        OpenHours = "10:00 - 22:00"
                    };
                    await Database.SaveRestaurantAsync(r4);

                    var r5 = new Restaurant
                    {
                        Name = "Ớt Xiêm Quán",
                        Description = "Đồ ăn cay nồng đậm đà",
                        Lat = 10.761345472033696,
                        Lng = 106.70569016657214,
                        ImagePath = "ot_xiem.jpg",
                        Address = "Vĩnh Khánh, Phường 8, Quận 4",
                        Rating = 4.4,
                        OpenHours = "11:00 - 21:00"
                    };
                    await Database.SaveRestaurantAsync(r5);

                    var r6 = new Restaurant
                    {
                        Name = "Bún Cá Châu Đốc - Dì Tư",
                        Description = "Bún cá Châu Đốc chính gốc",
                        Lat = 10.761060311531145,
                        Lng = 106.70668201075144,
                        ImagePath = "bun_ca.jpg",
                        Address = "Vĩnh Khánh, Phường 8, Quận 4",
                        Rating = 4.6,
                        OpenHours = "06:00 - 14:00"
                    };
                    await Database.SaveRestaurantAsync(r6);

                    var r7 = new Restaurant
                    {
                        Name = "Chilli Lẩu Nướng Quán",
                        Description = "Lẩu nướng buffet giá sinh viên",
                        Lat = 10.760840551806485,
                        Lng = 106.70405082000606,
                        ImagePath = "chili.jpg",
                        Address = "Vĩnh Khánh, Phường 8, Quận 4",
                        Rating = 4.3,
                        OpenHours = "10:00 - 23:00"
                    };
                    await Database.SaveRestaurantAsync(r7);

                    var r8 = new Restaurant
                    {
                        Name = "Thế Giới Bò",
                        Description = "Các món bò đa dạng chất lượng",
                        Lat = 10.764267370582093,
                        Lng = 106.70118183588556,
                        ImagePath = "tgbo.jpg",
                        Address = "Vĩnh Khánh, Phường 9, Quận 4",
                        Rating = 4.5,
                        OpenHours = "10:00 - 22:00"
                    };
                    await Database.SaveRestaurantAsync(r8);

                    var r9 = new Restaurant
                    {
                        Name = "Cơm Cháy Kho Quẹt",
                        Description = "Cơm cháy giòn rụm kho quẹt đậm đà",
                        Lat = 10.760625291110975,
                        Lng = 106.70371667475501,
                        ImagePath = "com_chay_kho_quet.jpg",
                        Address = "Vĩnh Khánh, Phường 10, Quận 4",
                        Rating = 4.4,
                        OpenHours = "10:00 - 21:00"
                    };
                    await Database.SaveRestaurantAsync(r9);

                    var r10 = new Restaurant
                    {
                        Name = "Bò Lá Lốt Cô Út",
                        Description = "Bò lá lốt nướng thơm ngon",
                        Lat = 10.761278781423528,
                        Lng = 106.70529381362458,
                        ImagePath ="bo_la_lot.jpg",
                        Address = "Vĩnh Khánh, Phường 8, Quận 4",
                        Rating = 4.7,
                        OpenHours = "15:00 - 23:00"
                    };
                    await Database.SaveRestaurantAsync(r10);

                    var r11 = new Restaurant
                    {
                        Name = "Bún Thịt Nướng Cô Nga",
                        Description = "Bún thịt nướng đặc biệt",
                        Lat = 10.760883450920542,
                        Lng = 106.70674182239293,
                        ImagePath = "bun_thit_nuong.jpg",
                        Address = "Vĩnh Khánh, Phường 8, Quận 4",
                        Rating = 4.5,
                        OpenHours = "06:00 - 20:00"
                    };
                    await Database.SaveRestaurantAsync(r11);
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DB Error: {ex.Message}");
            }
        }
    }
}