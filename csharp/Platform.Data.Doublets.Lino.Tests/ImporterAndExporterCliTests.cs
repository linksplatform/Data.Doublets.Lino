using System;
using System.IO;
using Platform.IO;
using Xunit;
using TLinkAddress = System.UInt64;

namespace Platform.Data.Doublets.Lino.Tests;

public class ImporterAndExporterCliTests
{
    [Theory]
    [InlineData("(1: 1 1)")]
    [InlineData("(1: 1 1)\n(2: 2 2)")]
    [InlineData("(1: 2 2)")]
    [InlineData("(1: 2 2)\n(2: 1 1)")]
    public void DefaultLinoStorageTest(string notation)
    {
        var notationFilePath = TemporaryFiles.UseNew();
        var linksStorageFilePath = TemporaryFiles.UseNew();
        var exportedNotationFilePath = TemporaryFiles.UseNew();
        File.WriteAllText(notationFilePath, notation);
        new LinoImporterCli<TLinkAddress>().Run(notationFilePath, linksStorageFilePath, "");
        new LinoExporterCli<TLinkAddress>().Run(linksStorageFilePath, exportedNotationFilePath, "");
        Assert.Equal(notation, File.ReadAllText(exportedNotationFilePath));
    }

    [Theory]
    [InlineData("(1: 1 1)")]
    [InlineData("(1: 1 1)\n(2: 2 2)")]
    [InlineData("(2: 2 2)")]
    [InlineData("(1: 2 2)")]
    [InlineData("(1: 2 2)\n(2: 1 1)")]
    [InlineData("(1: 2 (3: 3 3))")]
    [InlineData("(1: 2 (3: 3 3))\n(2: 1 1)")]
    [InlineData("(son: lovesMama)")]
    [InlineData("(papa: (lovesMama: loves mama))")]
    [InlineData("(papa: (lovesMama: loves mama)\nson lovesMama\ndaughter lovesMama\nall (love: mama))")]
    public void LinoDocumentsStorageTest(string notation)
    {
        var notationFilePath = TemporaryFiles.UseNew();
        var linksStorageFilePath = TemporaryFiles.UseNew();
        var exportedNotationFilePath = TemporaryFiles.UseNew();
        File.WriteAllText(notationFilePath, notation);
        new LinoImporterCli<TLinkAddress>().Run(notationFilePath, linksStorageFilePath, notationFilePath);
        new LinoExporterCli<TLinkAddress>().Run(linksStorageFilePath, exportedNotationFilePath, notationFilePath);
        Assert.Equal(notation, File.ReadAllText(exportedNotationFilePath));
    }
}
