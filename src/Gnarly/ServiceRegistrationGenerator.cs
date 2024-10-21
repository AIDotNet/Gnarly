using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Gnarly.Data;
using System;

namespace Gnarly
{
    [Generator]
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ServiceRegistrationGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {

            var compilation = context.Compilation;

            var scopeMethods = new List<string>();
            var singletonMethods = new List<string>();
            var transientMethods = new List<string>();

            foreach (var (type, namedTypeSymbol) in ScanService.ScanForDependencyTypes(compilation))
            {
                // 搜索namedTypeSymbol是否存在RegistrationAttribute特性,然后得到RegistrationAttribute具体类型
                var registrationAttribute = namedTypeSymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "RegistrationAttribute");

                if (registrationAttribute != null && registrationAttribute.ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol is { } registrationType && registrationType.TypeKind == TypeKind.Interface)
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
                // namedTypeSymbol中存在相同名称的接口I 
                else if (namedTypeSymbol.AllInterfaces.Any(i => i.Name == $"I{namedTypeSymbol.Name}"))
                {
                    var namedTypeSymbolInterface = namedTypeSymbol.AllInterfaces.First(i => i.Name.ToLower() == $"I{namedTypeSymbol.Name}".ToLower());

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
            }

            var scopeRegistrations = string.Join("\n", scopeMethods.Select(m => $"services.AddScoped<{m}>();"));

            var singletonRegistrations = string.Join("\n", singletonMethods.Select(m => $"services.AddSingleton<{m}>();"));

            var transientRegistrations = string.Join("\n", transientMethods.Select(m => $"services.AddTransient<{m}>();"));

            // Build up the source code
            string source = $@"// 这是由SourceGenerator自动生成的代码
using System;
using Microsoft.Extensions.DependencyInjection;

namespace System
{{
    public static class GnarlyExtensions
    {{
        /// <summary>
        /// SourceGenerator自动注入服务
        /// </summary>
        public static IServiceCollection AddAutoGnarly(this IServiceCollection services)
        {{
            {scopeRegistrations}
            {singletonRegistrations}
            {transientRegistrations}
            return services;
        }}
    }}
}}
";

            // Add the source code to the compilation
            context.AddSource($"Gnarly.g.cs", source);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required for this one
        }

        private string GetDebuggerDisplay()
        {
            return ToString();
        }
    }

}
