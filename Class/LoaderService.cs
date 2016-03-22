﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoaderService.cs" company="LeagueSharp.Loader">
//   Copyright (c) LeagueSharp.Loader. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace LeagueSharp.Loader.Class
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;

    using LeagueSharp.Loader.Data;
    using LeagueSharp.Sandbox.Shared;

    using PlaySharp.Service.WebService.Model;

    public class LoaderService : ILoaderService
    {
        public List<LSharpAssembly> GetAssemblyList(int pid)
        {
            var assemblies = new List<LSharpAssembly>();

            if (
                Config.Instance.Settings.GameSettings.First(s => s.Name == "Always Inject Default Profile")
                      .SelectedValue == "True" && Config.Instance.SelectedProfile != Config.Instance.Profiles[0]
                && Config.Instance.Profiles.Count > 0)
            {
                assemblies.AddRange(
                    Config.Instance.Profiles[0].InstalledAssemblies.Where(
                        a => a.InjectChecked && a.Type != AssemblyType.Library)
                                               .Select(
                                                   assembly =>
                                                   new LSharpAssembly
                                                   {
                                                       Name = assembly.Name, 
                                                       PathToBinary = assembly.PathToBinary
                                                   })
                                               .ToList());
            }

            assemblies.AddRange(
                Config.Instance.SelectedProfile.InstalledAssemblies.Where(
                    a => a.InjectChecked && a.Type != AssemblyType.Library)
                      .Select(
                          assembly => new LSharpAssembly { Name = assembly.Name, PathToBinary = assembly.PathToBinary })
                      .ToList());
            return assemblies;
        }

        public Configuration GetConfiguration(int pid)
        {
            var reload = 0x74;
            var recompile = 0x77;
            var antiAfk = false;
            var console = false;
            var towerRange = false;
            var extendedZoom = false;
            var drawings = true;
            var ping = true;
            var menuToggle = 0x78;
            var menuPress = 0x10;
            var selectedLanguage = string.Empty;
            var statistics = true;

            try
            {
                reload = KeyInterop.VirtualKeyFromKey(Config.Instance.Hotkeys.SelectedHotkeys.First(h => h.Name == "Reload").Hotkey);
                recompile = KeyInterop.VirtualKeyFromKey(Config.Instance.Hotkeys.SelectedHotkeys.First(h => h.Name == "CompileAndReload").Hotkey);
                antiAfk = Config.Instance.Settings.GameSettings.First(s => s.Name == "Anti-AFK").SelectedValue == "True";
                console = Config.Instance.Settings.GameSettings.First(s => s.Name == "Debug Console").SelectedValue == "True"
                          || Config.Instance.ShowDevOptions || Config.Instance.EnableDebug;
                towerRange = Config.Instance.Settings.GameSettings.First(s => s.Name == "Display Enemy Tower Range").SelectedValue == "True";
                extendedZoom = Config.Instance.Settings.GameSettings.First(s => s.Name == "Extended Zoom").SelectedValue == "True";
                drawings = Config.Instance.Settings.GameSettings.First(s => s.Name == "Show Drawings").SelectedValue == "True";
                ping = Config.Instance.Settings.GameSettings.First(s => s.Name == "Show Ping").SelectedValue == "True";
                statistics = Config.Instance.Settings.GameSettings.First(s => s.Name == "Send Anonymous Assembly Statistics").SelectedValue == "True";
                menuToggle = KeyInterop.VirtualKeyFromKey(Config.Instance.Hotkeys.SelectedHotkeys.First(h => h.Name == "ShowMenuToggle").Hotkey);
                menuPress = KeyInterop.VirtualKeyFromKey(Config.Instance.Hotkeys.SelectedHotkeys.First(h => h.Name == "ShowMenuPress").Hotkey);
                selectedLanguage = Config.Instance.SelectedLanguage;
            }
            catch
            {
                // ignored
            }

            return new Configuration
                   {
                       DataDirectory = Directories.AppDataDirectory, 
                       LeagueSharpDllPath = Directories.CoreBridgeRandomFilePath, 
                       LibrariesDirectory = Directories.CoreDirectory, 
                       ReloadKey = reload, 
                       ReloadAndRecompileKey = recompile, 
                       MenuToggleKey = menuToggle, 
                       MenuKey = menuPress, 
                       UnloadKey = 0x75, 
                       AntiAfk = antiAfk, 
                       Console = console, 
                       TowerRange = towerRange, 
                       SelectedLanguage = selectedLanguage, 
                       ExtendedZoom = extendedZoom, 
                       ShowPing = ping, 
                       ShowDrawing = drawings, 
                       SendStatistics = statistics, 
                       Permissions = null
                   };
        }

        public void Recompile(int pid)
        {
            var targetAssemblies =
                Config.Instance.SelectedProfile.InstalledAssemblies.Where(
                    a => a.InjectChecked || a.Type == AssemblyType.Library).ToList();

            foreach (var assembly in targetAssemblies)
            {
                if (assembly.Type == AssemblyType.Library)
                {
                    assembly.Compile();
                }
            }

            foreach (var assembly in targetAssemblies)
            {
                if (assembly.Type != AssemblyType.Library)
                {
                    assembly.Compile();
                }
            }
        }
    }
}