# OnboardingServiceClient 
Wrapped [HttpClient](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient?view=net-8.0) used to call [onboard data to Occtoo](https://docs.occtoo.com/docs/get-started/provide-data).



# Gettings started with Onboarding Service Client
* Open your solution and add the package through nuget.
* Create a instance of the OnboardingServiceClient and provide your Provider-Id and -Secret.

    (Follow [these steps](https://docs.occtoo.com/docs/get-started/provide-data#12-create-data-provider) to setup your Provider, if you do not have one.)
* Call one of the four overloads of StartEntityImportAsync

## Quick Start Example
```cs
// In appSettings or similar add your Provider ID and Secret
{
    "OnboardingClientSettings:DataProviderId": "",
    "OnboardingClientSettings:DataProviderSecret": ""
}

// add to Program.cs
services.AddOptions<OnboardingClientSettings>().Configure<IConfiguration>((settings, configuration) =>
    {
        configuration.GetSection(nameof(OnboardingClientSettings)).Bind(settings);
    })
    .ValidateDataAnnotations()
    .ValidateOnStart();

services.AddSingleton(sp => sp.GetRequiredService<IOptions<OnboardingClientSettings>>().Value);

services.AddHttpClient<IOnboardingServiceClient, OnboardingServiceClient>((httpClient, sp) =>
    {
        httpClient.BaseAddress = new Uri("https://ingest.occtoo.com");
        return new OnboardingServiceClient(httpClient);
    }).AddOcctooClientAuthentication();


private readonly string dataSource = "MyFirstOcctoDataSource";

static async Task Main(string[] args)
{
    var enties = new List<DynamicEntity>
    {
        new DynamicEntity
        {
            Key = "1",
            Properties= {new DynamicProperty { Id= "name", Value = "number one" }}
        },
        new DynamicEntity
        {
            Key = "2",
            Properties= {new DynamicProperty { Id= "name", Value = "number two" }}
        }
    };

    // DI IOnboardingServiceClient
    var response = await onboardingServliceClient.StartEntityImportAsync(dataSource, enties);
    if(response.StatusCode == 202)
    {
        // Data was onboarded!
    }
}
```

[Code repository on github](https://github.com/Occtoo/Occtoo.Onboarding.Sdk)

## Release Notes 4.00
* Support for .net 9 
* Breaking changes: Dropped syncronous api calls
