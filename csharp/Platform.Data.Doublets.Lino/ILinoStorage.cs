using System.Collections.Generic;
using Platform.Communication.Protocol.Lino;

namespace Platform.Data.Doublets.Lino;

public interface ILinoStorage<TLinkAddress>
{
    void CreateLinks(IList<Link> links);
    IList<Link> GetLinks();
}
