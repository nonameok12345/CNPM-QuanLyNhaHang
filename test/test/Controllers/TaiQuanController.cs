using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Controllers
{
    // Khách NGỒI TẠI QUÁN quét QR trên bàn để mở bàn và gọi món. Không cần đăng nhập.
    // {token} là mã QR riêng của từng bàn (cột Ban.MaQR), nên khách chỉ gọi được cho đúng bàn mình quét.
    //
    // Món gọi được ghi vào HÓA ĐƠN CHƯA THANH TOÁN của bàn (giống nhân viên gọi món),
    // nên nhân viên thấy ngay ở trang Quản lý hóa đơn và thanh toán như bình thường.
    [Route("ban/{token:guid}")]
    public class TaiQuanController : Controller
    {
        private readonly QuanLyNhaHangContext _context;

        public TaiQuanController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        // GET: /ban/{token} — trang đầu khi quét QR. Bàn đã mở thì vào thẳng thực đơn.
        [HttpGet("")]
        public async Task<IActionResult> Index(Guid token)
        {
            var phien = await TimPhienAsync(token);
            if (phien == null) return KhongTimThay();

            if (phien.TrangThai == TrangThaiPhienBan.DaCoPhien)
                return RedirectToAction(nameof(Menu), new { token });

            return View(phien);
        }

        // POST: /ban/{token}/mo — khách bấm "Ngồi bàn này": mở bàn (tạo hóa đơn rỗng cho bàn)
        [HttpPost("mo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoBan(Guid token)
        {
            // Dùng chung khóa với đặt bàn từ xa: hai bên không thể cùng lấy một bàn ở cùng lúc
            await KhoaDatBan.Khoa.WaitAsync();
            try
            {
                var phien = await TimPhienAsync(token);
                if (phien == null) return KhongTimThay();

                if (phien.TrangThai == TrangThaiPhienBan.CoTheMo)
                {
                    _context.HoaDons.Add(new HoaDon
                    {
                        MaBan = phien.Ban.MaBan,
                        // Khách đặt trước đã tới: gắn hóa đơn vào lịch đặt đó. Khách vãng lai: để trống.
                        MaDatBan = phien.DatBan?.MaDatBan,
                        MaKH = phien.DatBan?.MaKH,
                        NgayLap = DateTime.Now,
                        TrangThai = TrangThaiHoaDon.ChuaThanhToan
                    });
                    await _context.SaveChangesAsync();
                }
            }
            catch (DbUpdateException)
            {
                TempData["Loi"] = "Không mở được bàn, vui lòng thử lại hoặc gọi nhân viên.";
                return RedirectToAction(nameof(Index), new { token });
            }
            finally
            {
                KhoaDatBan.Khoa.Release();
            }

            // Nếu bàn không mở được (vd vừa có người đặt), Menu sẽ chuyển khách về trang đầu để đọc thông báo
            return RedirectToAction(nameof(Menu), new { token });
        }

        // GET: /ban/{token}/menu — thực đơn, lọc theo danh mục / từ khóa
        [HttpGet("menu")]
        public async Task<IActionResult> Menu(Guid token, string? tuKhoa, int? maDanhMuc)
        {
            var (phien, chuyenDen) = await YeuCauBanDaMoAsync(token);
            if (chuyenDen != null) return chuyenDen;

            var monAns = _context.MonAns.Include(m => m.MaDanhMucNavigation).AsQueryable();

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                tuKhoa = tuKhoa.Trim();
                monAns = monAns.Where(m => m.TenMon.Contains(tuKhoa));
            }
            if (maDanhMuc.HasValue)
                monAns = monAns.Where(m => m.MaDanhMuc == maDanhMuc.Value);

            var gio = GioTaiQuanSession.Doc(HttpContext.Session, token);

            return View(new MenuTaiQuanViewModel
            {
                Ban = phien!.Ban,
                MonAns = await monAns.OrderBy(m => m.MaDanhMuc).ThenBy(m => m.TenMon).ToListAsync(),
                DanhMucs = await _context.DanhMucs.OrderBy(d => d.TenDanhMuc).ToListAsync(),
                MaDanhMuc = maDanhMuc,
                TuKhoa = tuKhoa,
                SoMonTrongGio = gio.Values.Sum(),
                TongGio = await TinhTongGioAsync(gio)
            });
        }

        // POST: /ban/{token}/them — bấm "+ Thêm" ở một món
        [HttpPost("them")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Them(Guid token, int maMon, int soLuong = 1, string? tuKhoa = null, int? maDanhMuc = null)
        {
            if (!await _context.Bans.AnyAsync(b => b.MaQR == token)) return KhongTimThay();

            var mon = await _context.MonAns.FindAsync(maMon);
            if (mon == null) return NotFound();

            soLuong = Math.Clamp(soLuong, 1, 20);

            var gio = GioTaiQuanSession.Doc(HttpContext.Session, token);
            gio[maMon] = Math.Min(gio.GetValueOrDefault(maMon) + soLuong, 50);
            GioTaiQuanSession.Luu(HttpContext.Session, token, gio);

            TempData["ThongBao"] = $"Đã thêm {soLuong} × {mon.TenMon} vào giỏ";

            // Quay lại đúng trang thực đơn đang xem và cuộn tới món vừa chọn
            return RedirectToAction(nameof(Menu), null, new { token, tuKhoa, maDanhMuc }, $"mon-{maMon}");
        }

        // GET: /ban/{token}/gio — xem giỏ món trước khi gửi
        [HttpGet("gio")]
        public async Task<IActionResult> Gio(Guid token)
        {
            var (phien, chuyenDen) = await YeuCauBanDaMoAsync(token);
            if (chuyenDen != null) return chuyenDen;

            var gio = GioTaiQuanSession.Doc(HttpContext.Session, token);
            var maMons = gio.Keys.ToList();
            var monAns = await _context.MonAns.Where(m => maMons.Contains(m.MaMon)).ToListAsync();

            // Món đã bị quản lý xóa khỏi thực đơn thì bỏ khỏi giỏ
            var conTonTai = monAns.Select(m => m.MaMon).ToHashSet();
            var biXoa = maMons.Where(ma => !conTonTai.Contains(ma)).ToList();
            if (biXoa.Count > 0)
            {
                foreach (var ma in biXoa) gio.Remove(ma);
                GioTaiQuanSession.Luu(HttpContext.Session, token, gio);
            }

            var vm = new GioTaiQuanViewModel { Ban = phien!.Ban };
            foreach (var m in monAns)
            {
                vm.Dong.Add(new DongGioHang
                {
                    MaMon = m.MaMon,
                    TenMon = m.TenMon,
                    HinhAnh = m.HinhAnh,
                    Gia = m.Gia ?? 0,
                    SoLuong = gio[m.MaMon]
                });
            }
            return View(vm);
        }

        // POST: /ban/{token}/gio/capnhat — đổi số lượng (về 0 là xóa)
        [HttpPost("gio/capnhat")]
        [ValidateAntiForgeryToken]
        public IActionResult CapNhat(Guid token, int maMon, int soLuong)
        {
            var gio = GioTaiQuanSession.Doc(HttpContext.Session, token);
            if (soLuong <= 0)
                gio.Remove(maMon);
            else if (gio.ContainsKey(maMon))
                gio[maMon] = Math.Min(soLuong, 50);

            GioTaiQuanSession.Luu(HttpContext.Session, token, gio);
            return RedirectToAction(nameof(Gio), new { token });
        }

        // POST: /ban/{token}/gio/xoa — bỏ một món khỏi giỏ
        [HttpPost("gio/xoa")]
        [ValidateAntiForgeryToken]
        public IActionResult Xoa(Guid token, int maMon)
        {
            var gio = GioTaiQuanSession.Doc(HttpContext.Session, token);
            gio.Remove(maMon);
            GioTaiQuanSession.Luu(HttpContext.Session, token, gio);
            return RedirectToAction(nameof(Gio), new { token });
        }

        // POST: /ban/{token}/gui — gửi giỏ món vào hóa đơn của bàn
        [HttpPost("gui")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuiMon(Guid token)
        {
            var (phien, chuyenDen) = await YeuCauBanDaMoAsync(token);
            if (chuyenDen != null)
            {
                // Vd nhân viên vừa thanh toán hóa đơn của bàn trong lúc khách đang chọn món
                TempData["Loi"] = "Bàn chưa mở hoặc hóa đơn đã thanh toán. Vui lòng mở lại bàn hoặc gọi nhân viên.";
                return chuyenDen;
            }

            var hoaDon = phien!.HoaDon!;   // đã nạp kèm chi tiết món

            var gio = GioTaiQuanSession.Doc(HttpContext.Session, token);
            if (gio.Count == 0) return RedirectToAction(nameof(Gio), new { token });

            // Lấy giá từ database, KHÔNG tin giá do trình duyệt gửi lên
            var maMons = gio.Keys.ToList();
            var monAns = await _context.MonAns
                .Where(m => maMons.Contains(m.MaMon))
                .ToDictionaryAsync(m => m.MaMon);

            var soMonDaThem = 0;
            foreach (var (maMon, soLuong) in gio)
            {
                if (!monAns.TryGetValue(maMon, out var mon)) continue;   // món đã bị xóa khỏi thực đơn

                var chiTiet = hoaDon.ChiTietHoaDons.FirstOrDefault(c => c.MaMon == maMon);
                if (chiTiet == null)
                {
                    hoaDon.ChiTietHoaDons.Add(new ChiTietHoaDon
                    {
                        MaMon = maMon,
                        SoLuong = soLuong,
                        DonGia = mon.Gia ?? 0   // giá chốt tại lúc gọi
                    });
                }
                else
                {
                    chiTiet.SoLuong = Math.Min(chiTiet.SoLuong + soLuong, 99);
                }
                soMonDaThem++;
            }

            if (soMonDaThem == 0)
            {
                GioTaiQuanSession.Xoa(HttpContext.Session, token);
                TempData["Loi"] = "Các món trong giỏ không còn trong thực đơn.";
                return RedirectToAction(nameof(Menu), new { token });
            }

            hoaDon.TongTien = hoaDon.ChiTietHoaDons.Sum(c => c.SoLuong * c.DonGia);

            try
            {
                await _context.SaveChangesAsync();   // lưu chi tiết món + tổng tiền trong một lần
            }
            catch (DbUpdateException)
            {
                TempData["Loi"] = "Không gửi được món, vui lòng thử lại hoặc gọi nhân viên.";
                return RedirectToAction(nameof(Gio), new { token });
            }

            GioTaiQuanSession.Xoa(HttpContext.Session, token);
            TempData["ThongBao"] = "Đã gửi món! Nhà hàng sẽ phục vụ bạn sớm nhất.";
            return RedirectToAction(nameof(DaGoi), new { token });
        }

        // GET: /ban/{token}/da-goi — các món cả bàn đã gọi và tổng tiền tạm tính
        [HttpGet("da-goi")]
        public async Task<IActionResult> DaGoi(Guid token)
        {
            var (phien, chuyenDen) = await YeuCauBanDaMoAsync(token);
            if (chuyenDen != null) return chuyenDen;

            return View(new DaGoiViewModel { Ban = phien!.Ban, HoaDon = phien.HoaDon! });
        }

        // ---- Hàm phụ ----

        private IActionResult KhongTimThay()
        {
            Response.StatusCode = 404;
            return View("KhongTimThay");
        }

        // Các trang gọi món chỉ dùng được khi bàn đã mở. Chưa mở thì chuyển về trang đầu (Index).
        // Trả về (phien, null) nếu được vào; (_, kết quả chuyển trang) nếu không.
        private async Task<(PhienBanTaiQuan? Phien, IActionResult? ChuyenDen)> YeuCauBanDaMoAsync(Guid token)
        {
            var phien = await TimPhienAsync(token);
            if (phien == null) return (null, KhongTimThay());

            if (phien.TrangThai != TrangThaiPhienBan.DaCoPhien || phien.HoaDon == null)
                return (phien, RedirectToAction(nameof(Index), new { token }));

            return (phien, null);
        }

        private async Task<decimal> TinhTongGioAsync(Dictionary<int, int> gio)
        {
            if (gio.Count == 0) return 0;
            var maMons = gio.Keys.ToList();
            var gia = await _context.MonAns
                .Where(m => maMons.Contains(m.MaMon))
                .ToDictionaryAsync(m => m.MaMon, m => m.Gia ?? 0);
            return gio.Sum(g => gia.GetValueOrDefault(g.Key) * g.Value);
        }

        // Xác định bàn của mã QR đang ở tình trạng nào. Thứ tự kiểm tra:
        //   1) Có lịch đặt trước đang tới giờ  -> bàn thuộc về khách đặt đó
        //   2) Có hóa đơn khách vãng lai chưa thanh toán -> bàn đang có khách
        //   3) Sắp có lịch đặt chồng với 2 tiếng ăn -> chưa cho mở
        //   4) Còn lại -> bàn trống, mở được
        // Trả về null nếu mã QR không thuộc bàn nào (mã sai hoặc nhân viên đã đổi mã).
        private async Task<PhienBanTaiQuan?> TimPhienAsync(Guid token)
        {
            var ban = await _context.Bans.FirstOrDefaultAsync(b => b.MaQR == token);
            if (ban == null) return null;

            var phien = new PhienBanTaiQuan { Ban = ban };

            if (ban.TrangThai == TrangThaiBan.NgungSuDung)
            {
                phien.TrangThai = TrangThaiPhienBan.NgungSuDung;
                return phien;
            }

            var bayGio = DateTime.Now;
            var gioSom = bayGio.AddMinutes(QuyDinhDatBan.KhachDenSomPhut);     // lịch bắt đầu trong 15 phút tới coi như "khách đã tới"
            var hetGiu = bayGio.AddHours(-QuyDinhDatBan.ThoiLuongGio);         // lịch bắt đầu quá 2 tiếng trước thì đã xong

            // 1) Lịch đặt trước của bàn này đang tới giờ hoặc đang diễn ra
            var datBan = await _context.DatBans
                .Where(d => d.MaBan == ban.MaBan
                         && d.TrangThai != TrangThaiDatBan.DaHuy
                         && d.NgayGio <= gioSom && d.NgayGio > hetGiu)
                .OrderBy(d => d.NgayGio)
                .FirstOrDefaultAsync();
            if (datBan != null)
            {
                phien.DatBan = datBan;
                phien.HoaDon = await _context.HoaDons
                    .Include(h => h.ChiTietHoaDons).ThenInclude(c => c.MonAn)
                    .FirstOrDefaultAsync(h => h.MaDatBan == datBan.MaDatBan
                                           && h.TrangThai == TrangThaiHoaDon.ChuaThanhToan);
                phien.TrangThai = phien.HoaDon != null ? TrangThaiPhienBan.DaCoPhien : TrangThaiPhienBan.CoTheMo;
                return phien;
            }

            // 2) Khách vãng lai đã mở bàn (hóa đơn chưa thanh toán, không gắn lịch đặt).
            //    Chỉ xét hóa đơn trong vài giờ gần đây để hóa đơn cũ bị quên đóng không nuốt mất bàn.
            var tuLuc = bayGio.AddHours(-QuyDinhDatBan.PhienTaiQuanToiDaGio);
            var hoaDon = await _context.HoaDons
                .Include(h => h.ChiTietHoaDons).ThenInclude(c => c.MonAn)
                .Where(h => h.MaBan == ban.MaBan
                         && h.MaDatBan == null
                         && h.TrangThai == TrangThaiHoaDon.ChuaThanhToan
                         && h.NgayLap > tuLuc)
                .OrderByDescending(h => h.NgayLap)
                .FirstOrDefaultAsync();
            if (hoaDon != null)
            {
                phien.HoaDon = hoaDon;
                phien.TrangThai = TrangThaiPhienBan.DaCoPhien;
                return phien;
            }

            // 3) Bàn chưa có khách, nhưng sắp có lịch đặt rơi vào 2 tiếng tới (cùng quy tắc với đặt bàn từ xa)
            var denLuc = bayGio.AddHours(QuyDinhDatBan.ThoiLuongGio);
            var lichSapToi = await _context.DatBans
                .Where(d => d.MaBan == ban.MaBan
                         && d.TrangThai != TrangThaiDatBan.DaHuy
                         && d.NgayGio > gioSom && d.NgayGio < denLuc)
                .OrderBy(d => d.NgayGio)
                .Select(d => (DateTime?)d.NgayGio)
                .FirstOrDefaultAsync();
            if (lichSapToi != null)
            {
                phien.GioDatKeTiep = lichSapToi;
                phien.TrangThai = TrangThaiPhienBan.BanDaDat;
                return phien;
            }

            // 4) Bàn trống
            phien.TrangThai = TrangThaiPhienBan.CoTheMo;
            return phien;
        }
    }
}
