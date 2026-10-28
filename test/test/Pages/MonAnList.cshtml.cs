using Microsoft.AspNetCore.Mvc.RazorPages;
using test.Models;

public class MonAnListModel : PageModel
{
    private readonly QuanLyNhaHangContext _context;
    public MonAnListModel(QuanLyNhaHangContext context) => _context = context;

    public List<MonAn> DanhSachMon { get; set; } = new();

    public void OnGet()
    {
        DanhSachMon = _context.MonAns.ToList();
    }
}