using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CarRental.Application.Contracts.Auth;
using CarRental.Application.Contracts.Cars;
using CarRental.Application.Contracts.Common;
using CarRental.Application.Contracts.Profile;
using CarRental.Application.Contracts.Reservations;

namespace CarRental.IntegrationTests.Api;

public sealed class CarRentalApi(HttpClient http) : IDisposable
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public HttpClient Http { get; } = http;

    public AuthApi Auth { get; } = new(http);

    public CarsApi Cars { get; } = new(http);

    public ReservationsApi Reservations { get; } = new(http);

    public ProfileApi Profile { get; } = new(http);

    public Task<ApiResponse<List<string>>> CountriesAsync() => Http.GetAsAsync<List<string>>(Routes.Countries);

    public Task<ApiResponse<HealthResponse>> HealthAsync() => Http.GetAsAsync<HealthResponse>(Routes.Health);

    public void Authenticate(string accessToken) =>
        Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    public void SignOut() => Http.DefaultRequestHeaders.Authorization = null;

    public void Dispose() => Http.Dispose();
    
    public async Task<ApiResponse> PostRawAsync(string route, string body)
    {
        var response = await Http.PostAsync(route, new StringContent(body, Encoding.UTF8, "application/json"));

        return await response.ReadAsync();
    }
    
    public Task<ApiResponse<T>> PutOffContractAsync<T>(string route, object body) =>
        Http.PutAsAsync<T>(route, body);

    public async Task<ApiResponse> SendAsync(HttpMethod method, string route)
    {
        var response = await Http.SendAsync(new HttpRequestMessage(method, route));

        return await response.ReadAsync();
    }
}

public sealed record HealthResponse(string Status, Dictionary<string, string> Checks);

public sealed class AuthApi(HttpClient http)
{
    public Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request) =>
        http.PostAsAsync<AuthResponse>(Routes.Auth.Register, request);

    public Task<ApiResponse<AuthResponse>> LoginAsync(string email, string password) =>
        http.PostAsAsync<AuthResponse>(Routes.Auth.Login, new LoginRequest(email, password));

    public Task<ApiResponse<AuthResponse>> RefreshAsync() =>
        http.PostAsAsync<AuthResponse>(Routes.Auth.Refresh, body: null);

    public Task<ApiResponse> LogoutAsync() => http.PostAsAsync(Routes.Auth.Logout, content: null);

    public Task<ApiResponse> ConfirmEmailAsync(string email, string token) =>
        http.PostAsAsync(Routes.Auth.ConfirmEmail, new ConfirmEmailRequest(email, token));

    public Task<ApiResponse> ResendConfirmationAsync(string email) =>
        http.PostAsAsync(Routes.Auth.ResendConfirmation, new ResendConfirmationRequest(email));

    public Task<ApiResponse> ForgotPasswordAsync(string email) =>
        http.PostAsAsync(Routes.Auth.ForgotPassword, new ForgotPasswordRequest(email));

    public Task<ApiResponse> ResetPasswordAsync(string email, string token, string password) =>
        http.PostAsAsync(Routes.Auth.ResetPassword, new ResetPasswordRequest(email, token, password, password));

    public Task<ApiResponse> ResetPasswordAsync(ResetPasswordRequest request) =>
        http.PostAsAsync(Routes.Auth.ResetPassword, request);
}

public sealed class CarsApi(HttpClient http)
{
    public Task<ApiResponse<PagedResponse<CarResponse>>> SearchAsync(CarQuery? query = null) =>
        http.GetAsAsync<PagedResponse<CarResponse>>((query ?? new CarQuery()).ToRoute());

    public Task<ApiResponse<PagedResponse<CarResponse>>> QueryAsync(CarQueryRequest? request = null) =>
        http.QueryAsAsync<PagedResponse<CarResponse>>(Routes.Cars.Base, request ?? new CarQueryRequest());

    public Task<ApiResponse<PagedResponse<CarResponse>>> SearchRawAsync(string queryString) =>
        http.GetAsAsync<PagedResponse<CarResponse>>($"{Routes.Cars.Base}?{queryString}");

    public Task<ApiResponse<List<string>>> LocationsAsync() =>
        http.GetAsAsync<List<string>>(Routes.Cars.Locations);

    public Task<ApiResponse<CarResponse>> GetAsync(Guid id) =>
        http.GetAsAsync<CarResponse>(Routes.Cars.ById(id));

    public Task<ApiResponse<CarResponse>> CreateAsync(CreateCarRequest request) =>
        http.PostAsAsync<CarResponse>(Routes.Cars.Base, request);

    public Task<ApiResponse<CarResponse>> UpdateAsync(Guid id, UpdateCarRequest request) =>
        http.PutAsAsync<CarResponse>(Routes.Cars.ById(id), request);

