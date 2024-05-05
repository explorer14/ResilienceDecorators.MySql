using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.SignTool;
using Nuke.Common.Utilities.Collections;
using Nuke.Common;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.IO;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Tools.SignTool;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.IO;
using Nuke.Common.IO;
using Nuke.Common;
using static Nuke.Common.ControlFlow;
using static Nuke.Common.Logger;
using static Nuke.Common.IO.CompressionTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;
using static Nuke.Common.Tools.SignTool.SignToolTasks;
using static Nuke.Common.Tools.NuGet.NuGetTasks;
using static Nuke.Common.IO.TextTasks;
using static Nuke.Common.IO.XmlTasks;
using static Nuke.Common.EnvironmentInfo;

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Build);
    AbsolutePath PackagePublishDirPath => RootDirectory / "publish";

    object SemVer = CreateSemVer(1, 0, 0);
    AbsolutePath SolutionFilePath => RootDirectory / "MySql.Resilience.sln";

    object NuGetApiKey = GetVariable<string>("NUGET_API_KEY");
    /*
    Setup(ctx =>
    {
        var buildNumber = GetVariable<string>("BUILD_BUILDNUMBER");
        if (!string.IsNullOrWhiteSpace(buildNumber))
        {
            Info($"The build number was {buildNumber}");
            SemVer = buildNumber;
        }
        else
        {
            Info($"The build number was empty, using the default semantic version of {SemVer.ToString()}");
        }
    }); */
    /*
    Teardown(ctx =>
    {
    }); */

    Target Restore => _ => _
        .Executes(() =>
    {
        DotNetRestore(_ => _
            .SetProjectFile(SolutionFilePath));
    });


    Target Build => _ => _
        .DependsOn(Restore)
        .Executes(() =>
    {
        var config = new DotNetCoreBuildSettings
        {
            Configuration = "Release",
            NoRestore = true
        };
        DotNetBuild(_ => _
            .SetProjectFile(SolutionFilePath));
    });


    Target Test => _ => _
        .DependsOn(Build)
        .Executes(() =>
    {
        var settings = new DotNetCoreTestSettings()
        {
            Verbosity = DotNetVerbosity.Normal
        };
        var testProjectPath = RootDirectory / "ResilienceDecorators.Tests" / "ResilienceDecorators.Tests.csproj";
        DotNetTest(_ => _
            .SetProjectFile(testProjectPath));
    });


    Target Pack => _ => _
        .DependsOn(Build)
        .Executes(() =>
    {
        Info("Packing...");
        var publishSettings = new DotNetCorePackSettings
        {
            Configuration = "Release",
            OutputDirectory = PackagePublishDirPath,
            NoRestore = true
        };
        DotNetPack(_ => _
            .SetProjectFile(RootDirectory / "ResilienceDecorators.MySql" / "ResilienceDecorators.MySql.csproj"));
    });


    Target PushToNuget => _ => _
        .DependsOn(Pack)
        .Executes(() =>
    {
        Info("Publishing to Nuget...");
        var files = (RootDirectory).GlobFiles("**/ResilienceDecorators.MySql.*.nupkg");
        foreach (var file in files)
        {
            Info("File: {0}", file);
            using (var process = StartAndReturnProcess("dotnet", new ProcessSettings { Arguments = $"nuget push {file} --skip-duplicate -n true -s https://api.nuget.org/v3/index.json -k {NuGetApiKey}" }))
            {
                process.WaitForExit();
                // This should output 0 as valid arguments supplied
                var exitCode = process.GetExitCode();
                if (exitCode > 0)
                    throw new InvalidOperationException($"Failed to publish to Nuget with exit code {exitCode}.");
            }
        }
    // Enable this once the snupkg error has been resolved by MS:
    // https://github.com/NuGet/Home/issues/8148
    // var settings = new DotNetCoreNuGetPushSettings
    //  {
    //      Source = "https://api.nuget.org/v3/index.json",
    //      ApiKey = nugetApiKey,
    //      // this must be set for some reason because inline path
    //      // concatenation using string interpolation results in
    //      // the file not being found!
    //      WorkingDirectory = DirectoryPath.FromString(packagePublishDirPath),
    //      IgnoreSymbols = true
    //  };
    //  DotNetCoreNuGetPush("ResilienceDecorators.MySql.*.nupkg", settings);
    });
}
