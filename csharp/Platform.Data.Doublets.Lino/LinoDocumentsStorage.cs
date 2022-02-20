using System;
using System.Collections.Generic;
using System.Linq;
using Platform.Collections.Stacks;
using Platform.Converters;
using Platform.Data.Doublets.CriterionMatchers;
using Platform.Data.Doublets.Numbers.Rational;
using Platform.Data.Doublets.Numbers.Raw;
using Platform.Data.Doublets.Sequences;
using Platform.Data.Doublets.Sequences.Converters;
using Platform.Data.Doublets.Sequences.HeightProviders;
using Platform.Data.Doublets.Sequences.Walkers;
using Platform.Data.Doublets.Unicode;
using Platform.Data.Numbers.Raw;
using Platform.Numbers;
using LinoLink = Platform.Communication.Protocol.Lino.Link;

namespace Platform.Data.Doublets.Lino;

public class LinoDocumentsStorage<TLinkAddress> : ILinoStorage<TLinkAddress> where TLinkAddress : struct
{
        private readonly TLinkAddress _any;
        private static readonly TLinkAddress Zero = default;
        private static readonly TLinkAddress One = Arithmetic.Increment(Zero);

        // public readonly IConverter<IList<TLinkAddress>?, TLinkAddress> ListToSequenceConverter;
        private readonly TLinkAddress MeaningRoot;
        private readonly EqualityComparer<TLinkAddress> _equalityComparer = EqualityComparer<TLinkAddress>.Default;
        // Converters that are able to convert link's address (UInt64 value) to a raw number represented with another UInt64 value and back
        public readonly RawNumberToAddressConverter<TLinkAddress> NumberToAddressConverter = new();
        public readonly AddressToRawNumberConverter<TLinkAddress> AddressToNumberConverter = new();
        // Converters between BigInteger and raw number sequence
        public readonly BigIntegerToRawNumberSequenceConverter<TLinkAddress> BigIntegerToRawNumberSequenceConverter;
        public readonly RawNumberSequenceToBigIntegerConverter<TLinkAddress> RawNumberSequenceToBigIntegerConverter;
        // Converters between decimal and rational number sequence
        public readonly DecimalToRationalConverter<TLinkAddress> DecimalToRationalConverter;
        public readonly RationalToDecimalConverter<TLinkAddress> RationalToDecimalConverter;
        public readonly DefaultSequenceRightHeightProvider<TLinkAddress> DefaultSequenceRightHeightProvider;
        public readonly DefaultSequenceAppender<TLinkAddress> DefaultSequenceAppender;
        public ILinks<TLinkAddress> Storage { get; }

        public IConverter<string, TLinkAddress> StringToUnicodeSequenceConverter { get; }
        public IConverter<TLinkAddress, string> UnicodeSequenceToStringConverter { get; }
        public TLinkAddress DocumentMarker { get; }

        public TLinkAddress ReferenceMarker { get; }
        public TLinkAddress LinkWithoutIdMarker { get; }
        public TLinkAddress LinkWithIdMarker { get; }
        private TLinkAddress _markerIndex { get; set; }

        private IConverter<IList<TLinkAddress>?, TLinkAddress> _listToSequenceConverter;

        public LinoDocumentsStorage(ILinks<TLinkAddress> storage, IConverter<IList<TLinkAddress>?, TLinkAddress> listToSequenceConverter)
        {
            Storage = storage;
            // ListToSequenceConverter = listToSequenceConverter;
            // Initializes constants
            _any = storage.Constants.Any;
            var markerIndex = One;
            MeaningRoot = storage.GetOrCreate(markerIndex, markerIndex);
            var unicodeSymbolMarker = storage.GetOrCreate(MeaningRoot, Arithmetic.Increment(ref markerIndex));
            var unicodeSequenceMarker = storage.GetOrCreate(MeaningRoot, Arithmetic.Increment(ref markerIndex));
            DocumentMarker = storage.GetOrCreate(MeaningRoot, Arithmetic.Increment(ref markerIndex));
            ReferenceMarker = storage.GetOrCreate(MeaningRoot, Arithmetic.Increment(ref markerIndex));
            LinkWithoutIdMarker = storage.GetOrCreate(MeaningRoot, Arithmetic.Increment(ref markerIndex));
            LinkWithIdMarker = storage.GetOrCreate(MeaningRoot, Arithmetic.Increment(ref markerIndex));
            BalancedVariantConverter<TLinkAddress> balancedVariantConverter = new(storage);
            TargetMatcher<TLinkAddress> unicodeSymbolCriterionMatcher = new(storage, unicodeSymbolMarker);
            TargetMatcher<TLinkAddress> unicodeSequenceCriterionMatcher = new(storage, unicodeSequenceMarker);
            CharToUnicodeSymbolConverter<TLinkAddress> charToUnicodeSymbolConverter =
                new(storage, AddressToNumberConverter, unicodeSymbolMarker);
            UnicodeSymbolToCharConverter<TLinkAddress> unicodeSymbolToCharConverter =
                new(storage, NumberToAddressConverter, unicodeSymbolCriterionMatcher);
            StringToUnicodeSequenceConverter = new CachingConverterDecorator<string, TLinkAddress>(
                new StringToUnicodeSequenceConverter<TLinkAddress>(storage, charToUnicodeSymbolConverter,
                    balancedVariantConverter, unicodeSequenceMarker));
            RightSequenceWalker<TLinkAddress> sequenceWalker =
                new(storage, new DefaultStack<TLinkAddress>(), unicodeSymbolCriterionMatcher.IsMatched);
            UnicodeSequenceToStringConverter = new CachingConverterDecorator<TLinkAddress, string>(
                new UnicodeSequenceToStringConverter<TLinkAddress>(storage, unicodeSequenceCriterionMatcher, sequenceWalker,
                    unicodeSymbolToCharConverter));
            DecimalToRationalConverter = new(storage, BigIntegerToRawNumberSequenceConverter);
            RationalToDecimalConverter = new(storage, RawNumberSequenceToBigIntegerConverter);
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
            var source = Storage.GetSource(reference);
            return _equalityComparer.Equals(ReferenceMarker, source);
        }

