namespace RaccoonLand.Modules.FileStorage.SqlServer.FileStream.Internal;

internal static class StorageDisposeHelper
{
    internal static void DisposeQuietly(IDisposable? resource)
    {
        if (resource is null)
        {
            return;
        }

        try
        {
            resource.Dispose();
        }
        catch (Exception)
        {
            // Best-effort cleanup: continue disposing remaining resources.
        }
    }

    internal static async Task DisposeQuietlyAsync(IAsyncDisposable? resource)
    {
        if (resource is null)
        {
            return;
        }

        try
        {
            await resource.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Best-effort cleanup: continue disposing remaining resources.
        }
    }
}
