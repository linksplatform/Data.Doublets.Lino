using System.Collections.Generic;
using Platform.Communication.Protocol.Lino;

namespace Platform.Data.Doublets.Lino;

public class LinoExporter<TLinkAddress>
{
    private readonly EqualityComparer<TLinkAddress> _equalityComparer = EqualityComparer<TLinkAddress>.Default;
    private readonly ILinoStorage<TLinkAddress> _linoDocumentsStorage;

    public LinoExporter(ILinoStorage<TLinkAddress> linoDocumentsStorage)
    {
        _linoDocumentsStorage = linoDocumentsStorage;
    }

    public string GetAllLinks()
    {
        var allLinks = _linoDocumentsStorage.GetLinks();
        return allLinks.Format();
    }
}
