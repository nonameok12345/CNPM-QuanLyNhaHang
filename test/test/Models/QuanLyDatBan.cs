using System.ComponentModel.DataAnnotations;

namespace test.Models;

// ===== Dùng cho trang quản lý bàn và quản lý đặt bàn của nhân viên =====

// Trạng thái do nhân viên đặt tay cho từng bàn.
// Lưu ý: việc "bàn có trống vào giờ X hay không" vẫn do lịch đặt bàn quyết định (xem DatBanController).
// "Ngừng sử dụng" = bàn hỏng / đang sửa, khách sẽ không thấy bàn này khi tìm bàn trống.
public static class TrangThaiBan
{
    public const string Trong = "Trống";
    public const string DangDung = "Đang dùng";
    public const string NgungSuDung = "Ngừng sử dụng";

    public static readonly string[] TatCa = { Trong, DangDung, NgungSuDung };
}

// Dữ liệu của form Thêm / Sửa bàn
public class BanFormViewModel
{
    public int MaBan { get; set; }   // = 0 khi thêm mới

    [Required(ErrorMessage = "Vui lòng nhập số bàn")]
    [StringLength(20, ErrorMessage = "Số bàn tối đa 20 ký tự")]
    public string SoBan { get; set; } = "";

    [Range(1, 50, ErrorMessage = "Số chỗ ngồi phải từ 1 đến 50")]
    public int SoChoNgoi { get; set; } = 4;

    [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
    public string TrangThai { get; set; } = TrangThaiBan.Trong;
}

// Một dòng trong danh sách bàn: thông tin bàn + số lịch đặt sắp tới
public class BanQuanLyDong
{
    public Ban Ban { get; set; } = null!;
    public int SoLichSapToi { get; set; }
    public DateTime? LichKeTiep { get; set; }
}

// Một dòng trong danh sách đặt bàn: có thêm tên và số điện thoại khách
public class DatBanQuanLyDong
{
    public int MaDatBan { get; set; }
    public DateTime NgayGio { get; set; }
    public string SoBan { get; set; } = "";
    public int SoNguoi { get; set; }
    public string TenKhach { get; set; } = "";
    public string SDTKhach { get; set; } = "";
    public string TrangThai { get; set; } = "";
}
