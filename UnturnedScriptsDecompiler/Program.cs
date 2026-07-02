using System.Text;
using System.Text.Json.Nodes;
using UnturnedScriptsDecompiler.Services;

namespace UnturnedScriptsDecompiler
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("You must Specify OutputPath and SteamPath");
                return 1;
            }

            string outputPath = Path.GetFullPath(args[0]);
            if (!Directory.Exists(outputPath))
            {
                Console.WriteLine("OutputPath doesn't Exist!");
                return 1;
            }

            string steamPath = Path.GetFullPath(args[1]);
            if (!Directory.Exists(steamPath))
            {
                Console.WriteLine("SteamPath doesn't Exist!");
                return 1;
            }

            bool ignoreBuildVersion = false;
            if (args.Length >= 3 && !bool.TryParse(args[2], out ignoreBuildVersion))
            {
                Console.WriteLine("Unable to Parse IgnoreBuildVersion!");
                return 1;
            }

            BuildVersionChecker buildVersionChecker = new(outputPath, steamPath);
            if (!ignoreBuildVersion && !await buildVersionChecker.IsNewBuildAsync())
            {
                Console.WriteLine("Build is not Newer! Skipping..");
                return 2;
            }

            string fileDLLPath = Path.Combine(steamPath, "Unturned_Data", "Managed", "Assembly-CSharp.dll");
            if (!File.Exists(fileDLLPath))
            {
                Console.WriteLine("Assembly-CSharp.dll not Found!");
                return 1;
            }

            ScriptDecompiler scriptDecompiler = new(outputPath);
            scriptDecompiler.DecompileScripts(fileDLLPath);

            using var stream = File.OpenRead(Path.Combine(steamPath, "Status.json"));
            var statusGameSection = (await JsonNode.ParseAsync(stream))?["Game"];

            string version = $"3.{statusGameSection?["Major_Version"] ?? '?'}.{statusGameSection?["Minor_Version"] ?? '?'}.{statusGameSection?["Patch_Version"] ?? '?'}";
            string commitMessage = $"{DateTime.UtcNow:dd MMMM yyyy} - Version {version} ({await buildVersionChecker.GetBuildIdAsync()}){(ignoreBuildVersion ? " [Forced]" : string.Empty)}";
            await File.WriteAllTextAsync(Path.Combine(outputPath, ".commit"), commitMessage);

            return 0;
        }
    }
}
