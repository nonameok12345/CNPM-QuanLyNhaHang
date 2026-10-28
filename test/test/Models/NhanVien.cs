namespace test.Models;

// Khớp với bảng NhanVien trong database (tên thuộc tính = tên cột)
public class NhanVien
{
    public int MaNV { get; set; }

    public string HoTen { get; set; } = null!;

    public string ChucVu { get; set; } = null!;

    public string? SDT { get; set; }

    public DateOnly NgayVaoLam { get; set; }

    public string TaiKhoan { get; set; } = null!;

    // Lưu mật khẩu đã mã hóa (hash), không lưu chữ thường
    public string MatKhau { get; set; } = null!;

    // Cột mới theo ERD. Dùng kiểu có dấu ? vì trong SQL các cột này cho phép NULL
    public decimal? Luong { get; set; }

    public int? MaKho { get; set; }     // nhân viên phụ trách kho nào (có thể chưa có)
    public Kho? Kho { get; set; }
}