using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Gambling5.Web;
using Gambling5.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<GigService>();

// Add Microsoft Authentication
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    options.ProviderOptions.DefaultAccessTokenScopes.Add("openid");
    options.ProviderOptions.DefaultAccessTokenScopes.Add("profile");

    var apiScope = builder.Configuration["AzureAd:ApiScope"];
    if (string.IsNullOrWhiteSpace(apiScope))
    {
        var clientId = builder.Configuration["AzureAd:ClientId"];
        if (!string.IsNullOrWhiteSpace(clientId))
        {
            apiScope = $"api://{clientId}/access_as_user";
        }
    }

    if (!string.IsNullOrWhiteSpace(apiScope))
    {
        options.ProviderOptions.DefaultAccessTokenScopes.Add(apiScope);
    }

    options.ProviderOptions.LoginMode = "redirect";
});

await builder.Build().RunAsync();
