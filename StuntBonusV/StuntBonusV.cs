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
            var settingFilePath = Util.SettingRootPath + Path.DirectorySeparatorChar + SettingFileName;
            if (string.IsNullOrEmpty(settingFilePath))
            {
                throw new InvalidOperationException("The Internal setting path string is empty!");
            }
            var loader = new SettingLoader<T>();
            var setting = loader.Load(settingFilePath);

            return setting.Validate() ? setting : new T();
        }
        protected void SaveSetting<T>(T value) where T : Setting, new()
        {
            var settingFilePath = Util.SettingRootPath + Path.DirectorySeparatorChar + SettingFileName;
            if (string.IsNullOrEmpty(settingFilePath))
            {
                throw new InvalidOperationException("The Internal setting path string is empty!");
            }
            var loader = new SettingLoader<T>();
            loader.Save(settingFilePath, value);

            //return setting.Validate() ? setting : new T();
        }
    }
}
