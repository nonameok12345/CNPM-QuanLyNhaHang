using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Controllers
{
    // Giỏ hàng (lưu trong Session). Khách phải đặt bàn trước, sau đó mới gửi món cho lịch đặt bàn đó.
    public class GioHangController : Controller
    {
        private readonly QuanLyNhaHangContext _context;

        public GioHangController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        private int LayMaKH() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // GET: /GioHang — xem giỏ hàng
        public async Task<IActionResult> Index()
        {
            var gio = GioHangSession.Doc(HttpContext.Session);
            var maMons = gio.Keys.ToList();
            var monAns = await _context.MonAns.Where(m => maMons.Contains(m.MaMon)).ToListAsync();

            // Món đã bị quản lý xóa khỏi thực đơn thì bỏ khỏi giỏ
            var conTonTai = monAns.Select(m => m.MaMon).ToHashSet();
            var biXoa = maMons.Where(ma => !conTonTai.Contains(ma)).ToList();
            if (biXoa.Count > 0)
            {
                foreach (var ma in biXoa) gio.Remove(ma);
                GioHangSession.Luu(HttpContext.Session, gio);
            }

            var vm = new GioHangViewModel();
            foreach (var m in monAns)
            {
                vm.Dong.Add(new DongGioHang
                {
                    MaMon = m.MaMon,
                    TenMon = m.TenMon,
                    HinhAnh = m.HinhAnh,
                    Gia = m.Gia ?? 0,
                    SoLuong = gio[m.MaMon]
                });
            }

            // Khách đã đăng nhập: lấy các lịch đặt bàn còn hiệu lực để chọn gửi món
            if (User.IsInRole(VaiTro.KhachHang))
            {
                var maKH = LayMaKH();
                var hetHan = DateTime.Now.AddHours(-QuyDinhDatBan.ThoiLuongGio);
                vm.DatBanList = await _context.DatBans
                    .Include(d => d.Ban)
                    .Where(d => d.MaKH == maKH && d.TrangThai != TrangThaiDatBan.DaHuy && d.NgayGio > hetHan)
                    .OrderBy(d => d.NgayGio)
                    .ToListAsync();
            }

            return View(vm);
        }

        // POST: /GioHang/Them — bấm "Thêm vào giỏ" ở trang thực đơn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Them(int maMon, int soLuong = 1, string? returnUrl = null)
        {
            var mon = await _context.MonAns.FindAsync(maMon);
            if (mon == null) return NotFound();

            soLuong = Math.Clamp(soLuong, 1, 20);

            var gio = GioHangSession.Doc(HttpContext.Session);
            gio[maMon] = Math.Min(gio.GetValueOrDefault(maMon) + soLuong, 50);
            GioHangSession.Luu(HttpContext.Session, gio);

            TempData["ThongBao"] = $"Đã thêm {soLuong} × {mon.TenMon} vào giỏ";

            // Quay lại đúng trang thực đơn và cuộn tới đúng món vừa chọn
            var diaChi = (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                ? returnUrl
                : Url.Action("Index", "MonAn")!;
            return Redirect($"{diaChi}#mon-{maMon}");
        }

        // POST: /GioHang/CapNhat — đổi số lượng (về 0 là xóa)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CapNhat(int maMon, int soLuong)
        {
            var gio = GioHangSession.Doc(HttpContext.Session);
            if (soLuong <= 0)
                gio.Remove(maMon);
            else if (gio.ContainsKey(maMon))
                gio[maMon] = Math.Min(soLuong, 50);

            GioHangSession.Luu(HttpContext.Session, gio);
            return RedirectToAction(nameof(Index));
        }

        // POST: /GioHang/Xoa — bỏ một món khỏi giỏ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Xoa(int maMon)
        {
            var gio = GioHangSession.Doc(HttpContext.Session);
            gio.Remove(maMon);
            GioHangSession.Luu(HttpContext.Session, gio);
            return RedirectToAction(nameof(Index));
        }

        // POST: /GioHang/DatMon — gửi giỏ hàng thành món của một lịch đặt bàn
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = VaiTro.KhachHang)]
        public async Task<IActionResult> DatMon(int? maDatBan)
        {
            var maKH = LayMaKH();

            var gio = GioHangSession.Doc(HttpContext.Session);
            if (gio.Count == 0) return RedirectToAction(nameof(Index));

            if (maDatBan == null)
            {
                TempData["Loi"] = "Vui lòng chọn lịch đặt bàn để gửi món.";
                return RedirectToAction(nameof(Index));
            }

            // Chỉ dùng lịch đặt bàn CỦA CHÍNH khách này, chưa hủy, chưa quá giờ ăn
            var datBan = await _context.DatBans
                .FirstOrDefaultAsync(d => d.MaDatBan == maDatBan.Value && d.MaKH == maKH);
            if (datBan == null
                || datBan.TrangThai == TrangThaiDatBan.DaHuy
                || datBan.NgayGio.AddHours(QuyDinhDatBan.ThoiLuongGio) <= DateTime.Now)
            {
                TempData["Loi"] = "Lịch đặt bàn không hợp lệ hoặc đã hết hạn. Vui lòng chọn lại.";
                return RedirectToAction(nameof(Index));
            }

            // Lấy giá từ database, KHÔNG tin giá do trình duyệt gửi lên
            var maMons = gio.Keys.ToList();
            var monAns = await _context.MonAns
                .Where(m => maMons.Contains(m.MaMon))
                .ToDictionaryAsync(m => m.MaMon);

            // Lịch đặt bàn này đã có hóa đơn chưa thanh toán thì gọi thêm vào đó,
            // chưa có thì tạo hóa đơn mới
            var hoaDon = await _context.HoaDons
                .Include(h => h.ChiTietHoaDons)
                .FirstOrDefaultAsync(h => h.MaDatBan == datBan.MaDatBan && h.TrangThai == TrangThaiHoaDon.ChuaThanhToan);

            if (hoaDon == null)
            {
                hoaDon = new HoaDon
                {
                    MaBan = datBan.MaBan,
                    MaKH = maKH,
                    MaDatBan = datBan.MaDatBan,
                    NgayLap = DateTime.Now,
                    TrangThai = TrangThaiHoaDon.ChuaThanhToan
                };
                _context.HoaDons.Add(hoaDon);
            }

            var soMonDaThem = 0;
            foreach (var (maMon, soLuong) in gio)
            {
                if (!monAns.TryGetValue(maMon, out var mon)) continue;   // món đã bị xóa khỏi thực đơn

                var chiTiet = hoaDon.ChiTietHoaDons.FirstOrDefault(c => c.MaMon == maMon);
                if (chiTiet == null)
                {
                    hoaDon.ChiTietHoaDons.Add(new ChiTietHoaDon
                    {
                        MaMon = maMon,
                        SoLuong = soLuong,
                        DonGia = mon.Gia ?? 0
                    });
                }
                else
                {
                    chiTiet.SoLuong += soLuong;
                }
                soMonDaThem++;
            }

            if (soMonDaThem == 0)
            {
                GioHangSession.Xoa(HttpContext.Session);
                TempData["Loi"] = "Các món trong giỏ không còn trong thực đơn.";
                return RedirectToAction(nameof(Index));
            }

            hoaDon.TongTien = hoaDon.ChiTietHoaDons.Sum(c => c.SoLuong * c.DonGia);

            await _context.SaveChangesAsync();   // lưu hóa đơn + chi tiết trong một lần

            GioHangSession.Xoa(HttpContext.Session);
            TempData["MaHD"] = hoaDon.MaHD;
            return RedirectToAction(nameof(ThanhCong));
        }

        // GET: /GioHang/ThanhCong — báo gửi món thành công
        [Authorize(Roles = VaiTro.KhachHang)]
        public async Task<IActionResult> ThanhCong()
        {
            if (TempData.Peek("MaHD") is not int maHD)
                return RedirectToAction("Index", "MonAn");

            var maKH = LayMaKH();
            var hoaDon = await _context.HoaDons
                .Include(h => h.Ban)
                .Include(h => h.DatBan)
                .Include(h => h.ChiTietHoaDons).ThenInclude(c => c.MonAn)
                .FirstOrDefaultAsync(h => h.MaHD == maHD && h.MaKH == maKH);
            if (hoaDon == null) return RedirectToAction("Index", "MonAn");

            return View(hoaDon);
        }
    }
}