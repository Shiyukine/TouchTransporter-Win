using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TouchTransporter_Client.Settings;

namespace TouchTransporter_Client
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Graph tgraph = new Graph();
        List<string> proAdded = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void main_Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow = this;
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            welc.Content = "v" + Update.getVersion() + " - " + Update.getRevision() + "\nBy Shiyukine - Aketsuky";
            err.Visibility = Visibility.Hidden;
            Infos.Init(this);
            Set.syncSettings(this);
            Update.Init(this);
            Update.searchUpdate();
            if (Infos.sf.settingExists("Last_IP")) ip.Text = Infos.sf.getStringSetting("Last_IP");
            string selectProfil = null;
            if (Infos.sf.settingExists("Selected_Profile")) selectProfil = Infos.sf.getStringSetting("Selected_Profile");
            foreach (string str in Infos.sf.getSettings())
            {
                if (str.StartsWith("Profil_") && str.Split('_').Length == 4)
                {
                    string proName = str.Split('_')[1];
                    if (!proAdded.Contains(proName))
                    {
                        ComboBoxItem item = new ComboBoxItem() { Content = proName, Foreground = Brushes.Black };
                        pro_count.Items.Add(item);
                        proAdded.Add(proName);
                        if (selectProfil != null && selectProfil == proName) pro_count.SelectedItem = item;
                    }
                }
            }
            tgraph.profileNumber = pro_count.Text;
            Infos.addLog("TT Loaded.");
        }

        private void connect_Click(object sender, RoutedEventArgs e)
        {
            connectTCP();
        }

        private async void connectTCP()
        {
            try
            {
                Infos.sf.setSetting("Last_IP", ip.Text, null);
                tgraph.tcp = new TcpClient();
                Infos.newErr(null, "Connecting...");
                await tgraph.tcp.ConnectAsync(ip.Text, 30921);
                tgraph.udp = new UdpClient(30921);
                tgraph.udp.Connect(ip.Text, 30921);
                tgraph.show(false);
            }
            catch (Exception ee)
            {
                Infos.newErr(ee, "We can't connect to your server. Check that the server is enabled.");
                if (tgraph.tcp != null && tgraph.tcp.Connected) tgraph.tcp.Close();
            }
        }

        private void ps_change_Click(object sender, RoutedEventArgs e)
        {
            tgraph.show(true);
        }

        private void ps_reset_Click(object sender, RoutedEventArgs e)
        {
            tgraph.margin = new double[] { -1, -1, -1, -1};
        }

        private void upd_Click(object sender, RoutedEventArgs e)
        {
            Update.searchUpdate();
        }

        private void ip_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ip.SelectAll();
        }

        private void mod_osu_Click(object sender, RoutedEventArgs e)
        {
            tgraph.osuM = (bool)mod_osu.IsChecked;
            Infos.sf.setSetting("osuM", (bool)mod_osu.IsChecked, null);
        }

        private void mod_autom_Click(object sender, RoutedEventArgs e)
        {
            tgraph.automc = (bool)mod_autom.IsChecked;
            Infos.sf.setSetting("Automc", (bool)mod_autom.IsChecked, null);
        }

        private void ip_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                connectTCP();
            }
        }

        private void mod_debug_Click(object sender, RoutedEventArgs e)
        {
            tgraph.debug = (bool)mod_debug.IsChecked;
            Infos.sf.setSetting("Debug", (bool)mod_debug.IsChecked, null);
        }

        private void mod_hover_Click(object sender, RoutedEventArgs e)
        {
            tgraph.allowHover = (bool)mod_hover.IsChecked;
            Infos.sf.setSetting("Hover", (bool)mod_hover.IsChecked, null);
        }

        private void pro_name_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                if (pro_name.Text != "Default" && !proAdded.Contains(pro_name.Text))
                {
                    string proname = (pro_name.Text == "Default" ? "" : "Profil_" + pro_name.Text + "_") + "margin_";
                    Infos.sf.setSetting(proname + "0", -1, null);
                    Infos.sf.setSetting(proname + "1", -1, null);
                    Infos.sf.setSetting(proname + "2", -1, null);
                    Infos.sf.setSetting(proname + "3", -1, null);
                    ComboBoxItem item = new ComboBoxItem() { Content = pro_name.Text, Foreground = Brushes.Black };
                    pro_count.Items.Add(item);
                    pro_count.SelectedItem = item;
                    proAdded.Add(pro_name.Text);
                    //
                    tgraph.profileNumber = pro_count.Text;
                    Infos.sf.setSetting("Selected_Profile", pro_count.Text, null);
                    //
                    pro_name.Visibility = Visibility.Hidden;
                    pro_name.Text = "";
                    Infos.newErr(null, "Profil added");
                }
                else Infos.newErr(null, "Find a other name.");
            }
            if(e.Key == Key.Escape)
            {
                pro_name.Visibility = Visibility.Hidden;
                pro_name.Text = "";
            }
        }

        private void pro_delete_Click(object sender, RoutedEventArgs e)
        {
            if (pro_count.Text != "Default")
            {
                string proname = (pro_count.Text == "Default" ? "" : "Profil_" + pro_count.Text + "_") + "margin_";
                Infos.sf.removeSetting(proname + "0");
                Infos.sf.removeSetting(proname + "1");
                Infos.sf.removeSetting(proname + "2");
                Infos.sf.removeSetting(proname + "3");
                ComboBoxItem ritem = null;
                proAdded.Remove(pro_count.Text);
                foreach (ComboBoxItem item in pro_count.Items)
                {
                    if ((string)item.Content == pro_count.Text) ritem = item;
                }
                if(ritem != null) pro_count.Items.Remove(ritem);
                //
                pro_count.SelectedIndex = 0;
                tgraph.profileNumber = pro_count.Text;
                Infos.sf.setSetting("Selected_Profile", pro_count.Text, null);
                Infos.newErr(null, "Profil deleted");
            }
            else Infos.newErr(null, "You can't delete this profile");
        }

        private void pro_add_Click(object sender, RoutedEventArgs e)
        {
            Infos.newErr(null, "Enter a name to your new profile. ESC to quit, ENTER to add profile.");
            pro_name.Visibility = Visibility.Visible;
            pro_name.Focus();
            pro_name.Text = "";
        }

        private void pro_count_DropDownClosed(object sender, EventArgs e)
        {
            tgraph.profileNumber = pro_count.Text;
            Infos.sf.setSetting("Selected_Profile", pro_count.Text, null);
        }

        private void pro_count_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
    }
}
