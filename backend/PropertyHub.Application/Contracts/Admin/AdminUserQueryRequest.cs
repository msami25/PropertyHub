namespace PropertyHub.Application.Contracts.Admin;

public sealed record AdminUserQueryRequest
{
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
