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

    // public void Import(string content)
    // {
    //     var linoLinks = _parser.Parse(content);
    //     for (int i = 0; i < linoLinks.Count; i++)
    //     {
    //         var linoLink = linoLinks[i];
    //         Read(linoLink);
    //     }
    // }
    //
    // public TLinkAddress Read(Link parent)
    // {
    //     var left = parent.Values[0];
    //     var right = parent.Values[1];
    //     var source = left.Values != null ? Read(left) : _linoDocumentsStorage.GetOrCreateReference(left.Id);
    //     var target = right.Values != null ? Read(right) : _linoDocumentsStorage.GetOrCreateReference(right.Id);
    //     return _linoDocumentsStorage.Storage.GetOrCreate(source, target);
    // }
}
