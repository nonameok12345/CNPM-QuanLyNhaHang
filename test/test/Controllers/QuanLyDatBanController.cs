using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Controllers
{
    // Quản lý đặt bàn: nhân viên xem lịch của khách, xác nhận hoặc hủy lịch
    [Authorize(Roles = VaiTro.QuanLy + "," + VaiTro.NhanVien)]
    public class QuanLyDatBanController : Controller
    {
        private readonly QuanLyNhaHangContext _context;

        public QuanLyDatBanController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        // GET: /QuanLyDatBan — lịch đặt bàn của một ngày (mặc định hôm nay), lọc được theo trạng thái
        public async Task<IActionResult> Index(DateTime? ngay, string? trangThai)
        {
            var homNay = (ngay ?? DateTime.Today).Date;
            var ngaySau = homNay.AddDays(1);

            var goc = _context.DatBans.Where(d => d.NgayGio >= homNay && d.NgayGio < ngaySau);
            if (!string.IsNullOrEmpty(trangThai))
                goc = goc.Where(d => d.TrangThai == trangThai);

            // DatBan không có thuộc tính điều hướng tới KhachHang nên nối bảng bằng join
            var danhSach = await (
                from d in goc
                join b in _context.Bans on d.MaBan equals b.MaBan
                join k in _context.KhachHangs on d.MaKH equals k.MaKH
                orderby d.NgayGio
                select new DatBanQuanLyDong
                {
                    MaDatBan = d.MaDatBan,
                    NgayGio = d.NgayGio,
                    SoBan = b.SoBan,
                    SoNguoi = d.SoNguoi,
                    TenKhach = k.HoTen,
                    SDTKhach = k.SDT,
                    TrangThai = d.TrangThai
                }).ToListAsync();

            ViewBag.Ngay = homNay;
            ViewBag.TrangThai = trangThai;
            return View(danhSach);
        }

        // POST: /QuanLyDatBan/XacNhan/5 — chuyển "Chờ xác nhận" thành "Đã xác nhận"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhan(int id, string? ngay)
        {
            var datBan = await _context.DatBans.FindAsync(id);
            if (datBan == null) return NotFound();

            if (datBan.TrangThai == TrangThaiDatBan.ChoXacNhan && !DaHetGio(datBan))
            {
                datBan.TrangThai = TrangThaiDatBan.DaXacNhan;
                await _context.SaveChangesAsync();
                TempData["ThongBao"] = "Đã xác nhận lịch đặt bàn.";
            }
            else
            {
                TempData["Loi"] = "Không thể xác nhận lịch này (không ở trạng thái chờ hoặc đã hết giờ).";
            }
            return QuayLai(ngay);
        }

        // POST: /QuanLyDatBan/Huy/5 — nhân viên hủy lịch (ví dụ khách gọi điện báo hủy, hoặc không đến)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Huy(int id, string? ngay)
        {
            var datBan = await _context.DatBans.FindAsync(id);
            if (datBan == null) return NotFound();

            if (datBan.TrangThai == TrangThaiDatBan.DaHuy || DaHetGio(datBan))
            {
                TempData["Loi"] = "Không thể hủy lịch này (đã hủy hoặc đã hết giờ).";
                return QuayLai(ngay);
            }

            var chuaBatDau = datBan.NgayGio > DateTime.Now;
            datBan.TrangThai = TrangThaiDatBan.DaHuy;

            // Món khách gọi trước cho lịch này cũng hủy theo, nhưng CHỈ khi lịch chưa bắt đầu.
            // Nếu khách đã ngồi vào bàn thì hóa đơn có thể đang dùng thật, để thu ngân tự xử lý.
            if (chuaBatDau)
            {
                var hoaDons = await _context.HoaDons
                    .Where(h => h.MaDatBan == id && h.TrangThai == TrangThaiHoaDon.ChuaThanhToan)
                    .ToListAsync();
                foreach (var hd in hoaDons) hd.TrangThai = TrangThaiHoaDon.DaHuy;
            }

            await _context.SaveChangesAsync();
            TempData["ThongBao"] = "Đã hủy lịch đặt bàn.";
            return QuayLai(ngay);
        }

        // ---- Hàm phụ ----

        // Lịch đã hết giờ giữ bàn (giờ đặt + thời lượng đã qua)
        private static bool DaHetGio(DatBan d)
            => d.NgayGio.AddHours(QuyDinhDatBan.ThoiLuongGio) <= DateTime.Now;

        // Sau khi bấm nút thì quay lại đúng ngày đang xem
        private IActionResult QuayLai(string? ngay)
            => RedirectToAction(nameof(Index), new { ngay });
    }
}
