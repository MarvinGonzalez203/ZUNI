using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Zuni.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(
        new DirectoryInfo(
            Path.Combine(
                builder.Environment.ContentRootPath,
                "App_Data",
                "keys")))
    .SetApplicationName("Zuni");

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Cuenta/IniciarSesion";
        options.AccessDeniedPath = "/Cuenta/AccesoDenegado";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization();

// Sistema JSON actual.
// Todavía NO lo eliminamos.
builder.Services.AddSingleton<IUserStore, JsonUserStore>();

var app = builder.Build();

// Inicializar datos de PostgreSQL.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    // Crear los roles si todavía no existen.
    await DbInitializer.SeedRolesAsync(dbContext);

    // Migrar los usuarios de users.json hacia PostgreSQL.
    var migratedUsers = await JsonUserMigrator.MigrateAsync(
        dbContext,
        app.Environment);

    System.Diagnostics.Debug.WriteLine(
        $"Usuarios migrados desde JSON: {migratedUsers}");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    // The default HSTS value is 30 days.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();