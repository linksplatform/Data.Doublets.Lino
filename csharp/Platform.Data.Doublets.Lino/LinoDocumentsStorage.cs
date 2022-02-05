using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Platform.Collections;
using Platform.Collections.Stacks;
using Platform.Communication.Protocol.Lino;
using Platform.Converters;
using Platform.Data;
using Platform.Data.Doublets;
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

namespace DefaultNamespace;

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
        public TLinkAddress LinkMarker { get; }
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
            LinkMarker = storage.GetOrCreate(MeaningRoot, Arithmetic.Increment(ref markerIndex));
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

        public TLinkAddress GetOrCreateReferenceLink(string content) => Storage.GetOrCreate(ReferenceMarker, StringToUnicodeSequenceConverter.Convert(content));

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

        public bool IsReference(TLinkAddress reference) => IsReference(Storage.GetLink(reference));

        public bool IsReference(IList<TLinkAddress> reference) => _equalityComparer.Equals(ReferenceMarker, Storage.GetSource(reference));

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
            TLinkAddress currentReference = GetOrCreateReferenceLink(link.Id);
            if (link.Values == null)
            {
                return currentReference;
            }
            var valuesSequence = CreateValuesSequence(link);
            var idWithValues = Storage.GetOrCreate(currentReference, valuesSequence);
            return Storage.GetOrCreate(LinkMarker, idWithValues);
        }

        public TLinkAddress CreateValuesSequence(LinoLink parent)
        {
            var valueReferences = new List<TLinkAddress>(parent.Values.Count);
            for (int i = 0; i < parent.Values.Count; i++)
            {
                var currentValue = parent.Values[i];
                var currentValueReference = GetOrCreateReferenceLink(currentValue.Id);
                valueReferences.Add(currentValueReference);
            }
            return _listToSequenceConverter.Convert(valueReferences);
        }

        public IList<LinoLink> GetLinks()
        {
            var any = Storage.Constants.Any;
            var rightSequenceWalker = new RightSequenceWalker<TLinkAddress>(Storage, new DefaultStack<TLinkAddress>(), linkIndex => _equalityComparer.Equals(ReferenceMarker, Storage.GetSource(linkIndex)));
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

            }
            return default;
        }
}
