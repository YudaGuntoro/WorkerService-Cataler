using SqliteConfigStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Web.API.Persistence.Shared
{
    public static class PersistentValue
    {
        private static string _MYSQL_URL;
        private static string _MYSQL_USERNAME;
        private static string _MYSQL_PASSWORD;
        private static string _MYSQL_DBNAME;
        private static appsetting appsetting = new appsetting("dbconfig_persistence");

        static PersistentValue()
        {
            LoadConfig();
        }

        public static void LoadConfig()
        {
            #region "mysql"
            MYSQL_URL = TryParseString("mysql", "url", "127.0.0.1");
            MYSQL_USERNAME = TryParseString("mysql", "username", "root");
            MYSQL_PASSWORD = TryParseString("mysql", "password", "root_native");
            MYSQL_DBNAME = TryParseString("mysql", "dbname", "cataler");
            #endregion
        }

        #region "mysql"
        public static string MYSQL_URL
        {
            get { return _MYSQL_URL; }
            set
            {
                _MYSQL_URL = value;
                appsetting.WriteKey("mysql", "url", value.ToString());
            }
        }
        public static string MYSQL_USERNAME
        {
            get { return _MYSQL_USERNAME; }
            set
            {
                _MYSQL_USERNAME = value;
                appsetting.WriteKey("mysql", "username", value.ToString());
            }
        }
        public static string MYSQL_PASSWORD
        {
            get { return _MYSQL_PASSWORD; }
            set
            {
                _MYSQL_PASSWORD = value;
                appsetting.WriteKey("mysql", "password", value.ToString());
            }
        }
        public static string MYSQL_DBNAME
        {
            get { return _MYSQL_DBNAME; }
            set
            {
                _MYSQL_DBNAME = value;
                appsetting.WriteKey("mysql", "dbname", value.ToString());
            }
        }
        #endregion

        /// <summary>
        /// Read string from config
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="DefaultValue"></param>
        /// <returns></returns>
        private static string TryParseString(string section, string key, string DefaultValue)
        {
            string ValueOfRead = DefaultValue;
            var ReadValue = appsetting.ReadKey(section, key);
            if (!string.IsNullOrEmpty(ReadValue))
            {
                ValueOfRead = ReadValue;
            }
            return ValueOfRead;
        }

        /// <summary>
        /// Read int32 from config
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="DefaultValue"></param>
        /// <returns></returns>
        private static int TryParseInt32(string section, string key, int DefaultValue)
        {
            int ValueOfRead = DefaultValue;
            var StatusReadValue = Int32.TryParse(appsetting.ReadKey(section, key), out var OutReadValue);
            if (StatusReadValue)
                ValueOfRead = OutReadValue;
            else
                ValueOfRead = DefaultValue;
            return ValueOfRead;
        }
    }
}
