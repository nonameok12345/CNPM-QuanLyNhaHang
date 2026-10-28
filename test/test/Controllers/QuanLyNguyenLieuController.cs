using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Controllers
{
    // Danh mục nguyên liệu: chỉ quản lý
    [Authorize(Roles = VaiTro.QuanLy)]
    public class QuanLyNguyenLieuController : Controller
    {
        private readonly QuanLyNhaHangContext _context;

        public QuanLyNguyenLieuController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        // GET: /QuanLyNguyenLieu?q=...
        public async Task<IActionResult> Index(string? q)
        {
            var query = _context.NguyenLieus.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var tuKhoa = q.Trim();
                query = query.Where(n => n.TenNL.Contains(tuKhoa));
            }

            var nguyenLieus = await query.OrderBy(n => n.TenNL).ToListAsync();

            // Tổng tồn của từng nguyên liệu trong tất cả các kho
            var tongTon = await _context.TonKhos
                .Where(t => t.SoLuongTon > 0)
                .GroupBy(t => t.MaNL)
                .Select(g => new { MaNL = g.Key, Tong = g.Sum(x => x.SoLuongTon) })
                .ToDictionaryAsync(x => x.MaNL, x => x.Tong);

            ViewBag.TuKhoa = q ?? "";
            return View(nguyenLieus.Select(n => new NguyenLieuDong
            {
                NguyenLieu = n,
                TongTon = tongTon.GetValueOrDefault(n.MaNL)
            }).ToList());
        }

        // GET: /QuanLyNguyenLieu/Create
        public IActionResult Create() => View("Form", new NguyenLieuFormViewModel());

        // POST: /QuanLyNguyenLieu/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NguyenLieuFormViewModel model)
        {
            await KiemTraThemAsync(model, null);
            if (!ModelState.IsValid) return View("Form", model);

            _context.NguyenLieus.Add(new NguyenLieu
            {
                TenNL = model.TenNL.Trim(),
                DonViTinh = model.DonViTinh.Trim(),
                GiaNhap = model.GiaNhap,
                TrangThai = model.TrangThai
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Không lưu được nguyên liệu, vui lòng thử lại.");
                return View("Form", model);
            }

            TempData["ThongBao"] = $"Đã thêm nguyên liệu {model.TenNL.Trim()}.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /QuanLyNguyenLieu/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var nl = await _context.NguyenLieus.FindAsync(id);
            if (nl == null) return NotFound();

            return View("Form", new NguyenLieuFormViewModel
            {
                MaNL = nl.MaNL,
                TenNL = nl.TenNL,
                DonViTinh = nl.DonViTinh,
                GiaNhap = nl.GiaNhap,
                TrangThai = nl.TrangThai
            });
        }

        // POST: /QuanLyNguyenLieu/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(NguyenLieuFormViewModel model)
        {
            var nl = await _context.NguyenLieus.FindAsync(model.MaNL);
            if (nl == null) return NotFound();

            await KiemTraThemAsync(model, nl);
            if (!ModelState.IsValid) return View("Form", model);

            nl.TenNL = model.TenNL.Trim();
            nl.DonViTinh = model.DonViTinh.Trim();
            nl.GiaNhap = model.GiaNhap;
            nl.TrangThai = model.TrangThai;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Không lưu được thay đổi, vui lòng thử lại.");
                return View("Form", model);
            }

            TempData["ThongBao"] = $"Đã cập nhật nguyên liệu {nl.TenNL}.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /QuanLyNguyenLieu/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var nl = await _context.NguyenLieus.FindAsync(id);
            if (nl == null) return NotFound();

            var dangDung = await _context.TonKhos.AnyAsync(t => t.MaNL == id)
                        || await _context.CtKhoNhaps.AnyAsync(c => c.MaNL == id)
                        || await _context.CtNguyenLieus.AnyAsync(c => c.MaNL == id);
            if (dangDung)
            {
                TempData["Loi"] = $"{nl.TenNL} đã có trong tồn kho, phiếu nhập hoặc công thức món nên không xóa được. " +
                                  $"Hãy đổi trạng thái sang \"{TrangThaiKho.Ngung}\".";
                return RedirectToAction(nameof(Index));
            }

            _context.NguyenLieus.Remove(nl);
            await _context.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã xóa nguyên liệu {nl.TenNL}.";
            return RedirectToAction(nameof(Index));
        }

        private async Task KiemTraThemAsync(NguyenLieuFormViewModel model, NguyenLieu? hienTai)
        {
            if (!TrangThaiKho.TatCa.Contains(model.TrangThai) && model.TrangThai != hienTai?.TrangThai)
                ModelState.AddModelError(nameof(model.TrangThai), "Trạng thái không hợp lệ.");

            if (model.GiaNhap != Math.Round(model.GiaNhap))
                ModelState.AddModelError(nameof(model.GiaNhap), "Giá nhập phải là số nguyên (đồng).");

            var ten = model.TenNL?.Trim() ?? "";
            if (ten.Length > 0 && await _context.NguyenLieus.AnyAsync(n => n.TenNL == ten && n.MaNL != model.MaNL))
                ModelState.AddModelError(nameof(model.TenNL), $"Nguyên liệu \"{ten}\" đã tồn tại.");
        }
    }
}
