using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using GTA.Math;
using System.Runtime.InteropServices;


namespace StuntBonusV
{
    public abstract class StuntBonusScript : Script
    {
        public virtual string SettingFileName => null;

        public StuntBonusScript()
        {
           Interval = 0;
           Setup();
        }

        protected abstract void Setup();

        protected T LoadSetting<T>() where T : Setting, new()
        {
            if (string.IsNullOrEmpty(SettingFileName))
            {
                throw new InvalidOperationException("The Internal setting path string is empty!");
            }

            var settingFilePath = Util.SettingRootPath + Path.DirectorySeparatorChar + SettingFileName;
            var loader = new SettingLoader<T>();
            var setting = loader.Load(settingFilePath);

            return setting;
        }
        protected void SaveSetting<T>(T value) where T : Setting, new()
        {
            if (string.IsNullOrEmpty(SettingFileName))
            {
                throw new InvalidOperationException("The Internal setting path string is empty!");
            }

            var settingFilePath = Util.SettingRootPath + Path.DirectorySeparatorChar + SettingFileName;
            var loader = new SettingLoader<T>();
            loader.Save(settingFilePath, value);
        }

        protected enum ShowingResultStyle { Subtitle, Notification };

        protected void ShowResult(string message, ShowingResultStyle resultStyle, int durationForSubtitle = 3000)
        {
            switch(resultStyle)
            {
                case ShowingResultStyle.Subtitle:
                    GtaNativeUtil.ShowSubtitle(message, durationForSubtitle, false);
                    break;
                case ShowingResultStyle.Notification:
                    GtaNativeUtil.ShowNotification(message);
                    break;
            }
        }

        protected void ShowResult(string message, int durationForSubtitle)
        {
            GtaNativeUtil.ShowSubtitle(message, durationForSubtitle, false);
        }
    }
}
