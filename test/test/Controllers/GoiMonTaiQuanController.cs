using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Controllers
{
    // Trang giới thiệu cách gọi món tại quán bằng QR (mở từ menu "Đặt bàn").
    // Nhân viên / quản lý còn thấy danh sách bàn để mở thử trang gọi món của từng bàn khi demo.
    public class GoiMonTaiQuanController : Controller
    {
        private readonly QuanLyNhaHangContext _context;

        public GoiMonTaiQuanController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var laNhanVien = User.IsInRole(VaiTro.QuanLy) || User.IsInRole(VaiTro.NhanVien);
            var bans = laNhanVien
                ? await _context.Bans.OrderBy(b => b.SoBan).ToListAsync()
                : new List<Ban>();
            return View(bans);
        }
    }
}
