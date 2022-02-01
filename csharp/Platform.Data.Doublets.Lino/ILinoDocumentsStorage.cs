// using System.Collections.Generic;
// using Platform.Converters;
// using Platform.Data.Doublets;
//
// namespace DefaultNamespace;
//
// public interface ILinoDocumentsStorage<TLinkAddress> where TLinkAddress : struct
// {
//     ILinks<TLinkAddress> Storage { get; }
//     IConverter<string, TLinkAddress> StringToUnicodeSequenceConverter { get; }
//     IConverter<TLinkAddress, string> UnicodeSequenceToStringConverter { get; }
//     TLinkAddress ReferenceMarker { get; }
//
//     TLinkAddress GetOrCreateReferenceLink(string content);
//     string ReadReference(TLinkAddress reference);
//     string ReadReference(IList<TLinkAddress> reference);
//     bool IsReference(TLinkAddress reference);
//     bool IsReference(IList<TLinkAddress> reference);
// }
