namespace CarRental.IntegrationTests.Support.Api;

internal static class Routes
{
    internal const string Health = "/api/health";

    internal const string Countries = "/api/countries";

    internal static class Auth
    {
        internal const string Register = "/api/users";
        internal const string Login = "/api/tokens";
        internal const string Refresh = "/api/tokens/current";
        internal const string ConfirmEmail = "/api/email-confirmations";
        internal const string ResendConfirmation = "/api/email-confirmations";
        internal const string Logout = "/api/tokens/current";
        internal const string LogoutAll = "/api/tokens";
        internal const string ForgotPassword = "/api/password-resets";
        internal const string ResetPassword = "/api/password-resets";
    }

    internal static class Cars
    {
        internal const string Base = "/api/cars";
        internal const string Locations = "/api/cars/locations";

        internal static string ById(Guid id) => $"{Base}/{id}";

        internal const string Admin = "/api/admin/cars";

        internal static string AdminById(Guid id) => $"{Admin}/{id}";

        internal static string Retirement(Guid id) => $"{Admin}/{id}/retirement";

        internal static string AdminReservations(Guid id) => $"{Admin}/{id}/reservations";
    }

    internal static class Reservations
    {
        internal const string Base = "/api/reservations";

        internal static string ById(Guid id) => $"{Base}/{id}";

        internal static string Cancellation(Guid id) => $"{Base}/{id}/cancellation";
    }

    internal static class Profile
    {
        internal const string Base = "/api/profile";
        internal const string Password = "/api/profile/password";
    }
}
