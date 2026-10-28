using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Controllers
{
    // Danh mục món ăn (Khai vị, Món chính...): chỉ quản lý
    [Authorize(Roles = VaiTro.QuanLy)]
    public class QuanLyDanhMucController : Controller
    {
        private readonly QuanLyNhaHangContext _context;

        public QuanLyDanhMucController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        // GET: /QuanLyDanhMuc
        public async Task<IActionResult> Index()
        {
            var danhMucs = await _context.DanhMucs.OrderBy(d => d.MaDanhMuc).ToListAsync();

            var soMon = await _context.MonAns
                .Where(m => m.MaDanhMuc != null)
                .GroupBy(m => m.MaDanhMuc!.Value)
                .Select(g => new { Ma = g.Key, So = g.Count() })
                .ToDictionaryAsync(x => x.Ma, x => x.So);

            return View(danhMucs.Select(d => new DanhMucDong
            {
                DanhMuc = d,
                SoMon = soMon.GetValueOrDefault(d.MaDanhMuc)
            }).ToList());
        }

        public IActionResult Create() => View("Form", new DanhMucFormViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DanhMucFormViewModel model)
        {
            await KiemTraAsync(model, null);
            if (!ModelState.IsValid) return View("Form", model);

            // Cột MaDanhMuc không tự tăng (EF cấu hình ValueGeneratedNever) nên tự lấy mã lớn nhất + 1
            var maMoi = (await _context.DanhMucs.MaxAsync(d => (int?)d.MaDanhMuc) ?? 0) + 1;
            _context.DanhMucs.Add(new DanhMuc
            {
                MaDanhMuc = maMoi,
                TenDanhMuc = model.TenDanhMuc.Trim(),
                MoTa = string.IsNullOrWhiteSpace(model.MoTa) ? null : model.MoTa.Trim(),
                TrangThai = model.TrangThai
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Không lưu được danh mục (có thể có người khác vừa thêm cùng lúc), vui lòng thử lại.");
                return View("Form", model);
            }

            TempData["ThongBao"] = $"Đã thêm danh mục {model.TenDanhMuc.Trim()}.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var dm = await _context.DanhMucs.FindAsync(id);
            if (dm == null) return NotFound();

            return View("Form", new DanhMucFormViewModel
            {
                MaDanhMuc = dm.MaDanhMuc,
                TenDanhMuc = dm.TenDanhMuc,
                MoTa = dm.MoTa,
                TrangThai = dm.TrangThai
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DanhMucFormViewModel model)
        {
            var dm = await _context.DanhMucs.FindAsync(model.MaDanhMuc);
            if (dm == null) return NotFound();

            await KiemTraAsync(model, dm);
            if (!ModelState.IsValid) return View("Form", model);

            dm.TenDanhMuc = model.TenDanhMuc.Trim();
            dm.MoTa = string.IsNullOrWhiteSpace(model.MoTa) ? null : model.MoTa.Trim();
            dm.TrangThai = model.TrangThai;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Không lưu được thay đổi, vui lòng thử lại.");
                return View("Form", model);
            }

            TempData["ThongBao"] = $"Đã cập nhật danh mục {dm.TenDanhMuc}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var dm = await _context.DanhMucs.FindAsync(id);
            if (dm == null) return NotFound();

            if (await _context.MonAns.AnyAsync(m => m.MaDanhMuc == id))
            {
                TempData["Loi"] = $"Danh mục {dm.TenDanhMuc} còn món ăn nên không xóa được. " +
                                  $"Hãy chuyển các món sang danh mục khác hoặc đổi trạng thái sang \"{TrangThaiKho.Ngung}\".";
                return RedirectToAction(nameof(Index));
            }

            _context.DanhMucs.Remove(dm);
            await _context.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã xóa danh mục {dm.TenDanhMuc}.";
            return RedirectToAction(nameof(Index));
        }

        private async Task KiemTraAsync(DanhMucFormViewModel model, DanhMuc? hienTai)
        {
            if (!TrangThaiKho.TatCa.Contains(model.TrangThai) && model.TrangThai != hienTai?.TrangThai)
                ModelState.AddModelError(nameof(model.TrangThai), "Trạng thái không hợp lệ.");

            var ten = model.TenDanhMuc?.Trim() ?? "";
            if (ten.Length > 0 && await _context.DanhMucs.AnyAsync(d => d.TenDanhMuc == ten && d.MaDanhMuc != model.MaDanhMuc))
                ModelState.AddModelError(nameof(model.TenDanhMuc), $"Danh mục \"{ten}\" đã tồn tại.");
        }
    }
}
