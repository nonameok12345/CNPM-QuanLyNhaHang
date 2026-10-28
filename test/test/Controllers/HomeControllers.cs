using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Controllers
{
    public class HomeController : Controller
    {
        private readonly QuanLyNhaHangContext _context;

        public HomeController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        // Trang chủ: lấy danh mục kèm các món của từng danh mục từ database
        public async Task<IActionResult> Index()
        {
            var danhMucs = await _context.DanhMucs
                .Include(d => d.MonAns)          // nạp luôn danh sách món của mỗi danh mục
                .OrderBy(d => d.MaDanhMuc)
                .ToListAsync();

            return View(danhMucs);
        }

        public IActionResult About() => View();

        public IActionResult Contact() => View();
    }
}