using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FinalProject.Application.Dto.Member;
using Newtonsoft.Json;

namespace FinalProject.Tests.Helpers
{
    public static class AuthTestHelper
    {
        /// <summary>
        /// Gets an authentication token for testing protected endpoints
        /// </summary>
        /// <param name="client">The HTTP client instance</param>
        /// <param name="email">User email</param>
        /// <param name="password">User password</param>
        /// <returns>Authentication token</returns>
        public static async Task<string> GetAuthTokenAsync(HttpClient client, string email = "testuser@library.com", string password = "Password123!")
        {
            var loginDto = new LoginMemberDto
            {
                Email = email,
                Password = password
            };

            var response = await client.PostAsJsonAsync("/api/auth/login", loginDto);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<TokenResponseDto>(content);

            return result?.Token;
        }

        /// <summary>
        /// Sets the authentication token for the provided HTTP client
        /// </summary>
        /// <param name="client">The HTTP client instance</param>
        /// <param name="token">Authentication token</param>
        public static void SetAuthToken(HttpClient client, string token)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        /// <summary>
        /// Creates an authenticated HTTP client for testing protected endpoints
        /// </summary>
        /// <param name="factory">The CustomWebApplicationFactory instance</param>
        /// <param name="email">User email</param>
        /// <param name="password">User password</param>
        /// <returns>Authenticated HTTP client</returns>
        public static async Task<HttpClient> CreateAuthenticatedClientAsync<TStartup>(
            CustomWebApplicationFactory<TStartup> factory, 
            string email = "testuser@library.com", 
            string password = "Password123!") where TStartup : class
        {
            var client = factory.CreateClient();
            var token = await GetAuthTokenAsync(client, email, password);
            SetAuthToken(client, token);
            return client;
        }

        /// <summary>
        /// Response class for token authentication
        /// </summary>
        public class TokenResponseDto
        {
            public string Email { get; set; }
            public string Token { get; set; }
            public string[] Roles { get; set; }
        }
    }
}
