#addin nuget:?package=Cake.Git&version=0.19.0
#addin nuget:?package=Cake.CMake&version=0.2.2
#addin nuget:?package=Cake.FileHelpers&version=3.2.0
#addin nuget:?package=SharpZipLib&version=1.1.0
#addin nuget:?package=Cake.Compression&version=0.2.3

#load scripts/mergepublish.cake

var target = Argument("target", "Build");
var versionSetting = Argument("assemblyversion","git");
var configuration = Argument("configuration","Release");
var prefix = Argument("prefix","/usr/local/");
var destdir = Argument("destdir", (string)null);
var parallel = Argument("parallel", -1);

bool CheckCommand_Unix(string cmd) => (StartProcess("/bin/sh", new ProcessSettings() { Arguments = string.Format("-c 'command -v {0}'",cmd) }) == 0);

string GitLogTip_Shell()
{
	if(IsRunningOnWindows()) return GitLogTip(".").Sha;
	//Linux: Cake.Git seems to intermittently fail with method body errors
	//call git from shell first
	if(!CheckCommand_Unix("git")) {
		Warning("BUG: Git not found in PATH, GenerateVersion may fail!");
		return GitLogTip(".").Sha;
	}
	IEnumerable<string> gitOutput;
	StartProcess("git", new ProcessSettings { Arguments = "rev-parse HEAD", RedirectStandardOutput = true }, out gitOutput);
	return gitOutput.FirstOrDefault() ?? "invalid";
}

Task("GenerateVersion")
	.Does(() =>
{
	var version = versionSetting;
	if(version == "git") {
		var lastSha = GitLogTip_Shell();
		version = string.Format("{0}-git ({1})",lastSha.Substring(0,7),DateTime.Now.ToString("yyyyMMdd"));
	}
	FileWriteText(
        Directory("src") + File("CommonVersion.props"),
        $"<!-- This file is AutoGenerated -->\n<Project><PropertyGroup><AssemblyInformationalVersion>{version}</AssemblyInformationalVersion></PropertyGroup></Project>"
    );
	Information("Version: {0}",version);
});

Task("BuildNatives")
    .Does(() =>
{
	//Ensure Directories Exist
	if(!DirectoryExists("obj")) CreateDirectory("obj");
	if(!DirectoryExists("bin")) CreateDirectory("bin");
	if(!DirectoryExists("bin/natives")) CreateDirectory("bin/natives");
	if(!DirectoryExists("bin/natives")) CreateDirectory("bin/natives");
	if(!DirectoryExists("bin/natives/x86")) CreateDirectory("bin/natives/x86");
    if(!DirectoryExists("bin/natives/x64")) CreateDirectory("bin/natives/x64");
	//Build CMake
	if(IsRunningOnWindows())
	{
		//More directories! (this build is involved af)
		if(!DirectoryExists("obj/x86")) CreateDirectory("obj/x86");
		if(!DirectoryExists("obj/x64")) CreateDirectory("obj/x64");
		CopyFiles("./deps/x64/*.dll", "./bin/natives/x64");
		CopyFiles("./deps/x86/*.dll", "./bin/natives/x86");
		//x86 build first!
        CMake(".", new CMakeSettings() {
            OutputPath = "obj/x86",
            Generator = "Visual Studio 16 2019",
            Platform = "Win32"
        });
		var toolVersion = MSBuildToolVersion.VS2019;
		MSBuild("./obj/x86/librelancernatives.sln", new MSBuildSettings() {
			MaxCpuCount = 0, Configuration = "Release", ToolVersion = toolVersion
		});
		CopyFiles("./obj/x86/binaries/*.dll", "./bin/natives/x86/");
		//Then x64
        CMake(".", new CMakeSettings() {
            OutputPath = "obj/x64",
            Generator = "Visual Studio 16 2019",
            Platform = "x64"
        });
		MSBuild("./obj/x64/librelancernatives.sln", new MSBuildSettings() {
			MaxCpuCount = 0, Configuration = "Release", ToolVersion = toolVersion
		});
		CopyFiles("./obj/x64/binaries/*.dll", "./bin/natives/x64/");
	}
	else
	{
		CMake(".",new CMakeSettings() {
			OutputPath = "obj",
			Options = new []{ "-DCMAKE_INSTALL_PREFIX=" + prefix }
		});
		int code;
		string j = "";
		if(parallel > -1) j = "-j" + parallel;
		if((code = StartProcess("make", new ProcessSettings() { WorkingDirectory = "obj", Arguments = j })) != 0)
			throw new Exception("Make exited with error code " + code);
		CopyFiles("obj/binaries/*","./bin/natives/");
	}

});

