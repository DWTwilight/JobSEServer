using JobSEServer.DatabaseContext;
using JobSEServer.Models;
using JobSEServer.Services;
using JobSEServer.Utilities;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobSEServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //Logging
            var listener = new JobSETraceListener() { LogFileName = "JobSEServer.log" };
            listener.WriteLine(string.Format("\n------------ [{0}] Server Starting ------------", DateTime.Now.ToString("s")));
            services.AddLogging(builder => builder.AddTraceSource(new SourceSwitch("default", "All"), listener));

            //options
            services.Configure<ElasticOptions>(Configuration.GetSection("ElasticConfig"));
            services.Configure<JWTAuthOption>(Configuration.GetSection("JWTAuthOption"));
            services.AddSingleton(new MysqlOption() { ConnectionString = Configuration.GetConnectionString("JobSEDb") });

            services.AddDbContext<JobSEDbContext>(options => options.UseMySQL(Configuration.GetConnectionString("JobSEDb")));

            services.AddSingleton<ESClientManagerService>();

            services.AddScoped<ElasticService>();
            services.AddScoped<PositionService>();
            services.AddScoped<CompanyService>();
            services.AddScoped<TagService>();

            services.AddHostedService<DataUploadService>();

            services.AddCors(options => options.AddPolicy("AllowCors", builder => builder.WithOrigins("*").AllowAnyMethod().AllowAnyHeader()));

            var jwtOptions = Configuration.GetSection("JWTAuthOption").Get<JWTAuthOption>();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = false,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.PrivateKey))
                };
            });

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "JobSEServer", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "JobSEServer v1"));
            }

            app.UseRouting();

            app.UseCors("AllowCors");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
