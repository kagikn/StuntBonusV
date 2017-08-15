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
    public class StuntBonusV : Script
    {
        public StuntBonusV()
        {
            Tick += StuntBonusMonitor.InsaneStuntBonus.OnTick;
            Tick += StuntBonusMonitor.UniqueStuntBonus.OnTick;
            Tick += StuntBonusMonitor.SkiingBonus.OnTick;
            Tick += StuntBonusMonitor.WheelieBonus.OnTick;
            Tick += StuntBonusMonitor.StoppieBonus.OnTick;
            Interval = 0;
        }
    }
}
