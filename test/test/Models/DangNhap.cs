using System.ComponentModel.DataAnnotations;
using System.Text;

namespace test.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tài khoản")]
    public string TaiKhoan { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    public string MatKhau { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

// Vai trò dùng trong [Authorize(Roles = ...)].
// Dùng tên không dấu để không bị lỗi khi so sánh chữ tiếng Việt.
public static class VaiTro
{
    public const string QuanLy = "QuanLy";
    public const string NhanVien = "NhanVien";
    public const string KhachHang = "KhachHang";

    // Chức vụ trong database là "Quản lý" thì thành vai trò QuanLy, còn lại là NhanVien
    public static string TuChucVu(string chucVu)
    {
        var chuan = chucVu.Trim().Normalize(NormalizationForm.FormC);
        return string.Equals(chuan, "Quản lý", StringComparison.OrdinalIgnoreCase) ? QuanLy : NhanVien;
    }
}