        public TLinkAddress GetOrCreateReference(string content)
        {
            var sequence = StringToUnicodeSequenceConverter.Convert(content);
            return Storage.GetOrCreate(ReferenceMarker, sequence);
        }

        public string ReadReference(TLinkAddress reference) => ReadReference(Storage.GetLink(reference));

        public string ReadReference(IList<TLinkAddress> reference)
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

        public TLinkAddress CreateLink(LinoLink link)
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
            TLinkAddress currentReference = GetOrCreateReference(link.Id);
            if (link.Values == null)
            {
                return Storage.GetOrCreate(LinkWithIdMarker, currentReference);
            }
            var valuesSequence = CreateValuesSequence(link);
            var idWithValues = Storage.GetOrCreate(currentReference, valuesSequence);
            return Storage.GetOrCreate(LinkWithIdMarker, idWithValues);
        }

        public TLinkAddress CreateValuesSequence(LinoLink parent)
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
            var any = Storage.Constants.Any;
            bool IsElement(TLinkAddress linkIndex)
            {
                var source = Storage.GetSource(linkIndex);
                return _equalityComparer.Equals(LinkWithoutIdMarker, source) |  _equalityComparer.Equals(LinkWithIdMarker, source) || Storage.IsPartialPoint(linkIndex);
            }
            var rightSequenceWalker = new RightSequenceWalker<TLinkAddress>(Storage, new DefaultStack<TLinkAddress>(), IsElement);
            TLinkAddress documentLinksSequence = GetDocumentSequence();
            var documentLinks = rightSequenceWalker.Walk(documentLinksSequence);
            foreach (var documentLink in documentLinks)
            {
                resultLinks.Add(GetLink(documentLink));
            }
            return resultLinks;
        }

        public LinoLink GetLink(TLinkAddress link) => GetLink(Storage.GetLink(link));

        public LinoLink GetLink(IList<TLinkAddress> link)
        {
            string id = default;
            var values = new List<LinoLink>();
            var linkStruct = new Link<TLinkAddress>(link);
            if (!IsLink(linkStruct.Index))
            {
                throw new Exception("The source of the passed link is not the link marker.");
            }
            if (IsLinkWithId(linkStruct.Index))
            {
                if (IsReference(linkStruct.Target))
                {
                    id = ReadReference(linkStruct.Target);
                    return new LinoLink(id);
                }
                var rightSequenceWalker = new RightSequenceWalker<TLinkAddress>(Storage, new DefaultStack<TLinkAddress>(), IsElement);
                using var enumerator = rightSequenceWalker.Walk(linkStruct.Target).GetEnumerator();
                enumerator.MoveNext();
                id = ReadReference(enumerator.Current);
                enumerator.MoveNext();
                for (var currentValue = enumerator.Current; enumerator.MoveNext(); currentValue = enumerator.Current)
                {
                    var currentValueStruct = new Link<TLinkAddress>(Storage.GetLink(currentValue));
                    if (IsLink(currentValue))
                    {
                        var value = GetLink(currentValueStruct);
                        values.Add(value);
                    }
                    else if (IsReference(currentValueStruct.Index))
                    {
                        var reference = ReadReference(currentValueStruct);
                        var currentLinoLink = new LinoLink(reference);
                        values.Add(currentLinoLink);
                    }
                }
            }
            if (IsLinkWithoutId(linkStruct.Index))
            {
                var rightSequenceWalker = new RightSequenceWalker<TLinkAddress>(Storage, new DefaultStack<TLinkAddress>(), IsElement);
                foreach (var currentValue in rightSequenceWalker.Walk(linkStruct.Target))
                {
                    var currentValueStruct = new Link<TLinkAddress>(Storage.GetLink(currentValue));
                    if (IsLink(currentValue))
                    {
                        var value = GetLink(currentValueStruct);
                        values.Add(value);
                    }
                    else if (IsReference(currentValueStruct.Index))
                    {
                        var currentLinoLink = new LinoLink(ReadReference(currentValueStruct));
                        values.Add(currentLinoLink);
                    }
                }
            }
            bool IsElement(TLinkAddress linkIndex)
            {
                var source = Storage.GetSource(linkIndex);
                return _equalityComparer.Equals(LinkWithoutIdMarker, source) || _equalityComparer.Equals(LinkWithIdMarker, source) || _equalityComparer.Equals(ReferenceMarker, source) || Storage.IsPartialPoint(linkIndex);
            }
            return new LinoLink(id, values);
        }
}
