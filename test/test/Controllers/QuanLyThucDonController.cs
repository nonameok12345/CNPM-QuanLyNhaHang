using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Controllers
{
    // Khu vực dành cho quản lý: thêm / sửa / xóa món ăn.
    // Lưu ý: hiện chưa có đăng nhập nên ai biết đường dẫn vẫn vào được.
    // Khi làm xong đăng nhập, thêm [Authorize] ngay dưới đây để khóa lại.
    [Authorize(Roles = VaiTro.QuanLy)]
    public class QuanLyThucDonController : Controller
    {
        private readonly QuanLyNhaHangContext _context;
        private static readonly string[] DuoiAnhHopLe = { ".jpg", ".jpeg", ".png", ".webp" };

        public QuanLyThucDonController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        // GET: /QuanLyThucDon — bảng danh sách món, lọc được theo danh mục
        public async Task<IActionResult> Index(int? maDanhMuc)
        {
            var query = _context.MonAns
                .Include(m => m.MaDanhMucNavigation)
                .AsQueryable();

            if (maDanhMuc.HasValue)
            {
                query = query.Where(m => m.MaDanhMuc == maDanhMuc.Value);
            }

            ViewBag.MaDanhMuc = maDanhMuc;
            ViewBag.DanhMucList = await _context.DanhMucs.ToListAsync();

            return View(await query.OrderBy(m => m.MaDanhMuc).ThenBy(m => m.TenMon).ToListAsync());
        }

        // GET: /QuanLyThucDon/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.DanhMucList = await _context.DanhMucs.ToListAsync();
            return View();
        }

        // POST: /QuanLyThucDon/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TenMon,Gia,MaDanhMuc")] MonAn monAn, IFormFile? anhUpload)
        {
            KiemTraAnh(anhUpload);
            KiemTraMon(monAn);

            if (ModelState.IsValid)
            {
                var duongDanAnh = await LuuAnhAsync(anhUpload);
                if (duongDanAnh != null) monAn.HinhAnh = duongDanAnh;

                _context.Add(monAn);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.DanhMucList = await _context.DanhMucs.ToListAsync();
            return View(monAn);
        }

        // GET: /QuanLyThucDon/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var monAn = await _context.MonAns.FindAsync(id);
            if (monAn == null) return NotFound();

            ViewBag.DanhMucList = await _context.DanhMucs.ToListAsync();
            return View(monAn);
        }

        // POST: /QuanLyThucDon/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaMon,TenMon,Gia,MaDanhMuc,HinhAnh")] MonAn monAn, IFormFile? anhUpload)
        {
            if (id != monAn.MaMon) return NotFound();

            KiemTraAnh(anhUpload);
            KiemTraMon(monAn);

            if (ModelState.IsValid)
            {
                var duongDanAnh = await LuuAnhAsync(anhUpload);
                if (duongDanAnh != null) monAn.HinhAnh = duongDanAnh;   // không chọn ảnh mới thì giữ ảnh cũ

                _context.Update(monAn);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.DanhMucList = await _context.DanhMucs.ToListAsync();
            return View(monAn);
        }

        // GET: /QuanLyThucDon/Delete/5 — trang xác nhận xóa
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var monAn = await _context.MonAns
                .Include(m => m.MaDanhMucNavigation)
                .FirstOrDefaultAsync(m => m.MaMon == id);
            if (monAn == null) return NotFound();

            return View(monAn);
        }

        // POST: /QuanLyThucDon/Delete/5 — xóa thật
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var monAn = await _context.MonAns.FindAsync(id);
            if (monAn == null) return RedirectToAction(nameof(Index));

            // Món đã nằm trong hóa đơn thì database không cho xóa (khóa ngoại), nên báo rõ thay vì để web lỗi
            if (await _context.ChiTietHoaDons.AnyAsync(c => c.MaMon == id))
            {
                TempData["Loi"] = $"Món {monAn.TenMon} đã có trong hóa đơn nên không xóa được.";
                return RedirectToAction(nameof(Index));
            }

            // Công thức của món (nếu có) đi theo món bị xóa
            var congThuc = await _context.CtNguyenLieus.Where(c => c.MaMon == id).ToListAsync();
            _context.CtNguyenLieus.RemoveRange(congThuc);
            _context.MonAns.Remove(monAn);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["Loi"] = $"Không xóa được món {monAn.TenMon} vì còn dữ liệu liên quan.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ThongBao"] = $"Đã xóa món {monAn.TenMon}.";
            return RedirectToAction(nameof(Index));
        }

        // ---- Hàm phụ dùng chung cho Create và Edit ----

        // Tên và giá phải khớp cột trong database (MonAn.TenMon nvarchar(100), Gia decimal(10,2) và CHECK Gia >= 0).
        // ChiTietHoaDon.DonGia là decimal(18,0) nên giá có số lẻ sẽ bị làm tròn và lệch tổng tiền.
        private void KiemTraMon(MonAn monAn)
        {
            if ((monAn.TenMon?.Trim().Length ?? 0) > 100)
                ModelState.AddModelError(nameof(MonAn.TenMon), "Tên món tối đa 100 ký tự.");

            // Ô giá nhập sai định dạng (chữ, v.v.) đã có lỗi riêng từ model binding, không báo thêm
            if (ModelState.GetFieldValidationState(nameof(MonAn.Gia))
                == Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid) return;

            if (monAn.Gia == null || monAn.Gia <= 0)
                ModelState.AddModelError(nameof(MonAn.Gia), "Giá món phải lớn hơn 0.");
            else if (monAn.Gia != Math.Round(monAn.Gia.Value))
                ModelState.AddModelError(nameof(MonAn.Gia), "Giá món phải là số nguyên (đồng).");
            else if (monAn.Gia > 99_999_999m)
                ModelState.AddModelError(nameof(MonAn.Gia), "Giá món tối đa 99.999.999 đ.");
        }

        // Chỉ nhận file ảnh, tránh việc upload file lạ vào thư mục web
        private void KiemTraAnh(IFormFile? anhUpload)
        {
            if (anhUpload != null && anhUpload.Length > 0)
            {
                var duoi = Path.GetExtension(anhUpload.FileName).ToLowerInvariant();
                if (!DuoiAnhHopLe.Contains(duoi))
                {
                    ModelState.AddModelError("", "Chỉ chấp nhận ảnh có đuôi .jpg, .jpeg, .png hoặc .webp");
                }
            }
        }

        // Lưu ảnh vào wwwroot/images/monan với tên ngẫu nhiên, trả về đường dẫn để lưu vào database
        private async Task<string?> LuuAnhAsync(IFormFile? anhUpload)
        {
            if (anhUpload == null || anhUpload.Length == 0) return null;

            var tenFile = Guid.NewGuid() + Path.GetExtension(anhUpload.FileName).ToLowerInvariant();
            var thuMucLuu = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "monan");
            Directory.CreateDirectory(thuMucLuu);

            using (var stream = new FileStream(Path.Combine(thuMucLuu, tenFile), FileMode.Create))
            {
                await anhUpload.CopyToAsync(stream);
            }
            return "/images/monan/" + tenFile;
        }
    }
}