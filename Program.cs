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
using ShrineFox.IO;

namespace PatchedPKGGen
{
    class Program
    {
        public static string python = "";
        public static string programPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        public static List<Game> Games = new List<Game>()
        {
            /*new Game() { Name = "Persona 5 Royal", ID = "p5r", TitleID = "CUSA17416", Region = "usa", UpdatePKGMinSize = 870645760,
                UpdatePKGName = "UP0177-CUSA17416_00-PERSONA5R0000000-A0102-V0100.pkg" },
            new Game() { Name = "Persona 5 Royal", ID = "p5r", TitleID = "CUSA17419", Region = "eur", UpdatePKGMinSize = 870645760,
                UpdatePKGName = "EP0177-CUSA17419_00-PERSONA5R0000000-A0101-V0101.pkg" },
            /*new Game() { Name = "Persona 5", ID = "p5", TitleID = "CUSA06638", Region = "eur",
                UpdatePKGName = "EP4062-CUSA06638_00-PERSONA512345678-A0101-V0100.pkg" }, 
            new Game() { Name = "Persona 3 Dancing", ID = "p3d", TitleID = "CUSA12636", Region = "usa", UpdatePKGMinSize = 13631488,
                UpdatePKGName = "UP2611-CUSA12636_00-PERSONA3DUS00000-A0101-V0100.pkg" },*/
            new Game() { Name = "Persona 4 Dancing", ID = "p4d", TitleID = "CUSA12811", Region = "eur", UpdatePKGMinSize = 11993088,
                UpdatePKGName = "EP2475-CUSA12811_00-PERSONA4DEU00000-A0101-V0100.pkg" },
            new Game() { Name = "Persona 5 Dancing", ID = "p5d", TitleID = "CUSA12380", Region = "usa", UpdatePKGMinSize = 13631488,
                UpdatePKGName = "UP2611-CUSA12380_00-PERSONA5DUS00000-A0101-V0100.pkg" }
            
        };

        public static List<Patch> P5RPatches = new List<Patch>()
        {
            // Required Patches
            new Patch() { ID = "0505", Name = "5.05 Backport", ShortDesc = "Run on firmware 5.05+",
                LongDesc = "Allows the game to run on the lowest possible moddable PS4 firmware, and all those above it." , AlwaysOn = true },
            new Patch() { ID = "intro_skip", Name = "Intro Skip", ShortDesc = "Bypass opening logos/movie",
                LongDesc = "Skips boot logos and intro movie (can still be viewed in Thieves Den).", AlwaysOn = true },
            new Patch() { ID = "p5_save", Name = "P5 Save Bonus", ShortDesc = "Enables P5 save bonus without P5 saves present on system", AlwaysOn = true },
            new Patch() { ID = "share_button", Name = "Enable Share Button", ShortDesc = "Enables video recording and screenshots using share button", AlwaysOn = true },
            new Patch() { ID = "square", Name = "Global Square Menu", ShortDesc = "Square button menu usable everywhere",
                LongDesc = "Enables the square menu globally (e.g. in Thieves Den and in Velvet Room or during events or game sections which disable it).", AlwaysOn = true },
            // Mod Loader Patches (at least one is required)
            new Patch() { ID = "mod", Name = "Mod Support", ShortDesc = "mod.cpk file replacement via FTP with optional language support",
                LongDesc = "Loads modded files from a <kbd>mod.cpk</kbd> file in <code>/data/p5r</code> on the PS4's internal memory via FTP." +
                "<br>Also supports optional language-specific .cpk files, which take priority when the system language isn't English (e.x. <kbd>mod_F.cpk</kbd> for French)" +
                "<br>Additionally, loose files can be loaded from <kbd>data/p5r/bind</kbd> or language-dependent equivalent (e.x. <kbd>data/p5r/bind_F</kbd> for French)",
                AlwaysOn = true},
            // Optional Patches (all_dlc always includes dlc_msg)
            new Patch() { ID = "all_dlc", Name = "Content Enabler", ShortDesc = "Enables on-disc content",
                LongDesc = "<b>This will make saves created with this patch incompatible</b> with the game when the patch is disabled!"},
            new Patch() { ID = "dlc_msg", Name = "Skip DLC Messages", ShortDesc = "Skip DLC Messages on New Game",
                LongDesc = "Especially useful when using the Content Enabler patch together with a mod that skips the title screen and boots directly into a field." },
            // Optional Patches (bgm_ord or bgm_rnd can only be enabled separate from eachother)
            new Patch() { ID = "bgm_ord", Name = "Sequential Battle BGM", ShortDesc = "Plays different BGM track for each battle",
                LongDesc = "A different battle BGM track plays (in order) each time you encounter an enemy, regardless of equipped MC outfit." },
            new Patch() { ID = "bgm_rnd", Name = "Randomized Battle BGM", ShortDesc = "Plays randomly selected BGM track for each battle",
                LongDesc = "A different (random) battle BGM track plays each time you encounter an enemy, regardless of equipped MC outfit." },
            // Optional Patch
            new Patch() { ID = "no_trp", Name = "Disable Trophies", ShortDesc = "Prevents the game from unlocking trophies", },
        };