    public Task<ApiResponse> DeleteAsync(Guid id) => http.DeleteAsAsync(Routes.Cars.ById(id));
}

public sealed class ReservationsApi(HttpClient http)
{
    public Task<ApiResponse<ReservationResponse>> CreateAsync(CreateReservationRequest request) =>
        http.PostAsAsync<ReservationResponse>(Routes.Reservations.Base, request);

    public Task<ApiResponse<List<ReservationResponse>>> ListAsync() =>
        http.GetAsAsync<List<ReservationResponse>>(Routes.Reservations.Base);

    public Task<ApiResponse<ReservationResponse>> GetAsync(Guid id) =>
        http.GetAsAsync<ReservationResponse>(Routes.Reservations.ById(id));

    public Task<ApiResponse<ReservationResponse>> UpdateAsync(Guid id, UpdateReservationRequest request) =>
        http.PutAsAsync<ReservationResponse>(Routes.Reservations.ById(id), request);

    public Task<ApiResponse> CancelAsync(Guid id) => http.PostAsAsync(Routes.Reservations.Cancel(id), content: null);
}

public sealed class ProfileApi(HttpClient http)
{
    public Task<ApiResponse<ProfileResponse>> GetAsync() => http.GetAsAsync<ProfileResponse>(Routes.Profile.Base);

    public Task<ApiResponse<ProfileResponse>> UpdateAsync(UpdateProfileRequest request) =>
        http.PutAsAsync<ProfileResponse>(Routes.Profile.Base, request);

    public Task<ApiResponse> ChangePasswordAsync(string current, string next) =>
        http.PutAsAsync(Routes.Profile.Password, new ChangePasswordRequest(current, next, next));

    public Task<ApiResponse> ChangePasswordAsync(ChangePasswordRequest request) =>
        http.PutAsAsync(Routes.Profile.Password, request);
}

internal static class HttpClientExtensions
{
    extension(HttpClient http)
    {
        internal async Task<ApiResponse<T>> GetAsAsync<T>(string route) =>
            await (await http.GetAsync(route)).ReadAsync<T>();

        internal async Task<ApiResponse<T>> QueryAsAsync<T>(string route, object body) =>
            await (await http.SendAsync(new HttpRequestMessage(new HttpMethod("QUERY"), route)
            {
                Content = JsonContent.Create(body, options: CarRentalApi.Json),
            })).ReadAsync<T>();

        internal async Task<ApiResponse<T>> PostAsAsync<T>(string route, object? body) =>
            await (body is null
                ? await http.PostAsync(route, null)
                : await http.PostAsJsonAsync(route, body, CarRentalApi.Json)).ReadAsync<T>();

        internal async Task<ApiResponse> PostAsAsync(string route, object? content) =>
            await (content is null
                ? await http.PostAsync(route, null)
                : await http.PostAsJsonAsync(route, content, CarRentalApi.Json)).ReadAsync();

        internal async Task<ApiResponse<T>> PutAsAsync<T>(string route, object body) =>
            await (await http.PutAsJsonAsync(route, body, CarRentalApi.Json)).ReadAsync<T>();

        internal async Task<ApiResponse> PutAsAsync(string route, object body) =>
            await (await http.PutAsJsonAsync(route, body, CarRentalApi.Json)).ReadAsync();

        internal async Task<ApiResponse> DeleteAsAsync(string route) =>
            await (await http.DeleteAsync(route)).ReadAsync();
    }

    extension(HttpResponseMessage response)
    {
        internal async Task<ApiResponse> ReadAsync()
        {
            var body = await response.Content.ReadAsStringAsync();

            return new ApiResponse(response.StatusCode, ProblemFrom(body, response.StatusCode), body, RefreshCookie(response));
        }

        private async Task<ApiResponse<T>> ReadAsync<T>()
        {
            var body = await response.Content.ReadAsStringAsync();
            var value = default(T);

            if ((int)response.StatusCode is >= 200 and < 300 && body.Length > 0)
            {
                value = JsonSerializer.Deserialize<T>(body, CarRentalApi.Json);
            }

            return new ApiResponse<T>(
                response.StatusCode,
                value,
                ProblemFrom(body, response.StatusCode),
                body,
                response.Headers.Location,
                RefreshCookie(response));
        }
    }

    private static string? RefreshCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return null;
        }

        const string prefix = "cr_refresh=";

        return cookies
            .Where(cookie => cookie.StartsWith(prefix, StringComparison.Ordinal))
            .Select(cookie => cookie[prefix.Length..].Split(';')[0])
            .LastOrDefault(value => !string.IsNullOrEmpty(value));
    }

    private static Problem? ProblemFrom(string body, HttpStatusCode statusCode)
    {
        if ((int)statusCode < 400 || body.Length == 0)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Problem>(body, CarRentalApi.Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
