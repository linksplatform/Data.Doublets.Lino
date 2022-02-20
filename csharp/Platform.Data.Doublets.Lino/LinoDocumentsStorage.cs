using System;
using System.Collections.Generic;
using System.Linq;
using Platform.Collections.Stacks;
using Platform.Converters;
using Platform.Data.Doublets.CriterionMatchers;
using Platform.Data.Doublets.Numbers.Rational;
using Platform.Data.Doublets.Numbers.Raw;
using Platform.Data.Doublets.Sequences.Converters;
using Platform.Data.Doublets.Sequences.Walkers;
using Platform.Data.Doublets.Unicode;
using Platform.Data.Numbers.Raw;
using Platform.Numbers;
using LinoLink = Platform.Communication.Protocol.Lino.Link;

namespace Platform.Data.Doublets.Lino;

public class LinoDocumentsStorage<TLinkAddress> : ILinoStorage<TLinkAddress>
{
    private static readonly TLinkAddress Zero = default!;
    private static readonly TLinkAddress One = Arithmetic.Increment(Zero);

    private readonly EqualityComparer<TLinkAddress> _equalityComparer = EqualityComparer<TLinkAddress>.Default;

    // Converters that are able to convert link's address (UInt64 value) to a raw number represented with another UInt64 value and back
    private readonly RawNumberToAddressConverter<TLinkAddress> _numberToAddressConverter = new();
    private readonly AddressToRawNumberConverter<TLinkAddress> _addressToNumberConverter = new();

    private ILinks<TLinkAddress> Storage { get; }

    private IConverter<string, TLinkAddress> StringToUnicodeSequenceConverter { get; }

    private IConverter<TLinkAddress, string> UnicodeSequenceToStringConverter { get; }

    public TLinkAddress DocumentMarker { get; }

    public TLinkAddress ReferenceMarker { get; }

    public TLinkAddress LinkWithoutIdMarker { get; }

    public TLinkAddress LinkWithIdMarker { get; }

    private readonly IConverter<IList<TLinkAddress>?, TLinkAddress> _listToSequenceConverter;

    public LinoDocumentsStorage(ILinks<TLinkAddress> storage, IConverter<IList<TLinkAddress>?, TLinkAddress> listToSequenceConverter)
    {
        Storage = storage;
        // Initializes constants
        var markerIndex = One;
        var meaningRoot = storage.GetOrCreate(markerIndex, markerIndex);
        var unicodeSymbolMarker = storage.GetOrCreate(meaningRoot, Arithmetic.Increment(ref markerIndex));
        var unicodeSequenceMarker = storage.GetOrCreate(meaningRoot, Arithmetic.Increment(ref markerIndex));
        DocumentMarker = storage.GetOrCreate(meaningRoot, Arithmetic.Increment(ref markerIndex));
        ReferenceMarker = storage.GetOrCreate(meaningRoot, Arithmetic.Increment(ref markerIndex));
        LinkWithoutIdMarker = storage.GetOrCreate(meaningRoot, Arithmetic.Increment(ref markerIndex));
        LinkWithIdMarker = storage.GetOrCreate(meaningRoot, Arithmetic.Increment(ref markerIndex));
        BalancedVariantConverter<TLinkAddress> balancedVariantConverter = new(storage);
        TargetMatcher<TLinkAddress> unicodeSymbolCriterionMatcher = new(storage, unicodeSymbolMarker);
        TargetMatcher<TLinkAddress> unicodeSequenceCriterionMatcher = new(storage, unicodeSequenceMarker);
        CharToUnicodeSymbolConverter<TLinkAddress> charToUnicodeSymbolConverter = new(storage, _addressToNumberConverter, unicodeSymbolMarker);
        UnicodeSymbolToCharConverter<TLinkAddress> unicodeSymbolToCharConverter = new(storage, _numberToAddressConverter, unicodeSymbolCriterionMatcher);
        StringToUnicodeSequenceConverter = new CachingConverterDecorator<string, TLinkAddress>(new StringToUnicodeSequenceConverter<TLinkAddress>(storage, charToUnicodeSymbolConverter, balancedVariantConverter, unicodeSequenceMarker));
        RightSequenceWalker<TLinkAddress> sequenceWalker = new(storage, new DefaultStack<TLinkAddress>(), unicodeSymbolCriterionMatcher.IsMatched);
        UnicodeSequenceToStringConverter = new CachingConverterDecorator<TLinkAddress, string>(new UnicodeSequenceToStringConverter<TLinkAddress>(storage, unicodeSequenceCriterionMatcher, sequenceWalker, unicodeSymbolToCharConverter));
        _listToSequenceConverter = listToSequenceConverter;
    }

