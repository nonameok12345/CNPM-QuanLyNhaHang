using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace test.Models;

// ===== Dùng cho quản lý kho, nguyên liệu, tồn kho và nhập kho =====

public static class TrangThaiKho
{
    public const string HoatDong = "Hoạt động";
    public const string Ngung = "Ngừng hoạt động";

    public static readonly string[] TatCa = { HoatDong, Ngung };
}

public static class TrangThaiTonKho
{
    public const string ConHang = "Còn hàng";
    public const string HetHang = "Hết hàng";

    // Trạng thái lưu trong database chỉ phụ thuộc vào số lượng còn lại
    public static string TuSoLuong(decimal soLuong) => soLuong > 0 ? ConHang : HetHang;
}

public static class QuyDinhKho
{
    // Lô còn hạn dưới số ngày này thì cảnh báo "sắp hết hạn"
    public const int SapHetHanNgay = 7;

    public static readonly string[] DonViTinh = { "kg", "g", "lít", "ml", "gói", "hộp", "chai", "lon", "quả", "con", "bó" };
}

// Đọc số thập phân theo kiểu "1.5" hoặc "1,5" không phụ thuộc ngôn ngữ của máy chủ.
// Ô <input type="number"> của trình duyệt luôn gửi dấu chấm, nhưng máy chủ cài tiếng Việt
// sẽ hiểu "1.5" thành 15 nên cần binder riêng cho các ô số lượng.
public class DecimalInvariantBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext ctx)
    {
        var vr = ctx.ValueProvider.GetValue(ctx.ModelName);
        if (vr == ValueProviderResult.None) return Task.CompletedTask;

        ctx.ModelState.SetModelValue(ctx.ModelName, vr);
        var s = vr.FirstValue?.Trim();

        if (string.IsNullOrEmpty(s))
        {
            if (Nullable.GetUnderlyingType(ctx.ModelType) != null)
                ctx.Result = ModelBindingResult.Success(null);
            else
                ctx.ModelState.TryAddModelError(ctx.ModelName, "Vui lòng nhập số");
            return Task.CompletedTask;
        }

        s = s.Replace(',', '.');
        if (decimal.TryParse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                             CultureInfo.InvariantCulture, out var v))
            ctx.Result = ModelBindingResult.Success(v);
        else
            ctx.ModelState.TryAddModelError(ctx.ModelName, "Giá trị không phải số hợp lệ");

        return Task.CompletedTask;
    }
}

// ---------- Kho ----------

public class KhoFormViewModel
{
    public int MaKho { get; set; }   // = 0 khi thêm mới

    [Required(ErrorMessage = "Vui lòng nhập tên kho")]
    [StringLength(100, ErrorMessage = "Tên kho tối đa 100 ký tự")]
    public string TenKho { get; set; } = "";

    [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự")]
    public string? DiaChi { get; set; }

    [RegularExpression(@"^\+?\d{9,14}$", ErrorMessage = "Số điện thoại chỉ gồm chữ số (9-14 số)")]
    public string? SDT { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
    public string TrangThai { get; set; } = TrangThaiKho.HoatDong;
}

public class KhoDong
{
    public Kho Kho { get; set; } = null!;
    public int SoLoCon { get; set; }
    public int SoNhanVien { get; set; }
}

// ---------- Nguyên liệu ----------

public class NguyenLieuFormViewModel
{
    public int MaNL { get; set; }   // = 0 khi thêm mới

    [Required(ErrorMessage = "Vui lòng nhập tên nguyên liệu")]
    [StringLength(100, ErrorMessage = "Tên nguyên liệu tối đa 100 ký tự")]
    public string TenNL { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập đơn vị tính")]
    [StringLength(30, ErrorMessage = "Đơn vị tính tối đa 30 ký tự")]
    public string DonViTinh { get; set; } = "kg";

    // Giá nhập tham khảo (đ / 1 đơn vị), cột decimal(18,0) nên chỉ nhận số nguyên
    [ModelBinder(BinderType = typeof(DecimalInvariantBinder))]
    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Giá nhập phải là số không âm")]
    public decimal GiaNhap { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
    public string TrangThai { get; set; } = TrangThaiKho.HoatDong;
}

public class NguyenLieuDong
{
    public NguyenLieu NguyenLieu { get; set; } = null!;
    public decimal TongTon { get; set; }
}

// ---------- Tồn kho ----------

public class TonKhoTongHopDong
{
    public NguyenLieu NguyenLieu { get; set; } = null!;
    public decimal TongTon { get; set; }
    public int SoLo { get; set; }
    public DateOnly? HanGanNhat { get; set; }
}

// ---------- Phiếu nhập ----------

// Một dòng nguyên liệu trên phiếu nhập; mỗi dòng sẽ tạo thêm một lô trong TonKho
public class PhieuNhapDong
{
    public int? MaNL { get; set; }

    [ModelBinder(BinderType = typeof(DecimalInvariantBinder))]
    public decimal? SoLuong { get; set; }

    [ModelBinder(BinderType = typeof(DecimalInvariantBinder))]
    public decimal? DonGia { get; set; }

    public DateOnly? NgayHetHan { get; set; }
}

public class PhieuNhapFormViewModel
{
    public int? MaKho { get; set; }

    public DateOnly NgayNhap { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public List<PhieuNhapDong> Dong { get; set; } = new() { new PhieuNhapDong() };

    // Dữ liệu cho các ô chọn (controller điền, không nhận từ form)
    public List<SelectListItem> DanhSachKho { get; set; } = new();
    public List<NguyenLieu> DanhSachNguyenLieu { get; set; } = new();
}