using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Common.Http;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

// using Sample.Rido;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddTokenAcquisition();
builder.Services.AddInMemoryTokenCaches();


//builder.Services.AddScoped<MyCredentials>();
builder.Services.AddScoped<IHttpCredentials, ClientCredentials>();
builder.Services.Configure<MicrosoftIdentityApplicationOptions>("AzureAd", builder.Configuration.GetSection("AzureAd"));
AppBuilder appBuilder = new AppBuilder(builder.Services.BuildServiceProvider());
//appBuilder.AddCredentials<MyCredentials>();
builder.AddTeams(appBuilder);


WebApplication app = builder.Build();
App teams = app.UseTeams();

teams.OnMessage("hi", async context =>
{
    await context.Send("Message from Rido 1!");
});

teams.OnMessage("hi", async context =>
{
    await context.Send("Message from Rido 2!"); 
});

teams.OnActivity(async context =>
{
    await context.Send("Activity from Rido! " + context.Activity.Type);
});



app.Run();