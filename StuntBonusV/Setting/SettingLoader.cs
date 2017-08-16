using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace StuntBonusV
{
    public class SettingLoader<T> where T : Setting, new()
    {
        public T Load(string xmlPath)
        {
            var serializer = new XmlSerializer(typeof(T));

            T settings = null;

            if (File.Exists(xmlPath))
            {
                using (var stream = File.OpenRead(xmlPath))
                {
                    settings = (T)serializer.Deserialize(stream);
                }
            }

            return settings;
        }
        public bool Save(string xmlPath, T settings)
        {
            var serializer = new XmlSerializer(typeof(T));

            if (File.Exists(xmlPath))
            {
                using (var stream = new FileStream(xmlPath, File.Exists(xmlPath) ? FileMode.Truncate : FileMode.Create, FileAccess.ReadWrite))
                {
                    serializer.Serialize(stream, settings);
                    return true;
                }
            }

            return false;
        }
        public T Init(string xmlPath)
        {
            var ser = new XmlSerializer(typeof(T));
            T settings;

            using (var stream = File.OpenWrite(xmlPath))
            {
                ser.Serialize(stream, settings = new T());
            }

            return settings;
        }
    }
}
