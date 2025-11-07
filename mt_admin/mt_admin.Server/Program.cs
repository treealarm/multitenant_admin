using KeycloackAdmin;
using Microsoft.Extensions.DependencyInjection;
using mt_admin.Server;

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

builder.Services.AddScoped<IKeycloakAdminClient>(sp =>
    new KeycloakAdminClient(
        keycloakUrl: kcConfig.Url!,
        adminUser: kcConfig.AdminUser!,
        adminPassword: kcConfig.AdminPassword!
    )
);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
