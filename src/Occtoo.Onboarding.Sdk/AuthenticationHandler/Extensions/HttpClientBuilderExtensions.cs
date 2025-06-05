using Microsoft.Extensions.DependencyInjection.Extensions;
using Occtoo.Onboarding.Sdk.AuthenticationHandler;
using Occtoo.Onboarding.Sdk.Configuration;
using System;

// ***************************************************************************************
// DO NOT CHANGE THE NAMESPACE! It is .Net Core convention for configuration extensions. *
// ***************************************************************************************
// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class HttpClientBuilderExtensions
{
    public static IHttpClientBuilder AddOcctooClientAuthentication(this IHttpClientBuilder builder)
    {
        builder.Services.TryAddSingleton<AccessTokensCacheManager>();
        builder.AddHttpMessageHandler(provider => CreateDelegatingHandler(provider));
        return builder;
    }

    private static OcctooAuthenticationDelegatingHandler CreateDelegatingHandler(IServiceProvider provider)
    {
        var accessTokensCacheManager = provider.GetRequiredService<AccessTokensCacheManager>();
        var apiConfig = provider.GetRequiredService<OnboardingClientSettings>();
        return new OcctooAuthenticationDelegatingHandler(accessTokensCacheManager, apiConfig);
    }
}
