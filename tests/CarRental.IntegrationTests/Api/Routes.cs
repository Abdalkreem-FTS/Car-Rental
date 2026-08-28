namespace CarRental.IntegrationTests.Api;

internal static class Routes
{
    internal const string Health = "/api/health";

    internal const string Countries = "/api/countries";

    internal static class Auth
    {
        internal const string Register = "/api/auth/register";
        internal const string Login = "/api/auth/login";
        internal const string Refresh = "/api/auth/refresh";
        internal const string ConfirmEmail = "/api/auth/confirm-email";
        internal const string ResendConfirmation = "/api/auth/resend-confirmation";
        internal const string Logout = "/api/auth/logout";
        internal const string LogoutAll = "/api/auth/logout-all";
        internal const string ForgotPassword = "/api/auth/forgot-password";
        internal const string ResetPassword = "/api/auth/reset-password";
    }

    internal static class Cars
    {
        internal const string Base = "/api/cars";
        internal const string Locations = "/api/cars/locations";

        internal static string ById(Guid id) => $"{Base}/{id}";

        internal const string Admin = "/api/admin/cars";

        internal static string AdminById(Guid id) => $"{Admin}/{id}";
    }

    internal static class Reservations
    {
        internal const string Base = "/api/reservations";

        internal static string ById(Guid id) => $"{Base}/{id}";

        internal static string Cancel(Guid id) => $"{Base}/{id}/cancel";
    }

    internal static class Profile
    {
        internal const string Base = "/api/profile";
        internal const string Password = "/api/profile/password";
    }
}
