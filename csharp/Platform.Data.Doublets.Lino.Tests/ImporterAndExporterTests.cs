using Platform.Data.Doublets.Memory;
using Platform.Data.Doublets.Memory.United.Generic;
using Platform.Data.Doublets.Sequences.Converters;
using Platform.Memory;
using System;
using Xunit;
using TLinkAddress = System.UInt64;

namespace Platform.Data.Doublets.Lino.Tests;

public class ImporterAndExporterTests
{
    public static ILinks<TLinkAddress> CreateLinks() => CreateLinks(new IO.TemporaryFile());

    public static ILinks<TLinkAddress> CreateLinks(string dbFilename)
    {
        var linksConstants = new LinksConstants<TLinkAddress>(true);
        return new UnitedMemoryLinks<TLinkAddress>(new HeapResizableDirectMemory(), UnitedMemoryLinks<TLinkAddress>.DefaultLinksSizeStep, linksConstants, IndexTreeType.Default);
    }

    [InlineData("(1: 1 1)")]
    [InlineData("(1: 1 1)\n(2: 2 2)")]
    [InlineData("(1: 2 2)")]
    [InlineData("(1: 2 2)\n(2: 1 1)")]
    [Theory]
    public void LinoStorageTest(string notation)
    {
        var storage = CreateLinks();
        var linoStorage = new DefaultLinoStorage<TLinkAddress>(storage);
        var importer = new LinoImporter<TLinkAddress>(linoStorage);
        var documentName = Random.RandomHelpers.Default.Next(Int32.MaxValue).ToString();
        importer.Import(notation, documentName);
        var exporter = new LinoExporter<TLinkAddress>(linoStorage);
        var exportedLinks = exporter.GetAllLinks(documentName);
        Assert.Equal(notation, exportedLinks);
    }

    [InlineData("(1: 1 1)")]
    [InlineData("(1: 1 1)\n(2: 2 2)")]
    [InlineData("(2: 2 2)")]
    [InlineData("(1: 2 2)")]
    [InlineData("(1: 2 2)\n(2: 1 1)")]
    [InlineData("(1: 2 (3: 3 3))")]
    [InlineData("(1: 2 (3: 3 3))\n(2: 1 1)")]
    [InlineData("(son: lovesMama)")]
    [InlineData("(papa: (lovesMama: loves mama))")]
    [InlineData(@"(papa (lovesMama: loves mama))
(son lovesMama)
(daughter lovesMama)
(all (love mama))")]
    [Theory]
    public void LinoDocumentStorageTest(string notation)
    {
        var storage = CreateLinks();
        var linoStorage = new LinoDocumentsStorage<TLinkAddress>(storage, new BalancedVariantConverter<ulong>(storage));
        var importer = new LinoImporter<TLinkAddress>(linoStorage);
        var documentName = Random.RandomHelpers.Default.Next(Int32.MaxValue).ToString();
        importer.Import(notation, documentName);
        var exporter = new LinoExporter<TLinkAddress>(linoStorage);
        var exportedLinks = exporter.GetAllLinks(documentName);
        Assert.Equal(notation, exportedLinks);
    }

}
