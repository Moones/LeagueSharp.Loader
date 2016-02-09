﻿#region LICENSE

// Copyright 2015-2015 LeagueSharp.Loader
// PathRandomizer.cs is part of LeagueSharp.Loader.
// 
// LeagueSharp.Loader is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// LeagueSharp.Loader is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with LeagueSharp.Loader. If not, see <http://www.gnu.org/licenses/>.

#endregion

namespace LeagueSharp.Loader.Class
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows;

    using LeagueSharp.Loader.Data;

    internal class PathRandomizer
    {
        public static Random RandomNumberGenerator = new Random();

        private static string _leagueSharpBootstrapDllName = null;

        private static string _leagueSharpCoreDllName = null;

        private static string _leagueSharpDllName = null;

        private static string _leagueSharpSandBoxDllName = null;

        private static ModifyIATDelegate ModifyIAT = null;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate bool ModifyIATDelegate(
            [MarshalAs(UnmanagedType.LPWStr)] string modulePath,
            [MarshalAs(UnmanagedType.LPWStr)] string newModulePath,
            [MarshalAs(UnmanagedType.LPStr)] string moduleName,
            [MarshalAs(UnmanagedType.LPStr),] string newModuleName);

        public static string BaseDirectory
        {
            get
            {
                return Directories.AssembliesDir;
            }
        }

        public static string LeagueSharpBootstrapDllName
        {
            get
            {
                if (_leagueSharpBootstrapDllName == null)
                {
                    _leagueSharpBootstrapDllName = GetRandomName("LeagueSharp.Bootstrap.dll");
                }
                return _leagueSharpBootstrapDllName;
            }
        }

        public static string LeagueSharpBootstrapDllPath
        {
            get
            {
                return Path.Combine(BaseDirectory, LeagueSharpBootstrapDllName);
            }
        }

        public static string LeagueSharpCoreDllName
        {
            get
            {
                if (_leagueSharpCoreDllName == null)
                {
                    _leagueSharpCoreDllName = GetRandomName("LeagueSharp.Core.dll");
                }
                return _leagueSharpCoreDllName;
            }
        }

        public static string LeagueSharpCoreDllPath
        {
            get
            {
                return Path.Combine(BaseDirectory, LeagueSharpCoreDllName);
            }
        }

        public static string LeagueSharpDllName
        {
            get
            {
                if (_leagueSharpDllName == null)
                {
                    _leagueSharpDllName = GetRandomName("LeagueSharp.dll");
                }
                return _leagueSharpDllName;
            }
        }

        public static string LeagueSharpDllPath
        {
            get
            {
                return Path.Combine(BaseDirectory, LeagueSharpDllName);
            }
        }

        public static string LeagueSharpSandBoxDllName
        {
            get
            {
                //  return "LeagueSharp.SandBox.dll";
                if (_leagueSharpSandBoxDllName == null)
                {
                    _leagueSharpSandBoxDllName = GetRandomName("LeagueSharp.SandBox.dll");
                }
                return _leagueSharpSandBoxDllName;
            }
        }

        public static string LeagueSharpSandBoxDllPath
        {
            get
            {
                return Path.Combine(BaseDirectory, LeagueSharpSandBoxDllName);
            }
        }

        public static bool CopyFiles()
        {
            var result = true;
            if (ModifyIAT == null)
            {
                ResolveImports();
            }

            if (ModifyIAT == null)
            {
                return false;
            }

            if (!File.Exists(Path.Combine(Directories.CoreDirectory, "LeagueSharp.Core.dll")))
            {
                return false;
            }

            try
            {
                //result = result && Utility.OverwriteFile(Path.Combine(Directories.CoreDirectory, "LeagueSharp.dll"), LeagueSharpDllPath, true);
                //result = result && ModifyIAT(Path.Combine(Directories.CoreDirectory, "LeagueSharp.dll"), LeagueSharpDllPath, "LeagueSharp.Core.dll", LeagueSharpCoreDllName);
                result = result
                         && Utility.OverwriteFile(
                             Path.Combine(Directories.CoreDirectory, "LeagueSharp.Core.dll"),
                             LeagueSharpCoreDllPath,
                             true);
                result = result
                         && Utility.OverwriteFile(
                             Path.Combine(Directories.CoreDirectory, "LeagueSharp.Bootstrap.dll"),
                             LeagueSharpBootstrapDllPath,
                             true);
                result = result
                         && Utility.OverwriteFile(
                             Path.Combine(Directories.CoreDirectory, "LeagueSharp.SandBox.dll"),
                             LeagueSharpSandBoxDllPath,
                             true);

                //Temp solution :^) , for some reason calling ModifyIAT() crashes the loader.
                var byteArray = File.ReadAllBytes(Path.Combine(Directories.CoreDirectory, "LeagueSharp.dll"));
                byteArray = Utility.ReplaceFilling(
                    byteArray,
                    Encoding.ASCII.GetBytes("LeagueSharp.Core.dll"),
                    Encoding.ASCII.GetBytes(LeagueSharpCoreDllName));
                File.WriteAllBytes(LeagueSharpDllPath, byteArray);

                if (!File.Exists(Path.Combine(Directories.CoreDirectory, "sn.exe")))
                {
                    MessageBox.Show("sn.exe not found");
                    Environment.Exit(0);
                }

                var p = new Process
                    {
                        StartInfo =
                            new ProcessStartInfo
                                {
                                    UseShellExecute = true,
                                    WorkingDirectory = Directories.CoreDirectory,
                                    FileName = Path.Combine(Directories.CoreDirectory, "sn.exe"),
                                    Arguments = string.Format("-q -Ra \"{0}\" key.snk", LeagueSharpDllPath),
                                    WindowStyle = ProcessWindowStyle.Hidden
                                }
                    };
                p.Start();
                p.WaitForExit();

                if (p.ExitCode != 0)
                {
                    p = new Process
                        {
                            StartInfo =
                                new ProcessStartInfo
                                    {
                                        UseShellExecute = false,
                                        RedirectStandardOutput = true,
                                        WorkingDirectory = Directories.CoreDirectory,
                                        FileName = Path.Combine(Directories.CoreDirectory, "sn.exe"),
                                        Arguments = string.Format("-q -Ra \"{0}\" key.snk", LeagueSharpDllPath)
                                    }
                        };

                    p.Start();
                    var output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    if (p.ExitCode != 0)
                    {
                        MessageBox.Show(string.Format("Could not Sign LeagueSharp.dll, Output:\r\n {0}", output));
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static string GetRandomName(string oldName)
        {
            var ar1 = Utility.Md5Hash(oldName);
            var ar2 = Utility.Md5Hash(Config.Instance.Username);

            const string allowedChars = "0123456789abcdefhijkmnopqrstuvwxyz";
            var result = "";
            for (var i = 0; i < Math.Min(15, Math.Max(3, Config.Instance.Username.Length)); i++)
            {
                var j = (ar1.ToCharArray()[i] * ar2.ToCharArray()[i]) * 2;
                j = j % (allowedChars.Length - 1);
                result = result + allowedChars[j];
            }

            return result + ".dll";
        }

        public static void ResolveImports()
        {
            var hModule = Win32Imports.LoadLibrary(Directories.BootstrapFilePath);
            if (!(hModule != IntPtr.Zero))
            {
                return;
            }

            var procAddress = Win32Imports.GetProcAddress(hModule, "ModifyIAT");
            if (!(procAddress != IntPtr.Zero))
            {
                return;
            }

            ModifyIAT =
                Marshal.GetDelegateForFunctionPointer(procAddress, typeof(ModifyIATDelegate)) as ModifyIATDelegate;
        }
    }
}