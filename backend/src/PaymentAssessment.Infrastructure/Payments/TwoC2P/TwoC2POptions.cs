using System.ComponentModel.DataAnnotations;

namespace PaymentAssessment.Infrastructure.Payments.TwoC2P;

/// <summary>Settings for the 2C2P sandbox, bound from the "TwoC2P" config section.</summary>
public class TwoC2POptions
{
    public const string SectionName = "TwoC2P";

    [Required]
    public string MerchantId { get; set; } = string.Empty;

    [Required]
    public string SecretKey { get; set; } = string.Empty;

    [Required]
    public string BaseUrl { get; set; } = string.Empty;

    [Required]
    public string PaymentTokenPath { get; set; } = string.Empty;

    [Required]
    public string PaymentInquiryPath { get; set; } = string.Empty;

    [Required]
    public string BackendReturnUrl { get; set; } = string.Empty;

    [Required]
    public string FrontendReturnUrl { get; set; } = string.Empty;
}
