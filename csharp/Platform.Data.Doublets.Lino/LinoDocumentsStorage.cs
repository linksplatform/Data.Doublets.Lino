using System;
using System.Collections.Generic;
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
        public TLinkAddress LinkWithoutMarker { get; }
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
            LinkWithoutMarker = storage.GetOrCreate(MeaningRoot, Arithmetic.Increment(ref markerIndex));
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

        private bool IsLinkWithId(TLinkAddress linkAddress)
        {
            var source = Storage.GetSource(linkAddress);
            return _equalityComparer.Equals(LinkWithIdMarker, source);
        }

        private bool IsLinkWithoutId(TLinkAddress linkAddress)
        {
            var source = Storage.GetSource(linkAddress);
            return _equalityComparer.Equals(LinkWithoutMarker, source);
        }

        private bool IsLink(TLinkAddress linkAddress) => IsLinkWithId(linkAddress) || IsLinkWithoutId(linkAddress);

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
            return Storage.GetOrCreate(LinkWithoutMarker, valuesSequence);
        }

        private TLinkAddress CreateLinkWithId(LinoLink link)
        {
            TLinkAddress currentReference = GetOrCreateReference(link.Id);
            if (link.Values == null)
            {
                return currentReference;
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

        public IList<LinoLink> GetLinks()
        {
            var resultLinks = new List<LinoLink>();
            var any = Storage.Constants.Any;
            bool IsElement(TLinkAddress linkIndex)
            {
                var source = Storage.GetSource(linkIndex);
                return _equalityComparer.Equals(LinkWithoutMarker, source) |  _equalityComparer.Equals(LinkWithIdMarker, source) || Storage.IsPartialPoint(linkIndex);
            }
            var rightSequenceWalker = new RightSequenceWalker<TLinkAddress>(Storage, new DefaultStack<TLinkAddress>(), IsElement);
            TLinkAddress linksSequence = default;
            Storage.Each(DocumentMarker, any, link =>
            {
                linksSequence = Storage.GetTarget((IList<TLinkAddress>)link);
                return Storage.Constants.Continue;
            });
            if (_equalityComparer.Equals(default, linksSequence))
            {
                throw new Exception("No one link in storage.");
            }
            var sequence = rightSequenceWalker.Walk(linksSequence);
            foreach (var documentLink in sequence)
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
            var isLink = _equalityComparer.Equals(LinkWithoutMarker, linkStruct.Source) || _equalityComparer.Equals(LinkWithIdMarker, linkStruct.Source);
            if (!isLink)
            {
                throw new Exception("The source of the passed link is not the link marker.");
            }
            bool IsElement(TLinkAddress linkIndex)
            {
                var source = Storage.GetSource(linkIndex);
                return _equalityComparer.Equals(LinkWithoutMarker, source) || _equalityComparer.Equals(LinkWithIdMarker, source) || _equalityComparer.Equals(ReferenceMarker, source) || Storage.IsPartialPoint(linkIndex);
            }
            var rightSequenceWalker = new RightSequenceWalker<TLinkAddress>(Storage, new DefaultStack<TLinkAddress>(), IsElement);
            foreach (var currentLink in rightSequenceWalker.Walk(linkStruct.Target))
            {
                var currentLinkStruct = new Link<TLinkAddress>(Storage.GetLink(currentLink));
                if (_equalityComparer.Equals(LinkWithoutMarker, currentLinkStruct.Source) || _equalityComparer.Equals(LinkWithIdMarker, currentLinkStruct.Source))
                {
                    var value = GetLink(currentLinkStruct);
                    values.Add(value);
                }
                else if (_equalityComparer.Equals(ReferenceMarker, currentLinkStruct.Source))
                {
                    if (default == id)
                    {
                        id = ReadReference(currentLinkStruct);
                    }
                    else
                    {
                        var currentLinoLink = new LinoLink(ReadReference(currentLinkStruct));
                        values.Add(currentLinoLink);
                    }
                }

            }
            return new LinoLink(id, values);
        }
}
