using System;
using System.Collections.Generic;

namespace test.Models;

public partial class DanhMuc
{
    public int MaDanhMuc { get; set; }

    public string TenDanhMuc { get; set; } = null!;

    // Cột mới theo ERD
    public string? MoTa { get; set; }

    public string TrangThai { get; set; } = "Hoạt động";

    public virtual ICollection<MonAn> MonAns { get; set; } = new List<MonAn>();
}