using Maliev.MessagingContracts.Contracts.Invoices;
using Maliev.ReceiptService.Application.Exceptions;
using Maliev.ReceiptService.Application.Models.Requests;
using Maliev.ReceiptService.Application.Services;
using Maliev.ReceiptService.Domain.Enums;
using Maliev.ReceiptService.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Maliev.ReceiptService.Infrastructure.Consumers;

/// <summary>
/// Creates customer receipts when invoice payments are allocated.
/// </summary>
public class InvoicePaymentReceivedEventConsumer : IConsumer<InvoicePaymentReceivedEvent>
{
    private const string SystemStaffId = "InvoiceService";

    private readonly ReceiptDbContext _context;
    private readonly IReceiptService _receiptService;
    private readonly ILogger<InvoicePaymentReceivedEventConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InvoicePaymentReceivedEventConsumer"/> class.
    /// </summary>
    public InvoicePaymentReceivedEventConsumer(
        ReceiptDbContext context,
        IReceiptService receiptService,
        ILogger<InvoicePaymentReceivedEventConsumer> logger)
    {
        _context = context;
        _receiptService = receiptService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<InvoicePaymentReceivedEvent> context)
    {
        var message = context.Message;
        var payload = message.Payload;
        var correlationId = message.CorrelationId == Guid.Empty
            ? message.MessageId
            : message.CorrelationId;

        if (payload.AllocatedAmount <= 0)
        {
            _logger.LogWarning(
                "Skipping receipt creation for non-positive invoice payment allocation {PaymentId} on invoice {InvoiceId}: {Amount}",
                payload.PaymentId,
                payload.InvoiceId,
                payload.AllocatedAmount);
            return;
        }

        var alreadyCreated = await _context.Receipts
            .AsNoTracking()
            .AnyAsync(
                receipt =>
                    receipt.InvoiceId == payload.InvoiceId &&
                    (receipt.ExternalPaymentId == payload.PaymentId || receipt.CorrelationId == correlationId) &&
                    receipt.Status != ReceiptStatus.Void,
                context.CancellationToken);

        if (alreadyCreated)
        {
            _logger.LogInformation(
                "Receipt already exists for invoice payment allocation. InvoiceId={InvoiceId}, PaymentId={PaymentId}, CorrelationId={CorrelationId}",
                payload.InvoiceId,
                payload.PaymentId,
                correlationId);
            return;
        }

        try
        {
            await _receiptService.CreateReceiptAsync(
                new CreateReceiptRequest
                {
                    InvoiceId = payload.InvoiceId,
                    ExternalPaymentId = payload.PaymentId,
                    Amount = (decimal)payload.AllocatedAmount,
                    PaymentMethod = "PaymentService"
                },
                SystemStaffId,
                correlationId);
        }
        catch (Exception ex) when (ex is DuplicateReceiptException or OverReceiptingException)
        {
            if (await WaitForReceiptForPaymentAsync(payload.InvoiceId, payload.PaymentId, context.CancellationToken))
            {
                _logger.LogInformation(
                    "Receipt was created concurrently for invoice payment allocation. InvoiceId={InvoiceId}, PaymentId={PaymentId}",
                    payload.InvoiceId,
                    payload.PaymentId);
                return;
            }

            throw;
        }

        _logger.LogInformation(
            "Created receipt for invoice payment allocation. InvoiceId={InvoiceId}, PaymentId={PaymentId}, Amount={Amount} {Currency}",
            payload.InvoiceId,
            payload.PaymentId,
            payload.AllocatedAmount,
            payload.Currency);
    }

    private async Task<bool> WaitForReceiptForPaymentAsync(
        Guid invoiceId,
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 5;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var receiptExists = await _context.Receipts
                .AsNoTracking()
                .AnyAsync(
                    receipt =>
                        receipt.InvoiceId == invoiceId &&
                        receipt.ExternalPaymentId == paymentId &&
                        receipt.Status != ReceiptStatus.Void,
                    cancellationToken);

            if (receiptExists)
            {
                return true;
            }

            if (attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
            }
        }

        return false;
    }
}
