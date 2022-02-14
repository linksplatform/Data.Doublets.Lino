using System.IO;
using Platform.Data.Doublets.Memory.United.Generic;
using Platform.IO;
using Platform.Memory;

namespace Platform.Data.Doublets.Lino;

public class LinoImporterCli<TLinkAddress>
{
    public void Run(params string[] args)
    {
        var argumentsIndex = 0;
        var notationFilePath = ConsoleHelpers.GetOrReadArgument(argumentsIndex++, "A path to a notation file", args);
        var storageFilePath = ConsoleHelpers.GetOrReadArgument(argumentsIndex, "A path to a links storage", args);
        using var linksMemory = new FileMappedResizableDirectMemory(storageFilePath);
        var links = new UnitedMemoryLinks<TLinkAddress>(linksMemory).DecorateWithAutomaticUniquenessAndUsagesResolution();
        var linoStorage = new DefaultLinoStorage<TLinkAddress>(links);
        var importer = new LinoImporter<TLinkAddress>(linoStorage);
        var notation = File.ReadAllText(notationFilePath);
        importer.Import(notation);
    }
}
