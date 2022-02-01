using System.Collections.Generic;
using System.IO;
using Platform.Communication.Protocol.Lino;
using Platform.Data.Doublets;
using Platform.Data.Doublets.Memory;
using Platform.Data.Doublets.Memory.United.Generic;
using LinoLink = Platform.Communication.Protocol.Lino.Link;

namespace DefaultNamespace;

public class LinoExporter<TLinkAddress> where TLinkAddress : struct
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

    // public void GetAllLinks(Stream outputStream)
    // {
    //     var allLinks = _linoDocumentsStorage.Storage.All();
    //     var linksAsStrings = new Dictionary<TLinkAddress, string>(allLinks.Count);
    //     for (int i = 0; i < allLinks.Count; i++)
    //     {
    //         var currentLink = allLinks[i];
    //         linksAsStrings.Add(_linoDocumentsStorage.Storage.GetIndex(currentLink), ReadLinkAsString(currentLink));
    //     }
    // }
    //
    // private LinoLink ReadLinkAsString(IList<TLinkAddress> link)
    // {
    //     string source = "";
    //     string target = "";
    //     var linkStruct = new Link<TLinkAddress>(link);
    //     if (_linoDocumentsStorage.IsReference(linkStruct.Source))
    //     {
    //         source = _linoDocumentsStorage.ReadReference(linkStruct.Source);
    //     }
    //     if (_linoDocumentsStorage.IsReference(linkStruct.Target))
    //     {
    //         target = _linoDocumentsStorage.ReadReference(linkStruct.Target);
    //     }
    //     return new LinoLink(source, target);
    // }
}
