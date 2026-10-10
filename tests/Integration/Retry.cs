using Core;

namespace Integration;

/// <summary>A connection to a real service sometimes hangs. A short timeout and a few attempts keep one stall from failing a test, and a service that stays unreachable still fails it.</summary>
internal static class Retry
{
    private const int Attempts = 4;

    public static HttpClient Client() => new() { Timeout = TimeSpan.FromSeconds(15) };

    public static async Task<T> Run<T>(Func<Task<T>> call)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                return await call();
            }
            catch (Exception e) when (attempt < Attempts && e is HttpRequestException or TaskCanceledException or ForecastNoAnswerException)
            {
            }
        }
    }
}
