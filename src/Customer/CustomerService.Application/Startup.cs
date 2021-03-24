using CoolStore.AppContracts;
using CoolStore.AppContracts.RestApi;
using CustomerService.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using N8T.Core.Repository;
using N8T.Infrastructure;
using N8T.Infrastructure.Bus;
using N8T.Infrastructure.EfCore;
using N8T.Infrastructure.ServiceInvocation.Dapr;
using N8T.Infrastructure.Swagger;
using N8T.Infrastructure.Tye;

namespace CustomerService.Application
{
    public class Startup
    {
        public Startup(IConfiguration config, IWebHostEnvironment env)
        {
            Config = config;
            Env = env;
        }

        private IConfiguration Config { get; }
        private IWebHostEnvironment Env { get; }
        private bool IsRunOnTye => Config.IsRunOnTye();

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("api", policy =>
                {
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                });
            });

            services.AddCore(new[] {typeof(Startup)})
                .AddPostgresDbContext<MainDbContext, Infrastructure.Anchor>(
                    Config.GetConnectionString("postgres"),
                    svc =>
                    {
                        svc.AddScoped(typeof(IRepository<>), typeof(Repository<>));
                        svc.AddScoped(typeof(IGridRepository<>), typeof(Repository<>));
                    })
                .AddDaprClient()
                .AddRestClient(typeof(ICountryApi), AppConsts.SettingAppName, 5005, IsRunOnTye)
                .AddControllers()
                .AddMessageBroker()
                .AddSwagger<Startup>();
        }

        public void Configure(IApplicationBuilder app, IApiVersionDescriptionProvider provider)
        {
            if (Env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors("api");

            app.UseRouting();
            app.UseCloudEvents();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapSubscribeHandler();
                endpoints.MapDefaultControllerRoute();
            });

            app.UseSwagger(provider);
        }
    }
}
