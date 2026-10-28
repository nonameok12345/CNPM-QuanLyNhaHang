using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Controllers
{
    // Quản lý danh sách kho: chỉ quản lý
    [Authorize(Roles = VaiTro.QuanLy)]
    public class QuanLyKhoController : Controller
    {
        private readonly QuanLyNhaHangContext _context;

        public QuanLyKhoController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        // GET: /QuanLyKho
        public async Task<IActionResult> Index()
        {
            var khos = await _context.Khos.OrderBy(k => k.TenKho).ToListAsync();

            var soLo = await _context.TonKhos
                .Where(t => t.SoLuongTon > 0)
                .GroupBy(t => t.MaKho)
                .Select(g => new { MaKho = g.Key, So = g.Count() })
                .ToDictionaryAsync(x => x.MaKho, x => x.So);

            var soNv = await _context.NhanViens
                .Where(n => n.MaKho != null)
                .GroupBy(n => n.MaKho!.Value)
                .Select(g => new { MaKho = g.Key, So = g.Count() })
                .ToDictionaryAsync(x => x.MaKho, x => x.So);

            var danhSach = khos.Select(k => new KhoDong
            {
                Kho = k,
                SoLoCon = soLo.GetValueOrDefault(k.MaKho),
                SoNhanVien = soNv.GetValueOrDefault(k.MaKho)
            }).ToList();

            return View(danhSach);
        }

        // GET: /QuanLyKho/Create
        public IActionResult Create() => View("Form", new KhoFormViewModel());

        // POST: /QuanLyKho/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KhoFormViewModel model)
        {
            await KiemTraThemAsync(model, null);
            if (!ModelState.IsValid) return View("Form", model);

            _context.Khos.Add(new Kho
            {
                TenKho = model.TenKho.Trim(),
                DiaChi = string.IsNullOrWhiteSpace(model.DiaChi) ? null : model.DiaChi.Trim(),
                SDT = string.IsNullOrWhiteSpace(model.SDT) ? null : model.SDT.Trim(),
                TrangThai = model.TrangThai
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Không lưu được kho, vui lòng thử lại.");
                return View("Form", model);
            }

            TempData["ThongBao"] = $"Đã thêm kho {model.TenKho.Trim()}.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /QuanLyKho/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var kho = await _context.Khos.FindAsync(id);
            if (kho == null) return NotFound();

            return View("Form", new KhoFormViewModel
            {
                MaKho = kho.MaKho,
                TenKho = kho.TenKho,
                DiaChi = kho.DiaChi,
                SDT = kho.SDT,
                TrangThai = kho.TrangThai
            });
        }

        // POST: /QuanLyKho/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(KhoFormViewModel model)
        {
            var kho = await _context.Khos.FindAsync(model.MaKho);
            if (kho == null) return NotFound();

            await KiemTraThemAsync(model, kho);
            if (!ModelState.IsValid) return View("Form", model);

            kho.TenKho = model.TenKho.Trim();
            kho.DiaChi = string.IsNullOrWhiteSpace(model.DiaChi) ? null : model.DiaChi.Trim();
            kho.SDT = string.IsNullOrWhiteSpace(model.SDT) ? null : model.SDT.Trim();
            kho.TrangThai = model.TrangThai;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Không lưu được thay đổi, vui lòng thử lại.");
                return View("Form", model);
            }

            TempData["ThongBao"] = $"Đã cập nhật kho {kho.TenKho}.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /QuanLyKho/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var kho = await _context.Khos.FindAsync(id);
            if (kho == null) return NotFound();

            var dangDung = await _context.TonKhos.AnyAsync(t => t.MaKho == id)
                        || await _context.NhanViens.AnyAsync(n => n.MaKho == id);
            if (dangDung)
            {
                TempData["Loi"] = $"Kho {kho.TenKho} còn dữ liệu tồn kho hoặc có nhân viên phụ trách nên không xóa được. " +
                                  $"Hãy đổi trạng thái sang \"{TrangThaiKho.Ngung}\".";
                return RedirectToAction(nameof(Index));
            }

            _context.Khos.Remove(kho);
            await _context.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã xóa kho {kho.TenKho}.";
            return RedirectToAction(nameof(Index));
        }

        // Trạng thái hợp lệ (hoặc giữ nguyên giá trị cũ trong database) và tên kho không trùng
        private async Task KiemTraThemAsync(KhoFormViewModel model, Kho? hienTai)
        {
            if (!TrangThaiKho.TatCa.Contains(model.TrangThai) && model.TrangThai != hienTai?.TrangThai)
                ModelState.AddModelError(nameof(model.TrangThai), "Trạng thái không hợp lệ.");

            var ten = model.TenKho?.Trim() ?? "";
            if (ten.Length > 0 && await _context.Khos.AnyAsync(k => k.TenKho == ten && k.MaKho != model.MaKho))
                ModelState.AddModelError(nameof(model.TenKho), $"Kho \"{ten}\" đã tồn tại.");
        }
    }
}
