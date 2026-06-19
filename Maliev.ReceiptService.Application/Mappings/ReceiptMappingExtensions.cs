using Maliev.ReceiptService.Application.Models.Responses;
using Maliev.ReceiptService.Domain.Entities;

namespace Maliev.ReceiptService.Application.Mappings;

/// <summary>
/// Mapping extensions for Receipt entities and DTOs
/// Per research.md and Maliev guidelines (no AutoMapper)
/// </summary>
public static class ReceiptMappingExtensions
{
    /// <summary>
    /// Convert Receipt entity to ReceiptResponse DTO
    /// </summary>
    public static ReceiptResponse ToResponse(this Receipt receipt)
    {
        return new ReceiptResponse
        {
            Id = receipt.Id,
            ReceiptNumber = receipt.ReceiptNumber,
            InvoiceId = receipt.InvoiceId,
            ExternalPaymentId = receipt.ExternalPaymentId,
            InvoiceSegmentId = receipt.InvoiceSegmentId,
            IssueDate = receipt.IssueDate,
            CustomerName = receipt.CustomerName,
            CustomerTaxId = receipt.CustomerTaxId,
            CustomerAddress = receipt.CustomerAddress,
            Subtotal = receipt.Subtotal,
            TaxAmount = receipt.TaxAmount,
            WithholdingTaxAmount = receipt.WithholdingTaxAmount,
            TotalAmount = receipt.TotalAmount,
            Currency = receipt.Currency,
            PaymentMethod = receipt.PaymentMethod,
            Status = receipt.Status.ToString(),
            PdfReferenceId = receipt.PdfReferenceId,
            CreatedAt = receipt.CreatedAt,
            CreatedBy = receipt.CreatedBy,
            VoidedAt = receipt.VoidedAt,
            VoidedBy = receipt.VoidedBy,
            VoidReason = receipt.VoidReason,
            LineItems = receipt.LineItems.Select(li => li.ToResponse()).ToList()
        };
    }

    /// <summary>
    /// Convert ReceiptLineItem entity to ReceiptLineItemResponse DTO
    /// </summary>
    public static ReceiptLineItemResponse ToResponse(this ReceiptLineItem lineItem)
    {
        return new ReceiptLineItemResponse
        {
            LineNumber = lineItem.LineNumber,
            Description = lineItem.Description,
            Quantity = lineItem.Quantity,
            UnitPrice = lineItem.UnitPrice,
            TaxRate = lineItem.TaxRate,
            LineTotal = lineItem.LineTotal
        };
    }
}
