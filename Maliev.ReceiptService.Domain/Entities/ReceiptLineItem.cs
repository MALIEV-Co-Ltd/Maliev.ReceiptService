using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maliev.ReceiptService.Domain.Entities;

public class ReceiptLineItem
{
    [Key]
    public Guid Id { get; init; }

    [Required]
    public Guid ReceiptId { get; init; }

    public Guid? InvoiceLineItemId { get; init; }

    [Required]
    public int LineNumber { get; init; }

    [Required]
    [MaxLength(500)]
    public string Description { get; init; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; init; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; init; }

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal TaxRate { get; init; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal LineTotal { get; init; }

    [ForeignKey(nameof(ReceiptId))]
    public Receipt Receipt { get; init; } = null!;
}
