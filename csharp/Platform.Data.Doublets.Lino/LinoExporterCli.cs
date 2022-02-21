using System;
using System.IO;
using Platform.Data.Doublets.Memory.United.Generic;
using Platform.Data.Doublets.Sequences.Converters;
using Platform.IO;
using Platform.Memory;

namespace Platform.Data.Doublets.Lino;

public class LinoExporterCli<TLinkAddress> where TLinkAddress : struct
{
    public void Run(params string[] args)
    {
        var argumentsIndex = 0;
        var storageFilePath = ConsoleHelpers.GetOrReadArgument(argumentsIndex++, "A path to a links storage", args);
        var notationFilePath = ConsoleHelpers.GetOrReadArgument(argumentsIndex++, "A path to a notation file", args);
        var documentName = ConsoleHelpers.GetOrReadArgument(argumentsIndex++, "A document name", args);
        using var linksMemory = new FileMappedResizableDirectMemory(storageFilePath);
        var storage = new UnitedMemoryLinks<TLinkAddress>(linksMemory).DecorateWithAutomaticUniquenessAndUsagesResolution();
        ILinoStorage<TLinkAddress> linoStorage;
        if (String.IsNullOrWhiteSpace(documentName))
        {
            linoStorage = new DefaultLinoStorage<TLinkAddress>(storage);
        }
        else
        {
            linoStorage = new LinoDocumentsStorage<TLinkAddress>(storage, new BalancedVariantConverter<TLinkAddress>(storage));
        }
        var exporter = new LinoExporter<TLinkAddress>(linoStorage);
        var allLinksNotation = exporter.GetAllLinks(documentName);
        File.WriteAllText(notationFilePath, allLinksNotation);
    }
}
