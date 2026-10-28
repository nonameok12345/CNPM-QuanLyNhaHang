using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Controllers
{
    // Báo cáo doanh thu, món bán chạy, chi nhập kho: chỉ quản lý
    [Authorize(Roles = VaiTro.QuanLy)]
    public class ThongKeController : Controller
    {
        private const int SoNgayToiDa = 366;   // giới hạn khoảng xem để trang không quá nặng

        private readonly QuanLyNhaHangContext _context;

        public ThongKeController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        // GET: /ThongKe?tu=2026-10-01&den=2026-10-31 (mặc định: từ đầu tháng đến hôm nay)
        public async Task<IActionResult> Index(DateOnly? tu, DateOnly? den)
        {
            var homNay = DateOnly.FromDateTime(DateTime.Today);
            var denNgay = den ?? homNay;
            var tuNgay = tu ?? new DateOnly(denNgay.Year, denNgay.Month, 1);

            if (tuNgay > denNgay) (tuNgay, denNgay) = (denNgay, tuNgay);
            if (denNgay.DayNumber - tuNgay.DayNumber >= SoNgayToiDa)
            {
                tuNgay = denNgay.AddDays(-(SoNgayToiDa - 1));
                TempData["Loi"] = $"Chỉ xem tối đa {SoNgayToiDa} ngày một lần, đã thu hẹp khoảng thời gian cho bạn.";
            }

            var tuDt = tuNgay.ToDateTime(TimeOnly.MinValue);
            var denDt = denNgay.AddDays(1).ToDateTime(TimeOnly.MinValue);   // đến hết ngày cuối

            var model = new ThongKeViewModel { Tu = tuNgay, Den = denNgay };

            // Doanh thu = tiền đã thu thành công (một hóa đơn có thể trả nhiều lần, nhiều ngày)
            var thu = _context.ThanhToans.Where(t => t.TrangThai == TrangThaiThanhToan.ThanhCong
                                                     && t.NgayTT >= tuDt && t.NgayTT < denDt);
            model.TongThu = await thu.SumAsync(t => (decimal?)t.SoTien) ?? 0;

            model.TheoNgay = await thu
                .GroupBy(t => t.NgayTT.Date)
                .Select(g => new ThongKeNgayDong { Ngay = g.Key, SoTien = g.Sum(x => x.SoTien), SoLanThu = g.Count() })
                .OrderBy(x => x.Ngay)
                .ToListAsync();

            var hoaDons = _context.HoaDons.Where(h => h.NgayLap >= tuDt && h.NgayLap < denDt);
            model.SoHoaDonDaThanhToan = await hoaDons.CountAsync(h => h.TrangThai == TrangThaiHoaDon.DaThanhToan);
            model.SoHoaDonDangMo = await hoaDons.CountAsync(h => h.TrangThai == TrangThaiHoaDon.ChuaThanhToan);

            // Món bán chạy tính trên các hóa đơn đã thanh toán lập trong kỳ
            model.TopMon = await _context.ChiTietHoaDons
                .Where(c => c.HoaDon.TrangThai == TrangThaiHoaDon.DaThanhToan
                            && c.HoaDon.NgayLap >= tuDt && c.HoaDon.NgayLap < denDt)
                .GroupBy(c => c.MonAn.TenMon)
                .Select(g => new ThongKeMonDong
                {
                    TenMon = g.Key,
                    SoLuong = g.Sum(x => x.SoLuong),
                    DoanhThu = g.Sum(x => x.SoLuong * x.DonGia)
                })
                .OrderByDescending(x => x.SoLuong)
                .Take(10)
                .ToListAsync();

            model.ChiNhapKho = await _context.PhieuNhaps
                .Where(p => p.NgayNhap >= tuDt && p.NgayNhap < denDt)
                .SumAsync(p => (decimal?)p.TongTien) ?? 0;

            // Cảnh báo kho (tính theo hôm nay, không phụ thuộc khoảng ngày đang xem)
            var han = homNay.AddDays(QuyDinhKho.SapHetHanNgay);
            model.SoLoDaHetHan = await _context.TonKhos
                .CountAsync(t => t.SoLuongTon > 0 && t.NgayHetHan != null && t.NgayHetHan < homNay);
            model.SoLoSapHetHan = await _context.TonKhos
                .CountAsync(t => t.SoLuongTon > 0 && t.NgayHetHan != null && t.NgayHetHan >= homNay && t.NgayHetHan <= han);

            return View(model);
        }
    }
}
