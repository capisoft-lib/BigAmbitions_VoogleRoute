namespace VoogleRoute.Update;

/// <summary>Legacy entry point — use <see cref="UpdateService"/>.</summary>
internal static class UpdateChecker
{
    internal static void Initialize() => UpdateService.Initialize();
}
