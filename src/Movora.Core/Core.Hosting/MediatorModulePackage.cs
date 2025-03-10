using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;

namespace Movora.Core.Core.Hosting;

public static class MediatorModulePackage
{
    public static void Bootstrap(IServiceCollection services, string searchPattern)
    {
        var assemblies = GetAssemblies(searchPattern);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(assemblies.ToArray()));
    }

    private static ICollection<Assembly> GetAssemblies(string searchPattern)
    {
        return searchPattern.LoadFullAssemblies().ToList();
    }
    public static IEnumerable<Assembly> LoadFullAssemblies(this string searchPattern)
    {
        var assemblies = "Movora*".LoadAssemblyWithPattern(); 
        return assemblies.ToHashSet();
    }

    public static IEnumerable<Assembly> LoadAssemblyWithPattern(this string searchPattern)
    {
        var assemblies = new HashSet<Assembly>();
        var searchRegex = new Regex(searchPattern, RegexOptions.IgnoreCase);
        var defaultContext = DependencyContext.Default;
        if (defaultContext == null)
        {
            throw new InvalidOperationException("DependencyContext.Default is null.");
        }

        var moduleAssemblyFiles = defaultContext
            .RuntimeLibraries
            .Where(x => searchRegex.IsMatch(x.Name))
            .ToList();

        foreach (var assemblyFiles in moduleAssemblyFiles)
        {
            if (assemblyFiles.Name.Contains(".Reference")) continue;
            assemblies.Add(Assembly.Load(new AssemblyName(assemblyFiles.Name)));
        }

        return assemblies.ToList();
    }
}