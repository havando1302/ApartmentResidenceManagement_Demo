namespace ApartmentResidenceManagement.Application.Common;

public sealed record OperationResult(bool IsSuccess, string ErrorMessage)
{
    public static OperationResult Success() => new(true, string.Empty);

    public static OperationResult Failure(string errorMessage) => new(false, errorMessage);
}

public sealed record OperationResult<T>(bool IsSuccess, T? Value, string ErrorMessage)
{
    public static OperationResult<T> Success(T value) => new(true, value, string.Empty);

    public static OperationResult<T> Failure(string errorMessage) => new(false, default, errorMessage);
}
