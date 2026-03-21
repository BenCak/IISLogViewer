using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddScoped<IISLogViewer.Services.SelectionState>();

var logRoot = builder.Configuration["LogViewer:RootDirectory"];
if (string.IsNullOrWhiteSpace(logRoot))
{
    logRoot = Path.Combine(Directory.GetCurrentDirectory(), "LogFiles");
}

builder.Services.AddSingleton(new IISLogViewer.Services.LogParserService(logRoot));

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