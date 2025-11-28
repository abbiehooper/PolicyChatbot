using PolicyChatbot.Server.Middleware;
using PolicyChatbot.Server.Startup;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var services = builder.Services;

services.AddControllers();
services.AddDependencyInjection(configuration)
        .AddAnthropicApiClient(configuration)
        .AddHttpClient()
        .AddRazorPages();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiting();
app.MapRazorPages();
app.MapControllers();
app.MapStaticAssets();
app.MapFallbackToFile("index.html");

app.Run();
