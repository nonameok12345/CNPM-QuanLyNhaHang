using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace test.Models;

// ===== Công thức món (bảng CT_NguyenLieu) và tự trừ tồn kho =====

// Một dòng trong form công thức: món này dùng bao nhiêu nguyên liệu cho 1 phần
public class CongThucDong
{
    public int? MaNL { get; set; }

    [ModelBinder(BinderType = typeof(DecimalInvariantBinder))]
    public decimal? SoLuong { get; set; }
}

public class CongThucFormViewModel
{
    public int MaMon { get; set; }
    public string TenMon { get; set; } = "";
    public List<CongThucDong> Dong { get; set; } = new();

    // Dữ liệu cho ô chọn (controller điền, không nhận từ form)
    public List<NguyenLieu> DanhSachNguyenLieu { get; set; } = new();
}

// Một dòng trong danh sách món ở trang công thức
public class CongThucMonDong
{
    public MonAn MonAn { get; set; } = null!;
    public int SoNguyenLieu { get; set; }
    public decimal GiaVon { get; set; }   // ước tính theo giá nhập tham khảo của nguyên liệu
    public decimal GiaBan => MonAn.Gia ?? 0;
}

public class KetQuaTruKho
{
    public bool CoTru { get; set; }                       // có trừ ít nhất một nguyên liệu
    public int SoMonChuaCoCongThuc { get; set; }
    public List<string> Thieu { get; } = new();           // nguyên liệu không đủ tồn để trừ hết
}

public static class TruTonKho
{
    // Trừ tồn kho theo công thức của các món trong hóa đơn.
    // - Chỉ thay đổi dữ liệu đang theo dõi, KHÔNG gọi SaveChanges: người gọi lưu cùng lúc với thanh toán
    //   để hóa đơn đóng và kho bị trừ là một giao dịch duy nhất.
    // - Trừ từ lô có hạn dùng gần nhất trước (các lô đã hết hạn bị bỏ qua).
    // - Thiếu tồn thì vẫn trừ phần có và báo lại, không chặn việc thanh toán vì món đã được phục vụ.
    public static async Task<KetQuaTruKho> TruChoHoaDonAsync(
        QuanLyNhaHangContext db, HoaDon hoaDon)
    {
        var kq = new KetQuaTruKho();

        var maMons = hoaDon.ChiTietHoaDons.Select(c => c.MaMon).Distinct().ToList();
        var congThuc = await db.CtNguyenLieus.Where(c => maMons.Contains(c.MaMon)).ToListAsync();
        kq.SoMonChuaCoCongThuc = maMons.Count(m => congThuc.All(c => c.MaMon != m));

        // Tổng nguyên liệu cần dùng = số lượng trong công thức x số phần đã bán
        var can = new Dictionary<int, decimal>();
        foreach (var dong in hoaDon.ChiTietHoaDons)
        {
            foreach (var ct in congThuc.Where(c => c.MaMon == dong.MaMon))
                can[ct.MaNL] = can.GetValueOrDefault(ct.MaNL) + ct.SoLuong * dong.SoLuong;
        }
        if (can.Count == 0) return kq;

        var maNLs = can.Keys.ToList();
        var homNay = DateOnly.FromDateTime(DateTime.Today);

        var los = await db.TonKhos
            .Where(t => maNLs.Contains(t.MaNL) && t.SoLuongTon > 0
                        && (t.NgayHetHan == null || t.NgayHetHan >= homNay))
            .ToListAsync();
        var nguyenLieus = await db.NguyenLieus.Where(n => maNLs.Contains(n.MaNL)).ToDictionaryAsync(n => n.MaNL);

        foreach (var (maNL, canSo) in can)
        {
            // Cột tồn kho chỉ có 2 chữ số thập phân nên làm tròn nhu cầu một lần cho cả hóa đơn
            var conCan = Math.Round(canSo, 2, MidpointRounding.AwayFromZero);

            foreach (var lo in los.Where(l => l.MaNL == maNL)
                                  .OrderBy(l => l.NgayHetHan == null)   // lô có hạn dùng trước
                                  .ThenBy(l => l.NgayHetHan)
                                  .ThenBy(l => l.NgayNhap))
            {
                if (conCan <= 0) break;
                var tru = Math.Min(lo.SoLuongTon, conCan);
                lo.SoLuongTon -= tru;
                lo.TrangThai = TrangThaiTonKho.TuSoLuong(lo.SoLuongTon);
                conCan -= tru;
                kq.CoTru = true;
            }

            if (conCan > 0 && nguyenLieus.TryGetValue(maNL, out var nl))
                kq.Thieu.Add($"{nl.TenNL} thiếu {conCan:0.##} {nl.DonViTinh}");
        }

        return kq;
    }
}
