namespace ContentGapAnalyzer.Application.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> Ok(T data, string message = "Operation completed successfully") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors ?? new List<string>() };

    public static ApiResponse<T> Fail(List<string> errors) =>
        new() { Success = false, Message = "Validation failed", Errors = errors };
}

public class PagedResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public IReadOnlyList<T> Data { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public List<string> Errors { get; set; } = new();

    public static PagedResponse<T> Ok(IReadOnlyList<T> data, int totalCount, int page, int pageSize,
        string message = "Operation completed successfully") =>
        new()
        {
            Success = true,
            Message = message,
            Data = data,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
}
