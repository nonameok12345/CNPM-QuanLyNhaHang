using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Controllers
{
    // Quản lý khách hàng: chỉ quản lý (có số điện thoại, email, mật khẩu)
    [Authorize(Roles = VaiTro.QuanLy)]
    public class QuanLyKhachHangController : Controller
    {
        private readonly QuanLyNhaHangContext _context;
        private readonly PasswordHasher<KhachHang> _hasher = new();

        public QuanLyKhachHangController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        // GET: /QuanLyKhachHang?q=...
        public async Task<IActionResult> Index(string? q)
        {
            var query = _context.KhachHangs.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var tuKhoa = q.Trim();
                query = query.Where(k => k.HoTen.Contains(tuKhoa) || k.SDT.Contains(tuKhoa)
                                      || (k.Email != null && k.Email.Contains(tuKhoa)));
            }
            var khachs = await query.OrderBy(k => k.HoTen).Take(300).ToListAsync();

            var ids = khachs.Select(k => k.MaKH).ToList();
            var soDatBan = await _context.DatBans.Where(d => ids.Contains(d.MaKH))
                .GroupBy(d => d.MaKH).Select(g => new { Ma = g.Key, So = g.Count() })
                .ToDictionaryAsync(x => x.Ma, x => x.So);
            var soHoaDon = await _context.HoaDons.Where(h => h.MaKH != null && ids.Contains(h.MaKH.Value))
                .GroupBy(h => h.MaKH!.Value).Select(g => new { Ma = g.Key, So = g.Count() })
                .ToDictionaryAsync(x => x.Ma, x => x.So);

            ViewBag.TuKhoa = q ?? "";
            return View(khachs.Select(k => new KhachHangDong
            {
                KhachHang = k,
                SoLichDatBan = soDatBan.GetValueOrDefault(k.MaKH),
                SoHoaDon = soHoaDon.GetValueOrDefault(k.MaKH)
            }).ToList());
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var kh = await _context.KhachHangs.FindAsync(id);
            if (kh == null) return NotFound();

            return View(new KhachHangFormViewModel
            {
                MaKH = kh.MaKH,
                HoTen = kh.HoTen,
                SDT = kh.SDT,
                Email = kh.Email,
                DiaChi = kh.DiaChi,
                CoTaiKhoan = !string.IsNullOrEmpty(kh.MatKhau)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(KhachHangFormViewModel model)
        {
            var kh = await _context.KhachHangs.FindAsync(model.MaKH);
            if (kh == null) return NotFound();
            model.CoTaiKhoan = !string.IsNullOrEmpty(kh.MatKhau);

            var sdt = model.SDT?.Trim() ?? "";
            if (sdt.Length > 0 && await _context.KhachHangs.AnyAsync(k => k.SDT == sdt && k.MaKH != model.MaKH))
                ModelState.AddModelError(nameof(model.SDT), "Số điện thoại này đã thuộc về khách hàng khác.");

            if (!ModelState.IsValid) return View(model);

            kh.HoTen = model.HoTen.Trim();
            kh.SDT = sdt;
            kh.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
            kh.DiaChi = string.IsNullOrWhiteSpace(model.DiaChi) ? null : model.DiaChi.Trim();
            if (!string.IsNullOrWhiteSpace(model.MatKhauMoi))
                kh.MatKhau = _hasher.HashPassword(kh, model.MatKhauMoi);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Không lưu được thay đổi, vui lòng thử lại.");
                return View(model);
            }

            TempData["ThongBao"] = $"Đã cập nhật khách hàng {kh.HoTen}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var kh = await _context.KhachHangs.FindAsync(id);
            if (kh == null) return NotFound();

            if (await _context.DatBans.AnyAsync(d => d.MaKH == id) || await _context.HoaDons.AnyAsync(h => h.MaKH == id))
            {
                TempData["Loi"] = $"Khách {kh.HoTen} đã có lịch đặt bàn hoặc hóa đơn nên không xóa được. " +
                                  "Hãy đặt mật khẩu mới để khóa tài khoản nếu cần.";
                return RedirectToAction(nameof(Index));
            }

            _context.KhachHangs.Remove(kh);
            await _context.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã xóa khách hàng {kh.HoTen}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
