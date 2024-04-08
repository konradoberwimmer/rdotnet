// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#if FAKE
#r "paket: groupref Build //"
#load "./.fake/build.fsx/intellisense.fsx"
#else
#r "nuget: FAKE.Core.Target"
#r "nuget: FAKE.Core.ReleaseNotes"
#r "nuget: FAKE.DotNet.Cli"
#r "nuget: FAKE.DotNet.Fsi"
#r "nuget: FAKE.DotNet.AssemblyInfoFile"
#r "nuget: FAKE.Tools.Git"
#r "nuget: FAKE.DotNet.Testing.XUnit2"
#r "nuget: System.Reactive"
#r "nuget: MSBuild.StructuredLogger, 2.1.820"

let execContext = Fake.Core.Context.FakeExecutionContext.Create false "build.fsx" []

Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)
#endif

open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.DotNet
open Fake.DotNet.NuGet

// --------------------------------------------------------------------------------------
// Information about the project to be used at NuGet and in AssemblyInfo files
// --------------------------------------------------------------------------------------

let projectName = "R.NET"

let projectSummary =
    "Interoperability library to access the R statistical language runtime from .NET"

let projectDescription =
    """
  A .NET interoperability library to access the R statistical language runtime from .NET languages.
  The library is designed for fast data exchange, in process."""

let authors = [ "Kosei Abe"; "Jean-Michel Perraud" ]
let companyName = ""
let tags = ".NET R R.NET visualization statistics C# F#"

let license = "MIT"

let iconUrl =
    "https://raw.githubusercontent.com/jmp75/rdotnet/master/docs/img/logo.png"

let copyright = "© 2014-2018 Jean-Michel Perraud; © 2013 Kosei, evolvedmicrobe"
let packageProjectUrl = "https://github.com/jmp75/rdotnet/"
let repositoryType = "git"
let repositoryUrl = "https://github.com/jmp75/rdotnet"
let repositoryContentUrl = "https://raw.githubusercontent.com/jmp75/rdotnet"

// Specific conditions for the RDotNet.FSharp package:
let copyrightFSharp = "Unknown??"

// --------------------------------------------------------------------------------------
// The rest of the code is standard F# build script
// --------------------------------------------------------------------------------------

// Read release notes & version info from RELEASE_NOTES.md
System.Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let binDir = __SOURCE_DIRECTORY__ @@ "bin"

let release =
    System.IO.File.ReadLines "RELEASE_NOTES.md" |> Fake.Core.ReleaseNotes.parse

// Generate assembly info files with the right version & up-to-date information
Target.create "AssemblyInfo" (fun _ ->

    AssemblyInfoFile.createFSharpWithConfig
        "src/Common/AssemblyInfo.fs"
        [ Fake.DotNet.AssemblyInfo.Title projectName
          Fake.DotNet.AssemblyInfo.Company companyName
          Fake.DotNet.AssemblyInfo.Product projectName
          Fake.DotNet.AssemblyInfo.Description projectSummary
          Fake.DotNet.AssemblyInfo.Version release.AssemblyVersion
          Fake.DotNet.AssemblyInfo.FileVersion release.AssemblyVersion ]
        (AssemblyInfoFileConfig(false))

    AssemblyInfoFile.createCSharpWithConfig
        "src/Common/AssemblyInfo.cs"
        [ Fake.DotNet.AssemblyInfo.Title projectName
          Fake.DotNet.AssemblyInfo.Company companyName
          Fake.DotNet.AssemblyInfo.Product projectName
          Fake.DotNet.AssemblyInfo.Description projectSummary
          Fake.DotNet.AssemblyInfo.Version release.AssemblyVersion
          Fake.DotNet.AssemblyInfo.FileVersion release.AssemblyVersion ]
        (AssemblyInfoFileConfig(false)))

// --------------------------------------------------------------------------------------
// Check code formatting

let sourceFiles = !! "*.fs"

Target.create "CheckFormat" (fun _ ->
    let result =
        sourceFiles
        |> Seq.map (sprintf "\"%s\"")
        |> String.concat " "
        |> sprintf "%s --check"
        |> DotNet.exec id "fantomas"

    if result.ExitCode = 0 then
        Trace.log "No files need formatting"
    elif result.ExitCode = 99 then
        failwith "Some files need formatting, check output for more info"
    else
        Trace.logf "Errors while formatting: %A" result.Errors)

Target.create "Format" (fun _ ->
    let result =
        sourceFiles
        |> Seq.map (sprintf "\"%s\"")
        |> String.concat " "
        |> DotNet.exec id "fantomas"

    if not result.OK then
        printfn "Errors while formatting all files: %A" result.Messages)


// --------------------------------------------------------------------------------------
// Update the assembly version numbers in the script file.

open System.IO

//Target "UpdateFsxVersions" (fun _ ->
//    let pattern = "packages/RProvider.(.*)/lib"
//    let replacement = sprintf "packages/RProvider.%s/lib" release.NugetVersion
//    let path = "./src/RProvider/RProvider.fsx"
//    let text = File.ReadAllText(path)
//    let text = Text.RegularExpressions.Regex.Replace(text, pattern, replacement)
//    File.WriteAllText(path, text)
//)

// --------------------------------------------------------------------------------------
// Clean build results & restore NuGet packages

Target.create "Clean" (fun _ ->
    Fake.IO.Shell.cleanDirs [ "bin"; "temp" ]

    Fake.IO.Shell.cleanDirs [ "tests/Test.RProvider/bin"; "tests/Test.RProvider/obj" ])

Target.create "CleanDocs" (fun _ -> Fake.IO.Shell.cleanDirs [ "docs/output" ])

