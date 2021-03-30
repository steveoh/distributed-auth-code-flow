using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

namespace auth_tickets
{
  public class Startup
  {
    private const string authority = "https://login.dts.utah.gov:443/sso/oauth2";

    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddHttpContextAccessor();
      services.AddSingleton<ITicketStore, RedisTicketStore>();
      services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
        .Configure<ITicketStore>((options, store) =>
        {
          options.Cookie.Name = ".auth-ticket.auth";
          options.SessionStore = store;
          options.ExpireTimeSpan = TimeSpan.FromHours(1);

          options.ForwardChallenge = OpenIdConnectDefaults.AuthenticationScheme;
          options.LoginPath = "/authentication/login";
          options.AccessDeniedPath = "/authentication/access-denied";
          options.LogoutPath = "/";
        });

      var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
      var redisConfig = Environment.GetEnvironmentVariable("REDIS_IP") ?? "localhost:6379";

      if (environment == "docker") {
        redisConfig = "redis";
      }

      var redis = ConnectionMultiplexer.Connect(redisConfig);

      services.AddDataProtection().PersistKeysToStackExchangeRedis(redis, "data-protection-keys");

      services.AddStackExchangeRedisCache(options => options.Configuration = redisConfig);

      services.AddAuthentication(options =>
      {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
      })
        .AddCookie()
        .AddOpenIdConnect(options =>
        {
          var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
          options.ClientId = Environment.GetEnvironmentVariable("PLSS_CLIENT_ID_STAGING");
          options.ClientSecret = Environment.GetEnvironmentVariable("PLSS_CLIENT_SECRET_STAGING");

          options.Authority = authority;
          options.GetClaimsFromUserInfoEndpoint = true;
          options.RequireHttpsMetadata = true;

          if (environment == Environments.Development)
          {
            IConfigurationSection oidc = Configuration.GetSection("Authentication:UtahId");
            options.ClientId = oidc["ClientId"];
            options.ClientSecret = oidc["ClientSecret"];
          }

          options.ResponseType = "code";
          options.UsePkce = true;

          options.Scope.Clear();
          options.Scope.Add("openid app:agrcPLSS");
        });

      services.AddAuthorization(options =>
        options.AddPolicy(CookieAuthenticationDefaults.AuthenticationScheme,
          policy => policy.RequireAuthenticatedUser()));

      services.AddControllers();
      services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "auth_tickets", Version = "v1" }));

      services.Configure<ForwardedHeadersOptions>(options =>
      {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
      });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "auth_tickets v1"));
      }

      app.UseForwardedHeaders();
      app.UseRouting();

      app.UseAuthentication();
      app.UseAuthorization();

      app.UseEndpoints(endpoints => endpoints.MapControllers());
    }
  }
}
