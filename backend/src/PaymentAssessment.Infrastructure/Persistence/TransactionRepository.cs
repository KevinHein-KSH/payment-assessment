using Microsoft.EntityFrameworkCore;
using PaymentAssessment.Application.Transactions;
using PaymentAssessment.Domain.Entities;
using PaymentAssessment.Domain.Enums;

namespace PaymentAssessment.Infrastructure.Persistence;

public class TransactionRepository(AppDbContext db) : ITransactionRepository
{
    public async Task AddAsync(PaymentTransaction transaction, CancellationToken ct)
    {
        db.PaymentTransactions.Add(transaction);
        await db.SaveChangesAsync(ct);
    }

    public async Task<PaymentTransaction?> GetByInvoiceNoAsync(string invoiceNo, CancellationToken ct)
    {
        return await db.PaymentTransactions.AsNoTracking().FirstOrDefaultAsync(t => t.InvoiceNo == invoiceNo, ct);
    }

    public async Task<TransactionDetailsDto?> GetDetailsAsync(string invoiceNo, CancellationToken ct)
    {
        return await db.PaymentTransactions.AsNoTracking()
            .Where(t => t.InvoiceNo == invoiceNo)
            .Select(t => new TransactionDetailsDto(
                t.InvoiceNo,
                t.Status,
                t.CustomerId,
                t.ProductId,
                t.Product.Name,
                t.Amount,
                t.Currency,
                t.PaymentChannel,
                t.MaskedCardNo,
                t.RespCode,
                t.RespDesc
            ))
            .FirstOrDefaultAsync(ct);
    }

    public async Task SetPaymentTokenAsync(long id, string paymentToken, string redirectUrl, CancellationToken ct)
    {
        await db.PaymentTransactions
            .Where(t => t.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.PaymentToken, paymentToken)
                                      .SetProperty(t => t.RedirectUrl, redirectUrl), ct);
    }

    public async Task<bool> MarkSucceededAsync(string invoiceNo, string? paymentChannel, string? maskedCardNo,
        string? providerRef, DateTime paidAtUtc, CancellationToken ct)
    {
        var rows = await db.PaymentTransactions
            .Where(t => t.InvoiceNo == invoiceNo && t.Status == TransactionStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, TransactionStatus.Succeeded)
                                      .SetProperty(t => t.PaymentChannel, paymentChannel)
                                      .SetProperty(t => t.MaskedCardNo, maskedCardNo)
                                      .SetProperty(t => t.ProviderRef, providerRef)
                                      .SetProperty(t => t.PaidAt, paidAtUtc), ct);
        return rows == 1;
    }

    public async Task<bool> MarkFailedAsync(string invoiceNo, string respCode, string respDesc, CancellationToken ct)
    {
        // resp_desc is VARCHAR(100); 2C2P descriptions can be longer
        var desc = respDesc.Length > 100 ? respDesc[..100] : respDesc;
        var rows = await db.PaymentTransactions
            .Where(t => t.InvoiceNo == invoiceNo && t.Status == TransactionStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, TransactionStatus.Failed)
                                      .SetProperty(t => t.RespCode, respCode)
                                      .SetProperty(t => t.RespDesc, desc), ct);
        return rows == 1;
    }
}
