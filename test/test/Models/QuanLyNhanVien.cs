using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace test.Models;

// ===== Dùng cho trang quản lý nhân viên (chỉ quản lý được vào) =====

// Các chức vụ gợi ý trong ô chọn. Chức vụ "Quản lý" tương ứng vai trò QuanLy (xem VaiTro.TuChucVu).
public static class ChucVuNhanVien
{
    public const string QuanLy = "Quản lý";

    public static readonly string[] TatCa = { QuanLy, "Thu ngân", "Phục vụ", "Đầu bếp", "Thủ kho" };
}

// Dữ liệu của form Thêm / Sửa nhân viên
public class NhanVienFormViewModel
{
    public int MaNV { get; set; }   // = 0 khi thêm mới

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
    public string HoTen { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn chức vụ")]
    [StringLength(50, ErrorMessage = "Chức vụ tối đa 50 ký tự")]
    public string ChucVu { get; set; } = "";

    // Cột SDT trong SQL là varchar(15) nên chỉ nhận chữ số (có thể có dấu + ở đầu)
    [RegularExpression(@"^\+?\d{9,14}$", ErrorMessage = "Số điện thoại chỉ gồm chữ số (9-14 số)")]
    public string? SDT { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày vào làm")]
    public DateOnly NgayVaoLam { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "Vui lòng nhập tài khoản")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tài khoản từ 3 đến 50 ký tự")]
    [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "Tài khoản chỉ gồm chữ, số, dấu . _ - (không dấu, không khoảng trắng)")]
    public string TaiKhoan { get; set; } = "";

    // Thêm mới: bắt buộc (kiểm tra trong controller). Sửa: để trống = giữ mật khẩu cũ.
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [DataType(DataType.Password)]
    public string? MatKhau { get; set; }

    [Compare(nameof(MatKhau), ErrorMessage = "Mật khẩu nhập lại không khớp")]
    [DataType(DataType.Password)]
    public string? NhapLaiMatKhau { get; set; }

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Lương phải là số không âm")]
    public decimal? Luong { get; set; }

    public int? MaKho { get; set; }

    // Dữ liệu cho các ô chọn (controller điền, không nhận từ form)
    public List<string> DanhSachChucVu { get; set; } = new();
    public List<SelectListItem> DanhSachKho { get; set; } = new();
}

// Một dòng trong danh sách nhân viên
public class NhanVienDong
{
    public NhanVien NhanVien { get; set; } = null!;
    public int SoViecDangLam { get; set; }
    public bool LaTaiKhoanHienTai { get; set; }
}

// Dữ liệu cho trang phân công công việc của một nhân viên
public class PhanCongViewModel
{
    public NhanVien NhanVien { get; set; } = null!;
    public List<PhanCong> DanhSach { get; set; } = new();
    public List<CongViec> CongViecChuaGiao { get; set; } = new();
}
