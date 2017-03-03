using Microsoft.Owin.Hosting;
using System;
using Topshelf;

namespace DiamDev.Give.LicenseManager
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x => {
                x.Service<Service>(s => {
                    s.ConstructUsing(name => new Service());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsNetworkService();
                x.StartAutomaticallyDelayed();

                x.SetDescription("Reflex License Manager");
                x.SetDisplayName("Reflex License Manager");
                x.SetServiceName("DiamDev.Give.LicenseManager");
            });
        }

        private class Service
        {
            private const string url = "http://localhost:10000";
            private IDisposable webserver;

            public void Start()
            {
                webserver = WebApp.Start<Startup>(url);
                Console.WriteLine("License Manager listening on ", url);
            }

            public void Stop()
            {

            }
        }
    }
}
