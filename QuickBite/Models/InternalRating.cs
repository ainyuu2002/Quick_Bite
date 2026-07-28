using System.ComponentModel.DataAnnotations;

namespace QuickBite.Models;

public class InternalRating
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public Order? Order { get; set; }

    [Required, StringLength(11)]
    public string Phone { get; set; } = null!;

    [Range(1, 5)]
    public int Stars { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
