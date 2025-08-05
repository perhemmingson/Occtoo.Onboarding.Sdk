using NSubstitute;
using Occtoo.Onboarding.Sdk.AuthenticationHandler;
using Occtoo.Onboarding.Sdk.Configuration;
using System;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Occtoo.Onboarding.Sdk.Tests.AuthenticationHandler
{
	public class OcctooAuthenticationDelegatingHandlerTests
	{
		private AccessTokensCacheManager subAccessTokensCacheManager;
		private OnboardingClientSettings subOnboardingClientSettings;

		public OcctooAuthenticationDelegatingHandlerTests()
		{
			subAccessTokensCacheManager = Substitute.For<AccessTokensCacheManager>();
			subOnboardingClientSettings = Substitute.For<OnboardingClientSettings>();
		}

		private OcctooAuthenticationDelegatingHandler CreateOcctooAuthenticationDelegatingHandler()
		{
			return new OcctooAuthenticationDelegatingHandler(
				subAccessTokensCacheManager,
				subOnboardingClientSettings);
		}

		[Fact]
		public async Task GetOcctooTokenAsync_StateUnderTest_ExpectedBehavior()
		{
			// Arrange
			var occtooAuthenticationDelegatingHandler = CreateOcctooAuthenticationDelegatingHandler();
			CancellationToken cancellationToken = default(CancellationToken);

			// Act
			var result = await occtooAuthenticationDelegatingHandler.GetOcctooTokenAsync(cancellationToken);

			// Assert
			result.ShouldNotBeNull();
		}
	}
}
