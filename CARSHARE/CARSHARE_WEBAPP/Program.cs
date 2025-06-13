using CARSHARE_WEBAPP.Services;
using CARSHARE_WEBAPP.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using CARSHARE_WEBAPP.Models;      
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<CarshareContext>(opts =>
    opts.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

builder.Services
    .AddHttpClient<IKorisnikService, KorisnikService>(client =>
    {
        client.BaseAddress = new Uri("http://localhost:5194/api/Korisnik/");
    }); builder.Services.AddAuthorization();

builder.Services.AddHttpClient<KorisnikService>();
builder.Services.AddHttpClient<ImageService>();    

builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();
builder.Services
       .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
       .AddCookie(options =>
       {
           options.LoginPath = "/Korisnik/Login";
       });
var app = builder.Build();

app.UseSession();

app.Use(async (context, next) =>
{
    var token = context.Request.Cookies["JWToken"];
    if (!string.IsNullOrEmpty(token))
    {
        context.Session.SetString("JWToken", token);
        context.Session.SetString("Username", context.Request.Cookies["Username"]);
        context.Session.SetInt32("UserId", int.Parse(context.Request.Cookies["UserId"]));
        context.Session.SetString("Role", context.Request.Cookies["Role"]);
    }
    await next.Invoke();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
public partial class Program { }
