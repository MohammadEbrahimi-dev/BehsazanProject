namespace Behsazan.Application.DTOs;

public class RecentInvoiceDto
{
    public int Id { get; set; }
    public int InvoiceNumber { get; set; }
    public DateTime InvoiceDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public decimal TotalPrice { get; set; }
}
