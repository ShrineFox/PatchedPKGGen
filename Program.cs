using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
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
            new Game() { Name = "Persona 5 Royal", ID = "p5r", TitleID = "CUSA17419", Region = "eur" },
            //new Game() { Name = "Persona 3 Dancing", ID = "p3d", TitleID = "CUSA12636", Region = "usa" },
            //new Game() { Name = "Persona 4 Dancing", ID = "p4d", TitleID = "CUSA12811", Region = "eur" },
            //new Game() { Name = "Persona 5 Dancing", ID = "p5d", TitleID = "CUSA12380", Region = "usa" }
        };

        //  0505           PS4 FW 5.05 Backport
        //  all_dlc        Content Enabler
        //  dlc_msg        Skip DLC Unlock Messages
        //  intro_skip     Intro Skip
        //  mod            Mod Support (PKG)
        //  mod_efigs      Mod Support EFIGS (PKG)
        //  mod2           Mod Support (FTP)
        //  mod2_efigs     Mod Support EFIGS (FTP)
        //  mod3           Mod Support (FTP HostFS)
        //  mod3_efigs     Mod Support EFIGS (FTP HostFS)
        //  no_trp         Disable Trophies
        //  p5_save        P5 Save Bonus Enabler
        //  share_button   Enable Share Button
        //  square         Global Square Menu
        //  bgm_ord        Sequential Battle BGM
        //  bgm_rnd        Randomized Battle BGM

        public static List<Patch> P5RPatches = new List<Patch>()
        {
            // Required Patches
            new Patch() { ID = "0505", Name = "5.05 Backport", ShortDesc = "Run on firmware 5.05+", Image = "",
                LongDesc = "Allows the game to run on the lowest possible moddable PS4 firmware, and all those above it." , AlwaysOn = true },
            new Patch() { ID = "intro_skip", Name = "Intro Skip", ShortDesc = "Bypass opening logos/movie", Image = "",
                LongDesc = "Skips boot logos and intro movie (can still be viewed in Thieves Den)." , AlwaysOn = true },
            new Patch() { ID = "p5_save", Name = "P5 Save Bonus", ShortDesc = "Enables P5 save bonus without P5 saves present on system", Image = "" , AlwaysOn = true },
            new Patch() { ID = "share_button", Name = "Enable Share Button", ShortDesc = "Enables video recording and screenshots using share button", Image = "", AlwaysOn = true },
            new Patch() { ID = "square", Name = "Global Square Menu", ShortDesc = "Square button menu usable everywhere", Image = "",
                LongDesc = "Enables the square menu globally (e.g. in Thieves Den and in Velvet Room or during events or game sections which disable it).", AlwaysOn = true },
            // Mod Loader Patches (at least one is required)
            new Patch() { ID = "mod2_efigs", Name = "FTP Mod Support (EFIGS)", ShortDesc = "m.cpk file replacement via FTP with optional language support", Image = "https://66.media.tumblr.com/c3f99e21c7edb1df53e7f2fa02117621/tumblr_inline_pl680q6yWy1rp7sxh_500.gif",
                LongDesc = "Loads modded files from a <kbd>m.cpk</kbd> file in <code>/data/p5r</code> on the PS4's internal memory via FTP.<br>Optional language-specific .cpk files take priority when the system language isn't English (i.e. mF.cpk for French, mG.cpk for German etc.)" },
            new Patch() { ID = "mod3_efigs", Name = "FTP HostFS Mod Support (EFIGS)", ShortDesc = "Loose file replacement via system language dependent directories", Image = "https://66.media.tumblr.com/c3f99e21c7edb1df53e7f2fa02117621/tumblr_inline_pl680q6yWy1rp7sxh_500.gif",
                LongDesc = "<b>EXPERIMENTAL</b> - this patch uses a debug function and might be unstable. Loads from loose files placed on the PS4's internal memory via FTP." +
                "<br><code>/data/p5r/bind/</code> (English, always loaded - language specific directories below have a higher priority)" +
                "<br><code>/data/p5r/bindF/</code> (French)" +
                "<br><code>/data/p5r/bindI/</code> (Italian)" +
                "<br><code>/data/p5r/bindG/</code> (German)" +
                "<br><code>/data/p5r/bindS/</code> (Spanish)" },
            // Optional Patches (all_dlc always includes dlc_msg)
            new Patch() { ID = "all_dlc", Name = "Content Enabler", ShortDesc = "Enables on-disc content", Image = "",
                LongDesc = "<b>This will make saves created with this patch incompatible</b> with the game when the patch is disabled!"},
            new Patch() { ID = "dlc_msg", Name = "Skip DLC Messages", ShortDesc = "Skip DLC Messages on New Game", Image = "",
                LongDesc = "Especially useful when using the Content Enabler patch together with a mod that skips the title screen and boots directly into a field." },
            // Optional Patches (bgm_ord or bgm_rnd can only be enabled separate from eachother)
            new Patch() { ID = "bgm_ord", Name = "Sequential Battle BGM", ShortDesc = "Plays different BGM track for each battle", Image = "",
                LongDesc = "A different battle BGM track plays (in order) each time you encounter an enemy, regardless of equipped MC outfit." },
            new Patch() { ID = "bgm_rnd", Name = "Randomized Battle BGM", ShortDesc = "Plays randomly selected BGM track for each battle", Image = "",
                LongDesc = "A different (random) battle BGM track plays each time you encounter an enemy, regardless of equipped MC outfit." },
            // Optional Patch
            new Patch() { ID = "no_trp", Name = "Disable Trophies", ShortDesc = "Prevents the game from unlocking trophies", Image = "" },
        };

        public static List<Patch> P3DPatches = new List<Patch>()
        {
            new Patch() { ID = "mod", Name = "Mod Support", ShortDesc = "mod.cpk file replacement via PKG or FTP", Image = "",
                LongDesc = "Loads modded files from a <kbd>mod.cpk</kbd> file in the PKG's <code>USRDIR</code> directory," +
                           $"<br>or placed in <code>/data/p3d</code> on the PS4's internal memory via FTP." +
                            "<br>The latter takes priority.", AlwaysOn = true },
            new Patch() { ID = "intro_skip", Name = "Intro Skip", ShortDesc = "Bypass opening logos/movie", Image = "",
                LongDesc = "Skips boot logos and intro movie.", AlwaysOn = true },
            new Patch() { ID = "no_trp", Name = "Disable Trophies", ShortDesc = "Prevents the game from unlocking trophies", Image = "" },
            new Patch() { ID = "overlay", Name = "Disable Screenshot Overlay", ShortDesc = "Removes the annoying copyright overlay from in-game screenshots", Image = "", AlwaysOn = true }
        };
        public static List<Patch> P4DPatches = new List<Patch>()
        {
            new Patch() { ID = "mod", Name = "Mod Support", ShortDesc = "mod.cpk file replacement via PKG or FTP", Image = "",
                LongDesc = "Loads modded files from a <kbd>mod.cpk</kbd> file in the PKG's <code>USRDIR</code> directory," +
                           $"<br>or placed in <code>/data/p4d</code> on the PS4's internal memory via FTP." +
                            "<br>The latter takes priority.", AlwaysOn = true },
            new Patch() { ID = "intro_skip", Name = "Intro Skip", ShortDesc = "Bypass opening logos/movie", Image = "",
                LongDesc = "Skips boot logos and intro movie.", AlwaysOn = true },
            new Patch() { ID = "no_trp", Name = "Disable Trophies", ShortDesc = "Prevents the game from unlocking trophies", Image = "" },
        };
        public static List<Patch> P5DPatches = new List<Patch>()
        {
            new Patch() { ID = "mod", Name = "Mod Support", ShortDesc = "mod.cpk file replacement via PKG or FTP", Image = "",
                LongDesc = "Loads modded files from a <kbd>mod.cpk</kbd> file in the PKG's <code>USRDIR</code> directory," +
                           $"<br>or placed in <code>/data/p5d</code> on the PS4's internal memory via FTP." +
                            "<br>The latter takes priority.", AlwaysOn = true },
            new Patch() { ID = "intro_skip", Name = "Intro Skip", ShortDesc = "Bypass opening logos/movie", Image = "",
                LongDesc = "Skips boot logos and intro movie.", AlwaysOn = true },
            new Patch() { ID = "no_trp", Name = "Disable Trophies", ShortDesc = "Prevents the game from unlocking trophies", Image = "" },
            new Patch() { ID = "overlay", Name = "Disable Screenshot Overlay", ShortDesc = "Removes the annoying copyright overlay from in-game screenshots", Image = "", AlwaysOn = true }
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
            // Get all desired P5(R) combos ahead of time so we only have to calculate this once
            var P5RPatchCombos = Permutations(P5RPatches).Where(x => x.Count() > 0).ToList();
            P5RPatchCombos = P5RPatchCombos.Where(x =>
                // Make AlwaysOn options required as part of a permutation
                x.Where(z => z.AlwaysOn.Equals(true)).Count().Equals(P5RPatches.Where(y => y.AlwaysOn.Equals(true)).Count())
                // Require either mod2_efigs or mod3_efigs for mod support
                && x.Any(z => z.ID.Equals("mod2_efigs") || z.ID.Equals("mod3_efigs"))
                // Make sure both mod support types aren't present at the same time
                && !(x.Any(z => z.ID.Equals("mod2_efigs")) && x.Any(z => z.ID.Equals("mod3_efigs")))
                // Make sure both bgm shuffle types aren't present at the same time
                && !(x.Any(z => z.ID.Equals("bgm_ord")) && x.Any(z => z.ID.Equals("bgm_rnd")))
                // Make all_dlc include dlc_msg
                && !((x.Any(z => z.ID.Equals("all_dlc")) && !x.Any(z => z.ID.Equals("dlc_msg"))) 
                    || (x.Any(z => z.ID.Equals("dlc_msg")) && !x.Any(z => z.ID.Equals("all_dlc"))))
            ).ToList();
            
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
                List<string> combinationIDs = new List<string>(); // List of each patch name in combo
                string comboPath = gamePath; // New folder for combo results to go in
                foreach (var patch in combination)
                {
                    // Create path for each patch combination and save name to args list...
                    combinationNames.Add(patch.ID);
                    combinationIDs.Add(patch.ID);
                    comboPath = Path.Combine(comboPath, patch.ID);
                }

                if (combinationIDs.Count != 0)
                {
                    Console.WriteLine($"Patching {game.TitleID} ({game.Name}, {game.Region} EBOOT.BIN with {String.Join(", ", combinationIDs)} " +
                    $"\n({i}/{combinations.Count()})...");

                    // If input EBOOT exists...
                    using (WaitForFile(inputEbootPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 100)) { };
                    if (File.Exists(inputEbootPath))
                    {
                        // Patch EBOOT
                        Patch(inputEbootPath, String.Join(" ", combinationIDs));

                        // If Patched EBOOT exists...
                        using (WaitForFile(patchedEbootPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 100)) { };
                        if (File.Exists(patchedEbootPath))
                            CreatePKG(game.TitleID, patchedEbootPath, String.Join("\n", combinationNames), comboPath);
                        else
                            Console.WriteLine($"  EBOOT Patch failed. No patched EBOOT found at: {patchedEbootPath}");
                    }
                    else
                        Console.WriteLine($"  EBOOT Patch failed. Could not find input EBOOT at: {inputEbootPath}");
                    i++;
                }
            }

            Console.WriteLine($"\nDone Generating Pre-Patched Output for {game.TitleID} ({game.Name}, {game.Region})!");
        }

        private static void CreatePKG(string titleID, string ebootPath, string description, string comboPath)
        {
            string newEbootPath = Path.Combine(Path.Combine(Path.Combine(programPath, "GenGP4"), $"{titleID}-patch"), "eboot.bin");

            // Double-check that previous PKG generation or EBOOT patching isn't still running
            KillCMD();

            // Overwrite EBOOT in PKG with patched one
            using (WaitForFile(ebootPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 100)) { };
            if (File.Exists(newEbootPath))
                using (WaitForFile(newEbootPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 100)) { };
            File.Copy(ebootPath, newEbootPath, true);
            Console.WriteLine($"  Overwrote PKG EBOOT with patched one." +
                $"\n  Creating Update PKG...");
            
            string outputPKG = ""; // Path where we expect output PKG to be generated
            string temp = Path.Combine(Path.Combine(programPath, "GenGP4"), "temp"); // Temp PKG Builder Folder
            switch (titleID) 
            {
                case "CUSA17416": // P5R (USA)
                    outputPKG = Path.Combine(temp, "UP0177-CUSA17416_00-PERSONA5R0000000-A0102-V0100.pkg");
                    break;
                case "CUSA17419": // P5R (EUR)
                    outputPKG = Path.Combine(temp, $"EP0177-CUSA17419_00-PERSONA5R0000000-A0101-V0101.pkg");
                    break;
                case "CUSA06638": // P5 PS4 (EUR)
                    outputPKG = Path.Combine(temp, $"EP4062-CUSA06638_00-PERSONA512345678-A0101-V0100.pkg");
                    break;
                case "CUSA12636": // P3D (USA)
                    outputPKG = Path.Combine(temp, $"UP2611-CUSA12636_00-PERSONA3DUS00000-A0101-V0100.pkg");
                    break;
                case "CUSA12811": // P4D (EUR)
                    outputPKG = Path.Combine(temp, $"EP2475-CUSA12811_00-PERSONA4DEU00000-A0101-V0100.pkg");
                    break;
                case "CUSA12380": // P5D (USA)
                    outputPKG = Path.Combine(temp, $"UP2611-CUSA12380_00-PERSONA5DUS00000-A0101-V0100.pkg");
                    break;
            }

            // Delete existing Temp folder and recreate it
            if (Directory.Exists(temp))
                Directory.Delete(temp, true);
            Directory.CreateDirectory(temp);

            // Update PKG description
            Console.WriteLine("    Updating PKG description...");
            File.WriteAllText($"{programPath}\\GenGP4\\{titleID}-patch\\sce_sys\\changeinfo\\changeinfo.xml", $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<changeinfo>\n  <changes app_ver=\"01.01\">\n    <![CDATA[\nPatched using ShrineFox.com/UpdateCreator\n(based on zarroboogs's ppp):\n\n{description}\n    ]]>\n  </changes>\n</changeinfo>");

            // Create PKG from GP4
            Console.WriteLine("    Running orbis-pub-cmd...");
            Build($"img_create --oformat pkg --tmp_path ./temp {titleID}-patch.gp4 ./temp", outputPKG);

            // Clean up temp folders and end any lingering PKG creation/EBOOT patching processes
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
            try
            {
                foreach (Process proc in Process.GetProcessesByName("orbis-pub-cmd"))
                    proc.Kill();
                foreach (Process proc in Process.GetProcessesByName("cmd"))
                    proc.Kill();
            }
            catch { }
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

        private static void Build(string args, string outputPKG)
        {
            string pkgBuilder = $"{programPath}\\GenGP4\\orbis-pub-cmd.exe";
            Console.WriteLine($"  PKG Builder Args:\n  {args}");

            Process p = new Process();
            p.StartInfo.FileName = pkgBuilder;
            p.StartInfo.Arguments = args;
            p.StartInfo.WorkingDirectory = Path.GetDirectoryName(pkgBuilder);
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.Start();

            // Rename and move PKG after finished (make sure it exists and isn't full of blank bytes)
            using (WaitForFile(outputPKG, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 100)) { };
            while (true)
            {
                if (!File.Exists(outputPKG) || new FileInfo(outputPKG).Length < 870645760)
                    Thread.Sleep(100);
                else
                {
                    var started = DateTime.UtcNow;
                    while ((DateTime.UtcNow - started).TotalMilliseconds < 2000)
                    {
                        try
                        {
                            if (File.ReadAllBytes(outputPKG)[0] == 0x7F)
                                break;
                        }
                        catch { }
                    }
                    break;
                }
            }
            p.Close();
            KillCMD();
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
        public bool AlwaysOn { get; set; } = false;
    }
}
