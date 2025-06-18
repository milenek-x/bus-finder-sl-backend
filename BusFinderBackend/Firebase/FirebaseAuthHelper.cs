using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace BusFinderBackend.Firebase
{
    public class FirebaseAuthResult
    {
        public bool Success { get; set; }
        public string? Uid { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class FirebaseLoginResult
    {
        public bool Success { get; set; }
        public string? IdToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class FirebasePasswordUpdateResult
    {
        public bool Success { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public static class FirebaseAuthHelper
    {
        public static async Task<FirebaseAuthResult> CreateUserAsync(string apiKey, string email, string password)
        {
            using var client = new HttpClient();
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={apiKey}";

            var payload = new
            {
                email,
                password,
                returnSecureToken = true
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);

            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseString);
                if (doc.RootElement.TryGetProperty("localId", out var localId))
                    return new FirebaseAuthResult { Success = true, Uid = localId.GetString() };
                return new FirebaseAuthResult { Success = false, ErrorMessage = "No localId returned." };
            }
            else
            {
                string? errorCode = null;
                string? errorMessage = null;
                try
                {
                    using var doc = JsonDocument.Parse(responseString);
                    if (doc.RootElement.TryGetProperty("error", out var error))
                    {
                        errorCode = error.GetProperty("message").GetString();
                        errorMessage = errorCode;
                    }
                }
                catch { }
                return new FirebaseAuthResult { Success = false, ErrorCode = errorCode, ErrorMessage = errorMessage ?? responseString };
            }
        }

        public static async Task<FirebaseLoginResult> LoginWithEmailPasswordAsync(string apiKey, string email, string password)
        {
            using var client = new HttpClient();
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}";

            var payload = new
            {
                email,
                password,
                returnSecureToken = true
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseString);
                var idToken = doc.RootElement.GetProperty("idToken").GetString();
                var refreshToken = doc.RootElement.GetProperty("refreshToken").GetString();
                return new FirebaseLoginResult
                {
                    Success = true,
                    IdToken = idToken,
                    RefreshToken = refreshToken
                };
            }
            else
            {
                string? errorCode = null;
                string? errorMessage = null;
                try
                {
                    using var doc = JsonDocument.Parse(responseString);
                    if (doc.RootElement.TryGetProperty("error", out var error))
                    {
                        errorCode = error.GetProperty("message").GetString();
                        errorMessage = errorCode;
                    }
                }
                catch { }
                return new FirebaseLoginResult
                {
                    Success = false,
                    ErrorCode = errorCode,
                    ErrorMessage = errorMessage ?? responseString
                };
            }
        }

        public static async Task<FirebasePasswordUpdateResult> UpdatePasswordAsync(string apiKey, string idToken, string newPassword)
        {
            using var client = new HttpClient();
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:update?key={apiKey}";

            var payload = new
            {
                idToken,
                password = newPassword,
                returnSecureToken = true
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return new FirebasePasswordUpdateResult { Success = true };
            }
            else
            {
                string? errorCode = null;
                string? errorMessage = null;
                try
                {
                    using var doc = JsonDocument.Parse(responseString);
                    if (doc.RootElement.TryGetProperty("error", out var error))
                    {
                        errorCode = error.GetProperty("message").GetString();
                        errorMessage = errorCode;
                    }
                }
                catch { }
                return new FirebasePasswordUpdateResult
                {
                    Success = false,
                    ErrorCode = errorCode,
                    ErrorMessage = errorMessage ?? responseString
                };
            }
        }
    }
}
