using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PaymentAssessment.Domain.Enums;

namespace PaymentAssessment.Domain.Entities;

/// <summary>A seeded product that can be purchased, priced in SGD.</summary>
[Table("product")]
public class Product
{
    [Column("id")]
    public int Id { get; set; }

    [Column("name"), MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Column("description"), MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Column("price", TypeName = "decimal(12,2)")]
    public decimal Price { get; set; }

    [Column("currency", TypeName = "char(3)")]
    public string Currency { get; set; } = string.Empty;

    [Column("payment_type", TypeName = "enum('OneTime','Recurring')")]
    public PaymentType PaymentType { get; set; }
}
