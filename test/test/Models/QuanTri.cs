using System.ComponentModel.DataAnnotations;

namespace test.Models;

// ===== Backend quản trị: danh mục, khách hàng, công việc, thống kê =====

// ---------- Danh mục món ----------
public class DanhMucFormViewModel
{
    public int MaDanhMuc { get; set; }   // = 0 khi thêm mới (mã do hệ thống tự cấp)

    [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
    [StringLength(100, ErrorMessage = "Tên danh mục tối đa 100 ký tự")]
    public string TenDanhMuc { get; set; } = "";

    [StringLength(255, ErrorMessage = "Mô tả tối đa 255 ký tự")]
    public string? MoTa { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
    public string TrangThai { get; set; } = TrangThaiKho.HoatDong;
}

public class DanhMucDong
{
    public DanhMuc DanhMuc { get; set; } = null!;
    public int SoMon { get; set; }
}

// ---------- Khách hàng ----------
public class KhachHangFormViewModel
{
    public int MaKH { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
    public string HoTen { get; set; } = "";

    // Số điện thoại là tên đăng nhập của khách nên bắt buộc và không được trùng
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [RegularExpression(@"^\+?\d{9,14}$", ErrorMessage = "Số điện thoại chỉ gồm chữ số (9-14 số)")]
    public string SDT { get; set; } = "";

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự")]
    public string? Email { get; set; }

    [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự")]
    public string? DiaChi { get; set; }

    // Để trống = giữ mật khẩu cũ
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [DataType(DataType.Password)]
    public string? MatKhauMoi { get; set; }

    public bool CoTaiKhoan { get; set; }   // chỉ để hiển thị
}

public class KhachHangDong
{
    public KhachHang KhachHang { get; set; } = null!;
    public int SoLichDatBan { get; set; }
    public int SoHoaDon { get; set; }
}

// ---------- Công việc ----------
public class CongViecFormViewModel
{
    public int MaCV { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên công việc")]
    [StringLength(100, ErrorMessage = "Tên công việc tối đa 100 ký tự")]
    public string TenCV { get; set; } = "";

    [StringLength(255, ErrorMessage = "Mô tả tối đa 255 ký tự")]
    public string? MoTa { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
    public string TrangThai { get; set; } = TrangThaiKho.HoatDong;
}

public class CongViecDong
{
    public CongViec CongViec { get; set; } = null!;
    public int SoNguoiDangLam { get; set; }
}

// ---------- Thống kê ----------
public class ThongKeNgayDong
{
    public DateTime Ngay { get; set; }
    public decimal SoTien { get; set; }
    public int SoLanThu { get; set; }
}

public class ThongKeMonDong
{
    public string TenMon { get; set; } = "";
    public int SoLuong { get; set; }
    public decimal DoanhThu { get; set; }
}

public class ThongKeViewModel
{
    public DateOnly Tu { get; set; }
    public DateOnly Den { get; set; }

    public decimal TongThu { get; set; }          // tiền đã thu thành công trong kỳ
    public int SoHoaDonDaThanhToan { get; set; }  // hóa đơn lập trong kỳ và đã thanh toán đủ
    public int SoHoaDonDangMo { get; set; }       // hóa đơn lập trong kỳ còn chưa thanh toán
    public decimal ChiNhapKho { get; set; }       // tổng tiền các phiếu nhập trong kỳ

    public int SoLoSapHetHan { get; set; }
    public int SoLoDaHetHan { get; set; }

    public List<ThongKeNgayDong> TheoNgay { get; set; } = new();
    public List<ThongKeMonDong> TopMon { get; set; } = new();

    public decimal ChenhLech => TongThu - ChiNhapKho;
    public decimal TrungBinhHoaDon => SoHoaDonDaThanhToan == 0 ? 0 : TongThu / SoHoaDonDaThanhToan;
}
