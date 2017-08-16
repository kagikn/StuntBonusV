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
    }
}
