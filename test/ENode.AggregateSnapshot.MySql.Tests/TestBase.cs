﻿using ECommon.Configurations;
using ENode.Configurations;
using System;
using System.Reflection;
using ECommonConfiguration = ECommon.Configurations.Configuration;

namespace ENode.AggregateSnapshot.Tests
{
    public abstract class TestBase : IDisposable
    {
        public TestBase()
        {
            Initialize();
        }

        public void Dispose()
        {
            Clean();
        }

        private void Clean()
        {
        }

        private void Initialize()
        {
            var assemblies = new[] { Assembly.GetExecutingAssembly() };

            var enode = ECommonConfiguration.Create()
                .UseAutofac()
                .RegisterCommonComponents()
                .UseLog4Net()
                .UseJsonNet()
                .CreateENode(new ConfigurationSetting())
                .RegisterBusinessComponents(assemblies)
                .RegisterENodeComponents()
                .UseMySqlAggregateSnapshotter();

            enode.GetCommonConfiguration()
              .BuildContainer();

            enode.InitializeBusinessAssemblies(assemblies)
                .InitializeMySqlAggregateSnapshotter(
                "Datasource=127.0.0.1;Database=eventstore;uid=root;pwd=admin!@#;Allow User Variables=True;AutoEnlist=false;"
                );
        }
    }
}