        public static List<Patch> P3DPatches = new List<Patch>()
        {
            new Patch() { ID = "mod", Name = "Mod Support", ShortDesc = "mod.cpk file replacement via FTP with optional language support",
                LongDesc = "Loads modded files from a <kbd>mod.cpk</kbd> file in <code>/data/p3d</code> on the PS4's internal memory via FTP." +
                "<br>Additionally, loose files can be loaded from <kbd>data/p3d/bind</kbd>",
                AlwaysOn = true },
            new Patch() { ID = "intro_skip", Name = "Intro Skip", ShortDesc = "Bypass opening logos/movie",
                LongDesc = "Skips boot logos and intro movie.", AlwaysOn = true },
            new Patch() { ID = "no_trp", Name = "Disable Trophies", ShortDesc = "Prevents the game from unlocking trophies" },
            new Patch() { ID = "overlay", Name = "Disable Screenshot Overlay", ShortDesc = "Removes the annoying copyright overlay from in-game screenshots", AlwaysOn = true }
        };
        public static List<Patch> P4DPatches = new List<Patch>()
        {
            new Patch() { ID = "mod", Name = "Mod Support", ShortDesc = "mod.cpk file replacement via FTP with optional language support",
                LongDesc = "Loads modded files from a <kbd>mod.cpk</kbd> file in <code>/data/p4d</code> on the PS4's internal memory via FTP." +
                "<br>Additionally, loose files can be loaded from <kbd>data/p4d/bind</kbd>",
                AlwaysOn = true },
            new Patch() { ID = "intro_skip", Name = "Intro Skip", ShortDesc = "Bypass opening logos/movie",
                LongDesc = "Skips boot logos and intro movie.", AlwaysOn = true },
            new Patch() { ID = "no_trp", Name = "Disable Trophies", ShortDesc = "Prevents the game from unlocking trophies", },
        };
        public static List<Patch> P5DPatches = new List<Patch>()
        {
            new Patch() { ID = "mod", Name = "Mod Support", ShortDesc = "mod.cpk file replacement via FTP with optional language support",
                LongDesc = "Loads modded files from a <kbd>mod.cpk</kbd> file in <code>/data/p5d</code> on the PS4's internal memory via FTP." +
                "<br>Additionally, loose files can be loaded from <kbd>data/p5d/bind</kbd>",
                AlwaysOn = true },
            new Patch() { ID = "intro_skip", Name = "Intro Skip", ShortDesc = "Bypass opening logos/movie",
                LongDesc = "Skips boot logos and intro movie.", AlwaysOn = true },
            new Patch() { ID = "no_trp", Name = "Disable Trophies", ShortDesc = "Prevents the game from unlocking trophies" },
            new Patch() { ID = "overlay", Name = "Disable Screenshot Overlay", ShortDesc = "Removes the annoying copyright overlay from in-game screenshots", AlwaysOn = true }
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
            // Set Output Log
            Output.LogPath = "log.txt";
            
            // Get Python Installs
            Python.GetInstalls("3.0.0");
            if (Python.FoundLocations.Count() > 0)
            {
                python = Python.FoundLocations.First().Value;
                Output.Log($"Found Python version: {Python.FoundLocations.First().Key}");

                PreparePatches();
            }
            else
            {
                Output.Log($"Could not find a Python installation with version 3.0 or higher. Press any key to exit.");
                Console.ReadKey();
            }
        }

        private static void PreparePatches()
        {
            // Get all desired P5(R) combos ahead of time so we only have to calculate this once
            var P5RPatchCombos = Permutations(P5RPatches).Where(x => x.Count() > 0).ToList();
            P5RPatchCombos = P5RPatchCombos.Where(x =>
                // Make AlwaysOn = true required as part of a permutation
                x.Where(z => z.AlwaysOn.Equals(true)).Count().Equals(P5RPatches.Where(y => y.AlwaysOn.Equals(true)).Count())
                // Make sure both bgm shuffle types aren't present at the same time
                && !(x.Any(z => z.ID.Equals("bgm_ord")) && x.Any(z => z.ID.Equals("bgm_rnd")))
                // Make all_dlc include dlc_msg
                && !((x.Any(z => z.ID.Equals("all_dlc")) && !x.Any(z => z.ID.Equals("dlc_msg")))
                    || (x.Any(z => z.ID.Equals("dlc_msg")) && !x.Any(z => z.ID.Equals("all_dlc"))))
            ).ToList();

            // Generate PKG and EBOOT.BIN combos for each combination of patches
            foreach (var game in Games)
            {
                switch (game.ID)
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
            Output.Log($"\n\nCleared and Recreated Game Folder: {gamePath}");

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
                    Output.Log($"Patching {game.TitleID} ({game.Name}, {game.Region} EBOOT.BIN with {String.Join(", ", combinationIDs)} " +
                    $"\n({i}/{combinations.Count()})...");

                    // If input EBOOT exists...
                    using (FileSys.WaitForFile(inputEbootPath)) { };
                    if (File.Exists(inputEbootPath))
                    {
                        // Patch EBOOT
                        Patch(inputEbootPath, String.Join(" ", combinationIDs));

                        // If Patched EBOOT exists...
                        using (FileSys.WaitForFile(patchedEbootPath)) { };
                        if (File.Exists(patchedEbootPath))
                            CreatePKG(game, patchedEbootPath, String.Join("\n", combinationNames), comboPath);
                        else
                            Output.Log($"EBOOT Patch failed. No patched EBOOT found at: {patchedEbootPath}");
                    }
                    else
                        Output.Log($"EBOOT Patch failed. Could not find input EBOOT at: {inputEbootPath}");
                    i++;
                }
            }

