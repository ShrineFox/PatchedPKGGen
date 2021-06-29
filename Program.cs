using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PatchedPKGGen
{
    class Program
    {
        public static List<Tuple<string, string>> games = new List<Tuple<string, string>>()
        {
            new Tuple<string,string>( "CUSA17416", "Persona 5 Royal USA" ),
            new Tuple<string,string>( "CUSA17419", "Persona 5 Royal EU" ),
            new Tuple<string,string>( "CUSA06638", "Persona 5 (PS4) EU" )
        };

        public static List<Tuple<string, string>> toggles = new List<Tuple<string, string>>()
        {
            new Tuple<string,string>( "mod_support2", "Mod Support Alt by Lipsum" ),
            new Tuple<string,string>( "intro_skip", "Intro Skip by Lipsum" ),
            new Tuple<string,string>( "square", "Global Square Menu by Lipsum" ),
            new Tuple<string,string>( "all_dlc", "Content Enabler by Lipsum" ),
            new Tuple<string,string>( "no_trp", "Disable Trophies by Lipsum" ),
            new Tuple<string,string>( "env", "ENV Tests< by Lipsum" ),
            new Tuple<string,string>( "zzz", "Random Tests by Lipsum" ),
            new Tuple<string,string>( "mod_support", "Mod Support by Lipsum" ),
        };

        public static List<Tuple<string, string>> togglesR = new List<Tuple<string, string>>()
        {
            new Tuple<string,string>( "mod_support2", "Mod Support Alt by Lipsum" ),
            new Tuple<string,string>( "intro_skip", "Intro Skip by Lipsum" ),
            new Tuple<string,string>( "0505", "PS4 FW 5.05 Backport by Lipsum" ),
            new Tuple<string,string>( "square", "Global Square Menu by Lipsum" ),
            new Tuple<string,string>( "all_dlc", "Content Enabler by Lipsum" ),
            new Tuple<string,string>( "no_trp", "Disable Trophies by Lipsum" ),
            new Tuple<string,string>( "p5_save", "P5 Save Bonus Enabler by Lipsum" ),
            new Tuple<string,string>( "env", "ENV Tests< by Lipsum" ),
            new Tuple<string,string>( "zzz", "Random Tests by Lipsum" ),
            new Tuple<string,string>( "mod_support", "Mod Support by Lipsum" ),
        };

        public static IEnumerable<T[]> Permutations<T>(IEnumerable<T> source)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));

            T[] data = source.ToArray();

            return Enumerable
              .Range(0, 1 << (data.Length))
              .Select(index => data
                 .Where((v, i) => (index & (1 << i)) != 0)
                 .ToArray());
        }

        static void Main(string[] args)
        {
            foreach (var game in games)
            {
                if (game.Item1 == "CUSA06638")
                    CreatePatches(game.Item1, toggles);
                else
                    CreatePatches(game.Item1, togglesR);
            }
        }

        private static void CreatePatches(string game, List<Tuple<string, string>> patches)
        {
            var combinations = Permutations(patches);
            int i = 1;
            int total = combinations.Count();

            foreach (var combination in combinations.Where(x => x.Count() > 0))
            {
                // Get list of patches in this combo
                List<string> combinationNames = new List<string>();
                foreach (var patch in combination) { combinationNames.Add(patch.Item1); }
                string combinationNamesList = "";
                foreach (var patchName in combinationNames) { combinationNamesList += $"{patchName} "; }

                Console.WriteLine($"Patching {game} EBOOT with {combinationNamesList} ({i}/{combinations.Count()})...");

                //Kill cmd processes
                foreach (var process in Process.GetProcessesByName("cmd"))
                    process.Kill();

                //Patch eboot and create new PKG at location
                string description = "";
                string path = Environment.CurrentDirectory + $"\\ppp\\{game}";
                CreateFolder(path);
                foreach (var patch in combination)
                {
                    // Set up paths and Update PKG description
                    path = Path.Combine(path, patch.Item1);
                    CreateFolder(path);
                    description += patch.Item2 + Environment.NewLine;
                }

                // Copy EBOOT to Directory
                File.Copy(game + ".bin", Path.Combine(path, "eboot.bin"));

                // Patch EBOOT
                using (WaitForFile(Path.Combine(path, "eboot.bin"), FileMode.Open, FileAccess.ReadWrite, FileShare.None, 100)) { };
                Patch(path, combinationNamesList);

                // If Patched EBOOT exists...
                using (WaitForFile(Path.Combine(path, "eboot.bin--patched.bin"), FileMode.Open, FileAccess.ReadWrite, FileShare.None, 100)) { };
                if (File.Exists(Path.Combine(path, "eboot.bin--patched.bin")))
                {
                    // Rename back to eboot.bin
                    Console.WriteLine("  Patch Successful!");
                    File.Delete(Path.Combine(path, "eboot.bin"));
                    File.Move(Path.Combine(path, "eboot--patched.bin"), Path.Combine(path, "eboot.bin"));

                    // Create PKG and edit metadata with description
                    Console.WriteLine("  Creating Update PKG...");
                    CreatePKG(game, path, description);
                    if (File.Exists(""))
                    {
                        Console.WriteLine("  PKG Created Successfully!");
                    }
                    else
                        Console.WriteLine("  PKG Creation Failed.");
                }
                else
                {
                    Console.WriteLine("  Patch Failed.");
                }
                i++;
            }

            Console.WriteLine($"Done");
            Console.ReadKey();
        }

        private static void CreatePKG(string game, string path, string description)
        {
            string programPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string outputPKG = ""; // Default output PKG
            switch (game) 
            {
                case "CUSA17416":
                    outputPKG = Path.Combine(path, $"UP0177-CUSA17416_00-PERSONA5R0000000-A0101-V0100.pkg");
                    break;
                case "CUSA17419":
                    outputPKG = Path.Combine(path, $"EP0177-CUSA17419_00-PERSONA5R0000000-A0101-V0100.pkg");
                    break;
                case "CUSA06638":
                    outputPKG = Path.Combine(path, $"EP4062-CUSA06638_00-PERSONA512345678-A0101-V0100.pkg");
                    break;
            } 
            string newOutputPKG = Path.Combine(path, $"{game}_Patch.PKG"); // Renamed output PKG
            string eboot = Path.Combine(path, $"eboot.bin"); // Patched EBOOT

            // Remove existing output PKGs
            if (File.Exists(outputPKG))
                File.Delete(outputPKG);
            if (File.Exists(newOutputPKG))
                File.Delete(newOutputPKG);

            // Update PKG description
            Console.WriteLine("  Updating PKG description...");
            File.WriteAllText($"{programPath}\\GenGP4\\{game}-patch\\sce_sys\\changeinfo\\changeinfo.xml", $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<changeinfo>\n  <changes app_ver=\"01.01\">\n    <![CDATA[\n{description}\n    ]]>\n  </changes>\n</changeinfo>");

            // Replace EBOOT with new patched one
            Console.WriteLine("  Replacing EBOOT in PKG...");
            File.Copy(eboot, $"{programPath}\\GenGP4\\{game}-patch\\eboot.bin", true);
            File.Delete(eboot);

            //Create PKG from GP4
            Console.WriteLine("  Running orbis-pub-cmd...");
            RunCMD($"{programPath}\\GenGP4\\orbis-pub-cmd.exe", $"img_create --oformat pkg --tmp_path ./ {game}-patch.gp4 ./");

            //Rename and move PKG after finished (make sure it exists and isn't full of blank bytes)
            while (!File.Exists(outputPKG) || new FileInfo(outputPKG).Length <= 0) { Thread.Sleep(1000); }
            using (WaitForFile(outputPKG, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 100)) { };
            while (true)
            {
                if (File.ReadAllBytes(outputPKG)[0] == 0x7F)
                    break;
            }
            foreach (Process proc in Process.GetProcessesByName("orbis-pub-cmd"))
                proc.Kill();
            foreach (Process proc in Process.GetProcessesByName("cmd"))
                proc.Kill();
            Console.WriteLine("  Renaming output PKG...");
            File.Move(outputPKG, newOutputPKG);
            Console.WriteLine("  Removing temp folder...");
            Directory.Delete(Path.Combine($"{programPath}\\GenGP4", "ps4pub"), true);
        }

        private static void CreateFolder(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            Directory.CreateDirectory(path);
        }

        private static void Patch(string path, string combinationNamesList)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.WorkingDirectory = path;
            start.FileName = "cmd";
            start.Arguments = $"/C python patch.py eboot.bin --patch {combinationNamesList}";
            start.UseShellExecute = false;
            start.RedirectStandardOutput = false;
            start.CreateNoWindow = false;
            using (Process process = Process.Start(start))
            {
                process.WaitForExit();
            }
        }

        private static void RunCMD(string filename, string args)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "cmd";
            start.WorkingDirectory = Path.GetDirectoryName(filename);
            start.Arguments = $"/K {Path.GetFileName(filename)} {args}";
            start.UseShellExecute = true;
            start.RedirectStandardOutput = false;
            //start.WindowStyle = ProcessWindowStyle.Hidden;

            using (Process process = Process.Start(start))
            {

            }
        }

        public static void CopyDir(string sourceFolder, string destFolder)
        {
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);

            // Get Files & Copy
            string[] files = Directory.GetFiles(sourceFolder);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                string dest = Path.Combine(destFolder, name);
                File.Copy(file, dest, true);
            }

            // Get dirs recursively and copy files
            string[] folders = Directory.GetDirectories(sourceFolder);
            foreach (string folder in folders)
            {
                string name = Path.GetFileName(folder);
                string dest = Path.Combine(destFolder, name);
                CopyDir(folder, dest);
            }
        }

        public static FileStream WaitForFile(string fullPath, FileMode mode, FileAccess access, FileShare share, int sleepNumber)
        {
            for (int numTries = 0; numTries < 10; numTries++)
            {
                FileStream fs = null;
                try
                {
                    fs = new FileStream(fullPath, mode, access, share);
                    return fs;
                }
                catch (IOException)
                {
                    if (fs != null)
                    {
                        fs.Dispose();
                    }
                    Thread.Sleep(sleepNumber);
                }
            }

            return null;
        }

        private static void RemoveEmptyDirs(string dir)
        {
            foreach (var directory in Directory.GetDirectories(dir))
            {
                RemoveEmptyDirs(directory);
                if (Directory.GetFiles(directory).Length == 0 &&
                    Directory.GetDirectories(directory).Length == 0)
                {
                    Directory.Delete(directory, false);
                }
            }
        }
    }
}
