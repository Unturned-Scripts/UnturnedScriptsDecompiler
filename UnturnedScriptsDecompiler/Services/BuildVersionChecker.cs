using System;
using System.Collections.Generic;
using System.Text;
using ValveKeyValue;

namespace UnturnedScriptsDecompiler.Services
{
    internal class BuildVersionChecker
    {
        public string OutputPath { get; }
        public string SteamPath { get; }
        private string? BuildId { get; set; }

        public BuildVersionChecker(string outputPath, string steamPath)
        {
            OutputPath = outputPath;
            SteamPath = steamPath;
        }

        public async Task<string> GetBuildIdAsync()
        {
            if (BuildId == null)
                await IsNewBuildAsync(); // Populates BuildId & Updates .buildid File

            return BuildId!;
        }

        public async Task<bool> IsNewBuildAsync()
        {
            string currentBuildIdPath = Path.Combine(OutputPath, ".buildid");
            BuildId = await GetSteamBuildIdAsync();

            if (!File.Exists(currentBuildIdPath))
                goto newBuild;

            string currentBuildId = await File.ReadAllTextAsync(currentBuildIdPath, Encoding.UTF8);
            if (BuildId == currentBuildId) return false;

        newBuild:
            await File.WriteAllTextAsync(currentBuildIdPath, BuildId, Encoding.UTF8);
            return true;
        }

        private async Task<string> GetSteamBuildIdAsync()
        {
            string appManifestPath = Path.Combine(SteamPath, "appmanifest_304930.acf");

            await using var stream = File.OpenRead(appManifestPath);
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
            var appManifest = kv.Deserialize(stream);

            return appManifest["buildid"].ToString();
        }
    }
}
