using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;

namespace Opsi.Functions.Specs;

[TestClass]
public class ResponseSerialiserSpecs
{
    private const string _prop1 = "TEST PROP 1";
    private const int _prop2 = 987;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private FakeHttpResponseData _httpResponseData;
    private TestStruct _testObject;
    private ResponseSerialiser _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        var fakeContext = A.Fake<FunctionContext>();
        _httpResponseData = new FakeHttpResponseData(fakeContext);
        _testObject = new TestStruct(_prop1, _prop2);

        _testee = new ResponseSerialiser();
    }

    [TestMethod]
    public void WriteJsonToBody_SetsStatusCode200Ok()
    {
        _testee.WriteJsonToBody(_httpResponseData, _testObject);

        _httpResponseData.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public void WriteJsonToBody_SetsContentTypeAsApplicationJson()
    {
        _testee.WriteJsonToBody(_httpResponseData, _testObject);

        _httpResponseData.Headers.SingleOrDefault(header => header.Key == "Content-Type").Should().NotBeNull();
        _httpResponseData.Headers.SingleOrDefault(header => header.Key == "Content-Type").Value.Should().NotBeNull();
        _httpResponseData.Headers.SingleOrDefault(header => header.Key == "Content-Type").Value.Single().Should().Be(MediaTypeNames.Application.Json);
    }

    [TestMethod]
    public void WriteJsonToBody_SetsContentTypeUsingLowerCamelCase()
    {
        _testee.WriteJsonToBody(_httpResponseData, _testObject);

        var contentAsString = ParseBodyAsString(_httpResponseData.Body);

        contentAsString.Should().Contain("prop1");
        contentAsString.Should().Contain("testProp2");
    }

    [TestMethod]
    public void WriteJsonToBody_ContainsExpectedSerialisedContent()
    {
        var serialisationOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        var expectedContentAsString = JsonSerializer.Serialize(_testObject, serialisationOptions);

        _testee.WriteJsonToBody(_httpResponseData, _testObject);

        var contentAsString = ParseBodyAsString(_httpResponseData.Body);

        contentAsString.Should().Be(expectedContentAsString);
    }

    private static string? ParseBodyAsString(Stream responseBody)
    {
        var  buffer = new byte[responseBody.Length];
        responseBody.Read(buffer, 0, buffer.Length);

        return Encoding.UTF8.GetString(buffer);
    }

    private readonly struct TestStruct
    {
        public TestStruct(string prop1, int testProp2)
        {
            Prop1 = prop1;
            TestProp2 = testProp2;
        }

        public string Prop1 { get; }

        public int TestProp2 { get; }
    }
}
