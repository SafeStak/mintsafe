using System.Collections.Generic;
using FluentAssertions;
using Mintsafe.DataAccess.Models;
using Xunit;

namespace Mintsafe.DataAccess.UnitTests.Models
{
    public class NiftyShould
    {
        [Theory]
        [InlineData(new[] { "A", "B", "C" }, "A,B,C")]
        [InlineData(new[] { "Jon", "Mark", "Dave" }, "Jon,Mark,Dave")]
        public void Set_CreatorsAsString_Correctly(string[] creators, string expectedString)
        {
            var niftyCollection = new Nifty
            {
                Creators = creators
            };

            niftyCollection.CreatorsAsString.Should().Be(expectedString);
        }

        [Theory]
        [InlineData(new[] { "A", "B", "" }, "A,B")]
        [InlineData(new[] { "A", "B", null }, "A,B")]
        [InlineData(new string[0], "")]
        [InlineData(null, null)]
        public void Set_CreatorsAsString_Correctly_Given_Null_Or_Empty(string[] creators, string expectedString)
        {
            var niftyCollection = new Nifty
            {
                Creators = creators
            };

            niftyCollection.CreatorsAsString.Should().Be(expectedString);
        }

        [Theory]
        [InlineData(new[] { "A", "B", "C" }, "A,B,C")]
        [InlineData(new[] { "Jon", "Mark", "Dave" }, "Jon,Mark,Dave")]
        public void Set_Creators_Correctly(string[] expectedArray, string creators)
        {
            var niftyCollection = new Nifty
            {
                CreatorsAsString = creators
            };

            niftyCollection.Creators.Should().BeEquivalentTo(expectedArray);
        }

        [Fact]
        public void Set_AttributesAsString_Correctly()
        {
            var niftyCollection = new Nifty
            {
                Attributes = new List<KeyValuePair<string, string>>()
                {
                    new("Colour", "Orange"),
                    new("Size", "Large")
                }
            };

            niftyCollection.AttributesAsString.Should().Be("[{\"Key\":\"Colour\",\"Value\":\"Orange\"},{\"Key\":\"Size\",\"Value\":\"Large\"}]");
        }


        [Fact]
        public void Set_Attributes_Correctly()
        {
            var niftyCollection = new Nifty
            {
                AttributesAsString = "[{\"Key\":\"Colour\",\"Value\":\"Orange\"},{\"Key\":\"Size\",\"Value\":\"Large\"}]"
            };

            niftyCollection.Attributes.Should().BeEquivalentTo(new List<KeyValuePair<string, string>>()
            {
                new("Colour", "Orange"),
                new("Size", "Large")
            });
        }
    }
}
