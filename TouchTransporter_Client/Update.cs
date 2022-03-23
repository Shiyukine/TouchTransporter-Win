using ShiyukiUtils.Class;
using ShiyukiUtils.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace TouchTransporter_Client
{
    public class Update
    {
        private static string ver = "1.1";
        private static int rev = 1;
        private static int verCode = 4;

        static string dl = AppDomain.CurrentDomain.BaseDirectory + "\\";
        static string temp = dl + "Temp\\";

        static MainWindow _main;
        static DateTime time;
        static SettingsManager sm;

        public static void Init(MainWindow main)
        {
            _main = main;
        }

        public static string getVersion()
        {
            return ver;
        }

        public static int getRevision()
        {
            return rev;
        }

        public static int getVersionCode()
        {
            return verCode;
        }

        public static void searchUpdate()
        {
            string upd = null;
            _main.updl.Content = "Searching updates...";
            _main.updpb.Value = 0;
            Infos.addLog("Searching updates...");
            _main.updg.Visibility = Visibility.Visible;
            Storyboard sb = new Storyboard();
            sb.Children.Add(Infos.addDoubleAnimation(_main.updg, TimeSpan.FromMilliseconds(200), 0, 1, new PropertyPath(UIElement.OpacityProperty)));
            sb.Begin();
            System.Windows.Forms.WebBrowser wb = new System.Windows.Forms.WebBrowser();
            try
            {
                wb.Navigate("http://" + Infos.sf.getStringSetting("Url_Server") + "/dl/TouchTransporter/update_c2.php");
                wb.DocumentCompleted += (sender, e) =>
                {
                    if (!wb.DocumentText.Contains("<html>"))
                    {
                        WebClient wc = new WebClient();
                        wc.Encoding = Encoding.UTF8;
                        wc.DownloadProgressChanged += (sendere, ee) =>
                        {
                            _main.updpb.Maximum = 100;
                            _main.updpb.Value = ee.ProgressPercentage;
                        };
                        wc.DownloadStringCompleted += async (sendere, ee) =>
                        {
                            try
                            { 
                            Infos.addLog("Searching update for files...");
                            upd = ee.Result;
                            SettingsReaderString srs = new SettingsReaderString(upd.Replace("\r", ""));
                            _main.updpb.Maximum = srs.getSettings().Length - 1;
                            _main.updpb.Value = 0;
                            foreach (string l in srs.getSettings())
                            {
                                if (l == "***INFOS***") urlDl = srs.getStringSetting(l);
                                else
                                {
                                    bool b = await Task.Run(() =>
                                    {
                                        string fold = dl + l.Replace("/", "\\");
                                        int i = srs.getIntSetting(l);
                                        if (i > verCode || !File.Exists(fold))
                                        {
                                            files.Add(l, i);
                                            return true;
                                        }
                                        else return false;
                                    });
                                    if (!b)
                                    {
                                        _main.updpb.Value++;
                                        Infos.addLog("File " + l + " is up-to-date");
                                    }
                                }
                            }
                            numAll = files.Keys.Count;
                            if (numAll == 0)
                            {
                                _main.updl.Content = "Up-to-date.";
                                Infos.addLog("Up-to-date.");
                                Storyboard sb2 = new Storyboard();
                                sb2.Children.Add(Infos.addDoubleAnimation(_main.updg, TimeSpan.FromMilliseconds(200), 1, 0, new PropertyPath(UIElement.OpacityProperty)));
                                sb2.Completed += (s, eee) =>
                                {
                                    _main.updg.Visibility = Visibility.Collapsed;
                                };
                                sb2.Begin();
                            }
                            else dlFile();
                            wc.Dispose();
                            }
                            catch (Exception ei)
                            {
                                _main.updl.Content = "Update search error.";
                                _main.updg.Visibility = Visibility.Collapsed;
                                Infos.newErr(ei.InnerException, "Unable to connect to the server update.");
                            }
                        };
                        Infos.addLog("Downloading file list update.");
                        wc.DownloadStringAsync(wb.Url);
                    }
                    else
                    {
                        _main.updl.Content = "Unable to connect to the server update.";
                    }
                    wb.Stop();
                };
            }
            catch (Exception ei)
            {
                Infos.newErrLog(ei, "Unable to find \"update.php\".");
                _main.updl.Content = "Update search Infos.";
            }
        }

        static int numAll = 0;
        static int num = 1;
        static Dictionary<string, int> files = new Dictionary<string, int>();
        static string urlDl = "";

        private static void dlFile()
        {
            Directory.CreateDirectory(temp);
            string file = files.Keys.ToList()[num - 1];
            Infos.addLog("A new update for file " + file + " is available ! Version" + verCode + ", New version " + files[file]);
            try
            {
                WebClient wc = new WebClient();
                wc.Encoding = Encoding.UTF8;
                wc.DownloadProgressChanged += (sendere, ee) =>
                {
                    TextUtils.changeLabel(_main.updl, "Downloading update " + num + " of " + numAll + " - §-b§#FFFFA500§"
                        + Math.Round((ee.BytesReceived / 1024.0f) / 1024.0f, 2) + "MB/§#FFFFFFFF§"
                        + Math.Round((ee.TotalBytesToReceive / 1024.0f) / 1024.0f, 2) + "MB");
                    _main.updpb.Maximum = 100;
                    _main.updpb.Value = ee.ProgressPercentage;
                };
                wc.DownloadFileCompleted += (sendere, ee) =>
                {
                    if (numAll == num)
                    {
                        File.Copy(dl + App.appName, dl + "temp.exe");
                        Process.Start(dl + "temp.exe", "movefiles");
                        _main.Close();
                    }
                    else
                    {
                        num++;
                        dlFile();
                    }
                };
                Infos.addLog("Downloading " + file);
                time = DateTime.Now;
                string fold = temp + file.Replace("/", "\\");
                Directory.CreateDirectory(Path.GetDirectoryName(fold));
                wc.DownloadFileAsync(new Uri("http://" + urlDl + "Update/" + file), fold);
            }
            catch (Exception ei)
            {
                Infos.newErr(ei, "Unable to download " + file);
            }
        }
    }
}