namespace test.Models;

// Khớp với bảng DatBan trong database
public class DatBan
{
    public int MaDatBan { get; set; }
    public int MaKH { get; set; }
    public int MaBan { get; set; }
    public DateTime NgayGio { get; set; }
    public int SoNguoi { get; set; }
    public string TrangThai { get; set; } = TrangThaiDatBan.ChoXacNhan;

    public Ban Ban { get; set; } = null!;
}

public static class TrangThaiDatBan
{
    public const string ChoXacNhan = "Chờ xác nhận";
    public const string DaXacNhan = "Đã xác nhận";
    public const string DaHuy = "Đã hủy";
}

// Các quy định đặt bàn: muốn đổi giờ mở cửa, thời gian giữ bàn... chỉ cần sửa ở đây
public static class QuyDinhDatBan
{
    public const int ThoiLuongGio = 2;            // mỗi lượt đặt giữ bàn 2 tiếng
    public const int GioDauTien = 10;             // nhận đặt từ 10:00
    public const int GioCuoiCung = 21;            // đến 21:00 (ăn xong khoảng 23:00)
    public const int BuocPhut = 30;               // mỗi 30 phút một khung giờ
    public const int DatTruocToiThieuPhut = 30;   // phải đặt trước ít nhất 30 phút
    public const int DatTruocToiDaNgay = 30;      // chỉ nhận đặt trong 30 ngày tới
    public const int SoNguoiToiDa = 20;
    public const int SoLichDangGiuToiDa = 5;      // mỗi khách giữ tối đa 5 lịch cùng lúc
    public const int KhachDenSomPhut = 15;        // khách đặt trước được vào bàn sớm tối đa 15 phút (quét QR gọi món)
    public const int PhienTaiQuanToiDaGio = 6;    // hóa đơn tại bàn chưa thanh toán quá 6 tiếng coi là bị quên đóng

    // Danh sách giờ: "10:00", "10:30", ..., "21:00"
    public static List<string> DanhSachGio()
    {
        var ds = new List<string>();
        for (var phut = GioDauTien * 60; phut <= GioCuoiCung * 60; phut += BuocPhut)
        {
            ds.Add($"{phut / 60:00}:{phut % 60:00}");
        }
        return ds;
    }
}

// Khóa dùng chung cho mọi thao tác "kiểm tra bàn trống rồi giữ bàn":
// đặt bàn từ xa (DatBanController) và mở bàn tại quán bằng QR (TaiQuanController) cùng dùng khóa này,
// để hai bên không thể cùng lấy một bàn ở cùng lúc. Web chạy một tiến trình nên cách này đủ dùng.
public static class KhoaDatBan
{
    public static readonly SemaphoreSlim Khoa = new(1, 1);
}

// Dữ liệu cho trang tìm bàn
public class TimBanViewModel
{
    public DateTime? Ngay { get; set; }
    public string? Gio { get; set; }
    public int? SoNguoi { get; set; }
    public List<Ban>? BanTrong { get; set; }   // null = chưa tìm
    public string? Loi { get; set; }
}