using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Controllers
{
    // Danh mục công việc (dùng khi phân công nhân viên): chỉ quản lý
    [Authorize(Roles = VaiTro.QuanLy)]
    public class QuanLyCongViecController : Controller
    {
        private readonly QuanLyNhaHangContext _context;

        public QuanLyCongViecController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var congViecs = await _context.CongViecs.OrderBy(c => c.TenCV).ToListAsync();

            var homNay = DateOnly.FromDateTime(DateTime.Today);
            var soNguoi = await _context.PhanCongs
                .Where(p => p.NgayKetThuc == null || p.NgayKetThuc >= homNay)
                .GroupBy(p => p.MaCV)
                .Select(g => new { Ma = g.Key, So = g.Count() })
                .ToDictionaryAsync(x => x.Ma, x => x.So);

            return View(congViecs.Select(c => new CongViecDong
            {
                CongViec = c,
                SoNguoiDangLam = soNguoi.GetValueOrDefault(c.MaCV)
            }).ToList());
        }

        public IActionResult Create() => View("Form", new CongViecFormViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CongViecFormViewModel model)
        {
            await KiemTraAsync(model, null);
            if (!ModelState.IsValid) return View("Form", model);

            _context.CongViecs.Add(new CongViec
            {
                TenCV = model.TenCV.Trim(),
                MoTa = string.IsNullOrWhiteSpace(model.MoTa) ? null : model.MoTa.Trim(),
                TrangThai = model.TrangThai
            });
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã thêm công việc {model.TenCV.Trim()}.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var cv = await _context.CongViecs.FindAsync(id);
            if (cv == null) return NotFound();

            return View("Form", new CongViecFormViewModel
            {
                MaCV = cv.MaCV,
                TenCV = cv.TenCV,
                MoTa = cv.MoTa,
                TrangThai = cv.TrangThai
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CongViecFormViewModel model)
        {
            var cv = await _context.CongViecs.FindAsync(model.MaCV);
            if (cv == null) return NotFound();

            await KiemTraAsync(model, cv);
            if (!ModelState.IsValid) return View("Form", model);

            cv.TenCV = model.TenCV.Trim();
            cv.MoTa = string.IsNullOrWhiteSpace(model.MoTa) ? null : model.MoTa.Trim();
            cv.TrangThai = model.TrangThai;
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã cập nhật công việc {cv.TenCV}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var cv = await _context.CongViecs.FindAsync(id);
            if (cv == null) return NotFound();

            if (await _context.PhanCongs.AnyAsync(p => p.MaCV == id))
            {
                TempData["Loi"] = $"Công việc {cv.TenCV} đã được phân công cho nhân viên nên không xóa được. " +
                                  $"Hãy đổi trạng thái sang \"{TrangThaiKho.Ngung}\".";
                return RedirectToAction(nameof(Index));
            }

            _context.CongViecs.Remove(cv);
            await _context.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã xóa công việc {cv.TenCV}.";
            return RedirectToAction(nameof(Index));
        }

        private async Task KiemTraAsync(CongViecFormViewModel model, CongViec? hienTai)
        {
            if (!TrangThaiKho.TatCa.Contains(model.TrangThai) && model.TrangThai != hienTai?.TrangThai)
                ModelState.AddModelError(nameof(model.TrangThai), "Trạng thái không hợp lệ.");

            var ten = model.TenCV?.Trim() ?? "";
            if (ten.Length > 0 && await _context.CongViecs.AnyAsync(c => c.TenCV == ten && c.MaCV != model.MaCV))
                ModelState.AddModelError(nameof(model.TenCV), $"Công việc \"{ten}\" đã tồn tại.");
        }
    }
}
