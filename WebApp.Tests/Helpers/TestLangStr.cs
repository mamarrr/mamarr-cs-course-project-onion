using Base.Domain;

namespace WebApp.Tests.Helpers;

public static class TestLangStr
{
    public static LangStr Create(string en, string et)
    {
        return new LangStr
        {
            ["en"] = en,
            ["et"] = et
        };
    }
}
