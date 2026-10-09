using BikeFit.Web.Components;
using BikeFit.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// the catalogue is shared and read-only; everything a visitor owns is scoped to their session
builder.Services.AddSingleton<Catalogue>();
builder.Services.AddScoped<BikeLibrary>();
builder.Services.AddScoped<FitLibrary>();
builder.Services.AddScoped<UserDataExchange>();
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
// in a container TLS is terminated by the platform in front of it
if (!app.Configuration.GetValue<bool>("DOTNET_RUNNING_IN_CONTAINER"))
    app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
