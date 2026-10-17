using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PaymentAssessment.Domain.Enums;

namespace PaymentAssessment.Domain.Entities;

/// <summary>A payment attempt (the "transaction" table), one per idempotency key.</summary>
[Table("transaction")]
public class PaymentTransaction
{
    [Column("id")]
    public long Id { get; set; }

    [Column("customer_id")]
    public int CustomerId { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("invoice_no"), MaxLength(50)]
    public string InvoiceNo { get; set; } = string.Empty;

    [Column("idempotency_key"), MaxLength(100)]
    public string IdempotencyKey { get; set; } = string.Empty;

    [Column("amount", TypeName = "decimal(12,2)")]
    public decimal Amount { get; set; }

    [Column("currency", TypeName = "char(3)")]
    public string Currency { get; set; } = string.Empty;

    [Column("status", TypeName = "enum('Pending','Succeeded','Failed')")]
    public TransactionStatus Status { get; set; }

    [Column("payment_channel"), MaxLength(20)]
    public string? PaymentChannel { get; set; }

    [Column("masked_card_no"), MaxLength(30)]
    public string? MaskedCardNo { get; set; }

    [Column("provider_ref"), MaxLength(50)]
    public string? ProviderRef { get; set; }

    [Column("resp_code"), MaxLength(10)]
    public string? RespCode { get; set; }

    [Column("resp_desc"), MaxLength(100)]
    public string? RespDesc { get; set; }

    // UTC
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // UTC
    [Column("paid_at")]
    public DateTime? PaidAt { get; set; }

    /// <summary>Added column, see docs/04.</summary>
    [Column("payment_token"), MaxLength(500)]
    public string? PaymentToken { get; set; }

    /// <summary>Added column, see docs/04.</summary>
    [Column("redirect_url"), MaxLength(2048)]
    public string? RedirectUrl { get; set; }

    public Customer Customer { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
