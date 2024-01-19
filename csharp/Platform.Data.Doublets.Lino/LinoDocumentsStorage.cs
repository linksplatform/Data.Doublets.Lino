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

    public TLinkAddress DocumentType { get; }

    public TLinkAddress ReferenceType { get; }

    public TLinkAddress LinkWithoutIdType { get; }

    public TLinkAddress LinkWithIdType { get; }

    private readonly IConverter<IList<TLinkAddress>?, TLinkAddress> _listToSequenceConverter;

    public LinoDocumentsStorage(ILinks<TLinkAddress> storage, IConverter<IList<TLinkAddress>?, TLinkAddress> listToSequenceConverter)
    {
        Storage = storage;
        // Initializes constants
        var typeAddress = One;
        var meaningRoot = storage.GetOrCreate(typeAddress, typeAddress);
        var unicodeSymbolType = storage.GetOrCreate(meaningRoot, Arithmetic.Increment(ref typeAddress));
        var unicodeSequenceType = storage.GetOrCreate(meaningRoot, Arithmetic.Increment(ref typeAddress));
        DocumentType = storage.GetOrCreate(meaningRoot, Arithmetic.Increment(ref typeAddress));
        ReferenceType = storage.GetOrCreate(meaningRoot, Arithmetic.Increment(ref typeAddress));
        LinkWithoutIdType = storage.GetOrCreate(meaningRoot, Arithmetic.Increment(ref typeAddress));
        LinkWithIdType = storage.GetOrCreate(meaningRoot, Arithmetic.Increment(ref typeAddress));
        BalancedVariantConverter<TLinkAddress> balancedVariantConverter = new(storage);
        TargetMatcher<TLinkAddress> unicodeSymbolCriterionMatcher = new(storage, unicodeSymbolType);
        TargetMatcher<TLinkAddress> unicodeSequenceCriterionMatcher = new(storage, unicodeSequenceType);
        CharToUnicodeSymbolConverter<TLinkAddress> charToUnicodeSymbolConverter = new(storage, _addressToNumberConverter, unicodeSymbolType);
        UnicodeSymbolToCharConverter<TLinkAddress> unicodeSymbolToCharConverter = new(storage, _numberToAddressConverter, unicodeSymbolCriterionMatcher);
        StringToUnicodeSequenceConverter = new CachingConverterDecorator<string, TLinkAddress>(new StringToUnicodeSequenceConverter<TLinkAddress>(storage, charToUnicodeSymbolConverter, balancedVariantConverter, unicodeSequenceType));
        RightSequenceWalker<TLinkAddress> sequenceWalker = new(storage, new DefaultStack<TLinkAddress>(), unicodeSymbolCriterionMatcher.IsMatched);
        UnicodeSequenceToStringConverter = new CachingConverterDecorator<TLinkAddress, string>(new UnicodeSequenceToStringConverter<TLinkAddress>(storage, unicodeSequenceCriterionMatcher, sequenceWalker, unicodeSymbolToCharConverter, unicodeSequenceType));
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
        return _equalityComparer.Equals(LinkWithIdType, source);
    }

    private bool IsLinkWithoutId(IList<TLinkAddress> link)
    {
        var source = Storage.GetSource(link);
        return _equalityComparer.Equals(LinkWithoutIdType, source);
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
        return _equalityComparer.Equals(ReferenceType, source);
    }


    private TLinkAddress GetOrCreateReference(string content)
    {
        var sequence = StringToUnicodeSequenceConverter.Convert(content);
        return Storage.GetOrCreate(ReferenceType, sequence);
    }

    private string ReadReference(TLinkAddress reference) => ReadReference(Storage.GetLink(reference));

    private string ReadReference(IList<TLinkAddress> reference)
    {
        var referenceLink = new Link<TLinkAddress>(reference);
        if (!_equalityComparer.Equals(ReferenceType, referenceLink.Source))
        {
            throw new ArgumentException("The passed link is not a reference");
        }
        return UnicodeSequenceToStringConverter.Convert(referenceLink.Target);
    }

    public void CreateLinks(IList<LinoLink> links, string? documentName)
    {
        if (string.IsNullOrEmpty(documentName))
        {
            throw new Exception("No document name is passed.");
        }
        var sequenceList = new List<TLinkAddress>();
        for (int i = 0; i < links.Count; i++)
        {
            sequenceList.Add(CreateLink(links[i]));
        }
        var documentNameSequence = StringToUnicodeSequenceConverter.Convert(documentName);
        var document = Storage.SearchOrDefault(documentNameSequence, documentNameSequence);
        if (!_equalityComparer.Equals(default, document))
        {
            throw new Exception($"The document with name {documentName} already exists.");
        }
        document = Storage.CreateAndUpdate(DocumentType, documentNameSequence);
        Storage.GetOrCreate(document, _listToSequenceConverter.Convert(sequenceList));
    }

    private TLinkAddress CreateLink(LinoLink link)
    {
        if (link.Id != null)
        {
            return CreateLinkWithId(link);
        }
        var valuesSequence = CreateValuesSequence(link);
        return Storage.GetOrCreate(LinkWithoutIdType, valuesSequence);
    }

    private TLinkAddress CreateLinkWithId(LinoLink link)
    {
        var currentReference = GetOrCreateReference(link.Id);
        if (link.Values == null)
        {
            return Storage.GetOrCreate(LinkWithIdType, currentReference);
        }
        var valuesSequence = CreateValuesSequence(link);
        var idWithValues = Storage.GetOrCreate(currentReference, valuesSequence);
        return Storage.GetOrCreate(LinkWithIdType, idWithValues);
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

    private TLinkAddress GetDocument(string documentName)
    {
        var documentNameSequence = StringToUnicodeSequenceConverter.Convert(documentName);
        var document = Storage.SearchOrDefault(DocumentType, documentNameSequence);
        if (_equalityComparer.Equals(default, document))
        {
            throw new Exception($"No document in the storage with name {documentName}.");
        }
        return document;
    }

    private TLinkAddress GetDocumentSequence(string documentName)
    {
        var document = GetDocument(documentName);
        return GetDocumentSequence(document);
    }

    private TLinkAddress GetDocumentSequence(TLinkAddress document)
    {
        var any = Storage.Constants.Any;
        TLinkAddress documentLinksSequence = default;
        Storage.Each(new Link<TLinkAddress>(any, document, any), link =>
        {
            documentLinksSequence = Storage.GetTarget(link);
            return Storage.Constants.Break;
        });
        if (_equalityComparer.Equals(default, documentLinksSequence))
        {
            throw new Exception("No links assosiated with the passed document in the storage.");
        }
        return documentLinksSequence;
    }

    public IList<LinoLink> GetLinks(string? documentName)
    {
        if (string.IsNullOrEmpty(documentName))
        {
            throw new Exception("No document name is passed.");
        }
        var resultLinks = new List<LinoLink>();
        bool IsElement(TLinkAddress linkIndex)
        {
            var source = Storage.GetSource(linkIndex);
            return _equalityComparer.Equals(LinkWithoutIdType, source) | _equalityComparer.Equals(LinkWithIdType, source) || Storage.IsPartialPoint(linkIndex);
        }
        var document = GetDocument(documentName);
        var rightSequenceWalker = new RightSequenceWalker<TLinkAddress>(Storage, new DefaultStack<TLinkAddress>(), IsElement);
        var documentLinksSequence = GetDocumentSequence(document);
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

    private LinoLink GetLinkWithoutId(IList<TLinkAddress> linkWithoudId)
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

    private LinoLink GetLink(TLinkAddress link) => GetLink(Storage.GetLink(link));

    private LinoLink GetLink(IList<TLinkAddress> link)
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
