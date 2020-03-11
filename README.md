[![Build and Test](https://github.com/dafny-lang/dafny.msbuild/workflows/Build%20and%20Test/badge.svg)](https://github.com/dafny-lang/dafny.msbuild/actions?query=workflow%3A%22Build+and+Test%22)

# dafny.msbuild
MSBuild tasks for use in projects containing Dafny source code

## Publishing NuGet package

First make sure you've configured your API key using `nuget setApiKey`.

```
> cd Source/dafny.msbuild
> dotnet build --configuration Release
Microsoft (R) Build Engine version 16.4.0+e901037fe for .NET Core
Copyright (C) Microsoft Corporation. All rights reserved.

  Restore completed in 29.1 ms for /Users/salkeldr/Documents/GitHub/dafny.msbuild/Source/dafny.msbuild/dafny.msbuild.csproj.
  dafny.msbuild -> /Users/salkeldr/Documents/GitHub/dafny.msbuild/Source/dafny.msbuild/bin/Release/netcoreapp3.0/dafny.msbuild.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.01
> nuget push bin/Release/dafny.msbuild.0.3.0.nupkg -s https://api.nuget.org/v3/index.json
info : Pushing dafny.msbuild.0.3.0.nupkg to 'https://www.nuget.org/api/v2/package'...
info :   PUT https://www.nuget.org/api/v2/package/
warn : All published packages should have license information specified. Learn more: https://aka.ms/deprecateLicenseUrl.
info :   Created https://www.nuget.org/api/v2/package/ 832ms
info : Your package was pushed.
```
