﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Remoting.cs" company="LeagueSharp.Loader">
//   Copyright (c) LeagueSharp.Loader. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace LeagueSharp.Loader.Class
{
    using System;
    using System.ServiceModel;

    using LeagueSharp.Sandbox.Shared;

    internal class Remoting
    {
        private static ServiceHost _loaderServiceHost;

        public static void Init()
        {
            _loaderServiceHost = ServiceFactory.CreateService<ILoaderService, LoaderService>();
            _loaderServiceHost.Faulted += OnLoaderServiceFaulted;
        }

        private static void OnLoaderServiceFaulted(object sender, EventArgs eventArgs)
        {
            Console.WriteLine("ILoaderService faulted, trying restart");
            _loaderServiceHost.Faulted -= OnLoaderServiceFaulted;
            _loaderServiceHost.Abort();

            try
            {
                _loaderServiceHost = ServiceFactory.CreateService<ILoaderService, LoaderService>();
                _loaderServiceHost.Faulted += OnLoaderServiceFaulted;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}