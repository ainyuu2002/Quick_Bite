using System.ComponentModel.DataAnnotations;

namespace QuickBite.Models;

public class Category
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên danh mục không được để trống")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [StringLength(300)]
    public string? Description { get; set; }

    /// <summary>Thứ tự hiển thị trên menu (nhỏ hiện trước).</summary>
    public int DisplayOrder { get; set; }

    public List<MenuItem> MenuItems { get; set; } = new();
}
