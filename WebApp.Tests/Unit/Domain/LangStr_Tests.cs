using System.Text.Json;
using AwesomeAssertions;
using Base.Domain;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Unit.Domain;

[Collection("CultureSensitive")]
public class LangStr_Tests
{
    public LangStr_Tests()
    {
        // LangStr.DefaultCulture is a static, mutable property; reset to "en" before
        // each test so other tests can't poison the fallback chain.
        LangStr.DefaultCulture = "en";
    }

    [Fact]
    public void Ctor_WithValueAndCulture_StoresUnderNeutral_AndDefaults()
    {
        var s = new LangStr("hello", "et-EE");

        s["et"].Should().Be("hello");
        s["en"].Should().Be("hello");
    }

    [Fact]
    public void Ctor_WithValueAndCulture_DoesNotOverwriteDefaultIfAlreadySet()
    {
        var s = new LangStr();
        s["en"] = "english";
        s.SetTranslation("eestikeelne", "et");

        s["en"].Should().Be("english");
        s["et"].Should().Be("eestikeelne");
    }

    [Fact]
    public void Ctor_Default_IsEmpty()
    {
        var s = new LangStr();
        s.Count.Should().Be(0);
    }

    [Fact]
    public void Ctor_EmptyCulture_Throws()
    {
        Action act = () => new LangStr("hello", "");
        act.Should().Throw<ApplicationException>();
    }

    [Fact]
    public void Translate_ExactCultureMatch_ReturnsValue()
    {
        var s = new LangStr { ["en"] = "hello", ["et"] = "tere" };

        s.Translate("et").Should().Be("tere");
        s.Translate("en").Should().Be("hello");
    }

    [Fact]
    public void Translate_FallsBackToNeutralCulture()
    {
        var s = new LangStr { ["en"] = "hello", ["et"] = "tere" };

        s.Translate("et-EE").Should().Be("tere");
    }

    [Fact]
    public void Translate_FallsBackToDefaultCulture()
    {
        var s = new LangStr { ["en"] = "hello" };

        s.Translate("de").Should().Be("hello");
    }

    [Fact]
    public void Translate_NoMatches_ReturnsNull()
    {
        var s = new LangStr();

        s.Translate("en").Should().BeNull();
    }

    [Fact]
    public void Translate_NoCulture_UsesCurrentUiCulture()
    {
        var s = new LangStr { ["en"] = "hello", ["et"] = "tere" };

        using (new CultureScope("et"))
        {
            s.Translate().Should().Be("tere");
        }

        using (new CultureScope("en"))
        {
            s.Translate().Should().Be("hello");
        }
    }

    [Fact]
    public void SetTranslation_AddsCulture_PreservesOthers()
    {
        var s = new LangStr { ["en"] = "hello" };

        s.SetTranslation("tere", "et");

        s["en"].Should().Be("hello");
        s["et"].Should().Be("tere");
    }

    [Fact]
    public void SetTranslation_OverwritesExistingCulture()
    {
        var s = new LangStr { ["et"] = "tere" };

        s.SetTranslation("tere uuendatud", "et");

        s["et"].Should().Be("tere uuendatud");
    }

    [Fact]
    public void SetTranslation_NoCulture_UsesCurrentUiCulture()
    {
        var s = new LangStr { ["en"] = "hello" };

        using (new CultureScope("et"))
        {
            s.SetTranslation("tere");
        }

        s["et"].Should().Be("tere");
        s["en"].Should().Be("hello");
    }

    [Fact]
    public void ImplicitFromString_StoresCurrentCulturePlusDefault()
    {
        using var _ = new CultureScope("et");
        LangStr s = "tere";

        s["et"].Should().Be("tere");
        s["en"].Should().Be("tere");
    }

    [Fact]
    public void ImplicitToString_UsesCurrentUiCulture()
    {
        var s = new LangStr { ["en"] = "hello", ["et"] = "tere" };

        using (new CultureScope("et"))
        {
            string asString = s;
            asString.Should().Be("tere");
        }

        using (new CultureScope("en"))
        {
            string asString = s;
            asString.Should().Be("hello");
        }
    }

    [Fact]
    public void ImplicitToString_NullSource_ReturnsLiteralNull()
    {
        LangStr? source = null;
        string asString = source!;
        asString.Should().Be("null");
    }

    [Fact]
    public void JsonRoundTrip_PreservesAllEntries()
    {
        var original = new LangStr { ["en"] = "hello", ["et"] = "tere" };

        var json = JsonSerializer.Serialize(original, (JsonSerializerOptions?)null);
        var restored = JsonSerializer.Deserialize<LangStr>(json, (JsonSerializerOptions?)null)!;

        restored.Should().NotBeNull();
        restored["en"].Should().Be("hello");
        restored["et"].Should().Be("tere");
    }
}
