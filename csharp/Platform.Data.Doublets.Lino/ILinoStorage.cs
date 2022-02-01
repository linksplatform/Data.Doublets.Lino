using System.Collections.Generic;
using Platform.Communication.Protocol.Lino;

namespace DefaultNamespace;

public interface ILinoStorage<TLinkAddress> where TLinkAddress : struct
{
    void CreateLinks(IList<Link> links);
    IList<Link> GetLinks();
}
