# Gnarly

基于源生成器实现的自动依赖注入，只需要继承接口即可实现自动依赖注入，并且不需要反射，自动生成代码，只需要先引用项目，然后添加以下代码

```csharp
builder.Services.AddAutoGnarly();
```

SG的代码会自动注册到`AddAutoGnarly`当中。