// --------------------------------------------------------------------------------------
// Build library & test project

Target.create "Build" (fun _ ->
    Trace.log " --- Building the app --- "

    Fake.DotNet.DotNet.build
        (fun args ->
            { args with
                Configuration = DotNet.BuildConfiguration.Release })
        (projectName + ".sln"))

Target.create "BuildTests" (fun _ ->
    Trace.log " --- Building tests --- "

    DotNet.build
        (fun args ->
            { args with
                Configuration = DotNet.BuildConfiguration.Release })
        (projectName + ".Tests.sln"))

// --------------------------------------------------------------------------------------
// Run the unit tests using test runner & kill test runner when complete

Target.create "RunTests" (fun _ ->
    let rHome = Environment.environVarOrFail "R_HOME"
    Trace.logf "R_HOME is set as %s" rHome

    let result =
        DotNet.exec
            (fun args ->
                { args with
                    Verbosity = Some Fake.DotNet.DotNet.Verbosity.Normal
                    CustomParams = Some "-c Release" })
            "test"
            "tests/RDotNet.Tests/RDotNet.Tests.csproj"

    if result.ExitCode <> 0 then
        failwith "Tests failed")

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target.create "NuGet" (fun _ ->
    // Format the description to fit on a single line (remove \r\n and double-spaces)
    let projectDescription =
        projectDescription.Replace("\r", "").Replace("\n", "").Replace("  ", " ")

    // Format the release notes
    let releaseNotes = release.Notes |> String.concat "\n"

    let properties =
        [ ("Version", release.NugetVersion)
          ("Authors", authors |> String.concat ";")
          ("PackageProjectUrl", packageProjectUrl)
          ("PackageTags", tags)
          ("RepositoryType", repositoryType)
          ("RepositoryUrl", repositoryUrl)
          ("PackageLicenseExpression", license)
          ("PackageRequireLicenseAcceptance", "false")
          ("PackageReleaseNotes", releaseNotes)
          ("Summary", projectSummary)
          ("PackageDescription", projectDescription)
          ("PackageIcon", "logo.png")
          ("PackageIconUrl", iconUrl)
          ("EnableSourceLink", "true")
          ("PublishRepositoryUrl", "true")
          ("EmbedUntrackedSources", "true")
          ("IncludeSymbols", "true")
          ("IncludeSymbols", "false")
          ("SymbolPackageFormat", "snupkg")
          ("Copyright", copyright) ]

    DotNet.pack
        (fun p ->
            { p with
                Configuration = DotNet.BuildConfiguration.Release
                OutputPath = Some "bin"
                MSBuildParams =
                    { p.MSBuildParams with
                        Properties = properties } })
        "src/R.NET/RDotNet.csproj"

    DotNet.pack
        (fun p ->
            { p with
                Configuration = DotNet.BuildConfiguration.Release
                OutputPath = Some "bin"
                MSBuildParams =
                    { p.MSBuildParams with
                        Properties = properties } })
        "src/RDotNet.FSharp/RDotNet.FSharp.fsproj")


// --------------------------------------------------------------------------------------
// Generate the documentation

// There are currently no docs. Uncomment this when docs exist.

// Target.create
//     "DocsMeta"
//     (fun _ ->
//         [ "<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">"
//           "<PropertyGroup>"
//           sprintf "<Copyright>%s</Copyright>" copyright
//           sprintf "<Authors>%s</Authors>" (authors |> String.concat ";")
//           sprintf "<PackageProjectUrl>%s</PackageProjectUrl>" packageProjectUrl
//           sprintf "<RepositoryUrl>%s</RepositoryUrl>" repositoryUrl
//           sprintf "<PackageLicense>%s</PackageLicense>" license
//           sprintf "<PackageReleaseNotes>%s</PackageReleaseNotes>" (List.head release.Notes)
//           sprintf "<PackageIconUrl>%s/master/docs/content/logo.png</PackageIconUrl>" repositoryContentUrl
//           sprintf "<PackageTags>%s</PackageTags>" tags
//           sprintf "<Version>%s</Version>" release.NugetVersion
//           sprintf "<FsDocsLogoSource>%s/master/docs/img/logo.png</FsDocsLogoSource>" repositoryContentUrl
//           sprintf "<FsDocsLicenseLink>%s/blob/master/LICENSE.md</FsDocsLicenseLink>" repositoryUrl
//           sprintf "<FsDocsReleaseNotesLink>%s/blob/master/RELEASE_NOTES.md</FsDocsReleaseNotesLink>" repositoryUrl
//           "<FsDocsWarnOnMissingDocs>true</FsDocsWarnOnMissingDocs>"
//           "<FsDocsTheme>default</FsDocsTheme>"
//           "</PropertyGroup>"
//           "</Project>" ]
//         |> Fake.IO.File.write false "Directory.Build.props")

// Target.create
//     "GenerateDocs"
//     (fun _ ->
//         Fake.IO.Shell.cleanDir ".fsdocs"

//         DotNet.exec
//             id
//             "fsdocs"
//             ("build --clean --properties Configuration=Release --parameters fsdocs-package-version "
//              + release.NugetVersion)
//         |> ignore)

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override


Target.create "All" ignore

"Clean" ==> "AssemblyInfo" ==> "Build"

"Build"
==> "CleanDocs"
// ==> "DocsMeta"
// ==> "GenerateDocs"
==> "All"

"Build" ==> "NuGet" ==> "All"
"Build" ==> "All"
"Build" ==> "BuildTests" ==> "RunTests" ==> "All"

Target.runOrDefault "All"
