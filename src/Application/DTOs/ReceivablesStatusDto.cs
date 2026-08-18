namespace Behsazan.Application.DTOs;

public class ReceivablesStatusDto
{
    public int FullyPaidCount { get; set; }

    public int PartiallyPaidCount { get; set; }

    public int UnpaidCount { get; set; }

    public int OverpaidCount { get; set; }
}
