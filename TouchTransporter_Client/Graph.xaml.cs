using ShiyukiUtils.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TouchTransporter_Client
{
    /// <summary>
    /// Logique d'interaction pour Graph.xaml
    /// La vie est belle
    /// </summary>
    public partial class Graph : Window
    {
        public double[] margin = new double[] { -1, -1, -1, -1 };
        bool mouseDown = false;
        bool egraphMouseDown = false;
        int egraphnb = 0;
        bool isHovering = false;
        bool set = false;
        public bool osuM = false;
        public bool automc = false;
        public bool debug = false;
        public bool allowHover = false;
        public string profileNumber = "Default";
        public TcpClient tcp;
        public UdpClient udp;
        public NetworkStream net;

        Stopwatch sw_pointer = Stopwatch.StartNew();
        Stopwatch sw_write = Stopwatch.StartNew();
        Stopwatch sw_draw = Stopwatch.StartNew();

        public Graph()
        {
            InitializeComponent();
            canv.AddHandler(InkCanvas.MouseDownEvent, new MouseButtonEventHandler(tablet_MouseDown), true);
        }

        public void setMarginGraph()
        {
            margin[0] = tablet.Margin.Left;
            margin[1] = tablet.Margin.Top;
            margin[2] = tablet.Margin.Right;
            margin[3] = tablet.Margin.Bottom;
            string proname = (profileNumber == "Default" ? "" : "Profil_" + profileNumber + "_");
            Infos.sf.setSetting(proname + "margin_0", margin[0].ToString().Replace(",", "."), null);
            Infos.sf.setSetting(proname + "margin_1", margin[1].ToString().Replace(",", "."), null);
            Infos.sf.setSetting(proname + "margin_2", margin[2].ToString().Replace(",", "."), null);
            Infos.sf.setSetting(proname + "margin_3", margin[3].ToString().Replace(",", "."), null);
        }

        public void replaceAllEgraph()
        {
            egraph_1.Margin = new Thickness(tablet.Margin.Left + 10, tablet.Margin.Top + 10, 0, 0);
            egraph_2.Margin = new Thickness(0, tablet.Margin.Top + 10, tablet.Margin.Right + 10, 0);
            egraph_3.Margin = new Thickness(0, 0, tablet.Margin.Right + 10, tablet.Margin.Bottom + 10);
            egraph_4.Margin = new Thickness(tablet.Margin.Left + 10, 0, 0, tablet.Margin.Bottom + 10);
        }

        public void changePosEgraph(MouseEventArgs e)
        {
            Point m = e.GetPosition(main);
            if (egraphnb == 1) tablet.Margin = new Thickness(m.X - 30, m.Y - 30, tablet.Margin.Right, tablet.Margin.Bottom);
            if (egraphnb == 2) tablet.Margin = new Thickness(tablet.Margin.Left, m.Y - 30, main.ActualWidth - m.X - 45, tablet.Margin.Bottom);
            if (egraphnb == 3) tablet.Margin = new Thickness(tablet.Margin.Left, tablet.Margin.Top, main.ActualWidth - m.X - 45, main.ActualHeight - m.Y - 45);
            if (egraphnb == 4) tablet.Margin = new Thickness(m.X - 30, tablet.Margin.Top, tablet.Margin.Right, main.ActualHeight - m.Y - 45);
            setMarginGraph();
            replaceAllEgraph();
        }

        public void show(bool set)
        {
            if (!set) egraph_1.Visibility = egraph_2.Visibility = egraph_3.Visibility = egraph_4.Visibility = canv.Visibility = Visibility.Hidden;
            else egraph_1.Visibility = egraph_2.Visibility = egraph_3.Visibility = egraph_4.Visibility = canv.Visibility = Visibility.Visible;
            this.set = set;
            string proname = (profileNumber == "Default" ? "" : "Profil_" + profileNumber + "_");
            if (Infos.sf.settingExists(proname + "margin_0"))
            {
                margin[0] = Infos.sf.getDoubleSetting(proname + "margin_0");
                margin[1] = Infos.sf.getDoubleSetting(proname + "margin_1");
                margin[2] = Infos.sf.getDoubleSetting(proname + "margin_2");
                margin[3] = Infos.sf.getDoubleSetting(proname + "margin_3");
                debug_panel.Visibility = Visibility.Visible;
            }
            else
            {
                debug_panel.Visibility = Visibility.Hidden;
            }
            //https://tenor.com/bl2M3.gif
            if (Infos.sf.settingExists("Pressure_Intensifier")) pi.Value = Infos.sf.getDoubleSetting("Pressure_Intensifier");
            if (margin[0] != -1) tablet.Margin = new Thickness(margin[0], margin[1], margin[2], margin[3]);
            else tablet.Margin = new Thickness(10, 10, 10, 61);
            replaceAllEgraph();
            canv.Strokes.Clear();
            if (osuM || set) canv.Visibility = Visibility.Hidden;
            else canv.Visibility = Visibility.Visible;
            Show();
            //max
            if (!debug) debug_panel.Visibility = Visibility.Hidden;
            else debug_panel.Visibility = Visibility.Visible;
            if (!set)
            {
                net = tcp.GetStream();
                tcp.SendBufferSize = 1024;
                sendInfo("Max:" + tablet.ActualWidth + ";" + tablet.ActualHeight + (!osuM ? ";pen;" : ";mouse;") + Update.getVersionCode() + "|");
            }
            foreach (Button el in FindVisualChildren<Button>(grid))
            {
                if (el.Name.Contains("key_") || el.Name.Contains("setting"))
                {
                    if (osuM || set) el.Visibility = Visibility.Collapsed;
                    else el.Visibility = Visibility.Visible;
                }
            }
        }

        private void main_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            quit();
        }

        private void quit()
        {
            Hide();
            if (!set)
            {
                if(tcp != null) tcp.Close();
                if (net != null) net.Close();
                if (udp != null) udp.Close();
            }
        }

        private void tablet_MouseMove(object sender, MouseEventArgs e)
        {
            if (!set)
            {
                try
                {
                    Point m = e.GetPosition(tablet);
                    StylusDevice sd = Stylus.CurrentStylusDevice;
                    isHovering = !mouseDown;
                    double pressure = 1;
                    if (sd != null)
                    {
                        //sd.Synchronize();
                        if (!osuM)
                        {
                            //isHovering = canv.ActiveEditingMode == InkCanvasEditingMode.Ink;
                            pressure = getPressure(sd);
                        }
                        isHovering = sd.InAir;
                    }
                    //fix bug air stylus
                    if(isHovering && mouseDown) mouseUp();
                    //
                    if (allowHover || !isHovering)
                    {
                        sendInfo("Pos:" + m.X + ";" + m.Y + (!osuM ? ";" + pressure : "") + (isHovering ? ";hover" : "") + "|");
                    }
                    if (debug)
                    {
                        ms_X.Text = m.X.ToString();
                        ms_Y.Text = m.Y.ToString();
                        ms_isstylus.Text = (sd != null).ToString();
                        ms_ishovering.Text = isHovering.ToString();
                        ms_prs.Text = pressure.ToString();
                        ms_pointer.Text = sw_pointer.ElapsedMilliseconds + "ms";
                        sw_pointer.Restart();
                    }
                }
                catch (Exception ee)
                {
                    Infos.newErr(ee, null);
                }
            }
        }

        private void main_MouseMove(object sender, MouseEventArgs e)
        {
            if(set && egraphMouseDown)
            {
                changePosEgraph(e);
            }
        }

        private async void sendInfo(string txt)
        {
            try
            {
                byte[] data = System.Text.Encoding.ASCII.GetBytes(txt);
                await udp.SendAsync(data, data.Length);
                if (!osuM)
                {
                    SocketAsyncEventArgs sa = new SocketAsyncEventArgs();
                    sa.Completed += (sender, e) =>
                    {
                        if (e.SocketError != SocketError.Success)
                        {
                            Close();
                            Infos.newErr(null, "You are not connected :(");
                        }
                    };
                    byte[] data2 = new byte[0];
                    sa.SetBuffer(data2, 0, data2.Length);
                    tcp.Client.SendAsync(sa);
                }
                if (debug)
                {
                    ms_write.Text = sw_write.ElapsedMilliseconds + "ms";
                    sw_write.Restart();
                }
            }
            catch (Exception e)
            {
                Close();
                Infos.newErr(e, "You are not connected :(");
            }
        }

        bool isRight = false;

        private void tablet_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!set && automc && !mouseDown)
            {
                mouseDown = true;
                Point m = Mouse.GetPosition(tablet);
                double pressure = 1;
                StylusDevice d = Stylus.CurrentStylusDevice;
                ///if (d != null) d.Synchronize();
                if (!osuM)
                {
                    pressure = getPressure(d);
                }
                sendInfo("Pos:" + m.X + ";" + m.Y + (!osuM ? ";" + pressure : "") + "|");
                isRight = e.RightButton == MouseButtonState.Pressed;
                sendInfo("mouse_" + (isRight ? "r" : "") + "click|");
                if (debug)
                {
                    ms_X.Text = m.X.ToString();
                    ms_Y.Text = m.Y.ToString();
                    ms_isstylus.Text = (d != null).ToString();
                    ms_ishovering.Text = isHovering.ToString();
                    ms_prs.Text = pressure.ToString();
                    ms_pointer.Text = sw_pointer.ElapsedMilliseconds + "ms";
                    sw_pointer.Restart();
                }
            }
        }

        private double getPressure(StylusDevice d)
        {
            double pressure = 1;
            if (d != null)
            {
                StylusPointCollection pts = d.GetStylusPoints(tablet);
                float press = pts.Last().PressureFactor;
                if (pi.Value < 1) pressure = press * (1 + press);
                if (pi.Value > 1) pressure = press * pi.Value;
            }
            return pressure;
        }

        private void tablet_MouseUp(object sender, MouseButtonEventArgs e)
        {
            mouseUp();
        }

        private void mouseUp()
        {
            if (!set && automc && mouseDown)
            {
                mouseDown = false;
                sendInfo("mouse_" + (isRight ? "r" : "") + "up|");
                isRight = false;
            }
        }

        private void click_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!set)
            {
                Button b = (Button)((Label)sender).Parent;
                sendInfo("mouse_" + (b == click_r ? "r" : "") + "clickb| ");
            }
        }

        private void click_MouseUp(object sender, RoutedEventArgs e)
        {
            if (!set)
            {
                Button b = (Button)sender;
                sendInfo("mouse_" + (b == click_r ? "r" : "") + "upb|");
            }
        }

        private void main_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11)
            {
                if (WindowState == WindowState.Maximized)
                {
                    WindowState = WindowState.Normal;
                    WindowStyle = WindowStyle.SingleBorderWindow;
                }
                else
                {
                    WindowState = WindowState.Maximized;
                    WindowStyle = WindowStyle.None;
                }
            }
            if(e.Key == Key.Escape)
            {
                quit();
            }
        }

        private void canv_Gesture(object sender, InkCanvasGestureEventArgs e)
        {
            ms_draw.Text = sw_draw.ElapsedMilliseconds + "ms";
            sw_draw.Restart();
        }

        private void key_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!set)
            {
                Button b = (Button)((Image)sender).Parent;
                sendInfo(b.Name.Replace("_", "-") + "-kdown|");
            }
        }

        public IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void pi_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Infos.sf.setSetting("Pressure_Intensifier", pi.Value, null);
        }

        private void key_MouseUp(object sender, RoutedEventArgs e)
        {
            if (!set)
            {
                Button b = (Button)sender;
                sendInfo(b.Name.Replace("_", "-") + "-kup|");
            }
        }

        private void settings_Click(object sender, RoutedEventArgs e)
        {
            if (!set)
            {
                if (bset.Visibility == Visibility.Visible) bset.Visibility = Visibility.Hidden;
                else bset.Visibility = Visibility.Visible;
            }
        }

        private void tablet_StylusDown(object sender, StylusDownEventArgs e)
        {
            /*if (!set && automc)
            {
                mouseDown = true;
                Point m = Mouse.GetPosition(tablet);
                double pressure = 1;
                StylusDevice d = Stylus.CurrentStylusDevice;
                if (d != null) d.Synchronize();
                if (!osuM)
                {
                    pressure = getPressure(d);
                }
                sendInfo("Pos:" + m.X + ";" + m.Y + (!osuM ? ";" + pressure : "") + "|");
                sendInfo("mouse_click|");
            }*/
        }

        private void tablet_StylusUp(object sender, StylusEventArgs e)
        {
            //mouseUp();
        }

        private void grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (set)
            {
                egraphMouseDown = true;
                if (egraph_1.IsMouseOver) egraphnb = 1;
                if (egraph_2.IsMouseOver) egraphnb = 2;
                if (egraph_3.IsMouseOver) egraphnb = 3;
                if (egraph_4.IsMouseOver) egraphnb = 4;
            }
        }

        private void grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (set)
            {
                egraphMouseDown = false;
                egraphnb = 0;
            }
        }

        private void tablet_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!set)
            {
                sendInfo("mouse_wheel_" + e.Delta + "|");
            }
        }
    }
}
