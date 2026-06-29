using UnturnedScriptsDecompiler.Services;

namespace UnturnedScriptsDecompiler
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string output = "C:\\Users\\Admin\\Desktop\\OutputScripts";
            Array.ForEach(Directory.GetDirectories(output), d => Directory.Delete(d, true));
            Array.ForEach(Directory.GetFiles(output), File.Delete);
            
            ScriptDecompiler scriptDecompiler = new("C:\\Users\\Admin\\Desktop\\OutputScripts");
            scriptDecompiler.DecompileScripts("E:\\SteamLibrary\\steamapps\\common\\Unturned\\Unturned_Data\\Managed\\Assembly-CSharp.dll");

            while (true)
            {
                var key = Console.ReadKey();
                if (key.KeyChar == 'q') return;
            }
        }
    }
}
