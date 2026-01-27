namespace BasketService.Api.IntegrationTests;

public static class Eventually
{
    public static async Task<T> WaitFor<T>(
        Func<Task<T>> action,
        Func<T, bool> condition,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null)
    {
        timeout ??= TimeSpan.FromSeconds(10);
        pollInterval ??= TimeSpan.FromMilliseconds(100);

        var start = DateTime.UtcNow;

        while (true)
        {
            var result = await action();

            if (condition(result))
                return result;

            if (DateTime.UtcNow - start > timeout)
                throw new TimeoutException("Condition was not met within the timeout.");

            await Task.Delay(pollInterval.Value);
        }
    }
}