    private bool IsLinkWithId(TLinkAddress linkAddress) => IsLinkWithId(Storage.GetLink(linkAddress));

    private bool IsLinkWithoutId(TLinkAddress linkAddress) => IsLinkWithoutId(Storage.GetLink(linkAddress));

    private bool IsLink(TLinkAddress linkAddress)
    {
        var link = Storage.GetLink(linkAddress);
        return IsLinkWithId(link) || IsLinkWithoutId(link);
    }

    private bool IsLinkWithId(IList<TLinkAddress> link)
    {
        var source = Storage.GetSource(link);
        return _equalityComparer.Equals(LinkWithIdMarker, source);
    }

    private bool IsLinkWithoutId(IList<TLinkAddress> link)
    {
        var source = Storage.GetSource(link);
        return _equalityComparer.Equals(LinkWithoutIdMarker, source);
    }

    private bool IsLink(IList<TLinkAddress> link) => IsLinkWithId(link) || IsLinkWithoutId(link);

    private bool IsReference(TLinkAddress reference)
    {
        var link = Storage.GetLink(reference);
        return IsReference(link);
    }

    private bool IsReference(IList<TLinkAddress> reference)
    {
        var source = Storage.GetSource(reference);
        return _equalityComparer.Equals(ReferenceMarker, source);
    }


    private TLinkAddress GetOrCreateReference(string content)
    {
        var sequence = StringToUnicodeSequenceConverter.Convert(content);
        return Storage.GetOrCreate(ReferenceMarker, sequence);
    }

    private string ReadReference(TLinkAddress reference) => ReadReference(Storage.GetLink(reference));

    private string ReadReference(IList<TLinkAddress> reference)
    {
        var referenceLink = new Link<TLinkAddress>(reference);
        if (!_equalityComparer.Equals(ReferenceMarker, referenceLink.Source))
        {
            throw new ArgumentException("The passed link is not a reference");
        }
        return UnicodeSequenceToStringConverter.Convert(referenceLink.Target);
    }

    public void CreateLinks(IList<LinoLink> links)
    {
        var sequenceList = new List<TLinkAddress>();
        for (int i = 0; i < links.Count; i++)
        {
            sequenceList.Add(CreateLink(links[i]));
        }
        Storage.GetOrCreate(DocumentMarker, _listToSequenceConverter.Convert(sequenceList));
    }

    private TLinkAddress CreateLink(LinoLink link)
    {
        if (link.Id != null)
        {
            return CreateLinkWithId(link);
        }
        var valuesSequence = CreateValuesSequence(link);
        return Storage.GetOrCreate(LinkWithoutIdMarker, valuesSequence);
    }

    private TLinkAddress CreateLinkWithId(LinoLink link)
    {
        var currentReference = GetOrCreateReference(link.Id);
        if (link.Values == null)
        {
            return Storage.GetOrCreate(LinkWithIdMarker, currentReference);
        }
        var valuesSequence = CreateValuesSequence(link);
        var idWithValues = Storage.GetOrCreate(currentReference, valuesSequence);
        return Storage.GetOrCreate(LinkWithIdMarker, idWithValues);
    }

    private TLinkAddress CreateValuesSequence(LinoLink parent)
    {
        var values = new List<TLinkAddress>(parent.Values.Count);
        for (int i = 0; i < parent.Values.Count; i++)
        {
            var currentValue = parent.Values[i];
            if (currentValue.Values != null)
            {
                var valueLink = CreateLink(currentValue);
                values.Add(valueLink);
                continue;
            }
            var currentValueReference = GetOrCreateReference(currentValue.Id);
            values.Add(currentValueReference);
        }
        return _listToSequenceConverter.Convert(values);
    }

