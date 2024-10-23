﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Gnarly;

/// <summary>
/// 扫描
/// </summary>
public static class ScanService
{
    private static INamedTypeSymbol? iScopeDependencySymbol;
    private static INamedTypeSymbol? iSingletonDependencySymbol;
    private static INamedTypeSymbol? iTransientDependencySymbol;

    public static void ScanAndCollect(Compilation compilationContext, List<string> scopeMethods,
        List<string> singletonMethods, List<string> transientMethods)
    {
        var visitedAssemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);

        iScopeDependencySymbol ??= compilationContext.GetTypeByMetadataName("Gnarly.Data.IScopeDependency");
        iSingletonDependencySymbol ??= compilationContext.GetTypeByMetadataName("Gnarly.Data.ISingletonDependency");
        iTransientDependencySymbol ??= compilationContext.GetTypeByMetadataName("Gnarly.Data.ITransientDependency");

        void ScanAssembly(IAssemblySymbol assemblySymbol)
        {
            if (visitedAssemblies.Contains(assemblySymbol) || assemblySymbol.Name.StartsWith("Microsoft") ||
                assemblySymbol.Name.StartsWith("System"))
            {
                return;
            }

            visitedAssemblies.Add(assemblySymbol);

            foreach (var referencedAssembly in assemblySymbol.Modules.SelectMany(m => m.ReferencedAssemblySymbols))
            {
                ScanAssembly(referencedAssembly);
            }
        }

        void ScanType(INamedTypeSymbol namedTypeSymbol)
        {
            var registrationAttribute = namedTypeSymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "RegistrationAttribute");

            int type = 0;
            if (namedTypeSymbol.AllInterfaces.Contains(iScopeDependencySymbol))
            {
                type = 1;
            }
            else if (namedTypeSymbol.AllInterfaces.Contains(iSingletonDependencySymbol))
            {
                type = 2;
            }
            else if (namedTypeSymbol.AllInterfaces.Contains(iTransientDependencySymbol))
            {
                type = 3;
            }

            if ((registrationAttribute?.ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol) is
                {
                    TypeKind: TypeKind.Interface
                } registrationType)
            {
                switch (type)
                {
                    case 1:
                        scopeMethods.Add($"{registrationType},{namedTypeSymbol}");
                        break;
                    case 2:
                        singletonMethods.Add($"{registrationType},{namedTypeSymbol}");
                        break;
                    case 3:
                        transientMethods.Add($"{registrationType},{namedTypeSymbol}");
                        break;
                }
            }
            else if (namedTypeSymbol.AllInterfaces.Any(i => i.Name == $"I{namedTypeSymbol.Name}"))
            {
                var namedTypeSymbolInterface =
                    namedTypeSymbol.AllInterfaces.First(i =>
                        i.Name.ToLower() == $"I{namedTypeSymbol.Name}".ToLower());

                switch (type)
                {
                    case 1:
                        scopeMethods.Add($"{namedTypeSymbolInterface},{namedTypeSymbol}");
                        break;
                    case 2:
                        singletonMethods.Add($"{namedTypeSymbolInterface},{namedTypeSymbol}");
                        break;
                    case 3:
                        transientMethods.Add($"{namedTypeSymbolInterface},{namedTypeSymbol}");
                        break;
                }
            }
            else
            {
                switch (type)
                {
                    case 1:
                        scopeMethods.Add(namedTypeSymbol.ToString());
                        break;
                    case 2:
                        singletonMethods.Add(namedTypeSymbol.ToString());
                        break;
                    case 3:
                        transientMethods.Add(namedTypeSymbol.ToString());
                        break;
                }
            }

            foreach (var nestedType in namedTypeSymbol.GetTypeMembers())
            {
                ScanType(nestedType);
            }
        }

        foreach (var assemblySymbol in compilationContext.SourceModule.ReferencedAssemblySymbols)
        {
            ScanAssembly(assemblySymbol);
        }

        ScanAssembly(compilationContext.Assembly);

        void ScanNamespace(INamespaceSymbol namespaceSymbol)
        {
            foreach (var typeSymbol in namespaceSymbol.GetTypeMembers())
            {
                ScanType(typeSymbol);
            }

            foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                ScanNamespace(nestedNamespace);
            }
        }

        foreach (var item in visitedAssemblies)
        {
            ScanNamespace(item.GlobalNamespace);
        }
    }
}