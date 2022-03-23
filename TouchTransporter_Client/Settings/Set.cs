using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TouchTransporter_Client.Settings
{
    public class Set
    {
        public static void syncSettings(MainWindow main)
        {
            if (!Infos.sf.settingExists("osuM")) Infos.sf.setSetting("osuM", false, null);
            main.mod_osu.IsChecked = main.tgraph.osuM = Infos.sf.getBoolSetting("osuM");
            if (!Infos.sf.settingExists("Automc")) Infos.sf.setSetting("Automc", true, null);
            main.mod_autom.IsChecked = main.tgraph.automc = Infos.sf.getBoolSetting("Automc");
            if (!Infos.sf.settingExists("Debug")) Infos.sf.setSetting("Debug", false, null);
            main.mod_debug.IsChecked = main.tgraph.debug = Infos.sf.getBoolSetting("Debug");
            if (!Infos.sf.settingExists("Hover")) Infos.sf.setSetting("Hover", true, null);
            main.mod_hover.IsChecked = main.tgraph.allowHover = Infos.sf.getBoolSetting("Hover");
            //upd
            if (!Infos.sf.settingExists("Url_Server")) Infos.sf.setSetting("Url_Server", "aketsuky.eu", new string[] { "Server" });
            if (!Infos.sf.settingExists("Write_logs"))
            {
                Infos.sf.setSetting("Write_logs", false, null);
            }
            Infos.wlog = Infos.sf.getBoolSetting("Write_logs");
        }
    }
}
