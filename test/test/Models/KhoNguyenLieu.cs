namespace test.Models;

// ===== Nhóm Kho - Nguyên liệu - Nhập hàng (khớp bảng trong ERD_HoanChinh.sql) =====

public class Kho
{
    public int MaKho { get; set; }
    public string TenKho { get; set; } = null!;
    public string? DiaChi { get; set; }
    public string? SDT { get; set; }
    public string TrangThai { get; set; } = "Hoạt động";

    public List<TonKho> TonKhos { get; set; } = new();
}

public class NguyenLieu
{
    public int MaNL { get; set; }
    public string TenNL { get; set; } = null!;
    public string DonViTinh { get; set; } = null!;   // kg, lít, gói...
    public decimal GiaNhap { get; set; }
    public string TrangThai { get; set; } = "Hoạt động";
}

// Tồn kho theo từng lô: một nguyên liệu trong một kho
public class TonKho
{
    public int MaTon { get; set; }
    public int MaKho { get; set; }
    public int MaNL { get; set; }
    public decimal SoLuongTon { get; set; }
    public DateOnly NgayNhap { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? NgayHetHan { get; set; }
    public string TrangThai { get; set; } = "Còn hàng";

    public Kho Kho { get; set; } = null!;
    public NguyenLieu NguyenLieu { get; set; } = null!;
}

// Công thức món ăn: mỗi món dùng bao nhiêu nguyên liệu (bảng CT_NguyenLieu)
public class CtNguyenLieu
{
    public int MaMon { get; set; }
    public int MaNL { get; set; }
    public decimal SoLuong { get; set; }

    public MonAn MonAn { get; set; } = null!;
    public NguyenLieu NguyenLieu { get; set; } = null!;
}

public class PhieuNhap
{
    public int MaNhap { get; set; }
    public int MaNV { get; set; }
    public DateTime NgayNhap { get; set; } = DateTime.Now;
    public decimal TongTien { get; set; }
    public string TrangThai { get; set; } = "Đã nhập";

    public NhanVien NhanVien { get; set; } = null!;
    public List<CtKhoNhap> ChiTiets { get; set; } = new();
}

// Chi tiết phiếu nhập (bảng CT_KhoNhap)
public class CtKhoNhap
{
    public int MaNhap { get; set; }
    public int MaNL { get; set; }
    public decimal SoLuong { get; set; }
    public decimal DonGia { get; set; }

    // Cột ThanhTien trong SQL tự tính; ở C# chỉ cần tính lại, không lưu
    public decimal ThanhTien => SoLuong * DonGia;

    public PhieuNhap PhieuNhap { get; set; } = null!;
    public NguyenLieu NguyenLieu { get; set; } = null!;
}