using Gnarly.Data;

namespace Gnarly.WebApi;

[Registration(typeof(ITestService))]
public class Test : ITestService, ISingletonDependency
{

}
