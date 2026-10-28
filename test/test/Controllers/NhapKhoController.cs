using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Controllers
{
    // Nhập kho: nhân viên và quản lý đều lập được phiếu nhập
    [Authorize(Roles = VaiTro.QuanLy + "," + VaiTro.NhanVien)]
    public class NhapKhoController : Controller
    {
        private readonly QuanLyNhaHangContext _context;

        public NhapKhoController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        // GET: /NhapKho?tu=2026-10-01&den=2026-10-31
        public async Task<IActionResult> Index(DateOnly? tu, DateOnly? den)
        {
            var query = _context.PhieuNhaps.Include(p => p.NhanVien).Include(p => p.ChiTiets).AsQueryable();

            if (tu != null) query = query.Where(p => p.NgayNhap >= tu.Value.ToDateTime(TimeOnly.MinValue));
            if (den != null) query = query.Where(p => p.NgayNhap < den.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));

            var danhSach = await query.OrderByDescending(p => p.NgayNhap).Take(100).ToListAsync();

            ViewBag.Tu = tu?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.Den = den?.ToString("yyyy-MM-dd") ?? "";
            return View(danhSach);
        }

        // GET: /NhapKho/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var phieu = await _context.PhieuNhaps
                .Include(p => p.NhanVien)
                .Include(p => p.ChiTiets).ThenInclude(c => c.NguyenLieu)
                .FirstOrDefaultAsync(p => p.MaNhap == id);
            if (phieu == null) return NotFound();

            return View(phieu);
        }

        // GET: /NhapKho/Tao
        public async Task<IActionResult> Tao()
        {
            var model = new PhieuNhapFormViewModel();
            await NapDanhSachAsync(model);
            return View(model);
        }

        // POST: /NhapKho/Tao
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Tao(PhieuNhapFormViewModel model)
        {
            // Bỏ các dòng để trống hoàn toàn (do bấm "Thêm dòng" mà không điền)
            var dong = (model.Dong ?? new()).Where(d => d.MaNL != null || d.SoLuong != null
                                                      || d.DonGia != null || d.NgayHetHan != null).ToList();

            var maNV = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var ma) ? ma : 0;
            if (maNV == 0 || !await _context.NhanViens.AnyAsync(n => n.MaNV == maNV))
                return Forbid();

            await KiemTraAsync(model, dong);
            if (!ModelState.IsValid)
            {
                model.Dong = dong.Count > 0 ? dong : new() { new PhieuNhapDong() };
                await NapDanhSachAsync(model);
                return View(model);
            }

            var ngay = model.NgayNhap == DateOnly.FromDateTime(DateTime.Today)
                ? DateTime.Now
                : model.NgayNhap.ToDateTime(TimeOnly.MinValue);

            var phieu = new PhieuNhap { MaNV = maNV, NgayNhap = ngay };
            foreach (var d in dong)
            {
                var soLuong = d.SoLuong!.Value;
                var donGia = d.DonGia!.Value;

                phieu.ChiTiets.Add(new CtKhoNhap { MaNL = d.MaNL!.Value, SoLuong = soLuong, DonGia = donGia });

                // Mỗi dòng nhập thành một lô mới trong kho (để theo dõi hạn dùng từng lô)
                _context.TonKhos.Add(new TonKho
                {
                    MaKho = model.MaKho!.Value,
                    MaNL = d.MaNL.Value,
                    SoLuongTon = soLuong,
                    NgayNhap = model.NgayNhap,
                    NgayHetHan = d.NgayHetHan,
                    TrangThai = TrangThaiTonKho.TuSoLuong(soLuong)
                });
            }
            phieu.TongTien = phieu.ChiTiets.Sum(c => Math.Round(c.SoLuong * c.DonGia, 0, MidpointRounding.AwayFromZero));
            _context.PhieuNhaps.Add(phieu);

            try
            {
                // Một lần SaveChanges = một giao dịch: phiếu và tồn kho cùng thành công hoặc cùng thất bại
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Không lưu được phiếu nhập, vui lòng thử lại.");
                model.Dong = dong;
                await NapDanhSachAsync(model);
                return View(model);
            }

            TempData["ThongBao"] = $"Đã lập phiếu nhập #{phieu.MaNhap} ({phieu.TongTien:N0} đ) và cộng vào tồn kho.";
            return RedirectToAction(nameof(Details), new { id = phieu.MaNhap });
        }

        // ---- Hàm phụ ----

        private async Task NapDanhSachAsync(PhieuNhapFormViewModel model)
        {
            model.DanhSachKho = await _context.Khos
                .Where(k => k.TrangThai == TrangThaiKho.HoatDong)
                .OrderBy(k => k.TenKho)
                .Select(k => new SelectListItem(k.TenKho, k.MaKho.ToString()))
                .ToListAsync();

            model.DanhSachNguyenLieu = await _context.NguyenLieus
                .Where(n => n.TrangThai == TrangThaiKho.HoatDong)
                .OrderBy(n => n.TenNL)
                .ToListAsync();
        }

        private async Task KiemTraAsync(PhieuNhapFormViewModel model, List<PhieuNhapDong> dong)
        {
            if (model.MaKho == null ||
                !await _context.Khos.AnyAsync(k => k.MaKho == model.MaKho && k.TrangThai == TrangThaiKho.HoatDong))
                ModelState.AddModelError("", "Vui lòng chọn kho đang hoạt động.");

            if (dong.Count == 0)
            {
                ModelState.AddModelError("", "Phiếu nhập cần ít nhất một nguyên liệu.");
                return;
            }

            var idNguyenLieu = dong.Where(d => d.MaNL != null).Select(d => d.MaNL!.Value).Distinct().ToList();
            var coThat = await _context.NguyenLieus
                .Where(n => idNguyenLieu.Contains(n.MaNL))
                .ToDictionaryAsync(n => n.MaNL);

            for (var i = 0; i < dong.Count; i++)
            {
                var d = dong[i];
                var so = i + 1;

                if (d.MaNL == null) { ModelState.AddModelError("", $"Dòng {so}: chưa chọn nguyên liệu."); continue; }
                if (!coThat.TryGetValue(d.MaNL.Value, out var nl) || nl.TrangThai != TrangThaiKho.HoatDong)
                {
                    ModelState.AddModelError("", $"Dòng {so}: nguyên liệu không tồn tại hoặc đã ngừng sử dụng.");
                    continue;
                }

                if (d.SoLuong == null || d.SoLuong <= 0 || d.SoLuong > 1_000_000_000m
                    || d.SoLuong != Math.Round(d.SoLuong.Value, 2))
                    ModelState.AddModelError("", $"Dòng {so} ({nl.TenNL}): số lượng phải lớn hơn 0 và tối đa 2 chữ số thập phân.");

                if (d.DonGia == null || d.DonGia < 0 || d.DonGia > 1_000_000_000_000m
                    || d.DonGia != Math.Round(d.DonGia.Value))
                    ModelState.AddModelError("", $"Dòng {so} ({nl.TenNL}): đơn giá phải là số nguyên không âm (đồng).");

                if (d.NgayHetHan != null && d.NgayHetHan < model.NgayNhap)
                    ModelState.AddModelError("", $"Dòng {so} ({nl.TenNL}): hạn dùng không được trước ngày nhập.");
            }

            // Khóa của chi tiết phiếu là (phiếu, nguyên liệu) nên một phiếu không có 2 dòng cùng nguyên liệu
            foreach (var trung in dong.Where(d => d.MaNL != null).GroupBy(d => d.MaNL!.Value).Where(g => g.Count() > 1))
            {
                var ten = coThat.TryGetValue(trung.Key, out var n) ? n.TenNL : $"#{trung.Key}";
                ModelState.AddModelError("", $"Nguyên liệu {ten} xuất hiện nhiều lần. Hãy gộp thành một dòng, " +
                                             "hoặc lập phiếu riêng nếu hạn dùng khác nhau.");
            }
        }
    }
}
