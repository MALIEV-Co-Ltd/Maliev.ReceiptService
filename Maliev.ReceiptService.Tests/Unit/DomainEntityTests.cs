using Maliev.ReceiptService.Domain.Entities;
using Maliev.ReceiptService.Domain.Enums;
using Xunit;

namespace Maliev.ReceiptService.Tests.Unit;

public class ReceiptEntityTests
{
    [Fact]
    public void Void_WithActiveStatus_SetsVoidedProperties()
    {
        var receipt = new Receipt
        {
            Id = Guid.NewGuid(),
            ReceiptNumber = "TEST-001",
            InvoiceId = Guid.NewGuid(),
            IssueDate = DateTime.UtcNow,
            CustomerName = "Test Customer",
            Subtotal = 1000m,
            TaxAmount = 70m,
            TotalAmount = 1070m,
            Currency = "THB",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CorrelationId = Guid.NewGuid(),
            Status = ReceiptStatus.Active
        };

        receipt.Void("staff-001", "Test void reason");

        Assert.Equal(ReceiptStatus.Void, receipt.Status);
        Assert.NotNull(receipt.VoidedAt);
        Assert.Equal("staff-001", receipt.VoidedBy);
        Assert.Equal("Test void reason", receipt.VoidReason);
    }

    [Fact]
    public void Void_AlreadyVoided_ThrowsInvalidOperationException()
    {
        var receipt = new Receipt
        {
            Id = Guid.NewGuid(),
            ReceiptNumber = "TEST-001",
            InvoiceId = Guid.NewGuid(),
            IssueDate = DateTime.UtcNow,
            CustomerName = "Test Customer",
            Subtotal = 1000m,
            TaxAmount = 70m,
            TotalAmount = 1070m,
            Currency = "THB",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CorrelationId = Guid.NewGuid(),
            Status = ReceiptStatus.Void
        };

        Assert.Throws<InvalidOperationException>(() => receipt.Void("staff-001", "Test"));
    }

    [Fact]
    public void Void_PendingPdfStatus_CanBeVoided()
    {
        var receipt = new Receipt
        {
            Id = Guid.NewGuid(),
            ReceiptNumber = "TEST-001",
            InvoiceId = Guid.NewGuid(),
            IssueDate = DateTime.UtcNow,
            CustomerName = "Test Customer",
            Subtotal = 1000m,
            TaxAmount = 70m,
            TotalAmount = 1070m,
            Currency = "THB",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CorrelationId = Guid.NewGuid(),
            Status = ReceiptStatus.PendingPdf
        };

        receipt.Void("staff-001", "Cancelled by customer");

        Assert.Equal(ReceiptStatus.Void, receipt.Status);
        Assert.Equal("Cancelled by customer", receipt.VoidReason);
    }
}

public class InvoiceBalanceTrackerTests
{
    [Fact]
    public void InvoiceBalanceTracker_Properties_AreAccessible()
    {
        var tracker = new InvoiceBalanceTracker
        {
            InvoiceId = Guid.NewGuid(),
            SegmentId = Guid.Empty,
            TotalInvoiceAmount = 10000m,
            TotalReceiptedAmount = 3000m,
            RemainingBalance = 7000m,
            LastUpdatedAt = DateTime.UtcNow
        };

        Assert.Equal(10000m, tracker.TotalInvoiceAmount);
        Assert.Equal(3000m, tracker.TotalReceiptedAmount);
        Assert.Equal(7000m, tracker.RemainingBalance);
    }
}

public class ReceiptLineItemTests
{
    [Fact]
    public void ReceiptLineItem_Properties_AreAccessible()
    {
        var lineItem = new ReceiptLineItem
        {
            Id = Guid.NewGuid(),
            ReceiptId = Guid.NewGuid(),
            InvoiceLineItemId = Guid.NewGuid(),
            LineNumber = 1,
            Description = "Test Product",
            Quantity = 5m,
            UnitPrice = 200m,
            TaxRate = 7m,
            LineTotal = 1070m
        };

        Assert.Equal(1, lineItem.LineNumber);
        Assert.Equal("Test Product", lineItem.Description);
        Assert.Equal(5m, lineItem.Quantity);
        Assert.Equal(200m, lineItem.UnitPrice);
        Assert.Equal(7m, lineItem.TaxRate);
        Assert.Equal(1070m, lineItem.LineTotal);
    }
}

public class ReceiptAuditEventTests
{
    [Fact]
    public void ReceiptAuditEvent_Properties_AreAccessible()
    {
        var auditEvent = new ReceiptAuditEvent
        {
            Id = Guid.NewGuid(),
            ReceiptId = Guid.NewGuid(),
            EventType = AuditEventType.Created,
            Timestamp = DateTime.UtcNow,
            StaffMemberId = "staff-001",
            Reason = "Receipt created",
            CorrelationId = Guid.NewGuid()
        };

        Assert.Equal(AuditEventType.Created, auditEvent.EventType);
        Assert.Equal("staff-001", auditEvent.StaffMemberId);
        Assert.Equal("Receipt created", auditEvent.Reason);
    }
}
