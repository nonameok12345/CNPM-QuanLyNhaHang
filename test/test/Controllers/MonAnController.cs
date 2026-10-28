using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Controllers
{
    // Trang thực đơn dành cho khách hàng: chỉ xem (và sau này là đặt món).
    // Thêm / sửa / xóa đã chuyển sang QuanLyThucDonController.
    public class MonAnController : Controller
    {
        private readonly QuanLyNhaHangContext _context;

        public MonAnController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        // GET: /MonAn — xem thực đơn, có thể lọc theo danh mục

        public async Task<IActionResult> Index(string? tuKhoa, int? maDanhMuc)
        {
            // Lấy danh sách món ăn và thông tin danh mục
            var monAnQuery = _context.MonAns
                .Include(m => m.MaDanhMucNavigation)
                .Where(m => m.MaDanhMucNavigation == null
                            || m.MaDanhMucNavigation.TrangThai == TrangThaiKho.HoatDong)
                .AsQueryable();

            // Tìm kiếm theo tên món ăn
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                tuKhoa = tuKhoa.Trim();

                monAnQuery = monAnQuery.Where(m =>
                    m.TenMon.Contains(tuKhoa));
            }

            // Lọc theo danh mục nếu khách đã chọn
            if (maDanhMuc.HasValue)
            {
                monAnQuery = monAnQuery.Where(m =>
                    m.MaDanhMuc == maDanhMuc.Value);
            }

            // Gửi dữ liệu sang giao diện
            ViewBag.TuKhoa = tuKhoa;
            ViewBag.MaDanhMuc = maDanhMuc;
            ViewBag.DanhMucList = await _context.DanhMucs.Where(d => d.TrangThai == TrangThaiKho.HoatDong).ToListAsync();

            return View(await monAnQuery.ToListAsync());
        }
    }
}