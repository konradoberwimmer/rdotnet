# R.NET

R.NET is an in-process bridge for .NET to access the R statistical language. R.NET is cross-platform and works on Windows, Linux and macOS. Released under an [MIT](./License.txt) license.

## Builds

GitHub Actions |
:---: |
[![Github Actions](https://github.com/AndrewIOM/R.NET/actions/workflows/push.yml/badge.svg?branch=master)](https://github.com/AndrewIOM/R.NET/actions/workflows/push.yml) |

## NuGet 

Package | Stable | Prerelease
--- | --- | ---
R.NET | [![NuGet Badge](https://buildstats.info/nuget/R.NET)](https://www.nuget.org/packages/R.NET/) | [![NuGet Badge](https://buildstats.info/nuget/R.NET?includePreReleases=true)](https://www.nuget.org/packages/R.NET/)
R.NET.FSharp | [![NuGet Badge](https://buildstats.info/nuget/R.NET.FSharp)](https://www.nuget.org/packages/R.NET.FSharp/) | [![NuGet Badge](https://buildstats.info/nuget/R.NET.FSharp?includePreReleases=true)](https://www.nuget.org/packages/R.NET.FSharp/)

## Requirements

R.NET supports .NET Core 2.0 or greater on any platform, or .NET Framework 4.6.1 or greater on Windows. Access to native R libraries as installed with the R environment is also required.

R needs not necessarily be installed as a software on the executing machine, so long as DLL files are accessible (you may need to tweak environment variables for the latter to work, though).

## Getting started

For documentation to get started, the prefered entry point is at [http://rdotnet.github.io/rdotnet](http://rdotnet.github.io/rdotnet).

## Developer Instructions

R.NET uses FAKE to orchestrate building and testing. To build and test:

1. Restore dotnet tools: ```dotnet tool restore```

2. Run FAKE: ```dotnet fake build -t All```

FAKE will clean and build all projects, run tests, and generate nuget packages in the ``bin`` folder.

#### Tests

Unit tests can be run seperately from FAKE using:

```sh
dotnet test tests/RDotNet.Tests/RDotNet.Tests.csproj
```

Normally you should get something like:

```text
Total tests: 92. Passed: 84. Failed: 0. Skipped: 8.
Test Run Successful.
Test execution time: 5.2537 Seconds
```

However note that from time to time (or at the first `dotnet test` execution) tests may fail to start, for reasons as yet unknown:

```text
Starting test execution, please wait...
The active test run was aborted. Reason:
Test Run Aborted.
```

It may be that all subsequent calls then work as expected.

```sh
dotnet test RDotNet.FSharp.Tests/RDotNet.FSharp.Tests.fsproj
```
