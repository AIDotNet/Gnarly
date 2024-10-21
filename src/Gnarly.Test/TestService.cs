using Gnarly.Data;

namespace Gnarly.Test;

public class TestService : ISingletonDependency
{
    public void Test()
    {
        Console.WriteLine("Test");
    }
}