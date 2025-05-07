using CARSHARE_WEBAPP.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();



builder.Services.AddAuthorization();
builder.Services.AddHttpClient<KorisnikService>();
builder.Services.AddHttpClient<VoznjaService>();
builder.Services.AddHttpClient<ImageService>();
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();
app.UseSession();
// Configure the HTTP request pipeline.
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
