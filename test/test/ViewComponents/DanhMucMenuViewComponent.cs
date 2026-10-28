using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.ViewComponents
{
    public class DanhMucMenuViewComponent : ViewComponent
    {
        private readonly QuanLyNhaHangContext _context;

        public DanhMucMenuViewComponent(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var danhMucList = await _context.DanhMucs.ToListAsync();
            return View(danhMucList);
        }
    }
}