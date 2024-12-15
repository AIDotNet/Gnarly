using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Gnarly
{
    [Generator(LanguageNames.CSharp)]
    public class ServiceRegistrationGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 注册一个增量生成器
            var provider = context.CompilationProvider.Combine(context.AnalyzerConfigOptionsProvider);
            context.RegisterSourceOutput(provider, (spc, source) => Execute(source.Left, spc));
        }

        private void Execute(Compilation compilation, SourceProductionContext context)
        {
            var scopeMethods = new List<string>();
            var singletonMethods = new List<string>();
            var transientMethods = new List<string>();

            ScanService.ScanAndCollect(compilation, scopeMethods, singletonMethods, transientMethods);

            var scopeRegistrations = string.Join("\n            ", scopeMethods.Select(m => $"services.AddScoped<{m}>();"));
            var singletonRegistrations =
                string.Join("\n            ", singletonMethods.Select(m => $"services.AddSingleton<{m}>();"));
            var transientRegistrations =
                string.Join("\n            ", transientMethods.Select(m => $"services.AddTransient<{m}>();"));

            // 生成源代码
            string source = $@"// Gnarly自动生成的源代码，请勿手动修改
using System;
using Microsoft.Extensions.DependencyInjection;

namespace {compilation.AssemblyName}
{{
    public static class {compilation.AssemblyName?.Replace(".","")}Extensions
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

            // 添加生成的源代码到编译单元
            context.AddSource($"{compilation.AssemblyName?.Replace(".", "")}Extensions.g.cs", SourceText.From(source, Encoding.UTF8));
        }

        private string GetDebuggerDisplay()
        {
            return ToString();
        }
    }
}