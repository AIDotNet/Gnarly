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
    /// <summary>
    /// 扫描程序集中的所有类，找到实现了<see cref="IScopeDependency"/>、<see cref="ISingletonDependency"/>、<see cref="ITransientDependency"/>接口的类ConditionalExpressionSyntax conditionalExpression;
    /// </summary>
    /// <param name="compilation"></param>
    /// <returns></returns>
    public static IEnumerable<(int type, INamedTypeSymbol namedTypeSymbol)> ScanForDependencyTypes(Compilation compilation)
    {
        var iScopeDependencySymbol = compilation.GetTypeByMetadataName("Gnarly.Data.IScopeDependency");
        var iSingletonDependencySymbol = compilation.GetTypeByMetadataName("Gnarly.Data.ISingletonDependency");
        var iTransientDependencySymbol = compilation.GetTypeByMetadataName("Gnarly.Data.ITransientDependency");

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
}