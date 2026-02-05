using System.Reflection;
using VoicePoC.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

try
{
    var entryAssembly = Assembly.GetEntryAssembly();
    if (entryAssembly is not null)
    {
        builder.Configuration.AddUserSecrets(entryAssembly, optional: true);
    }
}
catch
{
    // If user secrets couldn't be added, continue without failing.
}

builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// Dependency Injection
builder.Services.AddSingleton<ICallStorageService, CallStorageService>();
builder.Services.AddScoped<ITwilioService, TwilioService>();
builder.Services.AddScoped<IAIService, GeminiService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();
