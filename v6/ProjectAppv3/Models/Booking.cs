using SQLite;

namespace ProjectApp.Models
{
    /// <summary>
    /// Lưu trữ đặt chỗ local trên thiết bị (SQLite).
    /// Đồng bộ lên server khi có mạng.
    /// </summary>
    [Table("bookings")]
    public class Booking
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int RestaurantId { get; set; }
        public string RestaurantName { get; set; } = string.Empty;

        // Thông tin đặt chỗ
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public int GuestCount { get; set; } = 2;
        public string BookingDate { get; set; } = string.Empty; // "dd/MM/yyyy"
        public string BookingTime { get; set; } = string.Empty; // "HH:mm"
        public string Note { get; set; } = string.Empty;

        // Thanh toán mô phỏng
        public string PaymentMethod { get; set; } = "cash";    // cash | vnpay | momo | zalopay
        public string PaymentStatus { get; set; } = "pending"; // pending | awaiting_payment | paid | cancelled
        public double DepositAmount { get; set; } = 0;

        // Trạng thái đặt chỗ
        public string Status { get; set; } = "confirmed"; // confirmed | cancelled | completed

        // Mã đặt chỗ hiển thị cho user
        public string BookingCode { get; set; } = string.Empty;

        public string SyncStatus { get; set; } = "pending"; // pending | synced

        public long CreatedAtTicks { get; set; } = DateTime.Now.Ticks;

        [Ignore]
        public DateTime CreatedAt
        {
            get => new DateTime(CreatedAtTicks, DateTimeKind.Local);
            set => CreatedAtTicks = value.Ticks;
        }

        [Ignore]
        public string StatusDisplay => Status switch
        {
            "confirmed" => "✅ Đã xác nhận",
            "cancelled"  => "❌ Đã hủy",
            "completed"  => "🎉 Hoàn thành",
            _            => "⏳ Chờ xác nhận"
        };

        [Ignore]
        public string PaymentDisplay => PaymentMethod switch
        {
            "vnpay"   => "💳 VNPay",
            "momo"    => "🟣 MoMo",
            "zalopay" => "🔵 ZaloPay",
            _         => "💵 Tiền mặt tại quán"
        };
    }
}
