using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using Funq;
using ServiceStack.DataAnnotations;
using ServiceStack.MiniProfiler;
using ServiceStack.MiniProfiler.Data;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStackConcurrency2.Web
{
    [Route("/hello")]
    [Route("/hello/{Name}")]
    public class Hello
    {
        public string Name { get; set; }
    }

    public class HelloResponse
    {
        public List<DummyTable> fromSqlServer { get; set; }
        public List<DummyTable> fromPostgres { get; set; }
        public String Exception { get; set; }
    }

    public class HelloService : Service
    {
        public object Get(Hello request)
        {
            //to make this fail: use the current OrmLite dll's from nuget.
            //to make it work: use OrmLite dll's from @modifiedDlls folder
            var postgresFactory = new OrmLiteConnectionFactory(ConfigurationManager.ConnectionStrings["servicestack_postgres"].ConnectionString, PostgreSqlDialect.Provider) {
                ConnectionFilter = x => new ProfiledDbConnection(x, Profiler.Current)
            };
            var sqlServerFactory = new OrmLiteConnectionFactory(ConfigurationManager.ConnectionStrings["servicestack_sqlserver"].ConnectionString, SqlServerDialect.Provider) {
                ConnectionFilter = x => new ProfiledDbConnection(x, Profiler.Current)
            };

            List<DummyTable> fromSqlServer = null;
            List<DummyTable> fromPostgres = null;

            try {

                using(var postgresConn = postgresFactory.OpenDbConnection()) {
                    postgresConn.DropAndCreateTable<DummyTable>();//fails at the drop and create step if ConnectionFilters are set
                    postgresConn.Insert(new DummyTable { DummyString = "elephant 1" }, new DummyTable { DummyString = "elephant 1" }, new DummyTable { DummyString = "elephant 1" });

                    fromPostgres = postgresConn.Select<DummyTable>(y => y.Limit(2));


                    using(var sqlConn = sqlServerFactory.OpenDbConnection()) {
                        sqlConn.DropAndCreateTable<DummyTable>();
                        sqlConn.Insert(new DummyTable { DummyString = "sql1" }, new DummyTable { DummyString = "sql2" }, new DummyTable { DummyString = "sql3" });
                        fromSqlServer = sqlConn.Select<DummyTable>(y => y.Limit(2));
                    }
                }
                return new HelloResponse { fromSqlServer = fromSqlServer, fromPostgres = fromPostgres };
            }
            catch(Exception exc) {
                return new HelloResponse { Exception = exc.ToString() };
            }
        }
    }

    public class DummyTable
    {
        [AutoIncrement]
        public int Id { get; set; }
        public String DummyString { get; set; }
    }

    public class HelloAppHost : AppHostBase
    {
        //Tell Service Stack the name of your application and where to find your web services
        public HelloAppHost() : base("Hello Web Services", typeof(HelloService).Assembly) { }

        public override void Configure(Container container)
        {
            SetConfig(new EndpointHostConfig { ServiceStackHandlerFactoryPath = "st" });
            //register any dependencies your services use, e.g:
            //container.Register<ICacheClient>(new MemoryCacheClient());
        }
    }
}