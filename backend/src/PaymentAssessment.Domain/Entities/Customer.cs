using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaymentAssessment.Domain.Entities;

/// <summary>A seeded customer who can make payments.</summary>
[Table("customer")]
public class Customer
{
    [Column("id")]
    public int Id { get; set; }

    [Column("first_name"), MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Column("last_name"), MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Column("email"), MaxLength(255)]
    public string Email { get; set; } = string.Empty;
}
