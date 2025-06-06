using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationTests
{
    /*
     *  to spin up the real ASP.NET Core app entirely in memory—no
     *  real web server or external database 
     */
    public class CustomWebApplicationFactory
        : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {

            return base.CreateHost(builder);
        }
    }
}
