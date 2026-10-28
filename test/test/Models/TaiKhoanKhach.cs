using System.ComponentModel.DataAnnotations;

namespace test.Models;

// Khớp với bảng KhachHang trong database
public class KhachHang
{
    public int MaKH { get; set; }
    public string HoTen { get; set; } = null!;
    public string SDT { get; set; } = null!;       // dùng làm tên đăng nhập
    public string? Email { get; set; }
    public string? MatKhau { get; set; }           // mật khẩu đã mã hóa (hash)
    public string? DiaChi { get; set; }            // cột mới theo ERD, cho phép để trống
}

public class DangKyViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
    public string HoTen { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    public string SDT { get; set; } = "";

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    public string MatKhau { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu")]
    [Compare(nameof(MatKhau), ErrorMessage = "Mật khẩu nhập lại không khớp")]
    public string NhapLaiMatKhau { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class DangNhapKhachViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    public string SDT { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    public string MatKhau { get; set; } = "";

    public string? ReturnUrl { get; set; }
}