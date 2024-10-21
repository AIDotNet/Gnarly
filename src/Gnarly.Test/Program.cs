using Gnarly.Test;
using Microsoft.Extensions.DependencyInjection;
using System;

partial class Program
{
    static void Main(string[] args)
    {
        var service = new ServiceCollection();
        service.AddAutoGnarly();

        var serviceProvider = service.BuildServiceProvider();

        var test = serviceProvider.GetService<TestService>();

        test.Test();

    }

}