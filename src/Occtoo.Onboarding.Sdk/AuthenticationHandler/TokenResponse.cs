using System.Collections.Generic;

namespace Occtoo.Onboarding.Sdk.AuthenticationHandler;

public class TokenResponse
{
	public TokenInfo Result { get; set; }
	public List<object> Errors { get; set; }
	public string RequestId { get; set; }
}

public class TokenInfo
{
	public string AccessToken { get; set; }
	public int ExpiresIn { get; set; }
	public string TokenType { get; set; }
	public object RefreshToken { get; set; }
	public string Scope { get; set; }
}

