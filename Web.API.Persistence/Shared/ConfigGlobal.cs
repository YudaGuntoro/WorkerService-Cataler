using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Persistence.Shared
{
    public class ConfigGlobal
    {
        public static readonly string SqliteConnString = "Data Source=dbconfig_persistence.db";

        private static string Serverurl = PersistentValue.MYSQL_URL;
        private static string User = PersistentValue.MYSQL_USERNAME;
        private static string Pwd = PersistentValue.MYSQL_PASSWORD;
        private static string Dbname = PersistentValue.MYSQL_DBNAME;
        private static bool pooling = false;

        // -- for mysql--
        public static readonly string MysqlConnString = $@"Server='" + Serverurl + "';" +
            "Database='" + Dbname + "';" +
            "Uid='" + User + "';" +
            "Pwd='" + Pwd + "';" +
            "pooling= " + pooling + " ;" +
            "Convert Zero Datetime=True;" +
            "SslMode=none;" +
            "allowPublicKeyRetrieval=true";

        //public static string SecretPassword => "";
    }
}
