using System.IO;
using Platform.Data.Doublets.Memory.United.Generic;
using Platform.IO;
using Platform.Memory;

namespace Platform.Data.Doublets.Lino;

public class LinoExporterCli<TLinkAddress>
{
    public void Run(params string[] args)
    {
        var argumentsIndex = 0;
        var storageFilePath = ConsoleHelpers.GetOrReadArgument(argumentsIndex, "A path to a links storage", args);
        var notationFilePath = ConsoleHelpers.GetOrReadArgument(argumentsIndex, "A path to a notation file", args);
        using var linksMemory = new FileMappedResizableDirectMemory(storageFilePath);
        var links = new UnitedMemoryLinks<TLinkAddress>(linksMemory).DecorateWithAutomaticUniquenessAndUsagesResolution();
        var linoStorage = new DefaultLinoStorage<TLinkAddress>(links);
        var exporter = new LinoExporter<TLinkAddress>(linoStorage);
        var allLinksNotation = exporter.GetAllLinks();
        File.WriteAllText(notationFilePath, allLinksNotation);
    }
}
