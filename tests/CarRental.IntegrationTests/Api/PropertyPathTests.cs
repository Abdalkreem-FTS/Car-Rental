using CarRental.Api.Extensions;
using Shouldly;

namespace CarRental.IntegrationTests.Api;

public sealed class PropertyPathTests
{
    [Theory]
    [InlineData("Email", "email")]
    [InlineData("email", "email")]
    [InlineData("PlateNumber", "plateNumber")]
    [InlineData("", "")]
    public void ToJsonName_ForAPlainProperty_MatchesWhatTheSerialiserWouldWrite(string property, string expected)
    {
        PropertyPath.ToJsonName(property).ShouldBe(expected);
    }

    [Theory]
    [InlineData("Address.City", "address.city")]
    [InlineData("Address.Line1.Value", "address.line1.value")]
    public void ToJsonName_ForANestedProperty_LowersEverySegment(string property, string expected)
    {
        PropertyPath.ToJsonName(property).ShouldBe(expected, "a client matching on the field name reads the whole path");
    }

    [Theory]
    [InlineData("Items[0].Name", "items[0].name")]
    public void ToJsonName_ForAnIndexedProperty_KeepsTheIndex(string property, string expected)
    {
        PropertyPath.ToJsonName(property).ShouldBe(expected);
    }

    [Theory]
    [InlineData("IDNumber", "idNumber")]
    [InlineData("ID", "id")]
    public void ToJsonName_ForAnAcronym_MatchesTheSerialiser(string property, string expected)
    {
        PropertyPath.ToJsonName(property).ShouldBe(expected);
    }
}
