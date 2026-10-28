namespace test.Models;

// ===== Nhóm Nhân sự (CongViec, PhanCong) và Thanh toán =====

public class CongViec
{
    public int MaCV { get; set; }
    public string TenCV { get; set; } = null!;
    public string? MoTa { get; set; }
    public string TrangThai { get; set; } = "Hoạt động";
}

// Nhân viên được phân công làm công việc nào, từ ngày nào
public class PhanCong
{
    public int MaNV { get; set; }
    public int MaCV { get; set; }
    public DateOnly NgayBatDau { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? NgayKetThuc { get; set; }

    public NhanVien NhanVien { get; set; } = null!;
    public CongViec CongViec { get; set; } = null!;
}

public static class TrangThaiThanhToan
{
    public const string ThanhCong = "Thành công";
    public const string ThatBai = "Thất bại";
}

// Mỗi lần thanh toán của một hóa đơn (một hóa đơn có thể trả nhiều lần)
public class ThanhToan
{
    public int MaTT { get; set; }
    public int MaHD { get; set; }
    public DateTime NgayTT { get; set; } = DateTime.Now;
    public decimal SoTien { get; set; }
    public string PhuongThuc { get; set; } = "Tiền mặt";   // Tiền mặt / Chuyển khoản / Thẻ
    public string TrangThai { get; set; } = TrangThaiThanhToan.ThanhCong;

    public HoaDon HoaDon { get; set; } = null!;
}