using ApprovalBot.Helpers;
using Autofac;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using System;
using System.Configuration;
using System.Reflection;
using System.Web.Http;

namespace ApprovalBot
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

            Conversation.UpdateContainer(
                builder =>
                {
                    builder.RegisterModule(new AzureModule(Assembly.GetExecutingAssembly()));

                    // This will create a CosmosDB store, suitable for production
                    // NOTE: Requires an actual CosmosDB instance and configuration in
                    // PrivateSettings.config
                    var databaseUri = new Uri(ConfigurationManager.AppSettings["DatabaseUri"]);
                    var databaseKey = ConfigurationManager.AppSettings["DatabaseKey"];
                    var store = new DocumentDbBotDataStore(databaseUri, databaseKey);

                    builder.Register(c => store)
                        .Keyed<IBotDataStore<BotData>>(AzureModule.Key_DataStore)
                        .AsSelf()
                        .SingleInstance();
                });

            // Initialize approvals database
            DatabaseHelper.Initialize();
        }
    }
}
