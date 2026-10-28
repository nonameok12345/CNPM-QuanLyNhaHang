using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using test.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddDbContext<QuanLyNhaHangContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng nhập bằng cookie: chưa đăng nhập thì chuyển tới /Account/Login
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.HttpOnly = true;

        // Chưa đăng nhập: trang quản lý chuyển tới đăng nhập nhân viên, các trang còn lại tới đăng nhập khách hàng
        options.Events.OnRedirectToLogin = context =>
        {
            var trangDangNhap = new[] { "/QuanLyThucDon", "/QuanLyNhanVien", "/QuanLyKho", "/QuanLyNguyenLieu", "/QuanLyCongThuc", "/QuanLyDanhMuc", "/QuanLyKhachHang", "/QuanLyCongViec", "/ThongKe", "/QuanTri", "/TonKho", "/NhapKho" }
                                    .Any(p => context.Request.Path.StartsWithSegments(p))
                ? "/Account/Login"
                : "/TaiKhoan/DangNhap";
            var quayLai = Uri.EscapeDataString(context.Request.Path.Value + context.Request.QueryString.Value);
            context.Response.Redirect($"{trangDangNhap}?returnUrl={quayLai}");
            return Task.CompletedTask;
        };
    });

// Session: nơi lưu giỏ hàng của từng khách
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Mã hóa các mật khẩu nhân viên còn ở dạng chữ thường (chạy được nhiều lần, không sao)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<QuanLyNhaHangContext>();
        KhoiTaoDuLieu.MaHoaMatKhauCu(db);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Không mã hóa được mật khẩu nhân viên lúc khởi động");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Phục vụ file tĩnh được thêm lúc chạy (vd: ảnh món ăn người dùng upload vào wwwroot/images/monan)
// MapStaticAssets bên dưới chỉ biết các asset có sẵn lúc build (css/js/lib), không đủ cho file upload runtime
app.UseStaticFiles();

app.UseRouting();

app.UseSession();          // phải đứng sau UseRouting và trước các trang dùng giỏ hàng

app.UseAuthentication();   // phải đứng TRƯỚC UseAuthorization
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.Run();