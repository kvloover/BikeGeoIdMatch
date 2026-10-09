using BikeFit.Web.Components;
using BikeFit.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<UserDataFolder>();
builder.Services.AddSingleton<BikeLibrary>();
builder.Services.AddSingleton<FitLibrary>();
builder.Services.AddSingleton<UserSettingsStore>();
builder.Services.AddScoped<FitSolver>();
builder.Services.AddScoped<GeometryImporter>();
builder.Services.AddHttpClient("geo", client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
    // Some geometry sites reject requests without a normal browser user agent.
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; BikeFitCalculator/1.0)");
    client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

// the user's own data, as files they can keep or carry to another machine
app.MapGet("/export/bikes", (BikeLibrary bikes) => Results.Text(bikes.ExportUserBikes(), "application/json"));
app.MapGet("/export/fits", (FitLibrary fits) => Results.Text(fits.Export(), "application/json"));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
