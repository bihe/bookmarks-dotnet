using System.Reflection;

namespace Api
{
    public static class SystemVersion
    {
        static readonly string _assemblyVersion = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";

        public static string GetAssemblyVersion() { return _assemblyVersion; }
    }
}
