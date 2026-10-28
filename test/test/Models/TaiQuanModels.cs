using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace test.Models;

// ===== Dùng cho luồng khách NGỒI TẠI QUÁN: quét QR trên bàn để gọi món =====
// (Luồng đặt bàn + gọi món từ xa của khách vẫn giữ nguyên ở DatBanController / GioHangController)

// Tình trạng của một bàn vào lúc khách quét QR
public static class TrangThaiPhienBan
{
    public const string DaCoPhien = "DaCoPhien";       // bàn đã mở (có hóa đơn chưa thanh toán) -> gọi món được
    public const string CoTheMo = "CoTheMo";           // bàn trống -> khách bấm "Ngồi bàn này" để mở bàn
    public const string BanDaDat = "BanDaDat";         // sắp có khách đặt trước nên chưa cho mở
    public const string NgungSuDung = "NgungSuDung";   // bàn hỏng / đang sửa
}

// Kết quả kiểm tra một bàn: bàn nào, đang ở tình trạng gì, gắn với lịch đặt / hóa đơn nào
public class PhienBanTaiQuan
{
    public Ban Ban { get; set; } = null!;
    public string TrangThai { get; set; } = TrangThaiPhienBan.CoTheMo;
    public DatBan? DatBan { get; set; }          // lịch đặt trước đang tới giờ của bàn này (nếu có)
    public HoaDon? HoaDon { get; set; }          // hóa đơn đang mở của bàn (nếu có)
    public DateTime? GioDatKeTiep { get; set; }  // giờ của lịch đặt sắp tới (dùng khi TrangThai = BanDaDat)
}

// Dữ liệu trang thực đơn gọi món tại bàn
public class MenuTaiQuanViewModel
{
    public Ban Ban { get; set; } = null!;
    public List<MonAn> MonAns { get; set; } = new();
    public List<DanhMuc> DanhMucs { get; set; } = new();
    public int? MaDanhMuc { get; set; }
    public string? TuKhoa { get; set; }
    public int SoMonTrongGio { get; set; }
    public decimal TongGio { get; set; }
}

// Dữ liệu trang giỏ món của bàn (dùng lại DongGioHang của giỏ hàng từ xa)
public class GioTaiQuanViewModel
{
    public Ban Ban { get; set; } = null!;
    public List<DongGioHang> Dong { get; set; } = new();
    public decimal TongTien => Dong.Sum(d => d.ThanhTien);
}

// Dữ liệu trang "Món đã gọi" của bàn
public class DaGoiViewModel
{
    public Ban Ban { get; set; } = null!;
    public HoaDon HoaDon { get; set; } = null!;
}

// ===== Giỏ món lưu trong Session: { mã món -> số lượng }, mỗi bàn một giỏ riêng =====
// Tách khỏi GioHangSession (giỏ của khách đặt từ xa) để hai luồng không lẫn vào nhau.
public static class GioTaiQuanSession
{
    private static string Khoa(Guid token) => $"GioTaiQuan:{token}";

    public static Dictionary<int, int> Doc(ISession session, Guid token)
    {
        var json = session.GetString(Khoa(token));
        if (string.IsNullOrEmpty(json)) return new Dictionary<int, int>();
        return JsonSerializer.Deserialize<Dictionary<int, int>>(json) ?? new Dictionary<int, int>();
    }

    public static void Luu(ISession session, Guid token, Dictionary<int, int> gio)
        => session.SetString(Khoa(token), JsonSerializer.Serialize(gio));

    public static void Xoa(ISession session, Guid token) => session.Remove(Khoa(token));
}
