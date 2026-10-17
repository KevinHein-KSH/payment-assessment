using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaymentAssessment.Domain.Entities;

/// <summary>A customer's saved card: the 2C2P customerToken plus a masked number, never the full card.</summary>
[Table("customer_card")]
public class CustomerCard
{
    [Column("id")]
    public int Id { get; set; }

    [Column("customer_id")]
    public int CustomerId { get; set; }

    [Column("customer_token"), MaxLength(100)]
    public string CustomerToken { get; set; } = string.Empty;

    [Column("masked_card_no"), MaxLength(30)]
    public string MaskedCardNo { get; set; } = string.Empty;

    // UTC
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    public Customer Customer { get; set; } = null!;
}
