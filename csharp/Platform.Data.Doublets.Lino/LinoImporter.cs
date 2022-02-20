using Platform.Communication.Protocol.Lino;

namespace Platform.Data.Doublets.Lino;

public class LinoImporter<TLinkAddress>
{
    private readonly ILinoStorage<TLinkAddress> _linoDocumentsStorage;
    private readonly Parser _parser = new Parser();

    public LinoImporter(ILinoStorage<TLinkAddress> linoDocumentsStorage)
    {
        _linoDocumentsStorage = linoDocumentsStorage;
    }

    public void Import(string content)
    {
        var linoLinks = _parser.Parse(content);
        _linoDocumentsStorage.CreateLinks(linoLinks);
    }

}