    private TLinkAddress GetDocumentSequence()
    {
        var constants = Storage.Constants;
        var any = constants.Any;
        TLinkAddress documentLinksSequence = default;
        Storage.Each(new Link<TLinkAddress>(any, DocumentMarker, any), link =>
        {
            documentLinksSequence = Storage.GetTarget((IList<TLinkAddress>)link);
            return constants.Continue;
        });
        if (_equalityComparer.Equals(default, documentLinksSequence))
        {
            throw new Exception("No document links in the storage.");
        }
        return documentLinksSequence;
    }

    public IList<LinoLink> GetLinks()
    {
        var resultLinks = new List<LinoLink>();
        bool IsElement(TLinkAddress linkIndex)
        {
            var source = Storage.GetSource(linkIndex);
            return _equalityComparer.Equals(LinkWithoutIdMarker, source) | _equalityComparer.Equals(LinkWithIdMarker, source) || Storage.IsPartialPoint(linkIndex);
        }
        var rightSequenceWalker = new RightSequenceWalker<TLinkAddress>(Storage, new DefaultStack<TLinkAddress>(), IsElement);
        var documentLinksSequence = GetDocumentSequence();
        var documentLinks = rightSequenceWalker.Walk(documentLinksSequence);
        foreach (var documentLink in documentLinks)
        {
            resultLinks.Add(GetLink(documentLink));
        }
        return resultLinks;
    }

    private bool IsLinkOrReferenceOrPartialPoint(TLinkAddress linkIndex)
    {
        var link = Storage.GetLink(linkIndex);
        return IsLink(link) || IsReference(link) || Storage.IsPartialPoint(linkIndex);
    }

    private LinoLink GetLinkWithId(IList<TLinkAddress> linkWithId)
    {
        string id;
        var values = new List<LinoLink>();
        var linkStruct = new Link<TLinkAddress>(linkWithId);
        if (IsReference(linkStruct.Target))
        {
            id = ReadReference(linkStruct.Target);
            return new LinoLink(id);
        }
        var rightSequenceWalker = new RightSequenceWalker<TLinkAddress>(Storage, new DefaultStack<TLinkAddress>(), IsLinkOrReferenceOrPartialPoint);
        using var enumerator = rightSequenceWalker.Walk(linkStruct.Target).GetEnumerator();
        enumerator.MoveNext();
        id = ReadReference(enumerator.Current);
        while (enumerator.MoveNext())
        {
            var currentValueStruct = new Link<TLinkAddress>(Storage.GetLink(enumerator.Current));
            if (IsLink(currentValueStruct))
            {
                var value = GetLink(currentValueStruct);
                values.Add(value);
            }
            else if (IsReference(currentValueStruct))
            {
                var reference = ReadReference(currentValueStruct);
                var currentLinoLink = new LinoLink(reference);
                values.Add(currentLinoLink);
            }
        }
        return new LinoLink(id, values);
    }

    public LinoLink GetLinkWithoutId(IList<TLinkAddress> linkWithoudId)
    {
        var values = new List<LinoLink>();
        var linkStruct = new Link<TLinkAddress>(linkWithoudId);
        var rightSequenceWalker = new RightSequenceWalker<TLinkAddress>(Storage, new DefaultStack<TLinkAddress>(), IsLinkOrReferenceOrPartialPoint);
        foreach (var currentValue in rightSequenceWalker.Walk(linkStruct.Target))
        {
            var currentValueStruct = new Link<TLinkAddress>(Storage.GetLink(currentValue));
            if (IsLink(currentValue))
            {
                var value = GetLink(currentValueStruct);
                values.Add(value);
            }
            else if (IsReference(currentValueStruct))
            {
                var currentLinoLink = new LinoLink(ReadReference(currentValueStruct));
                values.Add(currentLinoLink);
            }
        }
        return new LinoLink(null, values);
    }

    public LinoLink GetLink(TLinkAddress link) => GetLink(Storage.GetLink(link));

    public LinoLink GetLink(IList<TLinkAddress> link)
    {
        var linkStruct = new Link<TLinkAddress>(link);
        if (IsLinkWithId(linkStruct))
        {
            return GetLinkWithId(linkStruct);
        }
        if (IsLinkWithoutId(linkStruct))
        {
            return GetLinkWithoutId(linkStruct);
        }
        throw new Exception("The passed argument is not a link");
    }
}
