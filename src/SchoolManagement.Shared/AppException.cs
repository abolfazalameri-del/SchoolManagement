namespace SchoolManagement.Shared;

/// <summary>
/// Thrown only for truly exceptional, unexpected conditions (a corrupt database file, a broken invariant).
/// Expected business failures ("student not found", "invoice already paid") must use Result.Failure instead,
/// never an exception — exceptions here always surface to the user as "یک خطای غیرمنتظره رخ داد", never a
/// friendly, specific message.
/// </summary>
public class AppException : Exception
{
    public AppException(string message) : base(message) { }
    public AppException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>Simple precondition/invariant checks used across the Domain and Application layers.</summary>
public static class Guard
{
    public static void AgainstNull(object? value, string paramName)
    {
        if (value is null) throw new ArgumentNullException(paramName);
    }

    public static void AgainstNullOrEmpty(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value cannot be empty.", paramName);
    }

    public static void AgainstNegative(decimal value, string paramName)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(paramName, "Value cannot be negative.");
    }

    public static void AgainstNegativeOrZero(int value, string paramName)
    {
        if (value <= 0) throw new ArgumentOutOfRangeException(paramName, "Value must be greater than zero.");
    }
}
