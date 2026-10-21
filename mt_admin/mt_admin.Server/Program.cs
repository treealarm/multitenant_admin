using DbAdmin;
using KeycloakAdmin;
using KuberAdmin;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using mt_admin;
using Swashbuckle.AspNetCore.Filters;
using System.Net;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// MT_ADMIN_REALM set: one realm's user list inside a standalone TreeAlarm VMS (see StandaloneMode).
var standalone = StandaloneMode.FromEnvironment();

builder.Services.AddControllers().ConfigureApplicationPartManager(parts =>
{
  foreach (var provider in parts.FeatureProviders.OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerFeatureProvider>().ToList())
    parts.FeatureProviders.Remove(provider);
  parts.FeatureProviders.Add(new ModeControllers(standalone is not null));
});

// The multi-tenant defaults suit the dev setup they came from. A standalone installation names
// everything itself: a Keycloak that is not there should fail the start, not every login.
var kcConfig = standalone is null
  ? new KeycloakConfig
  {
    Url = Environment.GetEnvironmentVariable("KEYCLOAK_URL") ?? "http://localhost:8080",
    AdminUser = Environment.GetEnvironmentVariable("KEYCLOAK_ADMIN_USER") ?? "admin",
    AdminPassword = Environment.GetEnvironmentVariable("KEYCLOAK_ADMIN_PASSWORD") ?? "admin",
    ClientId = Environment.GetEnvironmentVariable("KEYCLOAK_CLIENT_ID") ?? "admin-cli",
  }
  : new KeycloakConfig
  {
    Url = StandaloneMode.Require("KEYCLOAK_URL"),
    AdminUser = StandaloneMode.Require("KEYCLOAK_ADMIN_USER"),
    AdminPassword = StandaloneMode.Require("KEYCLOAK_ADMIN_PASSWORD"),
    ClientId = standalone.ClientId,
  };

builder.Services.AddSingleton(kcConfig);
builder.Services.AddHttpClient();

if (standalone is null)
{
  // Seeds the customers realm, a tenant realm and its database: multi-tenant only.
  builder.Services.AddHostedService<InitHostedService>();
}
else
{
  builder.Services.AddSingleton(standalone);
}

builder.Services.AddSingleton<IKeycloakAdminClient>(sp =>
    new KeycloakAdminClient(
        keycloakUrl: kcConfig.Url!,
        adminUser: kcConfig.AdminUser!,
        adminPassword: kcConfig.AdminPassword!
    )
);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
  options.SwaggerDoc("v1", new() { Title = "MT Admin API", Version = "v1" });
  options.ExampleFilters();
  // Добавляем схему безопасности для Bearer токена
  options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
  {
    Name = "Authorization",
    Type = Microsoft.OpenApi.SecuritySchemeType.Http,
    Scheme = "bearer",
    BearerFormat = "JWT",
    In = Microsoft.OpenApi.ParameterLocation.Header,
    Description = "Input: <Bearer> {token}"
  });

  // Добавляем требование безопасности глобально.
  // Схема адресуется ссылкой на документ, который её содержит.
  options.AddSecurityRequirement(document => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.OpenApiSecuritySchemeReference(
                JwtBearerDefaults.AuthenticationScheme, document),
            new List<string>()
        }
    });
});

builder.Services.AddSwaggerExamplesFromAssemblies(Assembly.GetEntryAssembly());

///Auth
builder.Services.AddMemoryCache();

builder.Services.AddScoped<IKeycloakTokenValidator, KeycloakTokenValidatorService>();
builder.Services.AddTransient<IClaimsTransformation, KeycloakClaimsTransformer>();
builder.Services.AddTransient<IDBProvisioningService, DBProvisioningService>();
builder.Services.AddSingleton<IK8sService, K8sService>();


builder.Services.AddTransient<IConfigureOptions<JwtBearerOptions>, DynamicJwtBearerOptions>();

builder.Services
  .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(); // без параметров — всё задаётся через DynamicJwtBearerOptions

builder.Services.AddAuthorization(options =>
{
  if (standalone is not null)
    options.AddPolicy(StandaloneMode.AdminPolicy, policy =>
      policy.RequireAuthenticatedUser().RequireAssertion(context => standalone.IsAdmin(context.User)));
});

var http_port = 8013;

if (standalone is not null)
{
  http_port = int.TryParse(StandaloneMode.Require("HTTP_ADMIN_PORT"), out var port)
    ? port
    : throw new InvalidOperationException("HTTP_ADMIN_PORT must be a port number");
}
// Parsed into its own variable: TryParse zeroes its out argument on failure, which used to leave
// Kestrel on a random port instead of the default.
else if (int.TryParse(Environment.GetEnvironmentVariable("HTTP_ADMIN_PORT"), out var configuredPort))
{
  http_port = configuredPort;
  Console.WriteLine($"HTTP_ADMIN_PORT port: {http_port}");
}

builder.WebHost.ConfigureKestrel(options =>
{
  // this configuration is the same as:
  //- ASPNETCORE_URLS=http://+:8000;http://+:${GRPC_MAIN_PORT}
  //- Kestrel__Endpoints__gRPC__Url=http://*:${GRPC_MAIN_PORT}
  //- Kestrel__Endpoints__gRPC__Protocols=Http2
  //- Kestrel__Endpoints__Http__Url=http://*:8000
  options.Listen(IPAddress.Any, http_port);
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
//End Auth

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI(c =>
  {
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MT Admin API v1");
  });
}

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
