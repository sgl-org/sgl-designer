using AutoUpdaterDotNET;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;

namespace SglDesigner
{
    public partial class MainWindow
    {
        public static bool manual_check_update = false;
        public static string OTA_URL = "https://xfdr0805.github.io/software/sgl/update.json";
        public void ConfigureAutoUpdater()
        {
            AutoUpdater.ReportErrors = true;//如果没有更新可用或者无法从服务器获取xml,json文件
            AutoUpdater.CheckForUpdateEvent += AutoUpdater_CheckForUpdateEvent;
            AutoUpdater.ParseUpdateInfoEvent += AutoUpdaterOnParseUpdateInfoEvent;

            DispatcherTimer tm = new DispatcherTimer { Interval = TimeSpan.FromMinutes(60) };
            tm.Tick += delegate
            {
                //AutoUpdater.Start("https://xfdr0805.github.io/software/sgl/update.json");
                AutoUpdater.Start(OTA_URL);
            };
            tm.Start();
        }

        private void AutoUpdaterOnParseUpdateInfoEvent(ParseUpdateInfoEventArgs args)
        {
            dynamic json = JsonConvert.DeserializeObject(args.RemoteData);
            args.UpdateInfo = new UpdateInfoEventArgs
            {
                CurrentVersion = json.version,
                ChangelogURL = json.changelog,
                DownloadURL = json.url,
                Mandatory = new Mandatory
                {
                    Value = json.mandatory.value,
                    UpdateMode = json.mandatory.mode,
                    MinimumVersion = json.mandatory.minVersion
                },
                CheckSum = new CheckSum
                {
                    Value = json.checksum.value,
                    //此处是先zip压缩再计算Hash
                    HashingAlgorithm = json.checksum.hashingAlgorithm
                }
            };
        }
        private void AutoUpdater_CheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            //这是为了第一次检测到新版本不进行弹窗，在标题栏进行提示，需要升级 点击检查更新按钮即可

            if (manual_check_update)
            {
                manual_check_update = false;

                if (args.Error == null)
                {

                    if (args.IsUpdateAvailable)
                    {
                        AppendAnsiLog($"\x1B[32m有新版本可用（ {args.CurrentVersion}）,当前版本（ {args.InstalledVersion}）\n");
                        MessageBoxResult dialogResult;
                        if (args.Mandatory.Value)//强制更新
                        {
                            dialogResult =
                                MessageBox.Show(
                                    $@"有新版本可用（ {args.CurrentVersion}）,当前版本（ {args.InstalledVersion}）。点击OK按钮更新！", @"有更新可用",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            dialogResult =
                                MessageBox.Show(
                                    $@"有新版本可用（ {args.CurrentVersion}）,当前版本（ {args.InstalledVersion}）。是否更新?", @"有更新可用",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Information);
                        }

                        // Uncomment the following line if you want to show standard update dialog instead.
                        //AutoUpdater.ShowUpdateForm(args);

                        if (dialogResult.Equals(MessageBoxResult.Yes) || dialogResult.Equals(MessageBoxResult.OK))
                        {
                            try
                            {
                                if (AutoUpdater.DownloadUpdate(args))
                                {
                                    Environment.Exit(0);
                                }
                            }
                            catch (Exception exception)
                            {
                                AppendAnsiLog($"\x1B[31m{exception.Message}\n");
                                MessageBox.Show(exception.Message, exception.GetType().ToString(), MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                            }
                        }
                    }
                    else
                    {
                        AppendAnsiLog($"\x1B[37m没发现新版本,请稍候重试!\n");

                        MessageBox.Show(@"没发现新版本,请稍候重试!", @"提示",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    if (args.Error is WebException)
                    {
                        AppendAnsiLog($"\x1B[31m无法连接服务器，请检查网络连接或者稍候重试。\n");
                        MessageBox.Show(
                            @"无法连接服务器，请检查网络连接或者稍候重试。",
                            @"检查更新失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        AppendAnsiLog($"\x1B[31m{args.Error.Message}。\n");
                        MessageBox.Show(args.Error.Message,
                            args.Error.GetType().ToString(), MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                if (args.Error == null)
                {
                    String version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    //version = version.Substring(0, version.Length - 2);  //去掉最后一位版本号
                    //String BuildDateTime = System.IO.File.GetLastWriteTime(this.GetType().Assembly.Location).ToString();
                    if (args.IsUpdateAvailable)
                    {

                        this.Title = "Sgl Designer V" + version + " " + $"(有新版本可用{args.CurrentVersion})";
                    }
                    //else
                    //{
                    //    this.Title = "Sgl Designer V" + version;
                    //}

                }

            }

        }
    }
}
