using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Controllers
{
    // Trang quản trị: gom mọi chức năng của nhân viên và quản lý vào một chỗ
    [Authorize(Roles = VaiTro.QuanLy + "," + VaiTro.NhanVien)]
    public class QuanTriController : Controller
    {
        private readonly QuanLyNhaHangContext _context;

        public QuanTriController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        // GET: /QuanTri
        public async Task<IActionResult> Index()
        {
            var homNay = DateOnly.FromDateTime(DateTime.Today);
            var dauNgay = homNay.ToDateTime(TimeOnly.MinValue);
            var cuoiNgay = dauNgay.AddDays(1);

            var model = new TrangQuanTriViewModel
            {
                SoHoaDonChuaThanhToan = await _context.HoaDons
                    .CountAsync(h => h.TrangThai == TrangThaiHoaDon.ChuaThanhToan),
                SoDatBanChoXacNhan = await _context.DatBans
                    .CountAsync(d => d.TrangThai == TrangThaiDatBan.ChoXacNhan && d.NgayGio >= dauNgay),
                SoBanDangDung = await _context.Bans.CountAsync(b => b.TrangThai == TrangThaiBan.DangDung),
                SoBanDangHoatDong = await _context.Bans.CountAsync(b => b.TrangThai != TrangThaiBan.NgungSuDung),
                SoLoCanhBao = await _context.TonKhos.CountAsync(t => t.SoLuongTon > 0 && t.NgayHetHan != null
                    && t.NgayHetHan <= homNay.AddDays(QuyDinhKho.SapHetHanNgay))
            };

            // Doanh thu là thông tin nhạy cảm nên chỉ quản lý thấy
            if (User.IsInRole(VaiTro.QuanLy))
            {
                model.ThuHomNay = await _context.ThanhToans
                    .Where(t => t.TrangThai == TrangThaiThanhToan.ThanhCong && t.NgayTT >= dauNgay && t.NgayTT < cuoiNgay)
                    .SumAsync(t => (decimal?)t.SoTien) ?? 0;
            }

            return View(model);
        }
    }
}
