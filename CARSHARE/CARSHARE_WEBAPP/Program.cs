using CARSHARE_WEBAPP.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication()
    .AddCookie(options =>
    {
        options.LoginPath = "/Korisnik/Login";
        options.LogoutPath = "/Korisnik/Logout";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
    });
builder.Services.AddHttpClient<KorisnikService>();
builder.Services.AddHttpClient<VoziloService>();
builder.Services.AddHttpClient<VoznjaService>();
builder.Services.AddHttpContextAccessor();
var app = builder.Build();

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
