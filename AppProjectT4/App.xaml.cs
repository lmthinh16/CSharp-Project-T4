using ProjectApp.Models;
using ProjectApp.Services;

namespace ProjectApp
{
    public partial class App : Application
    {
        public static DatabaseService Database { get; private set; } = null!;

        public App()
        {
            InitializeComponent();
            Database = new DatabaseService();
            InitializeSampleData();
            MainPage = new MainTabbedPage();
        }

        private async void InitializeSampleData()
        {
            try
            {
                var restaurants = await Database.GetRestaurantsAsync();
                if (restaurants.Count == 0)
                {
                    await Database.SaveRestaurantAsync(new Restaurant
                    {
                        Name = "Ốc Oanh",
                        Description = "Ốc nướng tiêu, ốc hấp sả đặc sản",
                        Category = "Quán ăn",
                        Latitude = 10.760825909365554,
                        Longitude = 106.70331368648232,
                        Address = "Vĩnh Khánh, Phường 8, Quận 4",
                        ImageUrl = "oc_oanh.jpg",
                        Rating = 4.6,
                        OpenHours = "16:00 - 23:00"
                    });

                    await Database.SaveRestaurantAsync(new Restaurant
                    {
                        Name = "Ốc Sáu Nở",
                        Description = "Ốc tươi ngon đa dạng",
                        Category = "Quán ăn",
                        ImageUrl = "oc_sau_no.jpg",
                        Latitude = 10.761090779311022,
                        Longitude = 106.70289908345818,
                        Address = "Vĩnh Khánh, Phường 8, Quận 4",
                        Rating = 4.4,
                        OpenHours = "15:00 - 23:00"
                    });

                    await Database.SaveRestaurantAsync(new Restaurant
                    {
                        Name = "Ốc Thảo",
                        Description = "Ốc các loại chế biến đa dạng",
                        Category = "Quán ăn",
                        Latitude = 10.761758951046252,
                        Longitude = 106.70235823553499,
                        ImageUrl = "oc_thao.jpg",
                        Address = "Vĩnh Khánh, Phường 8, Quận 4",
                        Rating = 4.2,
                        OpenHours = "16:00 - 22:30"
                    });

                    await Database.SaveRestaurantAsync(new Restaurant
                    {
                        Name = "Lãng Quán",
                        Description = "Quán ăn phong cách trẻ trung",
                        Category = "Quán ăn",
                        Latitude = 10.761281731910726,
                        Longitude = 106.70537328006456,
                        ImageUrl = "lang.jpg",
                        Address = "Vĩnh Khánh, Phường 8, Quận 4",
                        Rating = 4.5,
                        OpenHours = "10:00 - 22:00"
                    });

                    await Database.SaveRestaurantAsync(new Restaurant
                    {
                        Name = "Ớt Xiêm Quán",
                        Description = "Đồ ăn cay nồng đậm đà",
                        Category = "Quán ăn",
                        Latitude = 10.761345472033696,
                        Longitude = 106.70569016657214,
                        ImageUrl = "ot_xiemjpg",
                        Address = "Vĩnh Khánh, Phường 8, Quận 4",
                        Rating = 4.4,
                        OpenHours = "11:00 - 21:00"
                    });

                    await Database.SaveRestaurantAsync(new Restaurant
                    {
                        Name = "Bún Cá Châu Đốc - Dì Tư",
                        Description = "Bún cá Châu Đốc chính gốc",
                        Category = "Quán ăn",
                        Latitude = 10.761060311531145,
                        Longitude = 106.70668201075144,

                        Address = "Vĩnh Khánh, Phường 8, Quận 4",
                        Rating = 4.6,
                        OpenHours = "06:00 - 14:00"
                    });

                    await Database.SaveRestaurantAsync(new Restaurant
                    {
                        Name = "Chilli Lẩu Nướng Quán",
                        Description = "Lẩu nướng buffet giá sinh viên",
                        Category = "Quán ăn",
                        Latitude = 10.760840551806485,
                        Longitude = 106.70405082000606,
                        ImageUrl = "bunca.jpg",
                        Address = "Vĩnh Khánh, Phường 8, Quận 4",
                        Rating = 4.3,
                        OpenHours = "10:00 - 23:00"
                    });

                    await Database.SaveRestaurantAsync(new Restaurant
                    {
                        Name = "Thế Giới Bò",
                        Description = "Các món bò đa dạng chất lượng",
                        Category = "Quán ăn",
                        Latitude = 10.764267370582093,
                        Longitude = 106.70118183588556,
                        ImageUrl = "tgbo.jpg",
                        Address = "Vĩnh Khánh, Phường 8, Quận 4",
                        Rating = 4.5,
                        OpenHours = "10:00 - 22:00"
                    });

                    await Database.SaveRestaurantAsync(new Restaurant
                    {
                        Name = "Cơm Cháy Kho Quẹt",
                        Description = "Cơm cháy giòn rụm kho quẹt đậm đà",
                        Category = "Quán ăn",
                        Latitude = 10.760625291110975,
                        Longitude = 106.70371667475501,
                        ImageUrl = "com_chay_kho_quet.jpg",
                        Address = "Vĩnh Khánh, Phường 8, Quận 4",
                        Rating = 4.4,
                        OpenHours = "10:00 - 21:00"
                    });

                    await Database.SaveRestaurantAsync(new Restaurant
                    {
                        Name = "Bò Lá Lốt Cô Út",
                        Description = "Bò lá lốt nướng thơm ngon",
                        Category = "Quán ăn",
                        Latitude = 10.761278781423528,
                        Longitude = 106.70529381362458,
                        ImageUrl ="bo_la_lot.jpg",
                        Address = "Vĩnh Khánh, Phường 8, Quận 4",
                        Rating = 4.7,
                        OpenHours = "15:00 - 23:00"
                    });

                    await Database.SaveRestaurantAsync(new Restaurant
                    {
                        Name = "Bún Thịt Nướng Cô Nga",
                        Description = "Bún thịt nướng đặc biệt",
                        Category = "Quán ăn",
                        Latitude = 10.760883450920542,
                        Longitude = 106.70674182239293,
                        ImageUrl = "bun_thit_nuong.jpg",
                        Address = "Vĩnh Khánh, Phường 8, Quận 4",
                        Rating = 4.5,
                        OpenHours = "06:00 - 20:00"
                    });
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DB Error: {ex.Message}");
            }
        }
    }
}