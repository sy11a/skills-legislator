namespace Legislator.Core.Abstractions;

/// <summary>
/// The child process could not be started at all - the executable is absent, or present and
/// not runnable. It is the one process failure that is not a result: a run that started and
/// failed reports its exit code, while this says the question was never asked, which is what
/// lets a job tell "no history" from "no instrument" (C-08, R-665).
/// </summary>
public sealed class ProcessStartException : Exception
{
    public ProcessStartException()
        : base("could not start the process")
    {
    }

    public ProcessStartException(string message)
        : base(message)
    {
    }

    public ProcessStartException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>The message an <see cref="IProcessRunner"/> raises when the executable it was handed cannot be started.</summary>
    public static ProcessStartException For(string fileName, Exception innerException) =>
        new($"could not start {fileName}", innerException);
}
