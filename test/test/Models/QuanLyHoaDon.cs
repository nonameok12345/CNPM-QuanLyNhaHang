namespace test.Models;

// ===== Dùng cho trang quản lý hóa đơn và thanh toán của nhân viên =====

public static class PhuongThucThanhToan
{
    public const string TienMat = "Tiền mặt";
    public const string ChuyenKhoan = "Chuyển khoản";
    public const string The = "Thẻ";
    public const string KetHop = "Kết hợp";   // một hóa đơn trả bằng nhiều hình thức

    public static readonly string[] TatCa = { TienMat, ChuyenKhoan, The };
}

// Một dòng trong danh sách hóa đơn
public class HoaDonDong
{
    public int MaHD { get; set; }
    public string SoBan { get; set; } = "";
    public string TenKhach { get; set; } = "Khách vãng lai";
    public DateTime NgayLap { get; set; }
    public decimal TongTien { get; set; }
    public decimal DaTra { get; set; }
    public string TrangThai { get; set; } = "";
    public decimal ConLai => TongTien - DaTra;
}

// Dữ liệu cho trang chi tiết một hóa đơn
public class HoaDonChiTietViewModel
{
    public HoaDon HoaDon { get; set; } = null!;
    public string TenKhach { get; set; } = "Khách vãng lai";
    public string? SDTKhach { get; set; }
    public string? TenNhanVien { get; set; }
    public List<ThanhToan> ThanhToans { get; set; } = new();
    public List<MonAn> MonAnList { get; set; } = new();   // để chọn thêm món

    // Chỉ tính các lần thanh toán thành công
    public decimal DaTra => ThanhToans
        .Where(t => t.TrangThai == TrangThaiThanhToan.ThanhCong)
        .Sum(t => t.SoTien);

    public decimal ConLai => HoaDon.TongTien - DaTra;

    // Hóa đơn còn "mở" thì mới được sửa món và nhận thanh toán
    public bool DangMo => HoaDon.TrangThai == TrangThaiHoaDon.ChuaThanhToan;
}
