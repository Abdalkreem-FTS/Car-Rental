using System.Net;
using CarRental.Domain.Common;
using Shouldly;

namespace CarRental.IntegrationTests.Api;

public static class ApiAssertions
{
    extension<T>(ApiResponse<T> response) where T : class
    {
        public T ShouldBeOk(string? why = null)
        {
            response.StatusCode.ShouldBe(HttpStatusCode.OK, why ?? $"body was: {response.RawBody}");

            return response.Value.ShouldNotBeNull();
        }

        public T ShouldBeCreated(string? atLocation = null)
        {
            response.ShouldHaveStatus(HttpStatusCode.Created);

            if (atLocation is not null)
            {
                response.Location?.ToString().ShouldBe(atLocation);
            }

            return response.Value.ShouldNotBeNull();
        }
    }

    extension(ApiResponse response)
    {
        public void ShouldBeNoContent(string? why = null) =>
            response.StatusCode.ShouldBe(HttpStatusCode.NoContent, why ?? $"body was: {response.RawBody}");

        public void ShouldBeAccepted() => response.ShouldHaveStatus(HttpStatusCode.Accepted);

        public void ShouldBeConflict(Error expected) => response.ShouldFail(HttpStatusCode.Conflict, expected);

        public void ShouldBeNotFound(Error expected) => response.ShouldFail(HttpStatusCode.NotFound, expected);

        public void ShouldBeForbidden(Error expected) => response.ShouldFail(HttpStatusCode.Forbidden, expected);

        public void ShouldBeUnauthorized(Error expected, string? why = null) =>
            response.ShouldFail(HttpStatusCode.Unauthorized, expected, why);

        public void ShouldRequireAuthentication() => response.ShouldHaveStatus(HttpStatusCode.Unauthorized);

        public void ShouldBeForbidden() => response.ShouldHaveStatus(HttpStatusCode.Forbidden);

        public Problem ShouldFailValidationOn(string field)
        {
            response.ShouldHaveStatus(HttpStatusCode.BadRequest);

            var problem = response.Problem.ShouldNotBeNull();

            problem.Errors.ShouldNotBeNull($"expected a validation problem but got: {response.RawBody}");
            problem.Errors.ShouldContainKey(field);

            return problem;
        }

        private void ShouldFail(HttpStatusCode expectedStatus, Error expected, string? why = null)
        {
            response.StatusCode.ShouldBe(expectedStatus, why ?? $"body was: {response.RawBody}");

            var problem = response.Problem.ShouldNotBeNull();

            problem.ErrorCode.ShouldBe(expected.Code);
            problem.Status.ShouldBe((int)expectedStatus);
            problem.Detail.ShouldNotBeNullOrWhiteSpace();
            problem.Errors.ShouldBeNull("a document must not carry both conventions at once");
        }

        private void ShouldHaveStatus(HttpStatusCode expected) =>
            response.StatusCode.ShouldBe(expected, $"body was: {response.RawBody}");
    }
}

public static class ValidationProblemAssertions
{
    public static Problem ShouldFailValidation(this ApiResponse response, params string[] fields)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, $"body was: {response.RawBody}");

        var problem = response.Problem.ShouldNotBeNull();

        problem.Errors.ShouldNotBeNull($"expected a validation problem but got: {response.RawBody}");
        problem.ErrorCode.ShouldBeNull("a document must not carry both conventions at once");
        problem.Errors.Keys.OrderBy(key => key).ShouldBe(fields.OrderBy(field => field), ignoreOrder: false);

        foreach (var field in fields)
        {
            problem.Errors[field].ShouldNotBeEmpty();
        }

        return problem;
    }
}
