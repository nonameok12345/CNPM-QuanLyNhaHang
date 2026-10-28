namespace test.Models;

// Số liệu nhanh hiển thị trên trang quản trị
public class TrangQuanTriViewModel
{
    public int SoHoaDonChuaThanhToan { get; set; }
    public int SoDatBanChoXacNhan { get; set; }
    public int SoBanDangDung { get; set; }
    public int SoBanDangHoatDong { get; set; }
    public int SoLoCanhBao { get; set; }          // lô kho sắp hết hạn hoặc đã hết hạn
    public decimal? ThuHomNay { get; set; }       // chỉ quản lý được xem
}
