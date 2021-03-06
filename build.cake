///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var buildNumber = Argument("buildNumber", EnvironmentVariable("GITHUB_RUN_NUMBER") ?? "0");
var branchName = Argument("branchName", "testing").ToLower();
var majorMinorVersion = "0.1";
string GetBuildNumber() => $"{majorMinorVersion}.{buildNumber}";

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Build Library")
.Does(() => {
   DotNetCoreBuild("Cake.BenchmarkDotNet.sln", new DotNetCoreBuildSettings {
      Configuration = configuration,
      
      ArgumentCustomization = args => 
               args
                  .Append($"/p:AssemblyVersion={GetBuildNumber()}")
                  .Append($"/p:AssemblyFileVersion={GetBuildNumber()}")
   });
});

Task("Run Unit Tests")
.IsDependentOn("Build Library")
.Does(() => {
   var settings = new DotNetCoreTestSettings
   {
      Configuration = configuration,
      NoBuild = true,
      NoRestore = true,
   };
   DotNetCoreTest($"Cake.BenchmarkDotNet.sln", settings);
});

Task("Package Library")
.IsDependentOn("Build Library")
.Does(() => {
   CreateNugetPackage(GetBuildNumber());
   CreateNugetPackage($"{GetBuildNumber()}-prerelease");
});


Task("clean")
.Does(() => {
    CleanDirectories("./**/bin");
    CleanDirectories("./**/obj");
});

Task("Set TeamCity Build Number")
.WithCriteria(() => TeamCity.IsRunningOnTeamCity)
.Does(() => {
   BuildSystem.TeamCity.SetBuildNumber(GetBuildNumber());
});

void CreateNugetPackage(string version)
{
   var settings = new DotNetCorePackSettings()
   {
      Configuration = configuration,
      NoBuild = true,
      OutputDirectory = ".",
      NoRestore = true,
      ArgumentCustomization = args => args.Append($"/p:PackageVersion={version}")
   };
   DotNetCorePack("Cake.BenchmarkDotNet/Cake.BenchmarkDotNet.csproj", settings);
}


Task("default")
.IsDependentOn("Build Library")
.IsDependentOn("Run Unit Tests")
.IsDependentOn("Package Library")
.Does(() => {
});

RunTarget(target);
