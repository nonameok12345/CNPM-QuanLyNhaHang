using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace test.Models;

// ===== Các bảng trong database =====

public class Ban
{
    public int MaBan { get; set; }
    public string SoBan { get; set; } = null!;
    public int SoChoNgoi { get; set; }
    public string TrangThai { get; set; } = "Trống";

    // Mã bí mật của bàn, in thành QR dán trên bàn (khách quét để gọi món tại quán).
    // Bàn mới tự có mã; nhân viên có thể đổi mã nếu QR bị lộ.
    public Guid MaQR { get; set; } = Guid.NewGuid();
}

public static class TrangThaiHoaDon
{
    public const string ChuaThanhToan = "Chưa thanh toán";
    public const string DaThanhToan = "Đã thanh toán";
    public const string DaHuy = "Đã hủy";
}

public class HoaDon
{
    public int MaHD { get; set; }
    public int MaBan { get; set; }
    public int? MaNV { get; set; }      // để trống khi khách tự đặt
    public int? MaKH { get; set; }      // để trống với khách vãng lai
    public int? MaDatBan { get; set; }  // hóa đơn thuộc lịch đặt bàn nào
    public DateTime NgayLap { get; set; }
    public decimal TongTien { get; set; }
    public string? PhuongThucTT { get; set; }
    public string TrangThai { get; set; } = TrangThaiHoaDon.ChuaThanhToan;

    public Ban Ban { get; set; } = null!;
    public DatBan? DatBan { get; set; }
    public List<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new();
}

public class ChiTietHoaDon
{
    public int MaHD { get; set; }
    public int MaMon { get; set; }
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }   // giá chốt tại lúc đặt

    public HoaDon HoaDon { get; set; } = null!;
    public MonAn MonAn { get; set; } = null!;
}

// ===== Dữ liệu hiển thị trang giỏ hàng =====

public class DongGioHang
{
    public int MaMon { get; set; }
    public string TenMon { get; set; } = "";
    public string? HinhAnh { get; set; }
    public decimal Gia { get; set; }
    public int SoLuong { get; set; }
    public decimal ThanhTien => Gia * SoLuong;
}

public class GioHangViewModel
{
    public List<DongGioHang> Dong { get; set; } = new();
    public List<DatBan> DatBanList { get; set; } = new();   // các lịch đặt bàn còn hiệu lực của khách
    public decimal TongTien => Dong.Sum(d => d.ThanhTien);
}

// ===== Giỏ hàng lưu trong Session: { mã món -> số lượng } =====

public static class GioHangSession
{
    private const string Khoa = "GioHang";

    public static Dictionary<int, int> Doc(ISession session)
    {
        var json = session.GetString(Khoa);
        if (string.IsNullOrEmpty(json)) return new Dictionary<int, int>();
        return JsonSerializer.Deserialize<Dictionary<int, int>>(json) ?? new Dictionary<int, int>();
    }

    public static void Luu(ISession session, Dictionary<int, int> gio)
        => session.SetString(Khoa, JsonSerializer.Serialize(gio));

    public static void Xoa(ISession session) => session.Remove(Khoa);

    public static int TongSoLuong(ISession session) => Doc(session).Values.Sum();
}