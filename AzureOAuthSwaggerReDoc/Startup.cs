using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace AzureOAuthSwaggerReDoc
{
    public class Startup
    {
        private string AzureTenant;
        private string oAuthAudience;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            // Get these from the appsettings.json file
            this.AzureTenant = Configuration.GetValue<string>("AzureTenant");
            this.oAuthAudience = Configuration.GetValue<string>("Audience");
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddCors(options =>
            {
                options.AddPolicy("MySpecificOrigins",
                builder =>
                {
                    builder.AllowAnyHeader()
                        .AllowAnyOrigin()
                        .AllowAnyMethod();
                });
            });

            services.AddMvc()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.WriteIndented = true;
                });

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            });

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Audience = oAuthAudience;
                    options.Authority = $"https://sts.windows.net/{AzureTenant}/";
                    //options.RequireHttpsMetadata = true;
                })
                .AddJwtBearer("oauth2", options =>
                {
                    options.Audience = oAuthAudience;
                    options.Authority = $"https://sts.windows.net/{AzureTenant}/";
                    //options.RequireHttpsMetadata = true;
                });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Azure/oAuth/Swagger/ReDoc/.NET Core 3.1 Api v1",
                    Version = "v1",

                    // This adds the logo for ReDoc
                    Extensions = new Dictionary<string, IOpenApiExtension>
                    {
                        {
                            "x-logo", new OpenApiObject
                            {
                                {"url", new OpenApiString("/documentation/saltycode.png")},
                                {"altText", new OpenApiString("The Logo")}
                            }
                        }
                    }
                });

                c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    In = ParameterLocation.Header,
                    Scheme = "bearer",
                    Flows = new OpenApiOAuthFlows
                    {
                        Implicit = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri($"https://login.microsoftonline.com/{AzureTenant}/oauth2/authorize"),
                            TokenUrl = new Uri($"https://login.microsoftonline.com/{AzureTenant}/oauth2/token"),
                            Scopes = new Dictionary<string, string>
                            {
                                { "readAccess", "Access read operations" },
                                { "writeAccess", "Access write operations" }
                            }
                        }
                    }
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement{
                    {
                        new OpenApiSecurityScheme{
                            Reference = new OpenApiReference{
                                Id = "oauth2", //The name of the previously defined security scheme.
                                Type = ReferenceType.SecurityScheme
                            }
                        },new List<string>()
                    }
                });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //------------------------------------------------------
            // BEGIN : Serve up Files for ReDoc
            //------------------------------------------------------
            PhysicalFileProvider fileprovider = new PhysicalFileProvider(Path.Combine(env.ContentRootPath, "Documentation"));
            app.UseDefaultFiles(new DefaultFilesOptions
            {
                FileProvider = fileprovider,
                RequestPath = new PathString("/Documentation"),
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = fileprovider,
                RequestPath = new PathString("/Documentation"),
            });

            app.UseFileServer(new FileServerOptions()
            {
                FileProvider = fileprovider,
                RequestPath = new PathString("/Documentation"),
                EnableDirectoryBrowsing = false
            });
            //------------------------------------------------------
            // END : Serve up Files for ReDoc
            //------------------------------------------------------

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors("MySpecificOrigins");

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Azure/oAuth/Swagger/ReDoc/.NET Core 3.1 Api v1");
                c.OAuthClientId(oAuthAudience);
                c.OAuthAdditionalQueryStringParams(new Dictionary<string, string>() { { "resource", oAuthAudience } });
            });

            app.UseHttpsRedirection();
            app.UseAuthentication();

            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
