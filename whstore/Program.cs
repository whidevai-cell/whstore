using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var env = builder.Environment;

// ১) Connection string: appsettings.json বা Environment Variable থেকে
var connectionString = configuration.GetConnectionString("DefaultConnection")
                        ?? Environment.GetEnvironmentVariable("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("⚠️ WARNING: DefaultConnection is missing! DB features will be disabled.");
}
else
{
    Console.WriteLine("✅ Database connection found.");
}

// ২) FormLimits — বড় ফাইল আপলোডের জন্য
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = 10 * 1024 * 1024;          // 10 MB per form value
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024;  // 100 MB upload limit
    options.MemoryBufferThreshold = 1 * 1024 * 1024;       // 1 MB buffer threshold
});

// ৩) MVC Controllers + Views
builder.Services.AddControllersWithViews();

// ৪) CORS — Development এ AllowAll, Production এ নির্দিষ্ট origin
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        if (env.IsDevelopment())
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
        else
        {
            var allowed = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                          ?? new[] { "*" };
            policy.WithOrigins(allowed).AllowAnyMethod().AllowAnyHeader();
        }
    });
});

var app = builder.Build();

// ৫) Error Handling & Security
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors("DefaultCors");
app.UseRouting();

app.UseAuthorization();

// ৬) Default Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

Console.WriteLine("🚀 WH A STORE is running...");
Console.WriteLine($"🌍 Environment: {env.EnvironmentName}");

app.Run();
