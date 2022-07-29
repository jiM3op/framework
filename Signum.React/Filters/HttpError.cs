

namespace Signum.React.Filters;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

public class HttpError
{
    public string ExceptionType { get; set; }
    public string ExceptionMessage { get; set; }
    public string? ExceptionId { get; set; }
    public string? StackTrace { get; set; }
    public ModelEntity? Model; /*{ get; set; }*/
    public HttpError? InnerException; /*{ get; set; }*/
}

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
