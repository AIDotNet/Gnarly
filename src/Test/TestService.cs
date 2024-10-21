using Gnarly.Data;
using System;
using System.Threading.Tasks;

namespace Test
{
    public class TestService : ITestService, ISingletonDependency
    {
        public async Task SendMessageAsync()
        {
            Console.WriteLine("Hello, Gnarly!");
            await Task.CompletedTask;
        }
    }
}