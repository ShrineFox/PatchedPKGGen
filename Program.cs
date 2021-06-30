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
        public static string python = @"C:\Users\Ryan\AppData\Local\Programs\Python\Python39\python.exe";

        public static List<Tuple<string, string>> games = new List<Tuple<string, string>>()
        {
            new Tuple<string,string>( "CUSA17416", "Persona 5 Royal USA" ),
            new Tuple<string,string>( "CUSA17419", "Persona 5 Royal EU" ),
            //new Tuple<string,string>( "CUSA06638", "Persona 5 (PS4) EU" )
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
            string programPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string gamePath = Path.Combine(Path.Combine(Environment.CurrentDirectory, "ppp"), game);
            
            // Remove previous folder contents
            if (Directory.Exists(gamePath))
                Directory.Delete(gamePath, true);
            Console.WriteLine($"Created Empty Game Folder: {gamePath}");

            // Iterate through all combos where both mod_support types aren't present at the same time
            var combinations = Permutations(patches).Where(x => x.Count() > 0 && !(x.Any(z => z.Item1.Equals("mod_support")) && x.Any(z => z.Item1.Equals("mod_support2"))));
            List<Tuple<string, string>[]> combinationCheck = combinations.ToList();
            int i = 1;
            int total = combinations.Count();
            foreach (var combination in combinations)
            {
                List<string> combinationNames = new List<string>(); // New list of patches in combo
                string comboPath = gamePath; // New folder for combos to go in

                foreach (var patch in combination)
                {
                    // Create folder for each patch and save name to args list...
                    combinationNames.Add(patch.Item1);
                    comboPath = Path.Combine(comboPath, patch.Item1);
                    Directory.CreateDirectory(comboPath);
                }

                Console.WriteLine($"Patching {game} EBOOT with {String.Join(", ", combinationNames)} " +
                    $"({i}/{combinations.Count()})...");

                string inputEbootPath = Path.Combine(programPath, $"{game}.bin");
                string patchedEbootPath = Path.Combine(programPath, $"{game}.bin--patched.bin");

                // If input EBOOT exists...
                using (WaitForFile(inputEbootPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 100)) { };
                if (File.Exists(inputEbootPath))
                {
                    // Patch EBOOT
                    Patch(inputEbootPath, String.Join(" ", combinationNames));
                        
                    // If Patched EBOOT exists...
                    using (WaitForFile(patchedEbootPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 100)) { };
                    if (File.Exists(patchedEbootPath))
                    {
                        CreatePKG(game, patchedEbootPath, String.Join(", ", combinationNames));
                    }
                    else
                        Console.WriteLine($"  EBOOT Patch failed. No patched EBOOT found at {patchedEbootPath}");
                }
                else
                    Console.WriteLine($"  EBOOT Patch failed. Could not find input EBOOT at {inputEbootPath}");
                
                i++;
            }
            Console.WriteLine($"Done");
            Console.ReadKey();
        }

        private static void CreatePKG(string game, string ebootPath, string description)
        {
            // Overwrite EBOOT in PKG with patched one
            KillCMD();
            string programPath = Path.GetDirectoryName(ebootPath);
            string newEbootPath = Path.Combine(Path.Combine(Path.Combine(programPath,"GenGP4"), $"{game}-patch"), "eboot.bin");
            File.Copy(ebootPath, newEbootPath, true);
            Console.WriteLine($"  Overwrote PKG EBOOT with patched one.\n" +
                $"  Creating Update PKG...");
            
            string outputPKG = "";
            switch (game) 
            {
                case "CUSA17416":
                    outputPKG = Path.Combine(programPath, $"UP0177-CUSA17416_00-PERSONA5R0000000-A0101-V0100.pkg");
                    break;
                case "CUSA17419":
                    outputPKG = Path.Combine(programPath, $"EP0177-CUSA17419_00-PERSONA5R0000000-A0101-V0100.pkg");
                    break;
                case "CUSA06638":
                    outputPKG = Path.Combine(programPath, $"EP4062-CUSA06638_00-PERSONA512345678-A0101-V0100.pkg");
                    break;
            }
            string temp = Path.Combine(Path.Combine(programPath, "GenGP4"), "temp"); // Temp PKG Builder Folder
            // Delete existing output PKG and Temp folder
            if (File.Exists(outputPKG))
                File.Delete(outputPKG);
            if (Directory.Exists(temp))
                Directory.Delete(temp, true);

            // Update PKG description
            Console.WriteLine("    Updating PKG description...");
            File.WriteAllText($"{programPath}\\GenGP4\\{game}-patch\\sce_sys\\changeinfo\\changeinfo.xml", $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<changeinfo>\n  <changes app_ver=\"01.01\">\n    <![CDATA[\n{description}\n    ]]>\n  </changes>\n</changeinfo>");

            // Create PKG from GP4
            Console.WriteLine("    Running orbis-pub-cmd...");
            RunCMD($"{programPath}\\GenGP4\\orbis-pub-cmd.exe", $"img_create --oformat pkg --tmp_path ./temp {game}-patch.gp4 ./temp");

            //Rename and move PKG after finished (make sure it exists and isn't full of blank bytes)
            while (!File.Exists(outputPKG) || new FileInfo(outputPKG).Length <= 0) { }
            while (true)
            {
                if (File.ReadAllBytes(outputPKG)[0] == 0x7F)
                    break;
            }

            // Clean up temp folders and end processes
            using (WaitForFile(outputPKG, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 100)) { };
            if (File.Exists(outputPKG))
            {
                KillCMD();
                Console.WriteLine($"  PKG Created successfully: {outputPKG}");
            }
            else
                Console.WriteLine($"  PKG Creation failed. No PKG found at: {outputPKG}");
        }

        private static void KillCMD()
        {
            foreach (Process proc in Process.GetProcessesByName("orbis-pub-cmd"))
                proc.Kill();
            foreach (Process proc in Process.GetProcessesByName("cmd"))
                proc.Kill();
        }

        private static void Patch(string ebootPath, string combinationNamesList)
        {
            string args = $"patch.py {Path.GetFileName(ebootPath)} --patch {combinationNamesList}";
            Console.WriteLine($"  Patch Args:\n  {args}");

            Process p = new Process();
            p.StartInfo.FileName = python;
            p.StartInfo.Arguments = args;
            p.StartInfo.WorkingDirectory = Path.GetDirectoryName(ebootPath);
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.Start();
            p.WaitForExit();
        }

        private static void RunCMD(string filename, string args)
        {
            Console.WriteLine($"  PKG Builder Args:\n  {args}");

            Process p = new Process();
            p.StartInfo.FileName = @"C:\Windows\system32\cmd.exe";
            p.StartInfo.Arguments = $"{Path.GetFileName(filename)} {args}";
            p.StartInfo.WorkingDirectory = Path.GetDirectoryName(filename);
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = false;
            p.StartInfo.RedirectStandardInput = false;
            //p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.Start();
            p.WaitForExit();
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
    }
}
