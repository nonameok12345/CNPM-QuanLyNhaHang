using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Controllers
{
    // Xem tồn kho: nhân viên và quản lý đều xem được
    [Authorize(Roles = VaiTro.QuanLy + "," + VaiTro.NhanVien)]
    public class TonKhoController : Controller
    {
        private readonly QuanLyNhaHangContext _context;

        public TonKhoController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        // GET: /TonKho?maKho=1&q=thịt&loc=saphethan
        // loc: con (mặc định, còn hàng) | saphethan | hethan | hethang | tatca
        public async Task<IActionResult> Index(int? maKho, string? q, string? loc)
        {
            loc = string.IsNullOrEmpty(loc) ? "con" : loc;
            var homNay = DateOnly.FromDateTime(DateTime.Today);
            var hanCanhBao = homNay.AddDays(QuyDinhKho.SapHetHanNgay);

            var query = _context.TonKhos
                .Include(t => t.Kho)
                .Include(t => t.NguyenLieu)
                .AsQueryable();

            if (maKho != null) query = query.Where(t => t.MaKho == maKho);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var tuKhoa = q.Trim();
                query = query.Where(t => t.NguyenLieu.TenNL.Contains(tuKhoa));
            }

            query = loc switch
            {
                "saphethan" => query.Where(t => t.SoLuongTon > 0 && t.NgayHetHan != null
                                                && t.NgayHetHan >= homNay && t.NgayHetHan <= hanCanhBao),
                "hethan" => query.Where(t => t.SoLuongTon > 0 && t.NgayHetHan != null && t.NgayHetHan < homNay),
                "hethang" => query.Where(t => t.SoLuongTon <= 0),
                "tatca" => query,
                _ => query.Where(t => t.SoLuongTon > 0)
            };

            var danhSach = await query
                .OrderBy(t => t.NguyenLieu.TenNL)
                .ThenBy(t => t.NgayHetHan)
                .Take(300)
                .ToListAsync();

            ViewBag.MaKho = maKho;
            ViewBag.TuKhoa = q ?? "";
            ViewBag.Loc = loc;
            ViewBag.DanhSachKho = await _context.Khos.OrderBy(k => k.TenKho).ToListAsync();
            return View(danhSach);
        }

        // GET: /TonKho/TongHop?maKho=1 — tổng tồn của từng nguyên liệu
        public async Task<IActionResult> TongHop(int? maKho)
        {
            var query = _context.TonKhos.Include(t => t.NguyenLieu).Where(t => t.SoLuongTon > 0);
            if (maKho != null) query = query.Where(t => t.MaKho == maKho);

            var lots = await query.ToListAsync();

            var danhSach = lots
                .GroupBy(t => t.MaNL)
                .Select(g => new TonKhoTongHopDong
                {
                    NguyenLieu = g.First().NguyenLieu,
                    TongTon = g.Sum(x => x.SoLuongTon),
                    SoLo = g.Count(),
                    HanGanNhat = g.Where(x => x.NgayHetHan != null).Select(x => x.NgayHetHan).Min()
                })
                .OrderBy(x => x.NguyenLieu.TenNL)
                .ToList();

            ViewBag.MaKho = maKho;
            ViewBag.DanhSachKho = await _context.Khos.OrderBy(k => k.TenKho).ToListAsync();
            return View(danhSach);
        }

        // POST: /TonKho/DieuChinh — sửa số lượng lô sau khi kiểm kê (hao hụt, xuất dùng...)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DieuChinh(int id,
            [ModelBinder(BinderType = typeof(DecimalInvariantBinder))] decimal soLuongMoi,
            int? maKho, string? q, string? loc)
        {
            var lo = await _context.TonKhos.Include(t => t.NguyenLieu).FirstOrDefaultAsync(t => t.MaTon == id);
            if (lo == null) return NotFound();

            var chuyenVe = RedirectToAction(nameof(Index), new { maKho, q, loc });

            if (!ModelState.IsValid || soLuongMoi < 0 || soLuongMoi > 1_000_000_000m
                || soLuongMoi != Math.Round(soLuongMoi, 2))
            {
                TempData["Loi"] = "Số lượng phải từ 0 đến 1.000.000.000 và tối đa 2 chữ số thập phân.";
                return chuyenVe;
            }

            lo.SoLuongTon = soLuongMoi;
            lo.TrangThai = TrangThaiTonKho.TuSoLuong(soLuongMoi);
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã cập nhật tồn của {lo.NguyenLieu.TenNL} thành {soLuongMoi:0.##} {lo.NguyenLieu.DonViTinh}.";
            return chuyenVe;
        }
    }
}
