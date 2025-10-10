using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqliteConfigStore
{
    public class SQLiteDatabase
    {
        public SQLiteConnection DBConnection { get; protected set; } = null;
        public string DBLocation { get; protected set; } = null;
        public string LastError { get; protected set; } = null;

        public int LastInsertID
        {
            get
            {
                if (this.DBConnection == null)
                {
                    return 0;
                }

                DataTable dt = this.DoQuery("SELECT last_insert_rowid() AS rv;");
                if (dt == null)
                {
                    return 0;
                }

                if (dt.Rows.Count <= 0)
                {
                    return 0;
                }

                int n;
                if (!int.TryParse(dt.Rows[0]["rv"].ToString(), out n))
                {
                    n = 0;
                }

                return n;
            }
        }

        protected string[] requiredTableList = new string[0];
        protected const string appSettingsTableName = "appconfig";
        public bool Create()
        {
            this.LastError = string.Empty;

            this.DBLocation = ":memory:";

            try
            {
                this.DBConnection = new SQLiteConnection(string.Concat("Data Source=\"", this.DBLocation, "\";Version=3;UTF8Encoding=True;"));
                this.DBConnection.Open();
            }
            catch (Exception exc)
            {
                this.LastError = exc.Message;

                return false;
            }

            return true;
        }

        public bool Create(string filename, bool overwriteFile = false, string password = null, bool useAppSettings = false)
        {
            bool rs = create(filename, overwriteFile, password);

            if (useAppSettings)
            {
                if (!rs)
                {
                    return false;
                }

                rs = prepareDatabase();
                if (!rs)
                {
                    return false;
                }

                return this.HasRequiredTables();
            }
            else
            {
                return rs;
            }
        }

        public bool LoadFile(string filename, string password = null)
        {
            this.LastError = string.Empty;

            if (!File.Exists(filename))
            {
                return false;
            }

            this.DBLocation = filename;

            try
            {
                this.DBConnection = new SQLiteConnection(string.Concat("Data Source=\"", filename, "\";Version=3;UTF8Encoding=True;", (password == null) ? string.Empty : string.Concat("Password=", encode64(password), ";")));
                this.DBConnection.Open();
            }
            catch (Exception exc)
            {
                this.LastError = exc.Message;

                return false;
            }

            return true;
        }

        public void Close()
        {
            this.LastError = string.Empty;

            if (this.DBConnection != null)
            {
                try
                {
                    this.DBConnection.Cancel();
                    this.DBConnection.Close();
                    this.DBConnection.Dispose();
                    this.DBConnection = null;

                    SQLiteConnection.ClearAllPools();
                    GC.Collect();
                }
                catch (Exception exc)
                {
                    this.LastError = exc.Message;
                }
            }
        }

        public DataTable DoQuery(string query)
        {
            this.LastError = string.Empty;

            if (this.DBConnection == null)
            {
                return null;
            }

            try
            {
                SQLiteCommand command = new SQLiteCommand(query, this.DBConnection);
                SQLiteDataReader dr = command.ExecuteReader();

                DataTable dt = new DataTable();
                dt.Load(dr);

                return dt;
            }
            catch (Exception exc)
            {
                this.LastError = exc.Message;

                return null;
            }
        }

        public DataTable DoQuery(string query, params string[] args) => DoQuery(prepareQuery(query, args));

        public int DoQueryCount(string query)
        {
            this.LastError = string.Empty;

            if (this.DBConnection == null)
            {
                return -1;
            }

            DataTable dt = this.DoQuery(query);
            if (dt == null)
            {
                return -1;
            }

            return dt.Rows.Count;
        }

        public int DoQueryCount(string query, params string[] args) => this.DoQueryCount(prepareQuery(query, args));

        public bool DoQueryExist(string query) => (this.DoQueryCount(query) > 0);

        public bool DoQueryExist(string query, params string[] args) => this.DoQueryExist(prepareQuery(query, args));

        public string DoQuerySingle(string query)
        {
            this.LastError = string.Empty;

            if (this.DBConnection == null)
            {
                return string.Empty;
            }

            DataTable dt = this.DoQuery(query);

            if (dt == null)
            {
                return string.Empty;
            }

            if (dt.Columns.Count <= 0)
            {
                return string.Empty;
            }

            if (dt.Rows.Count <= 0)
            {
                return string.Empty;
            }

            if (dt.Rows[0][0] is byte[])
            {
                return Encoding.UTF8.GetString(dt.Rows[0][0] as byte[]);
            }
            else
            {
                return dt.Rows[0][0].ToString();
            }
        }

        public string DoQuerySingle(string query, params string[] args) => this.DoQuerySingle(prepareQuery(query, args));

        public int DoNonQuery(string query)
        {
            this.LastError = string.Empty;

            if (this.DBConnection == null)
            {
                return -1;
            }

            int rv = 0;

            try
            {
                SQLiteCommand command = new SQLiteCommand(query, this.DBConnection);
                rv = command.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                this.LastError = exc.Message;
                rv = -1;
            }

            return rv;
        }

        public int DoNonQuery(string query, params string[] args) => this.DoNonQuery(prepareQuery(query, args));

        public bool HasTable(string tableName)
        {
            this.LastError = string.Empty;

            if (this.DBConnection == null)
            {
                return false;
            }

            int rv = this.DoQueryCount("SELECT 1 FROM sqlite_master WHERE type='table' AND name='" + escapeSQL(tableName) + "'");

            return (rv > 0);
        }

        public bool HasRequiredTables()
        {
            bool rv = true;
            foreach (string tbl in requiredTableList)
            {
                if (string.IsNullOrEmpty(tbl))
                {
                    continue;
                }

                if (!this.HasTable(tbl))
                {
                    rv = false;
                    break;
                }
            }

            return rv;
        }

        public bool SetConfig(string name, bool value) => this.SetConfig(name, (value ? "1" : "0"));

        public bool SetConfig(string name, int value) => this.SetConfig(name, value.ToString());

        public bool SetConfig(string name, string value)
        {
            prepareAppSettings();

            string sql = string.Empty;
            int rv = this.DoQueryCount("SELECT 1 FROM " + appSettingsTableName + " WHERE cfg_name='" + escapeSQL(name) + "'");
            if (rv <= 0)
            {
                sql = "INSERT INTO " + appSettingsTableName + " (cfg_name, cfg_value) VALUES ('[^1]', '[^2]');";
            }
            else
            {
                sql = "UPDATE " + appSettingsTableName + " SET cfg_value='[^2]' WHERE cfg_name='[^1]';";
            }

            sql = prepareQuery(sql, new string[] { name, value });

            return this.DoNonQuery(sql) > 0;
        }

        public string GetConfig(string name, string defaultValue = null)
        {
            prepareAppSettings();

            bool rv = this.DoQueryExist("SELECT 1 FROM " + appSettingsTableName + " WHERE cfg_name='" + escapeSQL(name) + "';");
            if (!rv)
            {
                return defaultValue;
            }

            return this.DoQuerySingle("SELECT cfg_value FROM " + appSettingsTableName + " WHERE cfg_name='" + escapeSQL(name) + "';");
        }

        public int GetIntConfig(string name, int defaultValue = 0)
        {
            string rv = this.GetConfig(name);
            if (string.IsNullOrWhiteSpace(rv))
            {
                return defaultValue;
            }

            int n;
            if (!int.TryParse(rv, out n))
            {
                n = defaultValue;
            }

            return n;
        }

        public bool GetBoolConfig(string name, bool defaultValue = false)
        {
            string rv = this.GetConfig(name);
            if (string.IsNullOrWhiteSpace(rv))
            {
                return defaultValue;
            }

            if (rv.Equals("1"))
            {
                return true;
            }

            if (rv.Equals("true", StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }

        protected bool create(string filename, bool overwriteFile, string password)
        {
            this.LastError = string.Empty;

            if (File.Exists(filename))
            {
                if (overwriteFile)
                {
                    try
                    {
                        File.Delete(filename);
                    }
                    catch (Exception exc)
                    {
                        this.LastError = exc.Message;

                        return false;
                    }

                    try
                    {
                        SQLiteConnection.CreateFile(filename);
                    }
                    catch (Exception exc)
                    {
                        this.LastError = exc.Message;

                        return false;
                    }

                    return this.LoadFile(filename, password);
                }
                else
                {
                    return this.LoadFile(filename, password);
                }
            }
            else
            {
                try
                {
                    SQLiteConnection.CreateFile(filename);
                }
                catch (Exception exc)
                {
                    this.LastError = exc.Message;

                    return false;
                }

                return this.LoadFile(filename, password);
            }
        }

        protected string prepareQuery(string query, params string[] arguments)
        {
            string rv = query;

            if (string.IsNullOrWhiteSpace(rv))
            {
                return string.Empty;
            }

            for (int i = 0; i < arguments.Length; i++)
            {
                rv = rv.Replace("[^" + (i + 1).ToString() + "]", escapeSQL(arguments[i]));
            }

            return rv;
        }

        protected string escapeSQL(string q) => q.Replace("'", "''").Trim();

        protected string escapeValue(string text) => text.Replace("\"", "\\\"").Replace("\t", "\\t").Replace("\r", " \\r").Replace("\n", "\\n");

        protected string encode64(string text) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text));

        protected bool prepareAppSettings()
        {
            if (this.HasTable(appSettingsTableName))
            {
                return true;
            }

            int rv = this.DoNonQuery("CREATE TABLE " + appSettingsTableName + " (cfg_name TEXT, cfg_value TEXT)");

            return rv > 0;
        }

        protected virtual bool prepareDatabase()
        {
            return true;
        }
    }
}
