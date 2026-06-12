using System.Reflection;

namespace ECode.Core.Services;

public static class VersionService
{
    public static string GetInformationalVersion(Assembly assembly, string fallback = "0.0.0")
    {
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return informationalVersion.Split('+', 2)[0];
        }

        return assembly.GetName().Version?.ToString() ?? fallback;
    }
}
