Use it in the database. EF provider will store the dictionary as a JSON object and do automatic serialization/deserialization.

public class Foo: DomainEntityId
{
[Display(ResourceType = typeof(App.Resources.App.Domain.Foo), Name = nameof(SomeStr))]
[Column(TypeName = "jsonb")]
public LangStr SomeStr { get; set; } = new();
}

Use ToString() method to get translation using current culture.
When updating/adding translations, load existing values from the database first - otherwise you will overwrite translations for other cultures that are already stored in the JSON.
Add support in program.cs for [Column(TypeName = "jsonb")]

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");


// used for older style [Column(TypeName = "jsonb")] for LangStr
#pragma warning disable CS0618 // Type or member is obsolete
NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();
#pragma warning restore CS0618 // Type or member is obsolete


LangStr.DefaultCulture = builder.Configuration.GetValue<string>("LangStrDefaultCulture") ?? "en";