using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gnarly;

/// <summary>
/// 扫描
/// </summary>
public class ScanService
{
    private static INamedTypeSymbol iScopeDependencySymbol;
    private static INamedTypeSymbol iSingletonDependencySymbol;
    private static INamedTypeSymbol iTransientDependencySymbol;

    /// <summary>
    /// 扫描程序集中的所有类，找到实现了<see cref="IScopeDependency"/>、<see cref="ISingletonDependency"/>、<see cref="ITransientDependency"/>接口的类ConditionalExpressionSyntax conditionalExpression;
    /// </summary>
    /// <param name="compilation"></param>
    /// <returns></returns>
    public static IEnumerable<(int type, INamedTypeSymbol namedTypeSymbol)> ScanForDependencyTypes(Compilation compilation)
    {
        iScopeDependencySymbol ??= compilation.GetTypeByMetadataName("Gnarly.Data.IScopeDependency");
        iSingletonDependencySymbol ??= compilation.GetTypeByMetadataName("Gnarly.Data.ISingletonDependency");
        iTransientDependencySymbol ??= compilation.GetTypeByMetadataName("Gnarly.Data.ITransientDependency");

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot();

            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var @class in classes)
            {
                if (semanticModel.GetDeclaredSymbol(@class) is INamedTypeSymbol symbol)
                {
                    if (symbol.AllInterfaces.Contains(iScopeDependencySymbol))
                    {
                        yield return (1, symbol);
                    }
                    else if (symbol.AllInterfaces.Contains(iSingletonDependencySymbol))
                    {
                        yield return (2, symbol);
                    }
                    else if (symbol.AllInterfaces.Contains(iTransientDependencySymbol))
                    {
                        yield return (3, symbol);
                    }
                }
            }
        }
    }
    public static void ScanAndCollect(Compilation compilationContext, List<string> scopeMethods, List<string> singletonMethods, List<string> transientMethods)
    {
        var visitedAssemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);

        iScopeDependencySymbol ??= compilationContext.GetTypeByMetadataName("Gnarly.Data.IScopeDependency");
        iSingletonDependencySymbol ??= compilationContext.GetTypeByMetadataName("Gnarly.Data.ISingletonDependency");
        iTransientDependencySymbol ??= compilationContext.GetTypeByMetadataName("Gnarly.Data.ITransientDependency");

        void ScanAssembly(IAssemblySymbol assemblySymbol)
        {
            if (visitedAssemblies.Contains(assemblySymbol) || assemblySymbol.Name.StartsWith("Microsoft") || assemblySymbol.Name.StartsWith("System"))
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

            if (registrationAttribute != null &&
                registrationAttribute.ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol is
                { } registrationType && registrationType.TypeKind == TypeKind.Interface)
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

        foreach (var item in visitedAssemblies)
        {
            foreach (var typeSymbol in item.GlobalNamespace.GetNamespaceMembers().SelectMany(ns => ns.GetTypeMembers()))
            {
                ScanType(typeSymbol);
            }
        }
    }
}