using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace EcobeeFurnaceEta.Blazor.Services;

/// <summary>
/// Handles Ecobee OAuth 2.0 PIN-based authentication.
///
/// Security: This flow NEVER handles the user's Ecobee password.
/// The user authenticates directly with Ecobee's website.
///
/// Flow:
/// 1. User provides their Ecobee API key (from developer.ecobee.com)
/// 2. App requests a PIN from Ecobee
/// 3. User enters PIN at ecobee.com/consumerportal
/// 4. App exchanges authorization for access/refresh tokens
/// </summary>
public class EcobeeAuthService
{
    private readonly HttpClient _httpClient;
    private readonly SecureTokenStorage _tokenStorage;
    private const string EcobeeApiBase = "https://api.ecobee.com";

    public EcobeeAuthService(HttpClient httpClient, SecureTokenStorage tokenStorage)
    {
        _httpClient = httpClient;
        _tokenStorage = tokenStorage;
    }

    /// <summary>
    /// Step 1: Request a PIN for user authorization.
    /// User must enter this PIN at ecobee.com/consumerportal
    /// </summary>
    public async Task<PinRequestResult> RequestPinAsync(string apiKey)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{EcobeeApiBase}/authorize?response_type=ecobeePin&client_id={apiKey}&scope=smartRead");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new PinRequestResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to request PIN: {response.StatusCode}. Check your API key."
                };
            }

            var result = await response.Content.ReadFromJsonAsync<EcobeePinResponse>();
            if (result == null)
            {
                return new PinRequestResult
                {
                    Success = false,
                    ErrorMessage = "Invalid response from Ecobee API"
                };
            }

            // Store API key for later use
            await _tokenStorage.StoreApiKeyAsync(apiKey);

            return new PinRequestResult
            {
                Success = true,
                Pin = result.EcobeePin,
                AuthorizationCode = result.Code,
                ExpiresInMinutes = result.ExpiresIn / 60,
                PollIntervalSeconds = result.Interval
            };
        }
        catch (Exception ex)
        {
            return new PinRequestResult
            {
                Success = false,
                ErrorMessage = $"Network error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Step 2: Exchange authorization code for tokens after user enters PIN.
    /// </summary>
    public async Task<TokenExchangeResult> ExchangeCodeForTokensAsync(string apiKey, string authorizationCode)
    {
        try
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "ecobeePin"),
                new KeyValuePair<string, string>("code", authorizationCode),
                new KeyValuePair<string, string>("client_id", apiKey)
            });

            var response = await _httpClient.PostAsync($"{EcobeeApiBase}/token", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();

                // Check if user hasn't authorized yet
                if (errorContent.Contains("authorization_pending"))
                {
                    return new TokenExchangeResult
                    {
                        Success = false,
                        IsPending = true,
                        ErrorMessage = "Waiting for you to enter the PIN at ecobee.com/consumerportal"
                    };
                }

                if (errorContent.Contains("authorization_expired"))
                {
                    return new TokenExchangeResult
                    {
                        Success = false,
                        IsExpired = true,
                        ErrorMessage = "PIN expired. Please request a new one."
                    };
                }

                return new TokenExchangeResult
                {
                    Success = false,
                    ErrorMessage = $"Token exchange failed: {response.StatusCode}"
                };
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<EcobeeTokenResponse>();
            if (tokenResponse == null)
            {
                return new TokenExchangeResult
                {
                    Success = false,
                    ErrorMessage = "Invalid token response"
                };
            }

            // Store tokens securely
            var tokens = new EcobeeTokens
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                TokenType = tokenResponse.TokenType,
                ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
            };

            await _tokenStorage.StoreTokensAsync(tokens);

            return new TokenExchangeResult
            {
                Success = true,
                Tokens = tokens
            };
        }
        catch (Exception ex)
        {
            return new TokenExchangeResult
            {
                Success = false,
                ErrorMessage = $"Network error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Refresh an expired access token using the refresh token.
    /// </summary>
    public async Task<TokenExchangeResult> RefreshTokensAsync()
    {
        var apiKey = await _tokenStorage.GetApiKeyAsync();
        var currentTokens = await _tokenStorage.GetTokensAsync();

        if (string.IsNullOrEmpty(apiKey) || currentTokens == null)
        {
            return new TokenExchangeResult
            {
                Success = false,
                ErrorMessage = "No stored credentials found"
            };
        }

        try
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", currentTokens.RefreshToken),
                new KeyValuePair<string, string>("client_id", apiKey)
            });

            var response = await _httpClient.PostAsync($"{EcobeeApiBase}/token", content);

            if (!response.IsSuccessStatusCode)
            {
                return new TokenExchangeResult
                {
                    Success = false,
                    ErrorMessage = "Token refresh failed. Please re-authenticate."
                };
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<EcobeeTokenResponse>();
            if (tokenResponse == null)
            {
                return new TokenExchangeResult
                {
                    Success = false,
                    ErrorMessage = "Invalid token response"
                };
            }

            var tokens = new EcobeeTokens
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                TokenType = tokenResponse.TokenType,
                ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
            };

            await _tokenStorage.StoreTokensAsync(tokens);

            return new TokenExchangeResult
            {
                Success = true,
                Tokens = tokens
            };
        }
        catch (Exception ex)
        {
            return new TokenExchangeResult
            {
                Success = false,
                ErrorMessage = $"Network error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Get a valid access token, refreshing if needed.
    /// </summary>
    public async Task<string?> GetValidAccessTokenAsync()
    {
        var tokens = await _tokenStorage.GetTokensAsync();
        if (tokens == null)
            return null;

        if (tokens.NeedsRefresh)
        {
            var refreshResult = await RefreshTokensAsync();
            if (!refreshResult.Success)
                return null;

            tokens = refreshResult.Tokens;
        }

        return tokens?.AccessToken;
    }

    /// <summary>
    /// Logout - clear all stored credentials.
    /// </summary>
    public async Task LogoutAsync()
    {
        await _tokenStorage.ClearAllAsync();
    }

    /// <summary>
    /// Check if user is authenticated.
    /// </summary>
    public async Task<bool> IsAuthenticatedAsync()
    {
        return await _tokenStorage.IsAuthenticatedAsync();
    }
}

#region API Response Models

public class EcobeePinResponse
{
    [JsonPropertyName("ecobeePin")]
    public string EcobeePin { get; set; } = "";

    [JsonPropertyName("code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = "";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("interval")]
    public int Interval { get; set; }
}

public class EcobeeTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = "";

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = "";
}

#endregion

#region Result Models

public class PinRequestResult
{
    public bool Success { get; set; }
    public string? Pin { get; set; }
    public string? AuthorizationCode { get; set; }
    public int ExpiresInMinutes { get; set; }
    public int PollIntervalSeconds { get; set; }
    public string? ErrorMessage { get; set; }
}

public class TokenExchangeResult
{
    public bool Success { get; set; }
    public bool IsPending { get; set; }
    public bool IsExpired { get; set; }
    public EcobeeTokens? Tokens { get; set; }
    public string? ErrorMessage { get; set; }
}

#endregion
