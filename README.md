# C#_Project 
Product Requirements Document
# RPD – ĐỒ ÁN: Vĩnh Khánh Tour Guide

## 1. Lý do chọn đề tài
Hiện nay, việc khám phá ẩm thực và các địa điểm ăn uống ở Việt Nam thường gặp nhiều khó khăn: thông tin chưa đồng bộ, thiếu hướng dẫn trực quan và trải nghiệm chưa thống nhất.  

Đồ án **Vĩnh Khánh Tour Guide** nhằm xây dựng một ứng dụng mobile & web giúp người dùng:  

- Khám phá các địa điểm ẩm thực một cách trực quan, dễ dàng.  
- Chuẩn hóa trải nghiệm khám phá bằng **audio guide**, giúp người dùng vừa đi vừa nghe thông tin chi tiết về địa điểm.  
- Cung cấp nền tảng quản lý nội dung cho quản trị viên trên web.  

---

## 2. Mục tiêu của đồ án
- Xây dựng **ứng dụng mobile đa nền tảng** (Android, iOS, Windows) sử dụng **.NET MAUI**.  
- Xây dựng **web portal quản lý nội dung** địa điểm và audio guide bằng **ASP.NET Core MVC**, kết nối **SQL Server**.  
- Tích hợp dữ liệu địa điểm, quán ăn, review và audio guide để người dùng có trải nghiệm trực quan, thuận tiện.  
- Hướng tới việc chuẩn hóa trải nghiệm khám phá ẩm thực qua audio, tăng tính tiện ích và hấp dẫn cho người dùng.  

---

## 3. Phạm vi đồ án
**Người dùng cuối:** người muốn khám phá địa điểm ăn uống, thưởng thức ẩm thực địa phương.  
**Quản trị viên:** quản lý danh sách địa điểm, audio guide, review trên web.  

**Hệ thống bao gồm:**  
- **Ứng dụng mobile:** xem danh sách địa điểm, nghe audio guide, xem đánh giá, tìm kiếm địa điểm.  
- **Web portal:** quản lý dữ liệu, upload audio, duyệt đánh giá, kết nối **SQL Server**.  

**Ngoài phạm vi:** thanh toán trực tuyến, đặt bàn online (có thể mở rộng trong tương lai).  

---

## 4. Yêu cầu chức năng

### 4.1 Ứng dụng Mobile
- Hiển thị danh sách địa điểm ẩm thực, thông tin chi tiết (tên, địa chỉ, giờ mở cửa, hình ảnh).  
- Nghe audio guide mô tả từng địa điểm.  
- Đánh giá và viết review cho địa điểm.  
- Tìm kiếm và lọc địa điểm theo loại ẩm thực, khu vực.  
- Đồng bộ dữ liệu với **SQLite (offline)** và server web (**SQL Server online**).  

### 4.2 Web Portal
- Quản lý danh sách địa điểm, thêm/sửa/xóa thông tin trên **SQL Server**.  
- Upload và quản lý audio guide.  
- Duyệt, xóa, chỉnh sửa review của người dùng.  
- Xem thống kê lượt truy cập, lượt nghe audio guide.  

---

## 5. Yêu cầu phi chức năng
- **Hiệu năng:** Mobile app phản hồi nhanh (<2s khi load danh sách).  
- **Độ ổn định:** Không crash khi offline, hỗ trợ đồng bộ dữ liệu.  
- **Bảo mật:** Quản trị viên đăng nhập bằng tài khoản bảo mật, dữ liệu người dùng được mã hóa.  
- **Giao diện:** Thân thiện, dễ sử dụng, hỗ trợ đa ngôn ngữ (tiếng Việt, tiếng Anh).  

---

## 6. Công nghệ sử dụng

| Hạng mục | Công nghệ / Thư viện |
|----------|--------------------|
| Mobile App | .NET MAUI, C#, SQLite-net, MAUI Essentials |
| Web Portal | ASP.NET Core MVC, C#, SQL Server |
| Audio Guide | MAUI MediaPlayer, file MP3/WAV |
| Database | SQLite (mobile), SQL Server (web) |
| Giao diện | XAML (mobile), Razor Pages (web) |
| Quản lý dữ liệu | EF Core, LINQ |

---

## 7. Thiết kế hệ thống

### 7.1 Kiến trúc hệ thống
- Mobile app: load dữ liệu offline từ SQLite, sync khi online với SQL Server.  
- Web portal: quản trị viên quản lý dữ liệu và audio guide, API cung cấp dữ liệu cho mobile.  

### 7.2 Thiết kế cơ sở dữ liệu (SQL Server, tóm tắt)
| Table | Các trường chính |
|-------|-----------------|
| DiaDiem | Id, Ten, DiaChi, MoTa, LoaiAmThuc, HinhAnh |
| AudioGuide | Id, DiaDiemId, TenFile, DuongDanFile |
| Review | Id, DiaDiemId, NguoiDung, NoiDung, Rating, NgayGio |
| User | Id, TenDangNhap, MatKhau, LoaiTaiKhoan |

> **Lưu ý:** SQL Server sẽ quản lý toàn bộ dữ liệu online, Mobile app dùng SQLite để offline, đồng bộ khi có kết nối mạng.  

### 7.3 Thiết kế giao diện
- **Mobile App:** danh sách địa điểm (`ListView` / `CollectionView`), trang chi tiết địa điểm, audio player, review.  
- **Web Portal:** dashboard quản lý, form thêm/sửa/xóa địa điểm, upload audio, review.  

---

## 8. Kế hoạch thực hiện & tiến độ

| Giai đoạn | Công việc chính | Thời gian dự kiến |
|-----------|----------------|----------------|
| 1 | Phân tích yêu cầu: Thu thập thông tin, xác định chức năng | 1 tuần |
| 2 | Thiết kế hệ thống: Thiết kế cơ sở dữ liệu, giao diện mobile & web | 1 tuần |
| 3 | Phát triển mobile: Code UI, kết nối SQLite, audio guide, review | 2 tuần |
| 4 | Phát triển web: Xây dựng web portal, API, quản lý dữ liệu trên SQL Server | 2 tuần |
| 5 | Kiểm thử & hoàn thiện: Test chức năng, sửa lỗi, tối ưu hiệu năng | 1 tuần |
| 6 | Báo cáo & thuyết trình: Viết báo cáo, RPD, slide thuyết trình | 1 tuần |

**Tổng thời gian:** ~8 tuần

---

**Chú ý khi triển khai SQL Server:**  
- Tạo **Database**: VinhKhanhTourDB  
- Tạo các **table** DiaDiem, AudioGuide, Review, User  
- Mobile app đồng bộ dữ liệu thông qua Web API (RESTful) với SQL Server.  
- Dùng **Entity Framework Core** để quản lý dữ liệu trên Web Portal.  
