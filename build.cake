///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////

var solutionFile = File("./AzuriteUI.sln");
var artifactsDirectory = "./artifacts";
var testResultsDirectory = "./artifacts/test-results";
var coverageOutputDirectory = "./artifacts/coverage";

// Projects
var testProjects = GetFiles("./tests/**/*.csproj");

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
    Information("========================================");
    Information("Starting build...");
    Information("Solution: {0}", solutionFile);
    Information("Configuration: {0}", configuration);
    Information("Target: {0}", target);
    Information("========================================");
});

Teardown(ctx =>
{
    Information("========================================");
    Information("Build completed!");
    Information("========================================");
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Clean")
    .Description("Cleans the solution output directories")
    .Does(() =>
{
    Information("Cleaning solution...");

    DotNetClean(solutionFile, new DotNetCleanSettings
    {
        Configuration = configuration
    });

    if (DirectoryExists(artifactsDirectory))
    {
        DeleteDirectory(artifactsDirectory, new DeleteDirectorySettings
        {
            Recursive = true,
            Force = true
        });
    }

    Information("Clean completed successfully.");
});

Task("DeepClean")
    .Description("Performs a deep clean - removes all bin, obj, and artifact directories")
    .Does(() =>
{
    Information("Performing deep clean...");

    // Clean the solution first
    DotNetClean(solutionFile, new DotNetCleanSettings
    {
        Configuration = configuration
    });

    // Remove all bin directories
    var binDirectories = GetDirectories("./**/bin");
    foreach (var dir in binDirectories)
    {
        Information("Deleting directory: {0}", dir);
        DeleteDirectory(dir, new DeleteDirectorySettings
        {
            Recursive = true,
            Force = true
        });
    }

    // Remove all obj directories
    var objDirectories = GetDirectories("./**/obj");
    foreach (var dir in objDirectories)
    {
        Information("Deleting directory: {0}", dir);
        DeleteDirectory(dir, new DeleteDirectorySettings
        {
            Recursive = true,
            Force = true
        });
    }

    // Remove artifacts directory (includes test results and coverage)
    if (DirectoryExists(artifactsDirectory))
    {
        Information("Deleting directory: {0}", artifactsDirectory);
        DeleteDirectory(artifactsDirectory, new DeleteDirectorySettings
        {
            Recursive = true,
            Force = true
        });
    }

    // Remove any legacy TestResults directories
    var testResultDirectories = GetDirectories("./**/TestResults");
    foreach (var dir in testResultDirectories)
    {
        Information("Deleting directory: {0}", dir);
        DeleteDirectory(dir, new DeleteDirectorySettings
        {
            Recursive = true,
            Force = true
        });
    }

    // Remove any .db files (test databases)
    var dbFiles = GetFiles("./**/*.db");
    foreach (var file in dbFiles)
    {
        Information("Deleting file: {0}", file);
        DeleteFile(file);
    }

    Information("Deep clean completed successfully.");
});

Task("Restore")
    .Description("Restores NuGet packages for the solution")
    .Does(() =>
{
    Information("Restoring NuGet packages...");

    DotNetRestore(solutionFile);

    Information("Restore completed successfully.");
});

Task("Build")
    .Description("Builds the solution")
    .IsDependentOn("Restore")
    .Does(() =>
{
    Information("Building solution...");

    DotNetBuild(solutionFile, new DotNetBuildSettings
    {
        Configuration = configuration,
        NoRestore = true
    });

    Information("Build completed successfully.");
});

Task("Test")
    .Description("Runs all tests with code coverage and generates reports")
    .IsDependentOn("Rebuild")
    .Does(() =>
{
    Information("Running tests with coverage...");

    // Clean up previous test artifacts
    if (DirectoryExists(testResultsDirectory))
    {
        Information("Removing previous test results...");
        DeleteDirectory(testResultsDirectory, new DeleteDirectorySettings
        {
            Recursive = true,
            Force = true
        });
    }

    if (DirectoryExists(coverageOutputDirectory))
    {
        Information("Removing previous coverage reports...");
        DeleteDirectory(coverageOutputDirectory, new DeleteDirectorySettings
        {
            Recursive = true,
            Force = true
        });
    }

    // Remove any legacy TestResults directories in project folders
    var legacyTestResultDirectories = GetDirectories("./**/TestResults");
    foreach (var dir in legacyTestResultDirectories)
    {
        Information("Removing legacy test results: {0}", dir);
        DeleteDirectory(dir, new DeleteDirectorySettings
        {
            Recursive = true,
            Force = true
        });
    }

    // Create directories
    EnsureDirectoryExists(testResultsDirectory);
    EnsureDirectoryExists(coverageOutputDirectory);

    // Run tests with coverage
    foreach (var project in testProjects)
    {
        Information("Testing: {0}", project.GetFilenameWithoutExtension());

        var testResultDir = MakeAbsolute(Directory(testResultsDirectory)).FullPath;

        DotNetTest(project.FullPath, new DotNetTestSettings
        {
            Configuration = configuration,
            NoBuild = true,
            NoRestore = true,
            Verbosity = DotNetVerbosity.Normal,
            ArgumentCustomization = args => args
                .Append($"--collect:\"XPlat Code Coverage\"")
                .Append("--results-directory")
                .AppendQuoted(testResultDir)
        });
    }

    // Find all coverage files in test results directory
    var coverageFiles = GetFiles(testResultsDirectory + "/**/coverage.*.xml");

    if (coverageFiles.Count == 0)
    {
        Warning("No coverage files found!");
    }
    else
    {
        Information("Found {0} coverage file(s)", coverageFiles.Count);

        // Generate coverage reports
        Information("Generating coverage reports...");

        // Use wildcard pattern to collect all coverage files in one pass
        var coveragePattern = $"{testResultsDirectory}/**/coverage.cobertura.xml";

        ReportGenerator(coverageFiles, coverageOutputDirectory, new ReportGeneratorSettings()
        {
            ReportTypes = [
                ReportGeneratorReportType.MarkdownSummaryGithub,
                ReportGeneratorReportType.TextSummary,
                ReportGeneratorReportType.lcov
            ],
            ClassFilters = [
                "+AzuriteUI.Web*"
            ],
            SourceDirectories = [
                "./src"
            ]
        });


        // Display coverage summary
        var summaryFile = coverageOutputDirectory + "/Summary.txt";
        if (FileExists(summaryFile))
        {
            Information("========================================");
            Information("Coverage Summary:");
            Information("========================================");
            var summary = System.IO.File.ReadAllText(summaryFile);
            Information(summary);
            Information("========================================");
            Information("Test results directory: {0}", MakeAbsolute(Directory(testResultsDirectory)).FullPath);
        }
    }

    Information("Tests completed successfully.");
});

Task("Docker")
    .Description("Builds the Docker container")
    .Does(() =>
{
    Information("Building Docker container...");

    var exitCode = StartProcess("docker", new ProcessSettings
    {
        Arguments = "build -t azurite-ui:latest -f Dockerfile .",
        WorkingDirectory = "."
    });

    if (exitCode != 0)
    {
        throw new Exception($"Docker build failed with exit code {exitCode}");
    }

    Information("Docker container built successfully.");
});

Task("Rebuild")
    .Description("Performs a clean build")
    .IsDependentOn("DeepClean")
    .IsDependentOn("Build");

Task("CI")
    .Description("Runs the full CI pipeline (DeepClean, Build, Test, Docker)")
    .IsDependentOn("Rebuild")
    .IsDependentOn("Test")
    .IsDependentOn("Docker");

Task("Default")
    .Description("Runs the default build (Build)")
    .IsDependentOn("CI");
///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(target);