string[] publishProjects = {
    "src/lancer/lancer.csproj",
    "src/Server/Server.csproj",
    "src/thorn2lua/thorn2lua.csproj",
    "src/Launcher/Launcher.csproj",
    "src/Editor/InterfaceEdit/InterfaceEdit.csproj",
    "src/Editor/LancerEdit/LancerEdit.csproj",
    "src/Editor/SystemViewer/SystemViewer.csproj",
    "src/Editor/ThnPlayer/ThnPlayer.csproj"
};

void DeleteIfExists(string directory)
{
    if(DirectoryExists(directory)) {
        var settings = new DeleteDirectorySettings();
        settings.Recursive = true;
        DeleteDirectory(directory, settings);
    }
}
string GetLinuxRid()
{
    IEnumerable<string> output;
	StartProcess("uname", new ProcessSettings { Arguments = "-m", RedirectStandardOutput = true }, out output);
	string uname = output.FirstOrDefault();
	if(string.IsNullOrEmpty(uname)) return "linux-x64";
	uname = uname.Trim().ToLowerInvariant();
	if(uname.StartsWith("aarch64"))
        return "linux-arm64";
    if(uname.StartsWith("armv"))
        return "linux-arm";
    if(uname.StartsWith("x86_64"))
        return "linux-x64";
    return "linux-x86";
}
void Clean(string rid)
{
    DotNetCoreClean("./src/LibreLancer.sln");
    DeleteIfExists("./obj/projs-" + rid);
    DeleteIfExists("./bin/librelancer-" + rid);
}
void FullBuild(string rid)
{
    foreach(var proj in publishProjects) {
        var name = System.IO.Path.GetFileName(proj);
        var publishSettings = new DotNetCorePublishSettings
        {
            Configuration = "Release",
            OutputDirectory = "./obj/projs-" + rid + "/" + name,
            SelfContained = true,
            Runtime = rid
        };
        DotNetCorePublish(proj, publishSettings);
	}
	MergePublish("./obj/projs-" + rid, "./bin/librelancer-" + rid);
}
Task("Clean")
  .Does(() =>
{
   if(IsRunningOnWindows()) {
        Clean("win7-x86");
        Clean("win7-x64");
   } else
        Clean(GetLinuxRid());
});

Task("Build")
	  .IsDependentOn("GenerateVersion")
      .IsDependentOn("BuildNatives")
	  .Does(() =>
{
	//Restore NuGet packages
	DotNetCoreRestore("./src/LibreLancer.sln");
	//Build C#
	if(IsRunningOnWindows()) {
        FullBuild("win7-x86");
        FullBuild("win7-x64");
	} else
        FullBuild(GetLinuxRid());
});


Task("LinuxDaily")
    .IsDependentOn("Build")
    .Does(() =>
{
	if(!DirectoryExists("packaging/packages")) CreateDirectory("packaging/packages");
	var lastCommit = GitLogTip_Shell();
	Information("Compressing");
	var name = "librelancer-" + lastCommit.Substring(0,7) + "-ubuntu-amd64";
	if(DirectoryExists("packaging/packages/" + name))
		CleanDirectories("packaging/packages/" + name);
	else
		CreateDirectory("packaging/packages/" + name);
	CopyFiles("bin/librelancer-" + GetLinuxRid() + "/*","packaging/packages/" + name);
	GZipCompress("packaging/packages/",
				"packaging/packages/librelancer-daily-ubuntu-amd64.tar.gz", 
				GetFiles("packaging/packages/" + name + "/*")
	);
	DeleteDirectory("packaging/packages/" + name, recursive:true);
	var unixTime = (long)((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds);
	FileWriteText("packaging/packages/timestamp",unixTime.ToString());
});

RunTarget(target);
