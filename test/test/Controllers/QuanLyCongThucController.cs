using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Controllers
{
    // Công thức món: chỉ quản lý (quyết định mỗi món dùng bao nhiêu nguyên liệu)
    [Authorize(Roles = VaiTro.QuanLy)]
    public class QuanLyCongThucController : Controller
    {
        private readonly QuanLyNhaHangContext _context;

        public QuanLyCongThucController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        // GET: /QuanLyCongThuc?q=...&chuaCo=true
        public async Task<IActionResult> Index(string? q, bool chuaCo = false)
        {
            var query = _context.MonAns.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var tuKhoa = q.Trim();
                query = query.Where(m => m.TenMon.Contains(tuKhoa));
            }
            var monAns = await query.OrderBy(m => m.TenMon).ToListAsync();

            // Số nguyên liệu và giá vốn ước tính của từng món
            var ct = await _context.CtNguyenLieus
                .Select(c => new { c.MaMon, c.SoLuong, c.NguyenLieu.GiaNhap })
                .ToListAsync();
            var theoMon = ct.GroupBy(c => c.MaMon)
                .ToDictionary(g => g.Key, g => new { So = g.Count(), Von = g.Sum(x => x.SoLuong * x.GiaNhap) });

            var danhSach = monAns.Select(m => new CongThucMonDong
            {
                MonAn = m,
                SoNguyenLieu = theoMon.TryGetValue(m.MaMon, out var t) ? t.So : 0,
                GiaVon = theoMon.TryGetValue(m.MaMon, out var t2) ? t2.Von : 0
            }).ToList();

            if (chuaCo) danhSach = danhSach.Where(d => d.SoNguyenLieu == 0).ToList();

            ViewBag.TuKhoa = q ?? "";
            ViewBag.ChuaCo = chuaCo;
            return View(danhSach);
        }

        // GET: /QuanLyCongThuc/Edit/5 — sửa công thức của món có mã 5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var mon = await _context.MonAns.FindAsync(id);
            if (mon == null) return NotFound();

            var model = new CongThucFormViewModel { MaMon = mon.MaMon, TenMon = mon.TenMon };
            model.Dong = await _context.CtNguyenLieus
                .Where(c => c.MaMon == id)
                .OrderBy(c => c.NguyenLieu.TenNL)
                .Select(c => new CongThucDong { MaNL = c.MaNL, SoLuong = c.SoLuong })
                .ToListAsync();

            await NapDanhSachAsync(model);
            return View(model);
        }

        // POST: /QuanLyCongThuc/Edit — lưu toàn bộ công thức của món
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CongThucFormViewModel model)
        {
            var mon = await _context.MonAns.FindAsync(model.MaMon);
            if (mon == null) return NotFound();
            model.TenMon = mon.TenMon;

            // Bỏ các dòng để trống hoàn toàn
            var dong = (model.Dong ?? new()).Where(d => d.MaNL != null || d.SoLuong != null).ToList();

            await KiemTraAsync(dong);
            if (!ModelState.IsValid)
            {
                model.Dong = dong;
                await NapDanhSachAsync(model);
                return View(model);
            }

            // Cập nhật tại chỗ: dòng cũ sửa số lượng, dòng mới thêm, dòng không còn trong form thì xóa
            var hienCo = await _context.CtNguyenLieus.Where(c => c.MaMon == model.MaMon).ToListAsync();
            var giuLai = dong.Select(d => d.MaNL!.Value).ToHashSet();

            _context.CtNguyenLieus.RemoveRange(hienCo.Where(c => !giuLai.Contains(c.MaNL)));
            foreach (var d in dong)
            {
                var cu = hienCo.FirstOrDefault(c => c.MaNL == d.MaNL);
                if (cu != null) cu.SoLuong = d.SoLuong!.Value;
                else _context.CtNguyenLieus.Add(new CtNguyenLieu { MaMon = model.MaMon, MaNL = d.MaNL!.Value, SoLuong = d.SoLuong!.Value });
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Không lưu được công thức, vui lòng thử lại.");
                model.Dong = dong;
                await NapDanhSachAsync(model);
                return View(model);
            }

            TempData["ThongBao"] = dong.Count == 0
                ? $"Đã xóa công thức của món {mon.TenMon}."
                : $"Đã lưu công thức món {mon.TenMon} ({dong.Count} nguyên liệu).";
            return RedirectToAction(nameof(Index));
        }

        // ---- Hàm phụ ----

        private async Task NapDanhSachAsync(CongThucFormViewModel model)
        {
            // Giữ cả nguyên liệu đã ngừng sử dụng nếu công thức cũ còn dùng, để không mất dòng khi sửa
            var dangDung = model.Dong.Where(d => d.MaNL != null).Select(d => d.MaNL!.Value).ToList();
            model.DanhSachNguyenLieu = await _context.NguyenLieus
                .Where(n => n.TrangThai == TrangThaiKho.HoatDong || dangDung.Contains(n.MaNL))
                .OrderBy(n => n.TenNL)
                .ToListAsync();
        }

        private async Task KiemTraAsync(List<CongThucDong> dong)
        {
            var ids = dong.Where(d => d.MaNL != null).Select(d => d.MaNL!.Value).Distinct().ToList();
            var coThat = await _context.NguyenLieus.Where(n => ids.Contains(n.MaNL)).ToDictionaryAsync(n => n.MaNL);

            for (var i = 0; i < dong.Count; i++)
            {
                var d = dong[i];
                var so = i + 1;

                if (d.MaNL == null) { ModelState.AddModelError("", $"Dòng {so}: chưa chọn nguyên liệu."); continue; }
                if (!coThat.ContainsKey(d.MaNL.Value)) { ModelState.AddModelError("", $"Dòng {so}: nguyên liệu không tồn tại."); continue; }

                var ten = coThat[d.MaNL.Value].TenNL;
                // Cột SoLuong là decimal(18,3): tối đa 3 chữ số thập phân
                if (d.SoLuong == null || d.SoLuong <= 0 || d.SoLuong > 1_000_000m
                    || d.SoLuong != Math.Round(d.SoLuong.Value, 3))
                    ModelState.AddModelError("", $"Dòng {so} ({ten}): số lượng phải lớn hơn 0, tối đa 3 chữ số thập phân.");
            }

            foreach (var trung in dong.Where(d => d.MaNL != null).GroupBy(d => d.MaNL!.Value).Where(g => g.Count() > 1))
                ModelState.AddModelError("", $"Nguyên liệu {coThat.GetValueOrDefault(trung.Key)?.TenNL} xuất hiện nhiều lần, hãy gộp thành một dòng.");
        }
    }
}
