#addin nuget:?package=Cake.SemVer&version=4.0.0
#addin nuget:?package=semver&version=2.0.4
#addin nuget:?package=Cake.FileHelpers&version=3.3.0

using System;
using System.Net.Http;
using System.IO;

var target = Argument("target", "Build");

var packagePublishDirPath = "./publish/";

var semVer = CreateSemVer(1,0,0);
var solutionFilePath = "./MySql.Resilience.sln";
var nugetApiKey = EnvironmentVariable("NUGET_API_KEY");

Setup(ctx=>
{
    var buildNumber = EnvironmentVariable("BUILD_BUILDNUMBER");

    if(!string.IsNullOrWhiteSpace(buildNumber))
    {
        Information($"The build number was {buildNumber}");
        semVer = buildNumber;
    }
    else
    {
        Information($"The build number was empty, using the default semantic version of {semVer.ToString()}");
    }
});

Teardown(ctx=>
{

});

Task("Restore")
    .Does(() => {		
		DotNetCoreRestore(solutionFilePath);	
});

Task("Build")
	.IsDependentOn("Restore")
    .Does(()=>{
		var config = new DotNetCoreBuildSettings
		{
			Configuration = "Release",
			NoRestore = true
		};
        DotNetCoreBuild(solutionFilePath, config);
});

Task("Test")
	 .IsDependentOn("Build")
     .Does(() =>
 {
     var settings = new DotNetCoreTestSettings()
     {
         Verbosity = DotNetCoreVerbosity.Normal
     };
     var testProjectPath = "./ResilienceDecorators.Tests/ResilienceDecorators.Tests.csproj";
     DotNetCoreTest(testProjectPath, settings);
 });

Task("Pack")
	.IsDependentOn("Build")
	.Does(()=>
{

	Information("Packing...");
	var publishSettings = new DotNetCorePackSettings
    {
        Configuration = "Release",
        OutputDirectory = packagePublishDirPath,
	    NoRestore = true
    };    

    DotNetCorePack(
        "./ResilienceDecorators.MySql/ResilienceDecorators.MySql.csproj", 
        publishSettings);
});

Task("PushToNuget")
	.IsDependentOn("Pack")
	.Does(()=>
{
	Information("Publishing to Nuget...");
    var files = GetFiles("./**/ResilienceDecorators.MySql.*.nupkg");

    foreach(var file in files)
    {
        Information("File: {0}", file);

        using(var process = StartAndReturnProcess("dotnet", 
            new ProcessSettings
            { 
                Arguments = $"nuget push {file} --skip-duplicate -n true -s https://api.nuget.org/v3/index.json -k {nugetApiKey}" 
            }))
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

RunTarget(target);