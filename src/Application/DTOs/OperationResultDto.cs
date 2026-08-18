namespace Behsazan.Application.DTOs;

public class OperationResultDto
{
    public bool Succeeded { get; init; }

    public string Message { get; init; } = string.Empty;

    public int? EntityId { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static OperationResultDto Ok(string message, int? entityId = null) => new()
    {
        Succeeded = true,
        Message = message,
        EntityId = entityId
    };

    public static OperationResultDto Fail(string message) => new()
    {
        Succeeded = false,
        Message = message
    };

    public static OperationResultDto Invalid(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Message = errors.Count > 0 ? errors[0] : "اطلاعات وارد شده معتبر نیست",
        Errors = errors
    };
}
