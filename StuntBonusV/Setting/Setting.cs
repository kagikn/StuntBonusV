using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace StuntBonusV
{
    public abstract class Setting 
    {
        public abstract bool Validate();
    }
}