            Output.Log($"\nDone Generating Pre-Patched Output for {game.TitleID} ({game.Name}, {game.Region})!");
        }

        private static void CreatePKG(Game game, string ebootPath, string description, string comboPath)
        {
            string newEbootPath = Path.Combine(Path.Combine(Path.Combine(programPath, "GenGP4"), $"{game.TitleID}-patch"), "eboot.bin");

            // (Re)create PKG generator temp dir
            string temp = Path.Combine(Path.Combine(programPath, "GenGP4"), "temp"); // Temp PKG Builder Folder
            Exe.CloseProcess("orbis-pub-cmd", true);
            Thread.Sleep(1000);
            if (Directory.Exists(temp))
                Directory.Delete(temp, true);
            Directory.CreateDirectory(temp);

            // Double-check that previous PKG generation or EBOOT patching isn't still running
            Exe.CloseProcess("orbis-pub-cmd");

            // Overwrite EBOOT in PKG with patched one
            using (FileSys.WaitForFile(ebootPath)) { };
            if (File.Exists(newEbootPath))
                using (FileSys.WaitForFile(newEbootPath)) { };
            File.Copy(ebootPath, newEbootPath, true);
            Output.Log($"Overwrote PKG EBOOT with patched one." +
                $"\n  Creating Update PKG...");

            string outputPKG = Path.Combine(temp, game.UpdatePKGName); // Path where we expect output PKG to be generated

            // Update PKG description
            File.WriteAllText($"{programPath}\\GenGP4\\{game.TitleID}-patch\\sce_sys\\changeinfo\\changeinfo.xml", $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<changeinfo>\n  <changes app_ver=\"01.01\">\n    <![CDATA[\nPatched using ShrineFox.com/UpdateCreator\n(based on zarroboogs's ppp):\n\n{description}\n    ]]>\n  </changes>\n</changeinfo>");
            Output.Log("Updated PKG description.");

            // Create PKG from GP4
            Build(game, outputPKG);

            // Clean up temp folders and end any lingering PKG creation/EBOOT patching processes
            if (File.Exists(outputPKG))
            {
                Exe.CloseProcess("orbis-pub-cmd", true);
                Output.Log($"PKG Created successfully: {outputPKG}");
                // Copy PKG and EBOOT to permutation folder
                Directory.CreateDirectory(comboPath);
                File.Copy(outputPKG, Path.Combine(comboPath, Path.GetFileName(outputPKG)));
                File.Copy(newEbootPath, Path.Combine(comboPath, "eboot.bin"));
                Output.Log($"Copied Patched PKG and EBOOT to: {comboPath}");
            }
            else
                Output.Log($"PKG Creation failed. No PKG found at: {outputPKG}");
        }

        private static void Patch(string ebootPath, string combinationNamesList)
        {
            string args = $"patch.py {Path.GetFileName(ebootPath)} --patch {combinationNamesList}";
            Exe.Run(python, args, true, programPath);
        }

        private static void Build(Game game, string outputPKG)
        {
            string pkgBuilder = $"{programPath}\\GenGP4\\orbis-pub-cmd.exe";
            string args = $"img_create --oformat pkg --tmp_path ./temp {game.TitleID}-patch.gp4 ./temp";

            Exe.Run(pkgBuilder, args, false);

            // Rename and move PKG after finished (make sure it exists and isn't full of blank bytes)
            using (FileSys.WaitForFile(outputPKG)) { };

            while (true)
            {
                if (!File.Exists(outputPKG) || new FileInfo(outputPKG).Length < game.UpdatePKGMinSize)
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
            Exe.CloseProcess("orbis-pub-cmd");
        }
    }

    public class Game
    {
        public string Name { get; set; } = "";
        public string ID { get; set; } = "";
        public string TitleID { get; set; } = "";
        public string Region { get; set; } = "";
        public string UpdatePKGName { get; set; } = "";
        public int UpdatePKGMinSize { get; set; } = 0;
    }
    
    public class Patch
    {
        public string Name { get; set; } = "";
        public string ID { get; set; } = "";
        public string ShortDesc { get; set; } = "";
        public string LongDesc { get; set; } = "";
        public bool AlwaysOn { get; set; } = false;
    }
}
