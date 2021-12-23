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
        public static string python = @"C:\Users\Ryan\AppData\Local\Programs\Python\Python310\python.exe";
        public static string programPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        public static List<Game> Games = new List<Game>()
        {
            //new Game() { Name = "Persona 5 Royal", ID = "p5r", TitleID = "CUSA17416", Region = "usa" },
            //new Game() { Name = "Persona 5 Royal", ID = "p5r", TitleID = "CUSA17419", Region = "eur" },
            new Game() { Name = "Persona 3 Dancing", ID = "p3d", TitleID = "CUSA12636", Region = "usa" },
            new Game() { Name = "Persona 4 Dancing", ID = "p4d", TitleID = "CUSA12811", Region = "eur" },
            new Game() { Name = "Persona 5 Dancing", ID = "p5d", TitleID = "CUSA12380", Region = "usa" }
        };

        public static List<Patch> P5RPatches = new List<Patch>()
        {
            new Patch() { ID = "mod_support", Name = "Mod Support", ShortDesc = "mod.cpk file replacement via PKG", Image = "https://66.media.tumblr.com/c3f99e21c7edb1df53e7f2fa02117621/tumblr_inline_pl680q6yWy1rp7sxh_500.gif",
                LongDesc = "Loads modded files from a <kbd>mod.cpk</kbd> file in the PKG's <code>USRDIR</code> directory." +
                            "<br>Only useful if you're downloading the patched eboot.bin and creating the PKG yourself." },
            new Patch() { ID = "mod_support2", Name = "Mod Support(Alt)", ShortDesc = "mod.cpk file replacement via FTP", Image = "https://66.media.tumblr.com/c3f99e21c7edb1df53e7f2fa02117621/tumblr_inline_pl680q6yWy1rp7sxh_500.gif",
                LongDesc = "Loads modded files from a <kbd>mod.cpk</kbd> file from <code>/data/p5r</code> on the PS4's internal memory via FTP.", Enabled = true },
            new Patch() { ID = "0505", Name = "5.05 Backport", ShortDesc = "Run on firmware 5.05+", Image = "",
                LongDesc = "Allows the game to run on the lowest possible moddable PS4 firmware, and all those above it.", Enabled = true },
            new Patch() { ID = "intro_skip", Name = "Intro Skip", ShortDesc = "Bypass opening logos/movie", Image = "",
                LongDesc = "Skips boot logos and intro movie (can still be viewed in Thieves Den).", Enabled = true },
            new Patch() { ID = "all_dlc", Name = "Content Enabler", ShortDesc = "Enables on-disc content", Image = "",
                LongDesc = "<b>This will make saves created with this patch incompatible</b> with the game when the patch is disabled!"},
            new Patch() { ID = "dlc_msg", Name = "Skip DLC Messages", ShortDesc = "Skip DLC Messages on New Game", Image = "",
                LongDesc = "Especially useful when using the Content Enabler patch together with a mod that skips the title screen and boots directly into a field.", Enabled = true },
            new Patch() { ID = "no_trp", Name = "Disable Trophies", ShortDesc = "Prevents the game from unlocking trophies", Image = "" },
            new Patch() { ID = "square", Name = "Global Square Menu", ShortDesc = "Square button menu usable everywhere", Image = "",
                LongDesc = "Enables the square menu globally (e.g. in Thieves Den and in Velvet Room or during events or game sections which disable it).", Enabled = true },
            new Patch() { ID = "p5_save", Name = "P5 Save Bonus", ShortDesc = "Enables P5 save bonus without P5 saves present on system", Image = "", Enabled = true },
            new Patch() { ID = "env", Name = "ENV Tests", ShortDesc = "Test same ENV on all fields", Image = "",
                LongDesc = "Maps all <code>env/env*.ENV</code> files to <code>env/env0000_000_000.ENV</code>." +
                "<br>Useful for testing custom/swapped ENV files on different fields without replacing them all manually." +
                "<br><b>Crashes the game</b> if <code>env/env0000_000_000.ENV</code> is not present in <kbd>mod.cpk</kbd>."},
            new Patch() { ID = "zzz", Name = "Random Tests", ShortDesc = "Only useful for very specific mod testing scenarios.", Image = "",
                LongDesc = "Only useful for very specific mod testing scenarios." },
            new Patch() { ID = "overlay", Name = "Disable Screenshot Overlay", ShortDesc = "Removes the annoying copyright overlay from in-game screenshots", Image = "", Enabled = true }
        };
        public static List<Patch> P3DPatches = new List<Patch>()
        {
            new Patch() { ID = "mod", Name = "Mod Support", ShortDesc = "mod.cpk file replacement via PKG or FTP", Image = "",
                LongDesc = "Loads modded files from a <kbd>mod.cpk</kbd> file in the PKG's <code>USRDIR</code> directory," +
                           $"<br>or placed in <code>/data/p3d</code> on the PS4's internal memory via FTP." +
                            "<br>The latter takes priority.", Enabled = true },
            new Patch() { ID = "intro_skip", Name = "Intro Skip", ShortDesc = "Bypass opening logos/movie", Image = "",
                LongDesc = "Skips boot logos and intro movie.", Enabled = true },
            new Patch() { ID = "no_trp", Name = "Disable Trophies", ShortDesc = "Prevents the game from unlocking trophies", Image = "" },
            new Patch() { ID = "overlay", Name = "Disable Screenshot Overlay", ShortDesc = "Removes the annoying copyright overlay from in-game screenshots", Image = "", Enabled = true }
        };
        public static List<Patch> P4DPatches = new List<Patch>()
        {
            new Patch() { ID = "mod", Name = "Mod Support", ShortDesc = "mod.cpk file replacement via PKG or FTP", Image = "",
                LongDesc = "Loads modded files from a <kbd>mod.cpk</kbd> file in the PKG's <code>USRDIR</code> directory," +
                           $"<br>or placed in <code>/data/p4d</code> on the PS4's internal memory via FTP." +
                            "<br>The latter takes priority.", Enabled = true },
            new Patch() { ID = "intro_skip", Name = "Intro Skip", ShortDesc = "Bypass opening logos/movie", Image = "",
                LongDesc = "Skips boot logos and intro movie.", Enabled = true },
            new Patch() { ID = "no_trp", Name = "Disable Trophies", ShortDesc = "Prevents the game from unlocking trophies", Image = "" },
        };
        public static List<Patch> P5DPatches = new List<Patch>()
        {
            new Patch() { ID = "mod", Name = "Mod Support", ShortDesc = "mod.cpk file replacement via PKG or FTP", Image = "",
                LongDesc = "Loads modded files from a <kbd>mod.cpk</kbd> file in the PKG's <code>USRDIR</code> directory," +
                           $"<br>or placed in <code>/data/p5d</code> on the PS4's internal memory via FTP." +
                            "<br>The latter takes priority.", Enabled = true },
            new Patch() { ID = "intro_skip", Name = "Intro Skip", ShortDesc = "Bypass opening logos/movie", Image = "",
                LongDesc = "Skips boot logos and intro movie.", Enabled = true },
            new Patch() { ID = "no_trp", Name = "Disable Trophies", ShortDesc = "Prevents the game from unlocking trophies", Image = "" },
            new Patch() { ID = "overlay", Name = "Disable Screenshot Overlay", ShortDesc = "Removes the annoying copyright overlay from in-game screenshots", Image = "", Enabled = true }
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
            // Get all P5(R) combos where both mod_support types aren't present at the same time
            // doing this ahead of time so we only have to calculate this once
            var P5RPatchCombos = Permutations(P5RPatches).Where(x => x.Count() > 0 && !(x.Any(z => z.ID.Equals("mod_support")) && x.Any(z => z.ID.Equals("mod_support2"))));
            
            // Generate PKG and EBOOT.BIN combos for each combination of patches
            foreach (var game in Games)
            {
                switch(game.ID)
                {
                    case "p5r":
                        CreatePatches(game, P5RPatchCombos);
                        break;
                    case "p3d":
                        CreatePatches(game, Permutations(P3DPatches));
                        break;
                    case "p4d":
                        CreatePatches(game, Permutations(P4DPatches));
                        break;
                    case "p5d":
                        CreatePatches(game, Permutations(P5DPatches));
                        break;
                }
            }
        }

        private static void CreatePatches(Game game, IEnumerable<Patch[]> combinations)
        {
            // Paths specific to this operation
            string gamePath = Path.Combine(Path.Combine(Path.Combine(Environment.CurrentDirectory, "ppp"), game.ID), game.TitleID);
            string inputEbootPath = Path.Combine(programPath, $"{game.TitleID}.bin");
            string patchedEbootPath = Path.Combine(programPath, $"{game.TitleID}.bin--patched.bin");

            // Remove previous folder contents and recreate gamePath
            if (Directory.Exists(gamePath))
                Directory.Delete(gamePath, true);
            Directory.CreateDirectory(gamePath);
            Console.WriteLine($"\n\nCleared and Recreated Game Folder: {gamePath}");

            // For tracking progress
            int i = 1; 
            int total = combinations.Count();

            // For each combination, patch EBOOT, copy to folder and rename, & generate new PKG
            foreach (var combination in combinations)
            {
                List<string> combinationNames = new List<string>(); // List of each patch name in combo
                string comboPath = gamePath; // New folder for combo results to go in
                foreach (var patch in combination)
                {
                    // Create path for each patch combination and save name to args list...
                    combinationNames.Add(patch.ID);
                    comboPath = Path.Combine(comboPath, patch.ID);
                }

                Console.WriteLine($"Patching {game} EBOOT with {String.Join(", ", combinationNames)} " +
                    $"\n({i}/{combinations.Count()})...");

                // If input EBOOT exists...
                using (WaitForFile(inputEbootPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 100)) { };
                if (File.Exists(inputEbootPath))
                {
                    // Patch EBOOT
                    Patch(inputEbootPath, String.Join(" ", combinationNames));
                        
                    // If Patched EBOOT exists...
                    using (WaitForFile(patchedEbootPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 100)) { };
                    if (File.Exists(patchedEbootPath))
                        CreatePKG(game.TitleID, patchedEbootPath, String.Join(", ", combinationNames), comboPath);
                    else
                        Console.WriteLine($"  EBOOT Patch failed. No patched EBOOT found at: {patchedEbootPath}");
                }
                else
                    Console.WriteLine($"  EBOOT Patch failed. Could not find input EBOOT at: {inputEbootPath}");
                
                i++;
            }
            Console.WriteLine($"\nDone Generating Pre-Patched Output for {game.TitleID} ({game.Name}, {game.Region})!");
        }

        private static void CreatePKG(string game, string ebootPath, string description, string comboPath)
        {
            string newEbootPath = Path.Combine(Path.Combine(Path.Combine(programPath, "GenGP4"), $"{game}-patch"), "eboot.bin");

            // Double-check that previous PKG generation or EBOOT patching isn't still running
            KillCMD();

            // Overwrite EBOOT in PKG with patched one
            File.Copy(ebootPath, newEbootPath, true);
            Console.WriteLine($"  Overwrote PKG EBOOT with patched one." +
                $"\n  Creating Update PKG...");
            
            string outputPKG = ""; // Path where we expect output PKG to be generated
            switch (game) 
            {
                case "CUSA17416": // P5R (USA)
                    outputPKG = Path.Combine(programPath, $"UP0177-CUSA17416_00-PERSONA5R0000000-A0101-V0100.pkg");
                    break;
                case "CUSA17419": // P5R (EUR)
                    outputPKG = Path.Combine(programPath, $"EP0177-CUSA17419_00-PERSONA5R0000000-A0101-V0100.pkg");
                    break;
                case "CUSA06638": // P5 PS4 (EUR)
                    outputPKG = Path.Combine(programPath, $"EP4062-CUSA06638_00-PERSONA512345678-A0101-V0100.pkg");
                    break;
                case "CUSA12636": // P3D (USA)
                    outputPKG = Path.Combine(programPath, $"UP2611-CUSA12636_00-PERSONA3DUS00000-A0101-V0100.pkg");
                    break;
                case "CUSA12811": // P4D (EUR)
                    outputPKG = Path.Combine(programPath, $"EP2475-CUSA12811_00-PERSONA4DEU00000-A0101-V0100.pkg");
                    break;
                case "CUSA12380": // P5D (USA)
                    outputPKG = Path.Combine(programPath, $"UP2611-CUSA12380_00-PERSONA5DUS00000-A0101-V0100.pkg");
                    break;
            }

            string temp = Path.Combine(Path.Combine(programPath, "GenGP4"), "temp"); // Temp PKG Builder Folder
            // Delete existing Temp folder and recreate it
            if (Directory.Exists(temp))
                Directory.Delete(temp, true);
            Directory.CreateDirectory(temp);

            // Update PKG description
            Console.WriteLine("    Updating PKG description...");
            File.WriteAllText($"{programPath}\\GenGP4\\{game}-patch\\sce_sys\\changeinfo\\changeinfo.xml", $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<changeinfo>\n  <changes app_ver=\"01.01\">\n    <![CDATA[\n{description}\n    ]]>\n  </changes>\n</changeinfo>");

            // Create PKG from GP4
            Console.WriteLine("    Running orbis-pub-cmd...");
            RunCMD($"{programPath}\\GenGP4\\orbis-pub-cmd.exe", $"img_create --oformat pkg --tmp_path ./temp {game}-patch.gp4 ./temp");

            // Rename and move PKG after finished (make sure it exists and isn't full of blank bytes)
            while (!File.Exists(outputPKG) || new FileInfo(outputPKG).Length <= 0) { }
            while (true)
            {
                if (File.ReadAllBytes(outputPKG)[0] == 0x7F)
                    break;
            }

            // Clean up temp folders and end any lingering PKG creation/EBOOT patching processes
            using (WaitForFile(outputPKG, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 100)) { };
            if (File.Exists(outputPKG))
            {
                KillCMD();
                Console.WriteLine($"  PKG Created successfully: {outputPKG}");
                // Copy PKG and EBOOT to permutation folder
                Directory.CreateDirectory(comboPath);
                File.Copy(outputPKG, Path.Combine(comboPath, Path.GetFileName(outputPKG)));
                File.Copy(newEbootPath, Path.Combine(comboPath, "eboot.bin"));
                Console.WriteLine($"    Copied Patched PKG and EBOOT to: {comboPath}");
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

    public class Game
    {
        public string Name { get; set; } = "";
        public string ID { get; set; } = "";
        public string TitleID { get; set; } = "";
        public string Region { get; set; } = "";
    }

    public class Patch
    {
        public string Name { get; set; } = "";
        public string ID { get; set; } = "";
        public string ShortDesc { get; set; } = "";
        public string LongDesc { get; set; } = "";
        public string Image { get; set; } = "";
        public bool Enabled { get; set; } = false;
    }
}
