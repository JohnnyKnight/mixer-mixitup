﻿using Microsoft.Owin.Hosting;
using MixItUp.Base.Services;
using MixItUp.Base.Services.External;
using Owin;
using System;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Web.Http;

namespace MixItUp.WPF.Services.DeveloperAPI
{
    public class WindowsDeveloperAPIService : IDeveloperAPIService
    {
        private IDisposable webApp;
        public readonly string[] DeveloperAPIHttpListenerServerAddresses = new string[] { "http://localhost:8911/", "http://127.0.0.1:8911/" };

        public string Name { get { return "Developer API"; } }

        public bool IsConnected { get; private set; }

        public Task<ExternalServiceResult> Connect()
        {
            // Ensure it is cleaned up first
            this.Disconnect();

            StartOptions opts = new StartOptions();
            foreach(var url in DeveloperAPIHttpListenerServerAddresses)
            {
                opts.Urls.Add(url);
            }

            this.webApp = WebApp.Start<WindowsDeveloperAPIServiceStartup>(opts);

            this.IsConnected = true;
            return Task.FromResult(new ExternalServiceResult());
        }

        public Task Disconnect()
        {
            if (this.webApp != null)
            {
                this.webApp.Dispose();
                this.webApp = null;
            }
            this.IsConnected = false;
            return Task.FromResult(0);
        }
    }

    public class WindowsDeveloperAPIServiceStartup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            HttpConfiguration config = new HttpConfiguration();

            config.Formatters.Clear();
            config.Formatters.Add(new JsonMediaTypeFormatter());

            config.MapHttpAttributeRoutes();
            config.MessageHandlers.Add(new NoCacheHeader());

            appBuilder.UseWebApi(config);
        }
    }
}
