namespace SocioTorcedor.Modules.Payments.Infrastructure.Persistence;

internal static class DesignTimeHostResolver
{
    /// <summary>
    /// Localiza a pasta do projeto Host (onde está appsettings.json) subindo a partir do assembly.
    /// </summary>
    public static string ResolveHostApiDirectory()
    {
        var start = Path.GetDirectoryName(typeof(DesignTimeHostResolver).Assembly.Location)
            ?? AppContext.BaseDirectory;

        var dir = new DirectoryInfo(Path.GetFullPath(start));
        while (dir is not null)
        {
            var direct = Path.Combine(dir.FullName, "SocioTorcedor.Api");
            if (Directory.Exists(direct) && File.Exists(Path.Combine(direct, "appsettings.json")))
                return direct;

            var underSrc = Path.Combine(dir.FullName, "Host", "SocioTorcedor.Api");
            if (Directory.Exists(underSrc) && File.Exists(Path.Combine(underSrc, "appsettings.json")))
                return underSrc;

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate Host/SocioTorcedor.Api with appsettings.json for EF design-time.");
    }
}
