using System.Collections.Generic;
using Platform.Converters;
using LinoLink = Platform.Communication.Protocol.Lino.Link;

namespace Platform.Data.Doublets.Lino;

public class DefaultLinoStorage<TLinkAddress> : ILinoStorage<TLinkAddress>
{
    private readonly ILinks<TLinkAddress> _storage;

    public DefaultLinoStorage(ILinks<TLinkAddress> storage)
    {
        _storage = storage;
    }

    public void CreateLinks(IList<LinoLink> links)
    {
        var checkedConverter = CheckedConverter<ulong, TLinkAddress>.Default;
        for (int i = 0; i < links.Count; i++)
        {
            _storage.Create();
        }
        for (int i = 0; i < links.Count; i++)
        {
            var index = ulong.Parse(links[i].Id);
            var source = ulong.Parse(links[i].Values[0].Id);
            var target = ulong.Parse(links[i].Values[1].Id);
            _storage.Update(checkedConverter.Convert(index), checkedConverter.Convert(source), checkedConverter.Convert(target));
        }
    }

    public IList<LinoLink> GetLinks()
    {
        var allLinks = _storage.All();
        var linoLinks = new List<LinoLink>(allLinks.Count);
        for (int i = 0; i < allLinks.Count; i++)
        {
            var link = new Link<TLinkAddress>(allLinks[i]);
            var linoLink = new LinoLink(link.Index.ToString(), new List<LinoLink> { link.Source.ToString(), link.Target.ToString() });
            linoLinks.Add(linoLink);
        }
        return linoLinks;
    }
}
