using Microsoft.AspNetCore.Components.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<IISLogViewer.Services.SelectionState>();

var logRoot = builder.Configuration["LogViewer:RootDirectory"];
var timeZoneId = builder.Configuration["LogViewer:TimeZoneId"];
var userCsvPath = builder.Configuration["LogViewer:UserCsvPath"];
if (string.IsNullOrWhiteSpace(logRoot))
{
    logRoot = Path.Combine(Directory.GetCurrentDirectory(), "LogFiles");
}

builder.Services.AddScoped(_ => new IISLogViewer.Services.LogParserService(logRoot, timeZoneId, userCsvPath));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<IISLogViewer.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
