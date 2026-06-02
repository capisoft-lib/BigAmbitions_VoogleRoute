namespace VoogleRoute.Update;

internal static class UpdatePaths
{
    internal const string PendingFileName = "pending_update.json";
    internal const string StagingFileName = "VoogleRoute.dll";

    internal static string StateDirectory
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VoogleRoute");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    internal static string PendingFilePath => Path.Combine(StateDirectory, PendingFileName);

    internal static string StagingFilePath => Path.Combine(StateDirectory, StagingFileName);
}
