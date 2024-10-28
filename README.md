# Gnarly

基于源生成器实现的自动依赖注入，只需要继承接口即可实现自动依赖注入，并且不需要反射，自动生成代码，只需要先引用项目，然后添加以下代码

<<<<<<< HEAD
## 简单入门

安装NuGet包在我们的Api层

安装NuGet包

```shell
dotnet add package Gnarly 
=======
```csharp
builder.Services.AddAutoGnarly();
>>>>>>> origin/main
```

然后我们只需要在需要注入的项目中在安装NuGet包

```shell
dotnet add package Gnarly.Data
```

然后根据需要注册的生命周期继承不同的接口,比如我们需要注册一个单例，我们只需要继承ISingletonDependency接口

```csharp
public class FileService:  ISingletonDependency
{
}
```

然后我们打开`Program.cs`文件，添加以下代码

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAutoGnarly(); // 添加自动依赖注入,那么SG就会扫描所有依赖的项目然后添加道`AddAutoGnarly`方法中，然后我们就可以直接使用依赖注入了

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapGet("/", async (ITestService service) =>
{
    await service.SendMessageAsync();
    return Results.Ok("Hello, Gnarly!");
});
app.Run();

```
