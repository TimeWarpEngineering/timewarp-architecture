# How to filter tests by name

https://github.com/fixie/fixie/wiki/Command-Line-Arguments#filtering-with---tests

Examples:

```console
dotnet fixie MyTestProject --tests Full.Namespace.MyTestClass.MyTestMethod
```

```console
dotnet fixie MyTestProject --tests MyTestClass
```

```console
dotnet fixie MyTestProject --tests MTC.ShouldValidateThat
```

```console
dotnet fixie MyTestProject --tests MTC.*Validate
```
