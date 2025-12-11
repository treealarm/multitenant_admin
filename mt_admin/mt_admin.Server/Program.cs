using DbAdmin;
using KeycloakAdmin;
using KuberAdmin;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using mt_admin;
using Swashbuckle.AspNetCore.Filters;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

var kcConfig = new KeycloakConfig
{
  Url = Environment.GetEnvironmentVariable("KEYCLOAK_URL") ?? "http://localhost:8080",
  AdminUser = Environment.GetEnvironmentVariable("KEYCLOAK_ADMIN_USER") ?? "admin",
  AdminPassword = Environment.GetEnvironmentVariable("KEYCLOAK_ADMIN_PASSWORD") ?? "admin",
  ClientId = Environment.GetEnvironmentVariable("KEYCLOAK_CLIENT_ID") ?? "admin-cli",
};

builder.Services.AddSingleton(kcConfig);
builder.Services.AddHttpClient();

builder.Services.AddHostedService<InitHostedService>();

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
  options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
  {
    Name = "Authorization",
    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
    Scheme = "bearer",
    BearerFormat = "JWT",
    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
    Description = "Input: <Bearer> {token}"
  });

  // Добавляем требование безопасности глобально
  options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            },
            Array.Empty<string>()
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

builder.Services.AddAuthorization();

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
