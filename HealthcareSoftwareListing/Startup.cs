using System.Collections.Generic;
using System.Linq;
using AspNetCoreRateLimit;
using HealthcareSoftwareListing.Entities;
using HealthcareSoftwareListing.Models;
using HealthcareSoftwareListing.Services;
using Library.API.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;

namespace HealthcareSoftwareListing
{
	public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
			services.AddMvc(
				setupAction =>
				{
					setupAction.ReturnHttpNotAcceptable = true; // Force consumer to specify data format
					setupAction.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter()); // Support returning XML
					setupAction.InputFormatters.Add(new XmlDataContractSerializerInputFormatter()); // Support incoming XML

					// Versioning support for date of demise with XML
					var xmlDataContractSerializerInputFormatter = new XmlDataContractSerializerInputFormatter();
					xmlDataContractSerializerInputFormatter.SupportedMediaTypes.Add("application/vnd.marvin.companywithdateofdemise.full+xml");
					// Don't bother adding support for XML for v3

					setupAction.InputFormatters.Add(xmlDataContractSerializerInputFormatter);

					// Versioning support for date of demise with JSON
					var jsonInputFormatter = setupAction.InputFormatters.OfType<JsonInputFormatter>().FirstOrDefault();
					if (jsonInputFormatter != null)
					{
						jsonInputFormatter.SupportedMediaTypes.Add("application/vnd.marvin.company.full+json");
						jsonInputFormatter.SupportedMediaTypes.Add("application/vnd.marvin.companywithdateofdemise.full+json");
						jsonInputFormatter.SupportedMediaTypes.Add("application/vnd.marvin.companywithlocation.full+json");
					}

					// Use custom media type to support HATEOAS
					var jsonOutputFormatter = setupAction.OutputFormatters.OfType<JsonOutputFormatter>().FirstOrDefault();
					if (jsonOutputFormatter != null)
					{
						jsonOutputFormatter.SupportedMediaTypes.Add("application/vnd.marvin.hateoas+json");
					}
				}).AddJsonOptions(
					options =>
					{
						// Prevent property name casing from being lost on serialization of JSON requests
						options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
					});

			// Approach using a DB Context (for fast development).
			// https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/
			// Add-Migration InitialCreate or dotnet ef migrations add InitialCreate
			// Update-Database or dotnet ef database update
			// Add-Migration AddDateOfDemise or dotnet ef migrations add AddDateOfDemise
			// TODO - Move connection string to environment variable
			var connectionString = Configuration["connectionStrings:dbConnectionString"];
			services.AddDbContext<RepositoryContext>(context => context.UseSqlServer(connectionString));

			//services.AddScoped<IRepository, MockRepository>();
			services.AddScoped<IRepository, EntityFrameworkRepository>();

			// Register URI helper to support metadata in responses
			services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
			services.AddScoped<IUrlHelper, UrlHelper>(
				implementationFactory =>
				{
					return new UrlHelper(actionContext: implementationFactory.GetService<IActionContextAccessor>().ActionContext);
				});


			services.AddTransient<IPropertyMappingService, PropertyMappingService>(); // Custom service to support sorting
			services.AddTransient<ITypeHelperService, TypeHelperService>(); // Custom service to support data shaping

			// Caching support for consumer using HTTP cache headers from Marvin
			services.AddHttpCacheHeaders(
				(expirationModelOptions)
					=> { expirationModelOptions.MaxAge = 600; }, // expiration caching
				(validationModelOptions)
					=> { validationModelOptions.MustRevalidate = true; }); // validation caching

			services.AddResponseCaching(); // MS .Net core package for response caching. Doesn't generate response headers. Buggy before v2. Not ideal!

			// Implement throttling
			// Can throttle by IP and / or by client.
			// Can throttle by calls to specific controllers or methods
			// Can throttle by (for example); requests per day, requests per hour, and requests per controller.
			// Request headers for this are; X-Rate-Limit-Limit, X-Rate-Limit-Remaining, and X-Rate-Limit-Reset.
			// Disallowed requests will return a 429 response with an optional Retry-After header and a body explaining the condition.

			services.AddMemoryCache(); // Used to store throttling counters and rules

			services.Configure<IpRateLimitOptions>(
				options =>
				{
					options.GeneralRules = new List<RateLimitRule>
					{
						new RateLimitRule // 10 requests every 5 minutes
						{
							Endpoint = "*",
							Limit = 10,
							Period = "5m"
						},
						new RateLimitRule // 2 requests every 10 seconds
						{
							Endpoint = "*",
							Limit = 2,
							Period = "10s"
						}
					};
				});

			services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
			services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
		}

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();

				// Production-friendly global error handling.
				app.UseExceptionHandler(
					appBuilder =>
					{
						appBuilder.Run(async context =>
						{
							var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
							if (exceptionHandlerFeature != null)
							{
								ILogger logger = loggerFactory.CreateLogger("Production global exception logger");
								logger.LogError(
									eventId: 500,
									exception: exceptionHandlerFeature.Error,
									message: exceptionHandlerFeature.Error.Message);
							}

							context.Response.StatusCode = 500;
							await context.Response.WriteAsync("An error occurred.");
						});
					});
			}

			AutoMapper.Mapper.Initialize(
				config =>
				{
					//config
					//.CreateMap<Company, CompanyDto>()
					//.ForMember(
					//	destination => destination.Age,
					//	option => option.MapFrom(
					//		source => source.StartDate.GetAge()));

					config
					.CreateMap<Company, CompanyDto>()
					.ForMember(
						destination => destination.Age,
						option => option.MapFrom(
							source => source.StartDate.GetAge(source.DateOfDemise)));

					config.CreateMap<CompanyForCreationDto, Company>();

					config.CreateMap<Product, ProductDto>();

					config.CreateMap<ProductForCreationDto, Product>();

					config.CreateMap<ProductForUpdateDto, Product>();

					config.CreateMap<DemisedCompanyForCreationDto, Company>();

					config.CreateMap<CompanyWithLocationForCreationDto, Company>();
				});

			app.UseIpRateLimiting(); // Throttling here so it can protect other services

			app.UseResponseCaching(); // Cache store before header handling so that header handler can generate cache responses

			// NB - When testing this in Postman must disable Settings -> General -> Headers -> Send no-cache header!
			// The cache headers will include an ETag. This allows a request header to be added of If-None-Match with a value of the ETag
			// which gives the client some control over the caching.
			app.UseHttpCacheHeaders(); // Cache headers before MVC because cache is protected MVC.

			app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
