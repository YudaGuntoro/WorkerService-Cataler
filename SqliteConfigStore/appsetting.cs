using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqliteConfigStore
{
    public class appsetting
    {
        private SQLiteDatabase appSettingsDB = new SQLiteDatabase();

        /// <summary>
        /// create file name of config (withoutn extension name .db)
        /// </summary>
        /// <param name="filename"></param>
        public appsetting(string filename)
        {
            //bool rs = appSettingsDB.Create(Path.ChangeExtension(Application.ExecutablePath, "db"), false, null, true);
            //filename = "dbsettings.db";
            bool rs = appSettingsDB.Create(filename + ".db", false, null, true);
            if (!rs)
            {
                return;
            }
        }

        /// <summary>
        /// Read Key (string)
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public string ReadKey(string section, string key)
        {
            var res = appSettingsDB.GetConfig(section + "_" + key);
            return res;
        }

        /// <summary>
        /// Read Key (int)
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public int ReadKey(string section, int key)
        {
            var res = appSettingsDB.GetIntConfig(section + "_" + key);
            return res;
        }

        /// <summary>
        /// Read Key (bool)
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ReadKey(string section, bool key)
        {
            var res = appSettingsDB.GetBoolConfig(section + "_" + key);
            return res;
        }

        /// <summary>
        /// Write key (string)
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void WriteKey(string section, string key, string value)
        {
            appSettingsDB.SetConfig(section + "_" + key, value);
        }

        /// <summary>
        /// Write Key (int)
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void WriteKey(string section, string key, int value)
        {
            appSettingsDB.SetConfig(section + "_" + key, value);
        }

        /// <summary>
        /// Write Key (bool)
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void WriteKey(string section, string key, bool value)
        {
            appSettingsDB.SetConfig(section + "_" + key, value);
        }
    }
}
