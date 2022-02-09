using Platform.Converters;
using Platform.Data.Doublets.CriterionMatchers;
using Platform.Data.Doublets.Memory;
using Platform.Data.Doublets.Memory.United.Generic;
using Platform.Data.Doublets.Sequences.Converters;
using Platform.Data.Doublets.Unicode;
using Platform.Data.Numbers.Raw;
using Platform.Memory;
using Platform.Numbers;
using Xunit;
using TLinkAddress = System.UInt64;

namespace Platform.Data.Doublets.Lino.Tests;

public class ImporterAndExporterTests
{
    public static ILinks<TLinkAddress> CreateLinks() => CreateLinks(new IO.TemporaryFile());

    public static ILinks<TLinkAddress> CreateLinks(string dbFilename)
    {
        var linksConstants = new LinksConstants<TLinkAddress>(enableExternalReferencesSupport: true);
        return new UnitedMemoryLinks<TLinkAddress>(new FileMappedResizableDirectMemory(dbFilename), UnitedMemoryLinks<TLinkAddress>.DefaultLinksSizeStep, linksConstants, IndexTreeType.Default);
    }

    // [InlineData("(1: 1 1)")]
    // [InlineData("(1: 1 1)\n(2: 2 2)")]
    // [InlineData("(1: 2 2)\n(2: 1 1)")]
    // [Theory]
    // public void Test1(string notation)
    // {
    //     var storage = CreateLinks();
    //     TLinkAddress Zero = default;
    //     TLinkAddress One = Arithmetic.Increment(Zero);
    //     var markerIndex = One;
    //     var meaningRoot = storage.GetOrCreate(markerIndex, markerIndex);
    //     var unicodeSymbolMarker = storage.GetOrCreate(meaningRoot, Arithmetic.Increment(ref markerIndex));
    //     var unicodeSequenceMarker = storage.GetOrCreate(meaningRoot, Arithmetic.Increment(ref markerIndex));
    //     var referenceMarker = storage.GetOrCreate(meaningRoot, Arithmetic.Increment(ref markerIndex));
    //     TargetMatcher<TLinkAddress> unicodeSymbolCriterionMatcher = new(storage, unicodeSymbolMarker);
    //     TargetMatcher<TLinkAddress> unicodeSequenceCriterionMatcher = new(storage, unicodeSequenceMarker);
    //     AddressToRawNumberConverter<TLinkAddress> addressToNumberConverter = new();
    //     RawNumberToAddressConverter<TLinkAddress> numberToAddressConverter = new();
    //     CharToUnicodeSymbolConverter<TLinkAddress> charToUnicodeSymbolConverter =
    //         new(storage, addressToNumberConverter, unicodeSymbolMarker);
    //     UnicodeSymbolToCharConverter<TLinkAddress> unicodeSymbolToCharConverter =
    //         new(storage, numberToAddressConverter, unicodeSymbolCriterionMatcher);
    //     var balancedVariantConverter = new BalancedVariantConverter<TLinkAddress>(storage);
    //     var stringToUnicodeSequenceConverter = new CachingConverterDecorator<string, TLinkAddress>(new StringToUnicodeSequenceConverter<TLinkAddress>(storage, charToUnicodeSymbolConverter, balancedVariantConverter, unicodeSequenceMarker));
    //     ILinoDocumentsStorage<TLinkAddress> linoDocumentsStorage = new LinoDocumentsStorage<TLinkAddress>(storage);
    //     LinoImporter<TLinkAddress> linoImporter = new(linoDocumentsStorage);
    //     linoImporter.Import(notation);
    //     var linoExporter = new LinoExporter<TLinkAddress>(linoDocumentsStorage);
    // }

    [InlineData("(1: 1 1)")]
    [InlineData("(1: 1 1)\n(2: 2 2)")]
    [InlineData("(1: 2 2)\n(2: 1 1)")]
    [Theory]
    public void LinoStorageTest(string notation)
    {
        var storage = CreateLinks();
        var linoStorage = new DefaultLinoStorage<TLinkAddress>(storage);
        var importer = new LinoImporter<TLinkAddress>(linoStorage);
        importer.Import(notation);
        var exporter = new LinoExporter<TLinkAddress>(linoStorage);
        var exportedLinks = exporter.GetAllLinks();
        Assert.Equal(notation, exportedLinks);
    }

    [InlineData("(1: 1 1)")]
    [InlineData("(1: 1 1)\n(2: 2 2)")]
    [InlineData("(2: 2 2)")]
    [InlineData("(1: 2 2)")]
    [InlineData("(1: 2 2)\n(2: 1 1)")]
    [InlineData("(1: 2 (3: 3 3))")]
    [InlineData("(1: 2 (3: 3 3))\n(2: 1 1)")]
    [Theory]
    public void LinoDocumentStorageTest(string notation)
    {
        var storage = CreateLinks();
        var linoStorage = new LinoDocumentsStorage<TLinkAddress>(storage, new BalancedVariantConverter<ulong>(storage));
        var importer = new LinoImporter<TLinkAddress>(linoStorage);
        importer.Import(notation);
        var anotherLinoStorage = new DefaultLinoStorage<TLinkAddress>(storage);
        // var exporter = new LinoExporter<TLinkAddress>(anotherLinoStorage);
        var exporter = new LinoExporter<TLinkAddress>(linoStorage);
        var exportedLinks = exporter.GetAllLinks();
        Assert.Equal(notation, exportedLinks);
    }

    // public void CreateLinksTest()
    // {
    //     var storage = CreateLinks();
    //     var linoStorage = new DefaultLinoStorage<TLinkAddress>(storage);
    //     linoStorage.CreateLinks();
    // }
    //
    // public void GetLinksTest()
    // {
    //     var storage = CreateLinks();
    //     var linoStorage = new DefaultLinoStorage<TLinkAddress>(storage);
    //     linoStorage.GetLinks();
    // }
}
