using System;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    AbsolutePath PackagePublishPath => AbsolutePath.Create("publish");
    AbsolutePath ProjectToPackage => AbsolutePath.Create("ResilienceDecorators.MySql");
    
    Target Compile => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(b=>
                b.SetConfiguration(Configuration));
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTasks.DotNetTest();
        });

    Target Pack => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetPack(c => 
                c.SetOutputDirectory(PackagePublishPath)
                    .SetProject(ProjectToPackage)
                    .SetConfiguration(Configuration));
        });

    Target PushToNuget => _ => _
        .DependsOn(Pack)
        .Executes(() =>
        {
            DotNetTasks.DotNetNuGetPush(n => n
                .SetApiKey(Environment.GetEnvironmentVariable("NUGET_API_KEY"))
                .SetSource("https://api.nuget.org/v3/index.json")
                .SetSkipDuplicate(true)
                .SetTargetPath(PackagePublishPath / ProjectToPackage + ".*.nupkg"));
        });
}
