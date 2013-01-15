using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using ServiceStack.MiniProfiler;

namespace ServiceStackConcurrency2.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            new HelloAppHost().Init();

            AreaRegistration.RegisterAllAreas();      
        }

        protected void Application_BeginRequest(object src, EventArgs e)
        {
            Profiler.Start();
        }

        protected void Application_EndRequest(object src, EventArgs e)
        {
            Profiler.Stop();
        }
    }
}