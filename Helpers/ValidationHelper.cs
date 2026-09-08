namespace RetailFlow.Helpers;

/// <summary>
/// Small, reusable numeric checks shared by services that validate business data
/// (e.g. Product prices and stock now, Sale quantities later), so the same rule
/// isn't reimplemented slightly differently in more than one place.
/// </summary>
public static class ValidationHelper
{
    public static bool IsPositive(decimal value) => value > 0;

    public static bool IsNonNegative(int value) => value >= 0;
}
