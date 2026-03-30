namespace MovieApi.Application.Common.Exceptions;
public sealed class ValidationException : Exception
{
    public string Code { get; }
    public IDictionary<string, string[]> Errors { get; }
    public ValidationException(string code, IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Code = code;
        Errors = errors;
    }
}