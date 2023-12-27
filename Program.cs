var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Enables Application Insights telemetry collection
builder.Services.AddApplicationInsightsTelemetry();

// Add services to the container.
builder.Services.AddRazorPages();

// Addd IHttpClientFactory https://learn.microsoft.com/aspnet/core/fundamentals/http-requests
builder.Services.AddHttpClient();

// Add session service https://learn.microsoft.com/aspnet/core/fundamentals/app-state
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSession();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.Run();


