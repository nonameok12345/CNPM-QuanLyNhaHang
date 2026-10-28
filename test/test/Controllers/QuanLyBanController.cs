using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Controllers
{
    // Quản lý bàn: dành cho nhân viên và quản lý (khách hàng không vào được)
    [Authorize(Roles = VaiTro.QuanLy + "," + VaiTro.NhanVien)]
    public class QuanLyBanController : Controller
    {
        private readonly QuanLyNhaHangContext _context;

        public QuanLyBanController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        // GET: /QuanLyBan — danh sách bàn kèm số lịch đặt sắp tới
        public async Task<IActionResult> Index()
        {
            // "Sắp tới" = lịch chưa hủy và chưa hết giờ giữ bàn
            var tu = DateTime.Now.AddHours(-QuyDinhDatBan.ThoiLuongGio);
            var lich = await _context.DatBans
                .Where(d => d.TrangThai != TrangThaiDatBan.DaHuy && d.NgayGio > tu)
                .GroupBy(d => d.MaBan)
                .Select(g => new { MaBan = g.Key, So = g.Count(), KeTiep = g.Min(x => x.NgayGio) })
                .ToListAsync();

            var bans = await _context.Bans.OrderBy(b => b.SoBan).ToListAsync();

            var danhSach = bans.Select(b =>
            {
                var l = lich.FirstOrDefault(x => x.MaBan == b.MaBan);
                return new BanQuanLyDong
                {
                    Ban = b,
                    SoLichSapToi = l?.So ?? 0,
                    LichKeTiep = l?.KeTiep
                };
            }).ToList();

            return View(danhSach);
        }

        // GET: /QuanLyBan/Create
        public IActionResult Create() => View("Form", new BanFormViewModel());

        // POST: /QuanLyBan/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BanFormViewModel model)
        {
            await KiemTraThemAsync(model);
            if (!ModelState.IsValid) return View("Form", model);

            _context.Bans.Add(new Ban
            {
                SoBan = model.SoBan.Trim(),
                SoChoNgoi = model.SoChoNgoi,
                TrangThai = model.TrangThai
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Không lưu được bàn, vui lòng thử lại.");
                return View("Form", model);
            }

            TempData["ThongBao"] = $"Đã thêm bàn {model.SoBan.Trim()}.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /QuanLyBan/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var ban = await _context.Bans.FindAsync(id);
            if (ban == null) return NotFound();

            return View("Form", new BanFormViewModel
            {
                MaBan = ban.MaBan,
                SoBan = ban.SoBan,
                SoChoNgoi = ban.SoChoNgoi,
                TrangThai = ban.TrangThai
            });
        }

        // POST: /QuanLyBan/Edit — mã bàn nằm trong ô ẩn của form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BanFormViewModel model)
        {
            var ban = await _context.Bans.FindAsync(model.MaBan);
            if (ban == null) return NotFound();

            await KiemTraThemAsync(model);
            if (!ModelState.IsValid) return View("Form", model);

            ban.SoBan = model.SoBan.Trim();
            ban.SoChoNgoi = model.SoChoNgoi;
            ban.TrangThai = model.TrangThai;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Không lưu được thay đổi, vui lòng thử lại.");
                return View("Form", model);
            }

            TempData["ThongBao"] = $"Đã cập nhật bàn {ban.SoBan}.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /QuanLyBan/DoiTrangThai — đổi nhanh trạng thái ngay trong danh sách
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoiTrangThai(int id, string trangThai)
        {
            var ban = await _context.Bans.FindAsync(id);
            if (ban == null) return NotFound();

            if (!TrangThaiBan.TatCa.Contains(trangThai))
            {
                TempData["Loi"] = "Trạng thái không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            ban.TrangThai = trangThai;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /QuanLyBan/QR hoặc /QuanLyBan/QR/5 — trang in mã QR gọi món (tất cả bàn, hoặc một bàn)
        public async Task<IActionResult> QR(int? id)
        {
            var query = _context.Bans.AsQueryable();
            if (id != null) query = query.Where(b => b.MaBan == id.Value);

            var bans = await query.OrderBy(b => b.SoBan).ToListAsync();
            if (bans.Count == 0) return NotFound();

            // Địa chỉ gợi ý ban đầu; nhân viên sửa lại trên trang nếu điện thoại khách cần địa chỉ khác (IP mạng quán, tên miền...)
            ViewBag.GocUrl = $"{Request.Scheme}://{Request.Host}";
            return View(bans);
        }

        // POST: /QuanLyBan/DoiMaQR/5 — cấp mã QR mới cho bàn (khi QR bị lộ); QR cũ đã in sẽ hết dùng được
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoiMaQR(int id)
        {
            var ban = await _context.Bans.FindAsync(id);
            if (ban == null) return NotFound();

            ban.MaQR = Guid.NewGuid();
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã đổi mã QR của bàn {ban.SoBan}. Hãy in lại và dán QR mới.";
            return RedirectToAction(nameof(QR), new { id });
        }

        // POST: /QuanLyBan/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ban = await _context.Bans.FindAsync(id);
            if (ban == null) return NotFound();

            // Bàn đã có lịch đặt hoặc hóa đơn thì không xóa được (sẽ làm hỏng dữ liệu cũ)
            var daDuocDung = await _context.DatBans.AnyAsync(d => d.MaBan == id)
                          || await _context.HoaDons.AnyAsync(h => h.MaBan == id);
            if (daDuocDung)
            {
                TempData["Loi"] = $"Bàn {ban.SoBan} đã có lịch đặt hoặc hóa đơn nên không xóa được. " +
                                  $"Hãy đổi trạng thái sang \"{TrangThaiBan.NgungSuDung}\".";
                return RedirectToAction(nameof(Index));
            }

            _context.Bans.Remove(ban);
            await _context.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã xóa bàn {ban.SoBan}.";
            return RedirectToAction(nameof(Index));
        }

        // ---- Hàm phụ dùng chung cho Create và Edit ----

        // Những kiểm tra mà DataAnnotations không làm được: trạng thái hợp lệ, số bàn không trùng
        private async Task KiemTraThemAsync(BanFormViewModel model)
        {
            if (!TrangThaiBan.TatCa.Contains(model.TrangThai))
                ModelState.AddModelError(nameof(model.TrangThai), "Trạng thái không hợp lệ.");

            var soBan = model.SoBan?.Trim() ?? "";
            if (soBan.Length > 0 &&
                await _context.Bans.AnyAsync(b => b.SoBan == soBan && b.MaBan != model.MaBan))
            {
                ModelState.AddModelError(nameof(model.SoBan), $"Số bàn {soBan} đã tồn tại.");
            }
        }
    }
}
