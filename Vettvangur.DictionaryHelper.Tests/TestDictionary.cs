using DictionaryHelper.Models;
using NSubstitute;
using Umbraco.Cms.Core.Models;

namespace DictionaryHelper.Tests;

/// <summary>
/// A made-up dictionary shaped like a real site's: folders a few levels deep, two languages,
/// some empty texts, keys with dashes, keys without texts, a text in a removed language and a
/// key that exists twice.
/// </summary>
internal sealed class TestDictionary
{
    public static readonly string[] Cultures = ["is-IS", "en-US"];

    public List<CmsDictionary> Keys { get; } = new();
    public List<CmsLanguageText> Texts { get; } = new();
    public List<ILanguage> Languages { get; } = new();

    public TestDictionary(int seed, int folders = 40, int itemsPerFolder = 12)
    {
        var random = new Random(seed);

        for (var i = 0; i < Cultures.Length; i++)
        {
            var language = Substitute.For<ILanguage>();
            language.Id.Returns(i + 1);
            language.IsoCode.Returns(Cultures[i]);
            Languages.Add(language);
        }

        var parents = new List<Guid> { Guid.Empty };
        var pk = 0;

        for (var f = 0; f < folders; f++)
        {
            var folder = AddKey($"Folder{f}", parents[random.Next(parents.Count)], random, ref pk);
            parents.Add(folder.id);

            for (var i = 0; i < itemsPerFolder; i++)
            {
                // Some keys with a dash, like "MgStatusV2-3"
                var name = random.Next(8) == 0 ? $"Status{f}x{i}-{random.Next(10)}" : $"Text{f}x{i}";
                var key = AddKey(name, folder.id, random, ref pk);

                // One key in twelve has no texts at all
                if (random.Next(12) == 0)
                {
                    continue;
                }

                foreach (var language in Languages)
                {
                    // Some texts are empty
                    var value = random.Next(6) == 0 ? "" : $"{name} ({language.IsoCode})";
                    Texts.Add(new CmsLanguageText { pk = ++pk, languageId = language.Id, UniqueId = key.id, value = value });
                }
            }
        }

        // A text in a language that no longer exists
        Texts.Add(new CmsLanguageText { pk = ++pk, languageId = 99, UniqueId = Keys[5].id, value = "orphan" });

        // The same key twice, under different parents: the cache keeps the first
        var twin = AddKey(Keys[7].key, parents[1], random, ref pk);
        Texts.Add(new CmsLanguageText { pk = ++pk, languageId = 1, UniqueId = twin.id, value = "twin" });
    }

    CmsDictionary AddKey(string key, Guid parent, Random random, ref int pk)
    {
        var id = new byte[16];
        random.NextBytes(id);
        var item = new CmsDictionary { pk = ++pk, id = new Guid(id), parent = parent, key = key };
        Keys.Add(item);
        return item;
    }

    /// <summary>The key as a caller might type it: same letters, random case.</summary>
    public static string RandomCase(string value, Random random) =>
        new(value.Select(c => random.Next(2) == 0 ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c)).ToArray());
}
