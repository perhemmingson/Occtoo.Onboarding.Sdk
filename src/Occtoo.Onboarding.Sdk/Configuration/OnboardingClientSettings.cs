using System.ComponentModel.DataAnnotations;

namespace Occtoo.Onboarding.Sdk.Configuration
{
	public class OnboardingClientSettings
	{
		[Required]
		public required string DataProviderId { get; set; }
		[Required]
		public required string DataProviderSecret { get; set; }

		public required string ClientId { get; set; } = "OcctooClient";
	}
}
