using Newtonsoft.Json;
using Occtoo.Onboarding.Sdk.Configuration;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Occtoo.Onboarding.Sdk.AuthenticationHandler;

public sealed class AuthenticationDelegatingHandler(AccessTokensCacheManager accessTokensCacheManager, OnboardingClientSettings applicationSettings) : DelegatingHandler
{
	private const string AuthorizationHeader = "Authorization";
	private const string Bearer = "Bearer";

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var token = await GetToken(cancellationToken);

		request.Headers.Authorization = new AuthenticationHeaderValue(Bearer, token.AccessToken);

		var response = await base.SendAsync(request, cancellationToken);

		if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
		{
			token = await RefreshToken(cancellationToken);
			request.Headers.Remove(AuthorizationHeader);
			request.Headers.Authorization = new AuthenticationHeaderValue(Bearer, token.AccessToken);
			response = await base.SendAsync(request, cancellationToken);
		}
		return response;
	}

	private async Task<TokenInfo> RefreshToken(CancellationToken cancellationToken)
	{
		var token = await GetOcctooTokenAsync(cancellationToken);
		accessTokensCacheManager.AddOrUpdateToken(applicationSettings.ClientId, token);
		return token;
	}

	private async Task<TokenInfo> GetToken(CancellationToken cancellationToken)
	{
		var token = accessTokensCacheManager.GetToken(applicationSettings.ClientId);
		if (token is null)
		{
			token = await GetOcctooTokenAsync(cancellationToken);
			accessTokensCacheManager.AddOrUpdateToken(applicationSettings.ClientId, token);
		}
		return token;
	}

	public async Task<TokenInfo> GetOcctooTokenAsync(CancellationToken cancellationToken = default)
	{
		var url = new Uri("https://ingest.occtoo.com/dataProviders/tokens");
		var tokenRequest = new HttpRequestMessage(HttpMethod.Post, url)
		{
			Content = new StringContent(JsonConvert.SerializeObject(new
			{
				id = applicationSettings.DataProviderId,
				secret = applicationSettings.DataProviderSecret
			}), Encoding.UTF8, MediaTypeNames.Application.Json)
		};

		var response = await base.SendAsync(tokenRequest, cancellationToken);

		response.EnsureSuccessStatusCode();
		var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken) ?? throw new ApplicationException();
		return tokenResponse.Result;
	}
}
