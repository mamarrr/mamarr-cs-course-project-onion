using App.BLL.Shared.Routing;
using AwesomeAssertions;

namespace WebApp.Tests.Unit.Routing;

public class SlugGenerator_Tests
{
    [Theory]
    [InlineData("Company Name", "company-name")]
    [InlineData("  Company Name  ", "company-name")]
    [InlineData("Company---Name", "company-name")]
    [InlineData("Company, Name!", "company-name")]
    [InlineData("---Company---Name---", "company-name")]
    public void GenerateSlug_NormalizesText(string source, string expected)
    {
        SlugGenerator.GenerateSlug(source).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void GenerateSlug_EmptyInput_ReturnsFallback(string? source)
    {
        SlugGenerator.GenerateSlug(source).Should().Be("item");
    }

    [Fact]
    public void GenerateSlug_RemovesEstonianDiacriticsPredictably()
    {
        SlugGenerator.GenerateSlug("Ää Öö Õõ Üü Šš Žž").Should().Be("aa-oo-oo-uu-ss-zz");
    }

    [Fact]
    public void GenerateSlug_RespectsMaxLength()
    {
        var source = new string('a', SlugGenerator.MaxSlugLength + 50);

        var slug = SlugGenerator.GenerateSlug(source);

        slug.Should().HaveLength(SlugGenerator.MaxSlugLength);
        slug.ToCharArray().Should().OnlyContain(ch => ch == 'a');
    }

    [Fact]
    public void EnsureUniqueSlug_ReturnsBase_WhenNoCollisionExists()
    {
        var slug = SlugGenerator.EnsureUniqueSlug("Company Name", ["other-company"]);

        slug.Should().Be("company-name");
    }

    [Fact]
    public void EnsureUniqueSlug_AppendsNextAvailableSuffix_WhenCollisionExists()
    {
        var slug = SlugGenerator.EnsureUniqueSlug(
            "Company Name",
            ["company-name", "company-name-2", "company-name-3"]);

        slug.Should().Be("company-name-4");
    }

    [Fact]
    public void EnsureUniqueSlug_ExistingSlugsAreTrimmedAndCaseInsensitive()
    {
        var slug = SlugGenerator.EnsureUniqueSlug("Company Name", [" COMPANY-NAME "]);

        slug.Should().Be("company-name-2");
    }

    [Fact]
    public void EnsureUniqueSlug_RespectsMaxLengthAfterSuffix()
    {
        var source = new string('a', SlugGenerator.MaxSlugLength);
        var existing = new[] { source };

        var slug = SlugGenerator.EnsureUniqueSlug(source, existing);

        slug.Should().EndWith("-2");
        slug.Should().HaveLength(SlugGenerator.MaxSlugLength);
    }

    [Fact]
    public void GenerateSlug_IsStableForSameInput()
    {
        var first = SlugGenerator.GenerateSlug("Stable Name 123");
        var second = SlugGenerator.GenerateSlug("Stable Name 123");

        second.Should().Be(first);
    }
}
