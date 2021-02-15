using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using NLog;
using System.Data.SqlClient;
using System.Configuration;
using RestSharp;
using System.IO;
using TPAPI.Provider;
using System.Text;
using Newtonsoft.Json.Linq;

namespace TPAPI.Models.Table
{
    public struct Migration
    {
        public string versions;
    }
    public static class Migrate
    {
        public static string programVersion = "____";
        private static string SqlMigrate(string version)
        {
            return 
                @"
                    insert into " + 
                    nameof(Migration) + 
                    @" (" + nameof(Migration.versions) + ") " +
                    "values " +
                    "('" + version + "')"
                    ;
        }
        public static void Start()
        {
            string version = "";
            Logger logger = LogManager.GetCurrentClassLogger();

            try
            {
                programVersion += File.ReadAllText(System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath + @".git\FETCH_HEAD").Substring(0, 7);
            }
            catch (Exception ex)
            {
                logger.Error("找不到programVersion : " + ex.ToString());
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(General.ConnString_TPDB()))
                {
                    conn.Open();

                    {
                        var exist = conn.ExecuteScalar<bool>(@"
                                IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('" + nameof(Migration) + @"'))
                                    select 1
                                ELSE
                                    select 0
                            ");

                        if (!exist)
                        {
                            string sql = @"
                            CREATE TABLE " + nameof(Migration) + @" (" +
                                    "id" + " " + @"INT PRIMARY KEY NOT NULL IDENTITY(1,1)" +
                                    "," +
                                    nameof(Migration.versions) + " " + @"CHAR(14) NOT NULL UNIQUE
                            );
                        ";
                            conn.Execute(sql);
                        }
                    }


                    var set = conn.Query<string>("Select " + nameof(Migration.versions) + " from " + nameof(Migration)).ToHashSet<string>();

                    version = "2020_0915_1041";
                    if (!set.Contains(version))
                    {
                        string sql = @"

                        create table [dbo].[Logs](
	                        [id] [int] IDENTITY(1,1) not null,
	                        [Level] [varchar](max) not null,
	                        [CallSite] [varchar](max) not null,
	                        [Type] [varchar](max) not null,
	                        [Message] [nvarchar](max) not null,
                            [LoggerName] [nvarchar](150) NOT NULL,
	                        [StackTrace] [varchar](max) not null,
	                        [InnerException] [varchar](max) not null,
	                        [AdditionalInfo] [nvarchar](max) not null,
	                        [LoggedOnDate] [datetime] not null constraint [df_logs_loggedondate]  default (dateadd( hour,8,getutcdate())),

	                        constraint [pk_logs] primary key clustered 
	                        (
		                        [id]
	                        )
                        )

                    ";

                        conn.Execute(sql);

                        conn.Execute(SqlMigrate(version));
                    }


                    {
                        var exist = conn.ExecuteScalar<bool>(@"
                                IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('InsertLog'))
                                    select 1
                                ELSE
                                    select 0
                            ");

                        if (!exist)
                        {
                            // 需要手動執行以下sql  透過C#執行會出錯
                            var sql = @"
                            go

                            create procedure InsertLog
                            (
	                            @level varchar(max),
	                            @callSite varchar(max),
                                @logger nvarchar(150),
	                            @type varchar(max),
	                            @message nvarchar(max),
	                            @stackTrace varchar(max),
	                            @innerException varchar(max),
	                            @additionalInfo nvarchar(max)
                            )
                            as
                            insert into dbo.Logs
                            (
	                            [Level],
	                            CallSite,
                                [LoggerName],
	                            [Type],
	                            [Message],
	                            StackTrace,
	                            InnerException,
	                            AdditionalInfo
                            )
                            values
                            (
	                            @level,
	                            @callSite,
                                @logger,
	                            @type,
	                            @message,
	                            @stackTrace,
	                            @innerException,
	                            @additionalInfo
                            )

                            go
                                ";

                            conn.Execute(sql);
                        }
                    }


                    version = "2020_0916_1003";
                    if (!set.Contains(version))
                    {
                        var trans = conn.BeginTransaction();
                        try
                        {
                            var sql = @"  CREATE TABLE " + nameof(Messages) + @" (" +
                               "id" + " " + @"INT UNIQUE NOT NULL IDENTITY(1,1)" +
                               "," +
                               nameof(Messages.provider) + " " + @"INT NOT NULL" +
                               "," +
                               nameof(Messages.type) + " " + @"INT NOT NULL" +
                               "," +
                               nameof(Messages.brandID) + " " + @"INT NOT NULL" +
                               "," +
                               nameof(Messages.mainAccountID) + " " + @"VARCHAR(20) NOT NULL" +
                               "," +
                               nameof(Messages.receiver) + " " + @"VARCHAR(100) NOT NULL" +
                               "," +
                               nameof(Messages.title) + " " + @"NVARCHAR(100) NOT NULL" +
                               "," +
                               nameof(Messages.content) + " " + @"NVARCHAR(max) NOT NULL" +
                               "," +
                               nameof(Messages.datetime) + " " + @"DATETIME NOT NULL" +
                                "," +
                                "INDEX IDX_" + nameof(Messages.brandID) + " NONCLUSTERED(" + nameof(Messages.brandID) + ")" +
                                "," +
                                "INDEX IDX_" + nameof(Messages.mainAccountID) + " NONCLUSTERED(" + nameof(Messages.mainAccountID) + ")" +
                               @");";

                            new SqlCommand(sql, conn, trans).ExecuteNonQuery();



                        }
                        catch
                        {
                            trans.Rollback();
                            throw;
                        }
                        trans.Commit();


                        conn.Execute(SqlMigrate(version));
                    }


                    version = "2020_0921_0945";
                    if (!set.Contains(version))
                    {
                        var trans = conn.BeginTransaction();
                        try
                        {
                            new SqlCommand(
                                "ALTER TABLE " +
                                nameof(Messages) +
                                " ADD " +
                                nameof(Messages.sender) +
                                " VARCHAR(100) NULL"
                                , conn, trans).ExecuteNonQuery();

                        }
                        catch
                        {
                            trans.Rollback();
                            throw;
                        }
                        trans.Commit();


                        conn.Execute(SqlMigrate(version));
                    }


                    version = "2020_1013_1400";
                    if (!set.Contains(version))
                    {
                        var trans = conn.BeginTransaction();
                        try
                        {
                            new SqlCommand(
                                "ALTER TABLE " +
                                nameof(Messages) +
                                " ALTER COLUMN " +
                                nameof(Messages.sender) +
                                " NVARCHAR(100) NULL"
                                , conn, trans).ExecuteNonQuery();

                        }
                        catch
                        {
                            trans.Rollback();
                            throw;
                        }
                        trans.Commit();

                        conn.Execute(SqlMigrate(version));
                    }


                    version = "2020_1208_1145";
                    if (!set.Contains(version))
                    {
                        var trans = conn.BeginTransaction();
                        try
                        {
                            new SqlCommand(@"  CREATE TABLE tempTable" + @" (" +
                                      "id" + " " + @"INT UNIQUE NOT NULL IDENTITY(1,1)" +
                                      "," +
                                      nameof(Messages.provider) + " " + @"INT NOT NULL" +
                                      "," +
                                      nameof(Messages.type) + " " + @"INT NOT NULL" +
                                      "," +
                                      nameof(Messages.brandID) + " " + @"INT NOT NULL" +
                                      "," +
                                      nameof(Messages.mainAccountID) + " " + @"VARCHAR(20) NOT NULL" +
                                      "," +
                                      nameof(Messages.receiver) + " " + @"VARCHAR(100) NOT NULL" +
                                      "," +
                                      nameof(Messages.title) + " " + @"NVARCHAR(100) NOT NULL" +
                                      "," +
                                      nameof(Messages.content) + " " + @"NVARCHAR(max) NOT NULL" +
                                      "," +
                                      nameof(Messages.datetime) + " " + @"DATETIME NOT NULL" +
                                      "," +
                                      nameof(Messages.sender) + " " + @"NVARCHAR(100) NULL" +
                                       "," +
                                       "INDEX IDX_" + nameof(Messages.brandID) + " NONCLUSTERED(" + nameof(Messages.brandID) + ")" +
                                       "," +
                                       "INDEX IDX_" + nameof(Messages.mainAccountID) + " NONCLUSTERED(" + nameof(Messages.mainAccountID) + ")" +
                                       "," +
                                       "INDEX IDX_" + nameof(Messages.type) + " NONCLUSTERED(" + nameof(Messages.type) + ")" +
                                      @");", conn, trans).ExecuteNonQuery();


                            new SqlCommand(@"
                                         INSERT INTO tempTable (
                                        " + nameof(Messages.provider) + @",
                                        " + nameof(Messages.type) + @",
                                        " + nameof(Messages.brandID) + @",
                                        " + nameof(Messages.mainAccountID) + @",
                                        " + nameof(Messages.receiver) + @",
                                        " + nameof(Messages.title) + @",
                                        " + nameof(Messages.content) + @",
                                        " + nameof(Messages.datetime) + @",
                                        " + nameof(Messages.sender) + @")
                                           SELECT 
                                        " + nameof(Messages.provider) + @",
                                        " + nameof(Messages.type) + @",
                                        " + nameof(Messages.brandID) + @",
                                        " + nameof(Messages.mainAccountID) + @",
                                        " + nameof(Messages.receiver) + @",
                                        " + nameof(Messages.title) + @",
                                        " + nameof(Messages.content) + @",
                                        " + nameof(Messages.datetime) + @",
                                        " + nameof(Messages.sender) +
                                        @" FROM " + nameof(Messages) + @"
                                         ", conn, trans).ExecuteNonQuery();


                            new SqlCommand(@"exec sp_rename '" + nameof(Messages) + "', 'MessagesDelete'", conn, trans).ExecuteNonQuery();

                            new SqlCommand(@"exec sp_rename 'tempTable', '" + nameof(Messages) + "'", conn, trans).ExecuteNonQuery();

                            new SqlCommand("Drop Table MessagesDelete", conn, trans).ExecuteNonQuery();

                        }
                        catch
                        {
                            trans.Rollback();
                            throw;
                        }
                        trans.Commit();


                        conn.Execute(SqlMigrate(version));
                    }

                    //-------------------example-------------------
                    bool FALSE = false;
                    if (FALSE)
                    {
                        version = "202X_XXXX_XXXX";
                        if (!set.Contains(version))
                        {
                            var trans = conn.BeginTransaction();
                            try
                            {
                                new SqlCommand(@"create table abc1 (id INT);", conn, trans).ExecuteNonQuery();
                                new SqlCommand(@"create table abc2 (id INT);", conn, trans).ExecuteNonQuery();
                            }
                            catch
                            {
                                trans.Rollback();
                                throw;
                            }
                            trans.Commit();


                            conn.Execute(SqlMigrate(version));
                        }
                    }
                    //-------------------example-------------------


                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Migrate Error!!! version:" + version);
                throw ex;
            }
        }
    }
}