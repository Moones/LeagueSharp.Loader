﻿#region LICENSE

// Copyright 2015-2015 LeagueSharp.Loader
// Updater.cs is part of LeagueSharp.Loader.
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
    #region

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows;

    using LeagueSharp.Loader.Data;
    using LeagueSharp.Loader.Views;

    #endregion

    internal class Updater
    {
        public const string CoreVersionCheckURL = "http://api.joduska.me/public/deploy/kernel/{0}";

        public const string VersionCheckURL = "http://api.joduska.me/public/deploy/loader/version";

        public static bool CheckedForUpdates = false;

        public static MainWindow MainWindow;

        public static string SetupFile = Path.Combine(Directories.CurrentDirectory, "LeagueSharp-update.exe");

        public static string UpdateZip = Path.Combine(Directories.CoreDirectory, "update.zip");

        public static bool Updating = false;

        public delegate void RepositoriesUpdateDelegate(List<string> list);

        public enum CoreUpdateState
        {
            Operational,

            Maintenance,

            Unknown
        }

        public static Tuple<bool, string> CheckLoaderVersion()
        {
            try
            {
                using (var client = new WebClient())
                {
                    var data = client.DownloadData(VersionCheckURL);
                    var ser = new DataContractJsonSerializer(typeof(UpdateInfo));
                    var updateInfo = (UpdateInfo)ser.ReadObject(new MemoryStream(data));
                    var v = Version.Parse(updateInfo.version);
                    if (Utility.VersionToInt(Assembly.GetEntryAssembly().GetName().Version) < Utility.VersionToInt(v))
                    {
                        return new Tuple<bool, string>(true, updateInfo.url);
                    }
                }
            }
            catch
            {
                return new Tuple<bool, string>(false, "");
            }

            return new Tuple<bool, string>(false, "");
        }

        public static async Task UpdateBlockedRepos()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response =
                        await client.GetAsync(
                            "https://raw.githubusercontent.com/LeagueSharp/LeagueSharp.Loader/master/Updates/BlockedRepositories.txt");

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        Config.Instance.BlockedRepositories = new List<string>(lines);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static async Task UpdateRepositories()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync("https://loader.joduska.me/repositories.txt");

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var result = new List<string>();

                        try
                        {
                            var matches = Regex.Matches(content, "<repo>(.*)</repo>");
                            result.AddRange(from Match match in matches select match.Groups[1].ToString());
                        }
                        catch (Exception)
                        {
                            // ignored
                        }

                        Config.Instance.KnownRepositories = new ObservableCollection<string>(result);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static async Task<bool> IsSupported(string path)
        {
            if (Directory.Exists(Path.Combine(Directories.CurrentDirectory, "iwanttogetbanned")))
            {
                return true;
            }

            var coreMd5 = Utility.Md5Checksum(Directories.CoreFilePath);
            var leagueMd5 = Utility.Md5Checksum(path);
            var wr = WebRequest.Create(string.Format(CoreVersionCheckURL, leagueMd5));
            wr.Timeout = 4000;
            wr.Method = "GET";
            var response = await wr.GetResponseAsync();

            return await Task<bool>.Factory.StartNew(
                () =>
                    {
                        try
                        {
                            using (var stream = response.GetResponseStream())
                            {
                                if (stream != null)
                                {
                                    var ser = new DataContractJsonSerializer(typeof(UpdateInfo));
                                    var updateInfo = (UpdateInfo)ser.ReadObject(stream);

                                    if (updateInfo.version != "0" && updateInfo.version == coreMd5)
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }

                        return false;
                    });
        }

        public static async Task<UpdateResponse> UpdateCore(string leagueOfLegendsFilePath, bool showMessages)
        {
            if (Directory.Exists(Path.Combine(Directories.CurrentDirectory, "iwanttogetbanned")))
            {
                return new UpdateResponse(CoreUpdateState.Operational, Utility.GetMultiLanguageText("NotUpdateNeeded"));
            }

            try
            {
                var leagueMd5 = Utility.Md5Checksum(leagueOfLegendsFilePath);
                var wr = WebRequest.Create(string.Format(CoreVersionCheckURL, leagueMd5));
                wr.Timeout = 4000;
                wr.Method = "GET";
                var response = await wr.GetResponseAsync();

                using (var stream = response.GetResponseStream())
                {
                    if (stream != null)
                    {
                        var ser = new DataContractJsonSerializer(typeof(UpdateInfo));
                        var updateInfo = (UpdateInfo)ser.ReadObject(stream);

                        if (updateInfo.version == "0")
                        {
                            var message = Utility.GetMultiLanguageText("WrongVersion") + leagueMd5;

                            if (showMessages)
                            {
                                MessageBox.Show(message);
                            }

                            return new UpdateResponse(CoreUpdateState.Maintenance, message);
                        }

                        if (updateInfo.version != Utility.Md5Checksum(Directories.CoreFilePath) && (
                            updateInfo.url.StartsWith("https://github.com/joduskame/") || 
                            updateInfo.url.StartsWith("https://github.com/LeagueSharp/")))
                        {
                            try
                            {
                                var result = CoreUpdateState.Unknown;

                                await Application.Current.Dispatcher.Invoke(
                                    async () =>
                                        {
                                            var window = new UpdateWindow(UpdateAction.Core, updateInfo.url);
                                            window.Show();

                                            if (await window.Update())
                                            {
                                                result = CoreUpdateState.Operational;
                                            }
                                        });

                                return new UpdateResponse(result, Utility.GetMultiLanguageText("UpdateSuccess"));
                            }
                            catch (Exception e)
                            {
                                var message = Utility.GetMultiLanguageText("FailedToDownload") + e;

                                if (showMessages)
                                {
                                    MessageBox.Show(message);
                                }

                                return new UpdateResponse(CoreUpdateState.Unknown, message);
                            }
                            finally
                            {
                                if (File.Exists(UpdateZip))
                                {
                                    File.Delete(UpdateZip);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return new UpdateResponse(CoreUpdateState.Unknown, Utility.GetMultiLanguageText("UpdateUnknown"));
            }

            return new UpdateResponse(CoreUpdateState.Operational, Utility.GetMultiLanguageText("NotUpdateNeeded"));
        }

        public static async Task UpdateLoader(Tuple<bool, string> versionCheckResult)
        {
            if (versionCheckResult.Item1
                && (versionCheckResult.Item2.StartsWith("https://github.com/LeagueSharp/")
                    || versionCheckResult.Item2.StartsWith("https://github.com/joduskame/")
                    || versionCheckResult.Item2.StartsWith("https://github.com/Esk0r/")))
            {
                var window = new UpdateWindow(UpdateAction.Loader, versionCheckResult.Item2);
                window.Show();
                await window.Update();
            }
        }

        public class UpdateResponse
        {
            public UpdateResponse(CoreUpdateState state, string message = "")
            {
                this.State = state;
                this.Message = message;
            }

            public string Message { get; set; }

            public CoreUpdateState State { get; set; }
        }

        [DataContract]
        internal class UpdateInfo
        {
            [DataMember]
            internal string url;

            [DataMember]
            internal bool valid;

            [DataMember]
            internal string version;
        }
    }
}