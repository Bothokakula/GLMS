// Program.cs
// Entry point for the GLMS Web MVC application.
// Configures services (MVC, session, HttpClient) and middleware pipeline.
// ASP.NET Core MVC pattern (Microsoft, 2024, ASP.NET Core MVC documentation)

using GLMS.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ASP.NET Core MVC pattern (Microsoft, 2024, ASP.NET Core MVC documentation)
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// Session-based token storage for stateless API authentication
// In-memory distributed cache backs session state; sufficient for single-server deployments.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// HttpClient pattern for service communication (Microsoft, 2024, Make HTTP requests using IHttpClientFactory)
// Typed client registration ensures each GlmsApiClient gets its own HttpClient with the correct base address.
builder.Services.AddHttpClient<GlmsApiClient>(client =>
{
    var baseUrl = builder.Configuration["ApiSettings:BaseUrl"]
                  ?? "http://localhost:5001";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
// Session middleware must appear before authorization so controllers can access session-stored tokens.
app.UseSession();
app.UseAuthorization();

// Convention-based routing: defaults to HomeController.Index when no route segments are provided.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
