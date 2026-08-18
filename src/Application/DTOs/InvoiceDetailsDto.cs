using Behsazan.Domain.Enums;

namespace Behsazan.Application.DTOs;

public class InvoiceDetailsDto
{
    public int Id { get; set; }

    public int InvoiceNumber { get; set; }

    public DateTime InvoiceDate { get; set; }

    public string? Title { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal TotalPrice { get; set; }

    public decimal? ShippingCost { get; set; }

    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public string ProjectAddress { get; set; } = string.Empty;

    public int? ProjectGeneralLedgerNumber { get; set; }

    public JoistType ProjectJoistType { get; set; }

    public int CustomerId { get; set; }

    public string CustomerFullName { get; set; } = string.Empty;

    public string CustomerNationalCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public IReadOnlyList<InvoiceItemPreviewDto> Items { get; set; } = Array.Empty<InvoiceItemPreviewDto>();
}
