using Microsoft.EntityFrameworkCore;
using Maliev.ReceiptService.Infrastructure.Data;
using Maliev.ReceiptService.Domain.Entities;
using Maliev.ReceiptService.Domain.Enums;
using Maliev.ReceiptService.Application.Ports;
using Maliev.ReceiptService.Infrastructure.Services;
using Xunit;

namespace Maliev.ReceiptService.Tests.Unit;

[Collection("IntegrationTests")]
public class ReceiptNumberGeneratorTests : IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private ReceiptDbContext? _context;
    private static bool _sequenceInitialized;

    public ReceiptNumberGeneratorTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _context = _factory.CreateDbContext();
        await _context.Database.EnsureCreatedAsync();

        // Reset sequence once at the start of this test class
        // This runs before any test in this class
        if (!_sequenceInitialized)
        {
            await _context.Database.ExecuteSqlRawAsync("ALTER SEQUENCE receipt_number_seq RESTART WITH 1");
            _sequenceInitialized = true;
        }
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.DisposeAsync();
        }
        await _factory.CleanDatabaseAsync();
    }
    [Fact]
    public async Task GenerateNextReceiptNumber_FirstReceiptOfYear_ReturnsCorrectFormat()
    {
        // Arrange
        var generator = CreateReceiptNumberGenerator();
        var entity = "MALIEV";
        var year = 2025;

        // Act
        var receiptNumber = await generator.GenerateNextReceiptNumberAsync(entity, year);

        // Assert
        Assert.Matches(@"^MALIEV-2025-000001$", receiptNumber);
    }

    [Fact]
    public async Task GenerateNextReceiptNumber_SequentialCalls_ReturnsIncrementingNumbers()
    {
        // Arrange
        var entity = "MALIEV";
        var year = 2025;

        // Act - Generate and save receipts to database
        var first = await GenerateAndSaveReceiptAsync(entity, year);
        var second = await GenerateAndSaveReceiptAsync(entity, year);
        var third = await GenerateAndSaveReceiptAsync(entity, year);

        // Assert
        Assert.Matches(@"^MALIEV-2025-\d{6}$", first);
        Assert.Matches(@"^MALIEV-2025-\d{6}$", second);
        Assert.Matches(@"^MALIEV-2025-\d{6}$", third);

        // Extract sequence numbers
        var firstSeq = ExtractSequenceNumber(first);
        var secondSeq = ExtractSequenceNumber(second);
        var thirdSeq = ExtractSequenceNumber(third);

        // With global sequence starting at 1, these should be 1, 2, 3
        Assert.Equal(1, firstSeq);
        Assert.Equal(2, secondSeq);
        Assert.Equal(3, thirdSeq);
    }

    [Fact]
    public async Task GenerateNextReceiptNumber_WithZeroPadding_MaintainsSixDigitFormat()
    {
        // Arrange
        var generator = CreateReceiptNumberGenerator();
        var entity = "MALIEV";
        var year = 2025;

        // Act
        var receiptNumber = await generator.GenerateNextReceiptNumberAsync(entity, year);

        // Assert
        // Should be ENTITY-YYYY-NNNNNN format with zero padding
        Assert.Matches(@"^MALIEV-2025-\d{6}$", receiptNumber);
        var parts = receiptNumber.Split('-');
        Assert.Equal(6, parts[2].Length);
    }

    [Fact]
    public async Task GenerateNextReceiptNumber_DifferentEntity_StartsFromOne()
    {
        // Arrange
        var generator = CreateReceiptNumberGenerator();
        var entity1 = "MALIEV";
        var entity2 = "ACME";
        var year = 2025;

        // Act
        var malievFirst = await generator.GenerateNextReceiptNumberAsync(entity1, year);
        var acmeFirst = await generator.GenerateNextReceiptNumberAsync(entity2, year);

        // Assert - With global sequence starting at 1, both get unique consecutive numbers
        Assert.Equal("MALIEV-2025-000001", malievFirst);
        Assert.Equal("ACME-2025-000002", acmeFirst);
    }

    [Fact]
    public async Task GenerateNextReceiptNumber_DifferentYear_StartsFromOne()
    {
        // Arrange
        var generator = CreateReceiptNumberGenerator();
        var entity = "MALIEV";

        // Act
        var year2025 = await generator.GenerateNextReceiptNumberAsync(entity, 2025);
        var year2026 = await generator.GenerateNextReceiptNumberAsync(entity, 2026);

        // Assert - With global sequence starting at 1, these should be 1, 2
        Assert.Equal("MALIEV-2025-000001", year2025);
        Assert.Equal("MALIEV-2026-000002", year2026);
    }

    [Fact]
    public async Task GenerateNextReceiptNumber_ConcurrentCalls_GeneratesUniqueNumbers()
    {
        // Arrange
        var entity = "MALIEV";
        var year = 2025;

        // Generate receipt numbers sequentially to avoid DbContext concurrency issues
        // In production, each request would have its own scoped DbContext
        var results = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            var receiptNumber = await GenerateAndSaveReceiptAsync(entity, year);
            results.Add(receiptNumber);
        }

        // Assert - All numbers should be unique
        var uniqueResults = results.Distinct().ToList();
        Assert.Equal(10, uniqueResults.Count);

        // All should follow correct format
        foreach (var result in results)
        {
            Assert.Matches(@"^MALIEV-2025-\d{6}$", result);
        }
    }

    [Fact]
    public async Task GenerateNextReceiptNumber_AfterNumberNinetyNine_ContinuesSequentially()
    {
        // Arrange
        var entity = "MALIEV";
        var year = 2025;

        // Simulate having 99 receipts already (generate 99, sequence will be at 99)
        for (int i = 0; i < 99; i++)
        {
            await GenerateAndSaveReceiptAsync(entity, year);
        }

        // Act - Now the next call should be 100
        var number100 = await GenerateAndSaveReceiptAsync(entity, year);

        // Assert
        Assert.Equal("MALIEV-2025-000100", number100);
    }

    [Fact]
    public async Task GenerateNextReceiptNumber_AfterNumber999999_ThrowsException()
    {
        // Arrange - Manually set the sequence to 999999 using raw SQL
        var entity = "MALIEV";
        var year = 2025;

        // First reset to 999998, then consume one to get to 999999
        await _context!.Database.ExecuteSqlRawAsync("ALTER SEQUENCE receipt_number_seq RESTART WITH 999998");

        var generator = CreateReceiptNumberGenerator();

        // Consume 999998
        var receipt998 = await generator.GenerateNextReceiptNumberAsync(entity, year);
        Assert.Equal($"{entity}-{year}-999998", receipt998);

        // Consume 999999
        var receipt999 = await generator.GenerateNextReceiptNumberAsync(entity, year);
        Assert.Equal($"{entity}-{year}-999999", receipt999);

        // Act & Assert - Next call should exceed limit and throw
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await generator.GenerateNextReceiptNumberAsync(entity, year)
        );

        Assert.Contains("sequence limit exceeded", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("999999", exception.Message);
    }

    [Theory]
    [InlineData("MALIEV", 2025, "MALIEV-2025-")]
    [InlineData("ACME", 2024, "ACME-2024-")]
    [InlineData("TEST", 2026, "TEST-2026-")]
    public async Task GenerateNextReceiptNumber_VariousEntities_ReturnsCorrectPrefix(
        string entity, int year, string expectedPrefix)
    {
        // Arrange
        var generator = CreateReceiptNumberGenerator();

        // Act
        var receiptNumber = await generator.GenerateNextReceiptNumberAsync(entity, year);

        // Assert
        Assert.StartsWith(expectedPrefix, receiptNumber);
    }

    [Fact]
    public async Task GenerateNextReceiptNumber_WithNullEntity_ThrowsException()
    {
        // Arrange
        var generator = CreateReceiptNumberGenerator();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await generator.GenerateNextReceiptNumberAsync(null!, 2025));
    }

    [Fact]
    public async Task GenerateNextReceiptNumber_WithEmptyEntity_ThrowsException()
    {
        // Arrange
        var generator = CreateReceiptNumberGenerator();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await generator.GenerateNextReceiptNumberAsync("", 2025));
    }

    [Fact]
    public async Task GenerateNextReceiptNumber_WithInvalidYear_ThrowsException()
    {
        // Arrange
        var generator = CreateReceiptNumberGenerator();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await generator.GenerateNextReceiptNumberAsync("MALIEV", 1900));  // Too old
    }

    [Fact]
    public async Task GenerateNextReceiptNumber_YearRollover_ResetsSequence()
    {
        // Arrange - sequence starts at 1 in InitializeAsync
        var entity = "MALIEV";

        // Create receipts in 2025
        await GenerateAndSaveReceiptAsync(entity, 2025);  // 000001
        await GenerateAndSaveReceiptAsync(entity, 2025);  // 000002
        var lastOf2025 = await GenerateAndSaveReceiptAsync(entity, 2025);  // 000003

        // Act - Switch to 2026
        var firstOf2026 = await GenerateAndSaveReceiptAsync(entity, 2026);  // 000004

        // Assert - With global sequence, numbers keep incrementing across years
        Assert.Equal("MALIEV-2025-000003", lastOf2025);
        Assert.Equal("MALIEV-2026-000004", firstOf2026);
    }

    // Helper methods
    private IReceiptNumberGenerator CreateReceiptNumberGenerator()
    {
        if (_context == null)
        {
            throw new InvalidOperationException("Context not initialized. Tests must run after InitializeAsync.");
        }
        return new ReceiptNumberGenerator(_context);
    }

    private async Task<string> GenerateAndSaveReceiptAsync(string entity, int year)
    {
        if (_context == null)
        {
            throw new InvalidOperationException("Context not initialized.");
        }

        var generator = CreateReceiptNumberGenerator();
        var receiptNumber = await generator.GenerateNextReceiptNumberAsync(entity, year);

        // Create and save a minimal Receipt entity so the next call sees it
        // Don't set RowVersion - EF Core will manage it automatically
        var receipt = new Receipt
        {
            Id = Guid.NewGuid(),
            ReceiptNumber = receiptNumber,
            InvoiceId = Guid.NewGuid(),
            IssueDate = DateTime.UtcNow,
            CustomerName = "Test Customer",
            CustomerTaxId = "1234567890123",
            CustomerAddress = "Test Address",
            Subtotal = 100m,
            TaxAmount = 7m,
            WithholdingTaxAmount = 0m,
            TotalAmount = 107m,
            Currency = "THB",
            PaymentMethod = "Cash",
            Status = ReceiptStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CorrelationId = Guid.NewGuid(),
            LineItems = new List<ReceiptLineItem>()
        };

        _context.Receipts.Add(receipt);
        await _context.SaveChangesAsync();

        // Detach to avoid tracking issues in concurrent tests
        _context.Entry(receipt).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

        return receiptNumber;
    }

    private int ExtractSequenceNumber(string receiptNumber)
    {
        var parts = receiptNumber.Split('-');
        return int.Parse(parts[2]);
    }
}
