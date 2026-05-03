-- ============================================================
-- FoodTourDB - Clean SQL Queries
-- Generated from: Vk.sql | Date: 29/04/2026
-- ============================================================


-- ============================================================
-- 1. CREATE DATABASE
-- ============================================================

CREATE DATABASE [FoodTourDB];
GO

USE [FoodTourDB];
GO


-- ============================================================
-- 2. CREATE TABLES
-- ============================================================

CREATE TABLE [dbo].[__EFMigrationsHistory] (
    [MigrationId]    NVARCHAR(150) NOT NULL,
    [ProductVersion] NVARCHAR(32)  NOT NULL,
    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED ([MigrationId] ASC)
);
GO

CREATE TABLE [dbo].[Audios] (
    [Id]                [int] IDENTITY(1,1) NOT NULL,
    [VoiceName]         NVARCHAR(MAX)  NOT NULL,
    [Title]             NVARCHAR(MAX)  NOT NULL,
    [UploadedAt]        DATETIME2(7)   NOT NULL,
    [FileName]          NVARCHAR(MAX)  NOT NULL,
    [FilePath]          NVARCHAR(MAX)  NOT NULL,
    [RestaurantId]      INT            NULL,
    [TextContent]       NVARCHAR(MAX)  NOT NULL,
    [IsGeneratedByTTS]  BIT            NOT NULL,
    [LanguageCode]      NVARCHAR(10)   NOT NULL,
    [Duration]          INT            NULL,
    [CooldownMinutes]   INT            NULL,
    CONSTRAINT [PK_Audios] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE TABLE [dbo].[Restaurants] (
    [Id]          INT IDENTITY(1,1) NOT NULL,
    [Name]        NVARCHAR(MAX)  NOT NULL,
    [Address]     NVARCHAR(MAX)  NOT NULL,
    [AudioPath]   NVARCHAR(MAX)  NULL,
    [Description] NVARCHAR(MAX)  NULL,
    [Lat]         FLOAT          NULL,
    [Lng]         FLOAT          NULL,
    [OpenHours]   NVARCHAR(100)  NULL,
    [Rating]      FLOAT          NULL,
    [ImagePath]   NVARCHAR(255)  NULL,
    [Radius]      FLOAT          NULL,
    [Priority]    INT            NULL,
    [IsActive]    BIT            NULL,
    CONSTRAINT [PK_Restaurants] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE TABLE [dbo].[TourDetails] (
    [TourId]       INT NOT NULL,
    [RestaurantId] INT NOT NULL,
    [OrderIndex]   INT NOT NULL,
    CONSTRAINT [PK_TourDetails] PRIMARY KEY CLUSTERED ([TourId] ASC, [RestaurantId] ASC)
);
GO

CREATE TABLE [dbo].[Tours] (
    [Id]                 INT IDENTITY(1,1) NOT NULL,
    [Title]              NVARCHAR(MAX)  NOT NULL,
    [Description]        NVARCHAR(MAX)  NULL,
    [CreatedAt]          DATETIME2(7)   NULL,
    [TotalEstimatedTime] NVARCHAR(100)  NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE TABLE [dbo].[UserActivityLogs] (
    [Id]               INT IDENTITY(1,1) NOT NULL,
    [UserId]           INT            NULL,
    [RestaurantId]     INT            NOT NULL,
    [Lat]              FLOAT          NOT NULL,
    [Lng]              FLOAT          NOT NULL,
    [ActionType]       NVARCHAR(50)   NULL,
    [DurationListened] INT            NULL,
    [CreatedAt]        DATETIME2(7)   NULL,
    [LanguageCode]     NVARCHAR(10)   NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE TABLE [dbo].[Users] (
    [Id]           INT IDENTITY(1,1) NOT NULL,
    [Username]     NVARCHAR(MAX)  NOT NULL,
    [Password]     NVARCHAR(MAX)  NOT NULL,
    [Role]         NVARCHAR(20)   NULL,
    [RestaurantId] INT            NULL,
    [Email]        NVARCHAR(255)  NULL,
    [IsLocked]     BIT            NOT NULL,
    [LastActiveAt] DATETIME       NULL,
    [CreatedAt]    DATETIME       NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO


-- ============================================================
-- 3. DEFAULT CONSTRAINTS
-- ============================================================

ALTER TABLE [dbo].[Audios]     ADD DEFAULT (N'')                   FOR [FileName];
ALTER TABLE [dbo].[Audios]     ADD DEFAULT (N'')                   FOR [FilePath];
ALTER TABLE [dbo].[Audios]     ADD DEFAULT (N'')                   FOR [TextContent];
ALTER TABLE [dbo].[Audios]     ADD DEFAULT (CONVERT([bit], (0)))   FOR [IsGeneratedByTTS];
ALTER TABLE [dbo].[Audios]     ADD DEFAULT ('vi-VN')               FOR [LanguageCode];
ALTER TABLE [dbo].[Audios]     ADD DEFAULT ((0))                   FOR [Duration];
ALTER TABLE [dbo].[Audios]     ADD DEFAULT ((10))                  FOR [CooldownMinutes];

ALTER TABLE [dbo].[Restaurants] ADD DEFAULT ((50))                 FOR [Radius];
ALTER TABLE [dbo].[Restaurants] ADD DEFAULT ((0))                  FOR [Priority];
ALTER TABLE [dbo].[Restaurants] ADD DEFAULT ((1))                  FOR [IsActive];

ALTER TABLE [dbo].[Tours]           ADD DEFAULT (GETDATE())        FOR [CreatedAt];
ALTER TABLE [dbo].[UserActivityLogs] ADD DEFAULT (GETDATE())       FOR [CreatedAt];

ALTER TABLE [dbo].[Users] ADD DEFAULT ('User')  FOR [Role];
ALTER TABLE [dbo].[Users] ADD DEFAULT ((0))     FOR [IsLocked];
ALTER TABLE [dbo].[Users] ADD DEFAULT (GETDATE()) FOR [CreatedAt];
GO


-- ============================================================
-- 4. INDEXES
-- ============================================================

CREATE NONCLUSTERED INDEX [IX_Audios_RestaurantId]
    ON [dbo].[Audios] ([RestaurantId] ASC);
GO


-- ============================================================
-- 5. FOREIGN KEY CONSTRAINTS
-- ============================================================

ALTER TABLE [dbo].[Audios]
    ADD CONSTRAINT [FK_Audios_Restaurants_RestaurantId]
    FOREIGN KEY ([RestaurantId]) REFERENCES [dbo].[Restaurants] ([Id]);

ALTER TABLE [dbo].[TourDetails]
    ADD CONSTRAINT [FK_TourDetails_Restaurants]
    FOREIGN KEY ([RestaurantId]) REFERENCES [dbo].[Restaurants] ([Id]);

ALTER TABLE [dbo].[TourDetails]
    ADD CONSTRAINT [FK_TourDetails_Tours]
    FOREIGN KEY ([TourId]) REFERENCES [dbo].[Tours] ([Id]);

ALTER TABLE [dbo].[UserActivityLogs]
    ADD CONSTRAINT [FK_Logs_Restaurants]
    FOREIGN KEY ([RestaurantId]) REFERENCES [dbo].[Restaurants] ([Id]);
GO


-- ============================================================
-- 6. CHECK CONSTRAINTS
-- ============================================================

ALTER TABLE [dbo].[Audios]
    ADD CONSTRAINT [CHK_Language]
    CHECK ([LanguageCode] IN ('vi-VN', 'en-US', 'zh-CN', 'ko-KR', 'ja-JP'));
GO


-- ============================================================
-- 7. SEED DATA - EF Migrations History
-- ============================================================

INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES
    (N'20260331072213_Init',                  N'9.0.14'),
    (N'20260331172038_AddAudio',              N'9.0.14'),
    (N'20260331195427_AddAudioTable',         N'9.0.14'),
    (N'20260331204918_AddAudioColumns',       N'9.0.14'),
    (N'20260408043419_UpdateAudioRelationship', N'9.0.14'),
    (N'20260412044419_InitDB',                N'9.0.14'),
    (N'20260414094858_AddRestaurantFields',   N'9.0.14'),
    (N'20260414100007_FixModelSync',          N'9.0.14'),
    (N'20260426202247_InitClean',             N'9.0.14');
GO


-- ============================================================
-- 8. SEED DATA - Restaurants
-- ============================================================

SET IDENTITY_INSERT [dbo].[Restaurants] ON;

INSERT INTO [dbo].[Restaurants] ([Id], [Name], [Address], [AudioPath], [Description], [Lat], [Lng], [OpenHours], [Rating], [ImagePath], [Radius], [Priority], [IsActive]) VALUES
    (2,  N'Ốc Oanh',                  N'Vĩnh Khánh, Phường 8, Quận 4', N'', N'Ốc nướng tiêu, ốc hấp sả đặc sản',            10.7608259,          106.7033137,          N'16:00~22:30',  46, N'/uploads/images/558e37c9-dc08-489b-989a-ffa256199459.jpg', 50, 0, 1),
    (4,  N'Chilli Lẩu Nướng Quán',    N'Vĩnh Khánh, Phường 8, Quận 4', N'', N'Lẩu nướng buffet giá sinh viên',               10.760840551806483,  106.704,              N'10:00 - 23:00', 43, N'/uploads/images/277fe139-567b-4a8e-9925-e0cd6a951b69.jpg', 50, 0, 1),
    (6,  N'Ốc Thảo',                  N'Vĩnh Khánh, Phường 8, Quận 4', N'', N'Ốc các loại chế biến đa dạng',                10.761758951046252,  106.702,              N'16:00~22:30',  42, N'/uploads/images/38f07a88-eb7a-4d59-a29f-627555ebce01.jpg', 50, 0, 1),
    (7,  N'Lãng Quán',                N'Vĩnh Khánh, Phường 8, Quận 4', N'', N'Quán ăn phong cách trẻ trung',                 10.761281731910726,  106.705,              N'10:00 - 22:00', 45, N'/uploads/images/c9898e45-080e-414b-a745-be2b39ad1e72.jpg', 50, 0, 1),
    (8,  N'Ớt Xiêm Quán',             N'Vĩnh Khánh, Phường 8, Quận 4', N'', N'Đồ ăn cay nồng đậm đà',                       10.761345472033696,  106.706,              N'11:00 - 21:00', 44, N'/uploads/images/ee5cf5c3-2231-48a3-9c77-454d4d7dce54.jpg', 50, 0, 1),
    (9,  N'Bún Cá Châu Đốc - Dì Tư',  N'Vĩnh Khánh, Phường 8, Quận 4', N'', N'Bún cá Châu Đốc chính gốc',                   10.761060311531145,  106.707,              N'06:00 - 14:00', 46, N'/uploads/images/16a5bd96-2206-4267-a1c2-4cdf8d2cc41c.jpg', 50, 0, 1),
    (10, N'Thế Giới Bò',              N'Vĩnh Khánh, Phường 8, Quận 4', N'', N'Các món bò đa dạng chất lượng',                10.764267370582093,  106.70118183588556,   N'10:00 - 22:00', 45, N'/uploads/images/3229c13e-5e57-426b-8871-08ef1ba9b567.jpg', 50, 0, 1),
    (11, N'Cơm Cháy Kho Quẹt',        N'Vĩnh Khánh, Phường 8, Quận 4', N'', N'Cơm cháy giòn rụm kho quẹt đậm đà',           10.760625291110975,  106.70371667475502,   N'10:00 - 21:00', 44, N'/uploads/images/acfe31ff-67e8-4bba-9b8c-db0146afbba1.jpg', 50, 0, 1),
    (12, N'Bò Lá Lốt Cô Út',          N'Vĩnh Khánh, Phường 8, Quận 4', N'', N'Bò lá lốt nướng thơm ngon',                   10.761278781423528,  106.70529381362458,   N'15:00 - 23:00', 47, N'/uploads/images/9254173c-7898-4b6a-82d6-f8647b25d88a.jpg', 50, 0, 1),
    (13, N'Bún Thịt Nướng Cô Nga',    N'Vĩnh Khánh, Phường 8, Quận 4', N'', N'Bún thịt nướng đặc biệt',                     10.760883450920542,  106.70674182239293,   N'06:00 - 20:00', 47, N'/uploads/images/ad4ea915-0526-4911-a632-1666f3e0a419.jpg', 50, 0, 1),
    (14, N'Ốc Sáu Nở',                N'Vĩnh Khánh, Phường 8, Quận 4', N'', N'Ốc tươi ngon đa dạng',                        10.761090779311022,  106.70289908345818,   N'15:00~23:00',  45, N'/uploads/images/176f16f6-7eff-4506-b0a8-f9a471fad949.jpg', 50, 0, 1);

SET IDENTITY_INSERT [dbo].[Restaurants] OFF;
GO


-- ============================================================
-- 9. SEED DATA - Tours
-- ============================================================

SET IDENTITY_INSERT [dbo].[Tours] ON;

INSERT INTO [dbo].[Tours] ([Id], [Title], [Description], [CreatedAt], [TotalEstimatedTime]) VALUES
    (5, N'Tour 1', N'Trãi nghiệm Vĩnh Khánh tour', CAST(N'2026-04-19T23:05:06.1334949' AS DateTime2), N'60-90 phút');

SET IDENTITY_INSERT [dbo].[Tours] OFF;
GO


-- ============================================================
-- 10. SEED DATA - Tour Details
-- ============================================================

INSERT INTO [dbo].[TourDetails] ([TourId], [RestaurantId], [OrderIndex]) VALUES
    (5, 12, 1),
    (5, 11, 2),
    (5,  2, 3),
    (5, 10, 4);
GO


-- ============================================================
-- 11. SEED DATA - Users
-- ============================================================

SET IDENTITY_INSERT [dbo].[Users] ON;

INSERT INTO [dbo].[Users] ([Id], [Username], [Password], [Role], [RestaurantId], [Email], [IsLocked], [LastActiveAt], [CreatedAt]) VALUES
    (1, N'admin', N'123', NULL, NULL, NULL, 0,
     CAST(N'2026-04-29T07:02:54.360' AS DateTime),
     CAST(N'2026-04-20T00:51:38.927' AS DateTime));

SET IDENTITY_INSERT [dbo].[Users] OFF;
GO


-- ============================================================
-- 12. SEED DATA - Audios
-- ============================================================

SET IDENTITY_INSERT [dbo].[Audios] ON;

-- Restaurant 14: Ốc Sáu Nở
INSERT INTO [dbo].[Audios] ([Id],[VoiceName],[Title],[UploadedAt],[FileName],[FilePath],[RestaurantId],[TextContent],[IsGeneratedByTTS],[LanguageCode],[Duration],[CooldownMinutes]) VALUES
(4, N'System Default', N'Ốc Sáu Nở', CAST(N'2026-04-15T01:18:04.0809921' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 14,
 N'Ốc Sáu Nở là điểm dừng chân cho những ai yêu thích sự đa dạng. Menu ở đây phong phú với đủ loại ốc tươi ngon được nhập mới mỗi ngày. Dù bạn thích xào tỏi, rang muối hay nướng mỡ hành, tay nghề chế biến đậm đà của quán chắc chắn sẽ khiến bạn hài lòng. Quán mở cửa từ 3 giờ chiều, rất hợp để bắt đầu cuộc vui sớm.',
 1, N'vi-VN', 0, 10),
(5, N'System Default', N'Ốc Sáu Nở', CAST(N'2026-04-15T01:23:31.9629616' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 14,
 N'Ốc Sáu Nở is the stop for those who love variety. The menu is rich with all kinds of fresh snails imported daily. Whether you like garlic sautéed, salt-toasted, or grilled with scallion oil, the restaurant''s flavorful cooking will surely satisfy you. Opening from 3 PM, it''s perfect for starting your fun early.',
 1, N'en-US', 0, 10),
(6, N'System Default', N'Ốc Sáu Nở', CAST(N'2026-04-15T01:34:30.9752103' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 14,
 N'Ốc Sáu Nở 是喜愛多樣化口味遊客的停靠站。這裡的菜單非常豐富，每天都有新鮮運達的各種螺類。無論您喜歡蒜炒、鹽焗還是蔥油烤，餐廳濃郁的烹飪手藝一定會讓您滿意。下午 3 點開始營業，非常適合早點開始您的美食之旅。',
 1, N'zh-CN', 0, 10),
(7, N'System Default', N'Ốc Sáu Nở', CAST(N'2026-04-15T01:35:22.2050247' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 14,
 N'옥 사우 너(Ốc Sáu Nở)는 다양성을 좋아하는 분들을 위한 최적의 장소입니다. 매일 들어오는 신선한 소라들로 메뉴가 아주 풍성합니다. 마늘 볶음, 소금 구이, 파기름 구이 등 사장님의 손맛이 깃든 진한 요리들은 여러분을 만족시키기에 충분합니다. 오후 3시부터 영업하여 일찍 모임을 시작하기에 좋습니다.',
 1, N'ko-KR', 0, 10),
(8, N'System Default', N'Ốc Sáu Nở', CAST(N'2026-04-15T01:36:08.1957503' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 14,
 N'Ốc Sáu Nở（オック・サウ・ノー）は、バラエティを楽しみたい方にぴったりです。毎日仕入れる新鮮な貝メニューが豊富に揃っています。ニンニク炒め、塩焼き、ネギ油焼きなど、コクのある味付けはどれも絶品です。午後3時から営業しているので、早めの宴会にも最適です。',
 1, N'ja-JP', 0, 10);

-- Restaurant 4: Chilli Lẩu Nướng
INSERT INTO [dbo].[Audios] ([Id],[VoiceName],[Title],[UploadedAt],[FileName],[FilePath],[RestaurantId],[TextContent],[IsGeneratedByTTS],[LanguageCode],[Duration],[CooldownMinutes]) VALUES
(9,  N'System Default', N'Chilli Lẩu Nướng', CAST(N'2026-04-15T01:37:07.4352840' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 4,
 N'Bạn đang tìm một không gian buffet trẻ trung với mức giá sinh viên? Chilli Lẩu Nướng chính là câu trả lời. Thực đơn kết hợp linh hoạt giữa các món lẩu nóng hổi và đồ nướng tẩm ướp cay tê, đúng như cái tên ''Chilli''. Đây là nơi tuyệt vời để tổ chức tiệc tùng hoặc tụ tập nhóm bạn đông người.',
 1, N'vi-VN', 0, 10),
(10, N'System Default', N'Chilli Lẩu Nướng', CAST(N'2026-04-15T01:37:51.3063450' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 4,
 N'Looking for a youthful buffet space with student prices? Chilli Lẩu Nướng is the answer. The menu flexibly combines hotpot and spicy grilled dishes, just like the name ''Chilli''. This is a great place for parties or large group gatherings.',
 1, N'en-US', 0, 10),
(11, N'System Default', N'Chilli Lẩu Nướng', CAST(N'2026-04-15T01:38:19.9975090' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 4,
 N'您正在尋找一個價格親民、風格年輕的自助餐空間嗎？Chilli Lẩu Nướng 就是您的答案。菜單靈活結合了熱氣騰騰的火鍋和辛辣入味的烤肉，正如其名『Chilli』。這裡是舉辦派對或多人聚會的絕佳場所。',
 1, N'zh-CN', 0, 10),
(12, N'System Default', N'Chilli Lẩu Nướng', CAST(N'2026-04-15T01:38:54.0849615' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 4,
 N'学生価格で楽しめる若々しいビュッフェをお探しなら、Chilli（チリ）が答えです。店名の通り、ピリ辛の焼き物と熱々の鍋を自由に組み合わせられます。パーティーや大人数での集まりに最適な、活気あふれるスポットです。',
 1, N'ja-JP', 0, 10),
(13, N'System Default', N'Chilli Lẩu Nướng', CAST(N'2026-04-15T01:39:31.7363630' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 4,
 N'학생 가격으로 즐길 수 있는 젊은 감각의 뷔페를 찾으시나요? 칠리(Chilli) 라우느엉이 정답입니다. 이름처럼 매콤하게 양념된 구이와 뜨끈한 전골이 조화를 이루는 메뉴 구성을 자랑합니다. 파티나 단체 모임을 갖기에 아주 좋은 곳입니다.',
 1, N'ko-KR', 0, 10);

-- Restaurant 7: Lãng Quán
INSERT INTO [dbo].[Audios] ([Id],[VoiceName],[Title],[UploadedAt],[FileName],[FilePath],[RestaurantId],[TextContent],[IsGeneratedByTTS],[LanguageCode],[Duration],[CooldownMinutes]) VALUES
(14, N'System Default', N'Lãng Quán', CAST(N'2026-04-15T01:40:58.3177421' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 7,
 N'Lãng Quán mang đến một làn gió mới cho phố ẩm thực Vĩnh Khánh với phong cách trẻ trung và năng động. Menu đa dạng với các món ăn bắt trend, phù hợp cho những buổi tối trò chuyện cùng bạn bè. Nếu bạn muốn tìm một nơi vừa có đồ ăn ngon, vừa có không khí ''chill'' thì đừng bỏ qua địa chỉ này nhé.',
 1, N'vi-VN', 0, 10),
(15, N'System Default', N'Lãng Quán', CAST(N'2026-04-15T01:41:27.4405211' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 7,
 N'Lãng Quán brings a new breeze to Vinh Khanh food street with its youthful and dynamic style. The diverse menu with trending dishes is suitable for evening chats with friends. If you want a place with good food and a ''chill'' atmosphere, don''t miss this address.',
 1, N'en-US', 0, 10),
(16, N'System Default', N'Lãng Quán', CAST(N'2026-04-15T01:41:48.1412263' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 7,
 N'Lãng Quán 以其年輕活力的風格為永慶美食街帶來了一股新風。多樣化的菜單搭配潮流菜餚，非常適合與朋友在夜晚聊天。如果您想找一個既有美食又有『Chill』氛圍的地方，千萬不要錯過這裡。',
 1, N'zh-CN', 0, 10),
(17, N'System Default', N'Lãng Quán', CAST(N'2026-04-15T01:42:26.2635867' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 7,
 N'랑 관(Lãng Quán)은 젊고 역동적인 스타일로 빈칸 음식 거리에 새로운 바람을 일으키고 있습니다. 트렌디한 메뉴가 가득해 친구들과 저녁 담소를 나누기에 제격입니다. 맛있는 음식과 ''Chill''한 분위기를 모두 원하신다면 이곳을 놓치지 마세요.',
 1, N'ko-KR', 0, 10),
(18, N'System Default', N'Lãng Quán', CAST(N'2026-04-15T01:42:54.8830297' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 7,
 N'Lãng Quán（ラン・クアン）は、若々しくダイナミックなスタイルでビンカン通りに新しい風を吹き込んでいます。トレンドを押さえた多彩なメニューは、夜の女子会や友人との語らいに最適です。美味しい料理と「チル」な雰囲気を求めるなら、ここをチェックしてください。',
 1, N'ja-JP', 0, 10);

-- Restaurant 8: Ớt Xiêm Quán
INSERT INTO [dbo].[Audios] ([Id],[VoiceName],[Title],[UploadedAt],[FileName],[FilePath],[RestaurantId],[TextContent],[IsGeneratedByTTS],[LanguageCode],[Duration],[CooldownMinutes]) VALUES
(19, N'System Default', N'Ớt Xiêm Quán', CAST(N'2026-04-15T01:45:13.3553132' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 8,
 N'Dành riêng cho những ai có ''gu'' ăn cay, Ớt Xiêm Quán sẽ đánh thức vị giác của bạn bằng những món ăn đậm đà và cay nồng. Sự kết hợp tinh tế giữa nguyên liệu tươi và vị cay đặc trưng của ớt xiêm xanh tạo nên dấu ấn riêng biệt, khiến mỗi bữa ăn trở thành một cuộc phiêu lưu hương vị đầy thú vị.',
 1, N'vi-VN', 0, 10),
(20, N'System Default', N'Ớt Xiêm Quán', CAST(N'2026-04-15T01:45:40.1663260' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 8,
 N'Dedicated to those with a ''spicy palate'', Ớt Xiêm Quán will awaken your taste buds with rich and pungent dishes. The delicate combination of fresh ingredients and the characteristic heat of green chili creates a unique mark, making every meal a flavorful adventure.',
 1, N'en-US', 0, 10),
(21, N'System Default', N'Ớt Xiêm Quán', CAST(N'2026-04-15T01:46:05.4678686' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 8,
 N'為『嗜辣族』打造，Ớt Xiêm Quán 將用濃郁辛辣的菜餚喚醒您的味蕾。新鮮食材與綠辣椒特有辣味的精緻結合營造出獨特的印記，讓每一餐都成為一場風味冒險。',
 1, N'zh-CN', 0, 10),
(22, N'System Default', N'Ớt Xiêm Quán', CAST(N'2026-04-15T01:46:30.2071223' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 8,
 N'매운맛을 즐기는 분들을 위해 얻 씨엠 관(Ớt Xiêm Quán)은 진하고 알싸한 요리로 여러분의 미각을 깨워줍니다. 신선한 재료와 녹색 땡초의 정교한 조합은 독보적인 풍미를 만들어내어, 매 끼니를 즐거운 모험으로 만들어줍니다.',
 1, N'ko-KR', 0, 10),
(23, N'System Default', N'Ớt Xiêm Quán', CAST(N'2026-04-15T01:47:43.0229591' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 8,
 N'激辛派の方には、Ớt Xiêm Quán（オット・シエム・クアン）がおすすめ。コク深くパンチの効いた料理が五感を刺激します。新鮮な素材と青唐辛子の絶妙なハーモニーが、一食一食を刺激的な味の冒険に変えてくれます。',
 1, N'ja-JP', 0, 10);

-- Restaurant 9: Bún Cá Châu Đốc - Dì Tư
INSERT INTO [dbo].[Audios] ([Id],[VoiceName],[Title],[UploadedAt],[FileName],[FilePath],[RestaurantId],[TextContent],[IsGeneratedByTTS],[LanguageCode],[Duration],[CooldownMinutes]) VALUES
(24, N'System Default', N'Bún Cá Châu Đốc - Dì Tư', CAST(N'2026-04-15T01:49:23.5352785' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 9,
 N'Bắt đầu buổi sáng tại Quận 4 với tô Bún Cá Châu Đốc của Dì Tư. Chỉ mở cửa từ 6 giờ sáng đến 2 giờ chiều, quán mang đúng vị miền Tây sông nước về giữa lòng Sài Gòn. Nước dùng vàng óng ánh từ nghệ, vị ngọt của cá lóc tươi và mùi thơm đặc trưng của ngải bún sẽ khiến bạn nhớ mãi không thôi.',
 1, N'vi-VN', 0, 10),
(25, N'System Default', N'Bún Cá Châu Đốc - Dì Tư', CAST(N'2026-04-15T01:49:44.8227296' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 9,
 N'4区の朝は、Dì Tư（ジー・トゥー）のチャウドック魚だし麺で始めましょう。朝6時から午後2時までの限定営業で、メコンデルタの本場の味をサイゴンに伝えています。ウコンの黄金色のスープと新鮮なライギョ、独特のハーブの香りは一度食べたら忘れられません。',
 1, N'ja-JP', 0, 10),
(26, N'System Default', N'Bún Cá Châu Đốc - Dì Tư', CAST(N'2026-04-15T01:50:06.1826586' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 9,
 N'4군에서의 아침은 디 뜨(Dì Tư)의 짜우독 생선 국수 한 그릇으로 시작해보세요. 오전 6시부터 오후 2시까지만 운영하는 이곳은 사이공 한복판에서 메콩강의 참맛을 전합니다. 강황의 황금빛 국물과 신선한 가물치, 그리고 ''응아이 분'' 특유의 향기는 잊지 못할 추억을 선사할 것입니다.',
 1, N'ko-KR', 0, 10),
(27, N'System Default', N'Bún Cá Châu Đốc - Dì Tư', CAST(N'2026-04-15T01:50:20.9979256' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 9,
 N'在第四郡的早晨，從 Dì Tư 的朱篤魚湯粉開始。這家店僅從早上 6 點營業到下午 2 點，將正宗的湄公河水鄉風味帶到了西貢市中心。薑黃色的金黃湯底、新鮮的雷魚和「凹唇薑」的獨特香氣會讓您流連忘返。',
 1, N'zh-CN', 0, 10),
(28, N'System Default', N'Bún Cá Châu Đốc - Dì Tư', CAST(N'2026-04-15T01:50:39.7547148' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 9,
 N'Start your morning in District 4 with a bowl of Dì Tư''s Chau Doc Fish Noodle Soup. Open only from 6 AM to 2 PM, the shop brings the true taste of the Mekong Delta to the heart of Saigon. The golden turmeric broth, fresh snakehead fish, and the signature aroma of fingerroot will be unforgettable.',
 1, N'en-US', 0, 10);

-- Restaurant 10: Thế Giới Bò
INSERT INTO [dbo].[Audios] ([Id],[VoiceName],[Title],[UploadedAt],[FileName],[FilePath],[RestaurantId],[TextContent],[IsGeneratedByTTS],[LanguageCode],[Duration],[CooldownMinutes]) VALUES
(29, N'System Default', N'Thế Giới Bò', CAST(N'2026-04-15T01:52:11.3777583' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 10,
 N'Đúng như tên gọi, Thế Giới Bò là thiên đường cho các tín đồ thịt đỏ. Tại đây, bạn có thể tìm thấy đủ mọi phong cách chế biến từ bò né, bò sốt hàu đến các món lẩu bò đậm vị. Nguyên liệu chất lượng và cách trình bày bắt mắt giúp quán luôn giữ vững phong độ với điểm đánh giá 4.5.',
 1, N'vi-VN', 0, 10),
(30, N'System Default', N'Thế Giới Bò', CAST(N'2026-04-15T01:52:23.9356156' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 10,
 N'名前の通り、Thế Giới Bò（テー・ゾイ・ボー）は牛肉好きの天国です。ボーネーから、オイスターソース炒め、濃厚な牛鍋まで、あらゆるスタイルが楽しめます。質の高い素材と美しい盛り付けで、常に4.5の高評価を維持しています。',
 1, N'ja-JP', 0, 10),
(31, N'System Default', N'Thế Giới Bò', CAST(N'2026-04-15T01:53:26.3953444' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 10,
 N'이름 그대로 테 저이 보(Thế Giới Bò)는 소고기 마니아들의 천국입니다. 보네(소고기 스테이크), 소고기 굴소스 볶음부터 진한 소고기 전골까지 모든 스타일을 만나보실 수 있습니다. 좋은 식재료와 보기 좋은 상차림으로 4.5점의 높은 평점을 유지하고 있습니다.',
 1, N'ko-KR', 0, 10),
(32, N'System Default', N'Thế Giới Bò', CAST(N'2026-04-15T01:52:52.7780047' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 10,
 N'名副其實，Thế Giới Bò 是紅肉愛好者的天堂。在這裡，您可以找到從鐵板牛肉、蠔油牛肉到濃郁牛肉火鍋的各種烹飪風格。優質的食材和精美的擺盤使餐廳始終保持 4.5 分的高評價。',
 1, N'zh-CN', 0, 10),
(33, N'System Default', N'Thế Giới Bò', CAST(N'2026-04-15T01:53:06.5818658' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 10,
 N'True to its name, Thế Giới Bò is a heaven for red meat lovers. Here, you can find all kinds of styles from sizzling beef, beef with oyster sauce to rich beef hotpots. Quality ingredients and eye-catching presentation help the restaurant maintain its form with a 4.5 rating.',
 1, N'en-US', 0, 10);

-- Restaurant 11: Cơm Cháy Kho Quẹt
INSERT INTO [dbo].[Audios] ([Id],[VoiceName],[Title],[UploadedAt],[FileName],[FilePath],[RestaurantId],[TextContent],[IsGeneratedByTTS],[LanguageCode],[Duration],[CooldownMinutes]) VALUES
(34, N'System Default', N'Cơm Cháy Kho Quẹt', CAST(N'2026-04-15T01:54:32.4939875' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 11,
 N'Đôi khi sự đơn giản lại tạo nên sức hút mãnh liệt nhất, và Cơm Cháy Kho Quẹt tại Vĩnh Khánh là minh chứng cho điều đó. Miếng cơm cháy vàng ươm, giòn rụm chấm cùng nước kho quẹt đặc quánh, đủ vị mặn ngọt và thơm mùi tóp mỡ. Đây là món ăn chơi nhưng lại gây nghiện cực kỳ cho bất kỳ ai nếm thử.',
 1, N'vi-VN', 0, 10),
(35, N'System Default', N'Cơm Cháy Kho Quẹt', CAST(N'2026-04-15T01:54:50.2369156' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 11,
 N'Sometimes simplicity creates the strongest attraction, and Cơm Cháy Kho Quẹt in Vinh Khanh is proof. The crispy, golden scorched rice dipped in thick, savory-sweet caramelized sauce with the aroma of pork fat is an addictive snack for anyone who tries it.',
 1, N'en-US', 0, 10),
(36, N'System Default', N'Cơm Cháy Kho Quẹt', CAST(N'2026-04-15T01:55:07.7851675' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 11,
 N'有時簡單反而能產生最強的吸引力，永慶街的 Cơm Cháy Kho Quẹt 就是證明。金黃酥脆的鍋巴蘸上濃稠、鹹甜適中且充滿豬油渣香氣的魚露。這是一道讓任何嘗試過的人都會上癮的小吃。',
 1, N'zh-CN', 0, 10),
(37, N'System Default', N'Cơm Cháy Kho Quẹt', CAST(N'2026-04-15T01:55:30.6941579' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 11,
 N'때로는 단순함이 가장 강력한 매력을 발산합니다. 빈칸의 껌짜이 코꿸이 그 증거입니다. 노릇하고 바삭한 누룽지를 짭조름하고 달콤하며 돼지비계의 고소함이 살아있는 코꿸 소스에 찍어 먹는 이 맛은 한 번 먹으면 멈출 수 없는 중독성을 자랑합니다.',
 1, N'ko-KR', 0, 10),
(38, N'System Default', N'Cơm Cháy Kho Quẹt', CAST(N'2026-04-15T01:55:50.3076840' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 11,
 N'シンプルさが最大の魅力になることもあります。ビンカン通りのコムチャイ・コークエットがその証拠です。カリカリに焼かれたおこげを、豚脂の香ばしさと甘辛さが凝縮された濃厚なタレに浸して食べる。一度食べたら止まらない、中毒性抜群の逸品です。',
 1, N'ja-JP', 0, 10);

-- Restaurant 12: Bò Lá Lốt Cô Út
INSERT INTO [dbo].[Audios] ([Id],[VoiceName],[Title],[UploadedAt],[FileName],[FilePath],[RestaurantId],[TextContent],[IsGeneratedByTTS],[LanguageCode],[Duration],[CooldownMinutes]) VALUES
(39, N'System Default', N'Bò Lá Lốt Cô Út', CAST(N'2026-04-15T01:57:07.8967698' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 12,
 N'驚異の4.7評価を誇るCô Út（コー・ウト）は、ビンカン通りのスター的な存在です。丁寧に包まれた牛肉を、ジューシーさを残しつつラロット葉の香りが引き立つよう絶妙に焼き上げます。ライスペーパーと野草で巻き、濃厚なマムネムに浸して食べる体験は、まさに感動の味です。',
 1, N'ja-JP', 0, 10),
(40, N'System Default', N'Bò Lá Lốt Cô Út', CAST(N'2026-04-15T01:57:27.2380602' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 12,
 N'.7점이라는 놀라운 점수를 받은 꼬 웃(Cô Út) 보라롯은 빈칸 거리의 빛나는 명소입니다. 육즙을 머금도록 정성껏 말아 구운 소고기는 라롯 잎 특유의 향이 가득합니다. 라이스페이퍼, 야생 채소와 함께 진한 맘넴 소스에 찍어 먹는 이 경험은 진정한 미식의 발견입니다.',
 1, N'ko-KR', 0, 10),
(41, N'System Default', N'Bò Lá Lốt Cô Út', CAST(N'2026-04-15T01:57:54.3010849' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 12,
 N'憑藉 4.7 的傲人評分，Bò Lá Lốt Cô Út 是永慶街上的一顆明珠。每一捲牛肉都經過精心包裹，烤得恰到好處以保持多汁，並散發出檳榔葉特有的香氣。搭配蘸上濃郁蝦醬的春捲、野菜和米紙，這確實是一次難忘的味覺體驗。',
 1, N'zh-CN', 0, 10),
(42, N'System Default', N'Bò Lá Lốt Cô Út', CAST(N'2026-04-15T01:58:12.0415157' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 12,
 N'With an impressive 4.7 score, Bò Lá Lốt Cô Út is a bright star on Vinh Khanh Street. Each beef roll is carefully wrapped and grilled just right to keep it juicy with the characteristic aroma of lolot leaves. Rolled with rice paper, wild herbs, and dipped in bold fermented fish sauce, this is truly an unforgettable culinary experience.',
 1, N'en-US', 0, 10),
(43, N'System Default', N'Bò Lá Lốt Cô Út', CAST(N'2026-04-15T01:58:46.2246995' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 12,
 N'Với số điểm ấn tượng 4.7, Bò Lá Lốt Cô Út là ngôi sao sáng trên phố Vĩnh Khánh. Những cuốn bò được gói ghém cẩn thận, nướng vừa chín tới để giữ độ mọng nước và hương thơm đặc trưng của lá lốt. Cuốn cùng bánh tráng, rau rừng và chấm mắm nêm đậm đà, đây thực sự là một trải nghiệm ẩm thực khó quên.',
 1, N'vi-VN', 0, 10);

-- Restaurant 13: Bún Thịt Nướng Cô Nga
INSERT INTO [dbo].[Audios] ([Id],[VoiceName],[Title],[UploadedAt],[FileName],[FilePath],[RestaurantId],[TextContent],[IsGeneratedByTTS],[LanguageCode],[Duration],[CooldownMinutes]) VALUES
(44, N'System Default', N'Bún Thịt Nướng Cô Nga', CAST(N'2026-04-15T01:59:55.1929651' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 13,
 N'Một địa chỉ ăn sáng và ăn trưa quen thuộc của người dân Vĩnh Khánh chính là Bún Thịt Nướng Cô Nga. Thịt được ướp đậm đà, nướng thơm phức trên than hồng kết hợp cùng chả giò giòn rụm và nước mắm chua ngọt đặc trưng. Một tô đầy đặn tại đây chắc chắn sẽ tiếp đủ năng lượng cho bạn cả ngày dài.',
 1, N'vi-VN', 0, 10),
(45, N'System Default', N'Bún Thịt Nướng Cô Nga', CAST(N'2026-04-15T02:00:12.5488340' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 13,
 N'A familiar breakfast and lunch spot for Vinh Khanh residents is Bún Thịt Nướng Cô Nga. The meat is richly marinated and grilled over charcoal, combined with crispy spring rolls and signature sweet and sour fish sauce. A full bowl here will surely give you enough energy for the whole day.',
 1, N'en-US', 0, 10),
(46, N'System Default', N'Bún Thịt Nướng Cô Nga', CAST(N'2026-04-15T02:00:35.2672169' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 13,
 N'永慶街居民熟悉的早午餐地址就是 Bún Thịt Nướng Cô Nga。豬肉醃製入味，在炭火上烤得香氣四溢，搭配酥脆的春捲和特製的酸甜魚露。這裡的一大碗粉絕對能為您提供一整天的能量。',
 1, N'zh-CN', 0, 10),
(47, N'System Default', N'Bún Thịt Nướng Cô Nga', CAST(N'2026-04-15T02:00:48.9636402' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 13,
 N'ビンカン通りの住民に愛される朝食・昼食スポットといえば、Cô Nga（コー・ガ）のブンティットヌォンです。炭火で香ばしく焼かれた秘伝のタレ漬け肉に、サクサクの春巻きと甘酸っぱいヌクマムが絶妙にマッチします。ボリューム満点の一杯で、一日の活力をチャージしてください。',
 1, N'ja-JP', 0, 10),
(48, N'System Default', N'Bún Thịt Nướng Cô Nga', CAST(N'2026-04-15T02:01:05.5143557' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 13,
 N'빈칸 거리 주민들의 단골 아침 및 점심 식사처는 바로 꼬 응아(Cô Nga) 분팃느엉입니다. 숯불에 향긋하게 구워진 양념 돼지고기와 바삭한 짜조, 그리고 새콤달콤한 느옥맘 소스의 조화가 일품입니다. 이곳의 푸짐한 한 그릇은 하루 종일 든든한 에너지를 채워줄 것입니다.',
 1, N'ko-KR', 0, 10);

-- Restaurant 2: Ốc Oanh
INSERT INTO [dbo].[Audios] ([Id],[VoiceName],[Title],[UploadedAt],[FileName],[FilePath],[RestaurantId],[TextContent],[IsGeneratedByTTS],[LanguageCode],[Duration],[CooldownMinutes]) VALUES
(49, N'System Default', N'Ốc Oanh', CAST(N'2026-04-15T02:07:06.5759538' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 2,
 N'Nếu là tín đồ của hải sản Quận 4, bạn không thể bỏ qua Ốc Oanh. Nổi tiếng nhất ở đây chính là món ốc nướng tiêu cay nồng và ốc hấp sả thơm lừng, giữ trọn vị ngọt tự nhiên. Với không gian mở đặc trưng và điểm đánh giá cực cao 4.6, đây là điểm hẹn lý tưởng để tận hưởng không khí nhộn nhịp của ''phố không ngủ'' Vĩnh Khánh về đêm.',
 1, N'vi-VN', 0, 10),
(50, N'System Default', N'Ốc Oanh', CAST(N'2026-04-15T02:07:22.4148987' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 2,
 N'If you are a seafood lover in District 4, you cannot miss Ốc Oanh. Most famous here are the spicy pepper-grilled snails and fragrant lemongrass steamed snails, preserving their natural sweetness. With its signature open space and a high 4.6 rating, this is the ideal spot to enjoy the bustling atmosphere of Vinh Khanh ''sleepless street'' at night.',
 1, N'en-US', 0, 10),
(51, N'System Default', N'Ốc Oanh', CAST(N'2026-04-15T02:07:38.4195803' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 2,
 N'如果您是第四郡的海鮮愛好者，絕對不能錯過 Ốc Oanh。這裡最著名的是辛辣的黑胡椒烤螺和清香的香茅蒸螺，保留了海鮮最自然的鮮甜。憑藉其標誌性的開放式空間和 4.6 的高分，這裡是感受永慶『不夜街』熱鬧氛圍的理想聚會點。',
 1, N'zh-CN', 0, 10),
(52, N'System Default', N'Ốc Oanh', CAST(N'2026-04-15T02:07:52.8833826' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 2,
 N'4군 해산물 마니아라면 옥 오안(Ốc Oanh)을 빼놓을 수 없습니다. 이곳에서 가장 유명한 메뉴는 매콤한 후추 소라 구이와 자연의 단맛을 그대로 간직한 레몬그라스 소라 찜입니다. 탁 트인 공간과 4.6점의 높은 평점을 자랑하는 이곳은 밤의 빈칸 ''잠들지 않는 거리''의 활기찬 분위기를 즐기기에 이상적인 장소입니다.',
 1, N'ko-KR', 0, 10),
(53, N'System Default', N'Ốc Oanh', CAST(N'2026-04-15T02:08:04.3101672' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 2,
 N'4区のシーフード好きなら、Ốc Oanh（オック・オアン）は外せません。一番の人気は、ピリッと辛い胡椒焼きと、素材の甘みを活かしたレモングラス蒸しです。開放感のある空間と4.6という高評価を誇るこの店は、夜のビンカン通りの活気を楽しむのに最高の場所です。',
 1, N'ja-JP', 0, 10);

-- Restaurant 6: Ốc Thảo
INSERT INTO [dbo].[Audios] ([Id],[VoiceName],[Title],[UploadedAt],[FileName],[FilePath],[RestaurantId],[TextContent],[IsGeneratedByTTS],[LanguageCode],[Duration],[CooldownMinutes]) VALUES
(54, N'System Default', N'Ốc Thảo', CAST(N'2026-04-15T02:09:04.0339633' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 6,
 N'こだわり派のグルメには、調理法が非常に多彩なỐc Thảo（オック・タオ）がおすすめです。定番からユニークな味付けまで、一皿ごとに異なる個性が光ります。大衆的で親しみやすい雰囲気も魅力で、友人と夜通し語り合うのにぴったりです。',
 1, N'ja-JP', 0, 10),
(55, N'System Default', N'Ốc Thảo', CAST(N'2026-04-15T02:09:17.9279280' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 6,
 N'섬세한 미식가들을 위해 옥 타오(Ốc Thảo)는 매우 다양한 조리 방식의 소라 메뉴를 선보입니다. 익숙한 요리부터 독특한 양념까지, 접시마다 특별한 맛을 담고 있습니다. 서민적이고 친근한 공간은 친구들과 밤새도록 술잔을 기울이기에 더할 나위 없는 장점입니다.',
 1, N'ko-KR', 0, 10),
(56, N'System Default', N'Ốc Thảo', CAST(N'2026-04-15T02:09:31.0758028' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 6,
 N'對於追求烹飪細緻入微的食客來說，Ốc Thảo 提供了風格極其多樣的螺肉菜單。從熟悉的菜餚到獨特的調味，每一盤螺都帶有獨特的風味。平民化且親切的空間是加分項，讓您和朋友可以整夜暢談品嚐。',
 1, N'zh-CN', 0, 10),
(57, N'System Default', N'Ốc Thảo', CAST(N'2026-04-15T02:09:45.6119679' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 6,
 N'For foodies who love meticulousness, Ốc Thảo offers a snail menu with extremely diverse cooking styles. From familiar dishes to unique seasoning, each plate of snails carries a distinct flavor. The casual, close-to-home space is a plus for lingering with friends throughout the night.',
 1, N'en-US', 0, 10),
(58, N'System Default', N'Ốc Thảo', CAST(N'2026-04-15T02:10:00.5968157' AS DateTime2), N'OFFLINE_SCRIPT', N'OFFLINE_MODE', 6,
 N'Dành cho những tâm hồn ăn uống yêu thích sự tỉ mỉ, Ốc Thảo mang đến thực đơn ốc với phong cách chế biến cực kỳ đa dạng. Từ những món quen thuộc đến cách gia giảm gia vị độc đáo, mỗi đĩa ốc đều mang một hương vị riêng biệt. Không gian bình dân, gần gũi chính là điểm cộng giúp bạn cùng bạn bè lai rai xuyên màn đêm.',
 1, N'vi-VN', 0, 10);

SET IDENTITY_INSERT [dbo].[Audios] OFF;
GO
