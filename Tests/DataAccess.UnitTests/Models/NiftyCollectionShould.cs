using FluentAssertions;
using Mintsafe.DataAccess.Models;
using Xunit;

namespace Mintsafe.DataAccess.UnitTests.Models
{
    public class NiftyCollectionShould
    {
        [Theory]
        [InlineData(new[] { "A", "B", "C" }, "A,B,C")]
        [InlineData(new[] { "Jon", "Mark", "Dave" }, "Jon,Mark,Dave")]
        public void Set_PublishersAsString_Correctly(string[] publishers, string expectedString)
        {
            var niftyCollection = new NiftyCollection
            {
                Publishers = publishers
            };

            niftyCollection.PublishersAsString.Should().Be(expectedString);
        }

        [Theory]
        [InlineData(new[] { "A", "B", "" }, "A,B")]
        [InlineData(new[] { "A", "B", null }, "A,B")]
        [InlineData(new string[0], "")]
        [InlineData(null, null)]
        public void Set_PublishersAsString_Correctly_Given_Null_Or_Empty(string[] publishers, string expectedString)
        {
            var niftyCollection = new NiftyCollection
            {
                Publishers = publishers
            };

            niftyCollection.PublishersAsString.Should().Be(expectedString);
        }

        [Theory]
        [InlineData(new[] { "A", "B", "C" }, "A,B,C")]
        [InlineData(new[] { "Jon", "Mark", "Dave" }, "Jon,Mark,Dave")]
        public void Set_Publishers_Correctly(string[] expectedArray, string publishers)
        {
            var niftyCollection = new NiftyCollection
            {
                PublishersAsString = publishers
            };

            niftyCollection.Publishers.Should().BeEquivalentTo(expectedArray);
        }
    }
}
