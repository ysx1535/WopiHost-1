﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.DotNet.ProjectModel.Loader;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;

namespace WopiHost.Web
{
	public class Startup
	{
		public IConfiguration Configuration { get; set; }

		public Startup(IHostingEnvironment env)
		{
			var appEnv = PlatformServices.Default.Application;
			var builder = new ConfigurationBuilder().SetBasePath(appEnv.ApplicationBasePath)
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).
				AddInMemoryCollection(new Dictionary<string, string>
					{ { nameof(env.WebRootPath), env.WebRootPath },
					{ nameof(appEnv.ApplicationBasePath), appEnv.ApplicationBasePath } })
				/*.AddJsonFile("config.json")
				.AddJsonFile($"config.{env.EnvironmentName}.json", optional: true)*/;

			builder.AddEnvironmentVariables();

			if (env.IsDevelopment())
			{
				// Override with user secrets (http://go.microsoft.com/fwlink/?LinkID=532709)
				builder.AddUserSecrets();
			}
			Configuration = builder.Build();
		}


		/// <summary>
		/// Sets up the DI container. Loads types dynamically (http://docs.autofac.org/en/latest/register/scanning.html)
		/// </summary>
		public IServiceProvider ConfigureServices(IServiceCollection services)
		{
			services.AddMvc();

			// Autofac resolution
			var builder = new ContainerBuilder();

			// Configuration
			builder.RegisterInstance(Configuration).As<IConfiguration>().SingleInstance();

			// File provider implementation
			var providerAssembly = Configuration.GetSection("WopiFileProviderAssemblyName").Value;
			var assembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(providerAssembly));
			builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();

			builder.Populate(services);
			var container = builder.Build();
			return container.Resolve<IServiceProvider>();
		}

		// Configure is called after ConfigureServices is called.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			loggerFactory.AddConsole(Configuration.GetSection("Logging"));
			loggerFactory.AddDebug();

			app.UseDeveloperExceptionPage();

			// Add static files to the request pipeline.
			app.UseStaticFiles();

			// Add MVC to the request pipeline.
			app.UseMvc(routes =>
			{
				routes.MapRoute(
					name: "default",
					template: "{controller=Home}/{action=Index}/{id?}");
			});
		}
	}
}