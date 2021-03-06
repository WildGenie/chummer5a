/*  This file is part of Chummer5a.
 *
 *  Chummer5a is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Chummer5a is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Chummer5a.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  You can obtain the full source code for Chummer5a at
 *  https://github.com/chummer5a/chummer5a
 */
using Chummer.Annotations;
using Chummer.Backend.Equipment;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;

namespace Chummer.Backend.Attributes
{
    /// <summary>
    /// Character CharacterAttribute. 
    /// If using databinding, you should generally be using AttributeSection.{ATT}Binding
    /// </summary>
    [DebuggerDisplay("{" + nameof(_strAbbrev) + "}")]
    public class CharacterAttrib : INotifyPropertyChanged
    {
        private int _intMetatypeMin = 1;
        private int _intMetatypeMax = 6;
        private int _intMetatypeAugMax = 10;
        private int _intAugModifier;
        private int _intBase;
        private int _intKarma;
        private string _strAbbrev;
        private readonly Character _objCharacter;
		private AttributeCategory _enumCategory;
		private AttributeCategory _enumMetatypeCategory;

		public event PropertyChangedEventHandler PropertyChanged;

		#region Constructor, Save, Load, and Print Methods

		/// <summary>
		/// Character CharacterAttribute.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="abbrev"></param>
		/// <param name="enumCategory"></param>
		public CharacterAttrib(Character character, string abbrev, AttributeCategory enumCategory = AttributeCategory.Standard)
        {
	        _strAbbrev = abbrev;
	        MetatypeCategory = enumCategory;
	        _objCharacter = character;
			_objCharacter.AttributeImprovementEvent += OnImprovementEvent;
			_objCharacter.PropertyChanged += OnCharacterChanged;
		}

        public void UnbindAttribute()
        {
            _objCharacter.AttributeImprovementEvent -= OnImprovementEvent;
            _objCharacter.PropertyChanged -= OnCharacterChanged;
        }

        /// <summary>
        /// Save the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        public void Save(XmlTextWriter objWriter)
        {
            objWriter.WriteStartElement("attribute");
            objWriter.WriteElementString("name", _strAbbrev);
            objWriter.WriteElementString("metatypemin", _intMetatypeMin.ToString());
            objWriter.WriteElementString("metatypemax", _intMetatypeMax.ToString());
            objWriter.WriteElementString("metatypeaugmax", _intMetatypeAugMax.ToString());
            objWriter.WriteElementString("base", _intBase.ToString());
            objWriter.WriteElementString("karma", _intKarma.ToString());
            objWriter.WriteElementString("augmodifier", _intAugModifier.ToString());
			objWriter.WriteElementString("metatypecategory", MetatypeCategory.ToString());
            // External reader friendly stuff.
            objWriter.WriteElementString("totalvalue", TotalValue.ToString());
            objWriter.WriteEndElement();
        }

        /// <summary>
        /// Load the CharacterAttribute from the XmlNode.
        /// </summary>
        /// <param name="objNode">XmlNode to load.</param>
        public void Load(XmlNode objNode)
        {
            objNode.TryGetStringFieldQuickly("name", ref _strAbbrev);
            objNode.TryGetInt32FieldQuickly("metatypemin", ref _intMetatypeMin);
            objNode.TryGetInt32FieldQuickly("metatypemax", ref _intMetatypeMax);
            objNode.TryGetInt32FieldQuickly("metatypeaugmax", ref _intMetatypeAugMax);
            objNode.TryGetInt32FieldQuickly("base", ref _intBase);
            objNode.TryGetInt32FieldQuickly("karma", ref _intKarma);
            if (!BaseUnlocked)
			{
				_intBase = 0;
			}
			//Converts old attributes to split metatype minimum and base. Saves recalculating Base - TotalMinimum all the time.
            int i = 0;
			if (objNode.TryGetInt32FieldQuickly("value", ref i))
			{
				i -= _intMetatypeMin;
				if (BaseUnlocked)
				{
					_intBase = Math.Max(_intBase - _intMetatypeMin, 0);
					i -= _intBase;
				}
				if (i > 0)
				{
					_intKarma = i;
				}
			}

            int intCreateKarma = 0;
            // Shim for that one time karma was split into career and create values
            if (objNode.TryGetInt32FieldQuickly("createkarma", ref intCreateKarma))
            {
                _intKarma += intCreateKarma;
            }
            if (_intBase < 0)
                _intBase = 0;
            if (_intKarma < 0)
                _intKarma = 0;
            _enumMetatypeCategory = ConvertToAttributeCategory(objNode["category"]?.InnerText);
			_enumCategory = ConvertToAttributeCategory(Abbrev);
	        _enumMetatypeCategory = ConvertToMetatypeAttributeCategory(objNode["metatypecategory"]?.InnerText ?? "Standard");
            objNode.TryGetInt32FieldQuickly("augmodifier", ref _intAugModifier);
        }

        /// <summary>
        /// Print the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        /// <param name="objCulture">Culture in which to print.</param>
        /// <param name="strLanguageToPrint">Language in which to print.</param>
        public void Print(XmlTextWriter objWriter, CultureInfo objCulture, string strLanguageToPrint)
        {
            if (Abbrev == "MAGAdept" && (!_objCharacter.Options.MysAdeptSecondMAGAttribute || !_objCharacter.IsMysticAdept))
                return;
            objWriter.WriteStartElement("attribute");
            objWriter.WriteElementString("name_english", Abbrev);
            objWriter.WriteElementString("name", GetDisplayAbbrev(strLanguageToPrint));
            objWriter.WriteElementString("base", Value.ToString(objCulture));
            objWriter.WriteElementString("total", TotalValue.ToString(objCulture));
            objWriter.WriteElementString("min", TotalMinimum.ToString(objCulture));
            objWriter.WriteElementString("max", TotalMaximum.ToString(objCulture));
            objWriter.WriteElementString("aug", TotalAugmentedMaximum.ToString(objCulture));
			objWriter.WriteElementString("bp", TotalKarmaCost.ToString(objCulture));
			objWriter.WriteElementString("metatypecategory", MetatypeCategory.ToString());
			objWriter.WriteEndElement();
        }
        #endregion
        /// <summary>
        /// Type of Attribute.
        /// </summary>
        public enum AttributeCategory
        {
            Standard = 0,
            Special,
            Shapeshifter
        }

        #region Properties

        public Character CharacterObject => _objCharacter;

        public AttributeCategory Category
	    {
		    get => _enumCategory;
            set => _enumCategory = value;
        }

		public AttributeCategory MetatypeCategory
		{
			get => _enumMetatypeCategory;
		    set => _enumMetatypeCategory = value;
		}

		/// <summary>
		/// Minimum value for the CharacterAttribute as set by the character's Metatype.
		/// </summary>
		public int MetatypeMinimum
        {
            get
            {
                int intReturn = _intMetatypeMin;
                Improvement objImprovement = _objCharacter.Improvements.LastOrDefault(x => x.ImproveType == Improvement.ImprovementType.ReplaceAttribute && x.ImprovedName == Abbrev && x.Enabled);
                if (objImprovement != null)
                {
                    intReturn = objImprovement.Minimum;
                }
                return intReturn;
            }
            set
            {
                if (value != _intMetatypeMin)
                {
                    _intMetatypeMin = value;
                    OnPropertyChanged(nameof(TotalMinimum));
                }
            }
        }

        /// <summary>
        /// Maximum value for the CharacterAttribute as set by the character's Metatype.
        /// </summary>
        public int MetatypeMaximum
        {
            get
            {
                int intReturn = _intMetatypeMax;
                Improvement objImprovement = _objCharacter.Improvements.LastOrDefault(x => x.ImproveType == Improvement.ImprovementType.ReplaceAttribute && x.ImprovedName == Abbrev && x.Enabled);
                if (objImprovement != null)
                {
                    intReturn = objImprovement.Maximum;
                }
                return intReturn;
            }
            set
            {
                if (value != _intMetatypeMax)
                {
                    _intMetatypeMax = value;
                    OnPropertyChanged(nameof(TotalMaximum));
                }
            }
        }

        /// <summary>
        /// Maximum augmented value for the CharacterAttribute as set by the character's Metatype.
        /// </summary>
        public int MetatypeAugmentedMaximum
        {
            get => _intMetatypeAugMax;
            set
            {
                if (value != _intMetatypeAugMax)
                {
                    _intMetatypeAugMax = value;
                    OnPropertyChanged(nameof(TotalAugmentedMaximum));
                }
            }
        }

        /// <summary>
        /// Current base value (priority points spent) of the CharacterAttribute.
        /// </summary>
        public int Base
        {
            get => _intBase;
            set
            {
                if (value != _intBase)
                {
                    _intBase = value;
                    OnPropertyChanged(nameof(Base));
                }
            }
        }

        /// <summary>
        /// Total Value of Base Points as used by internal methods
        /// </summary>
        public int TotalBase => Math.Max(Base + FreeBase + RawMinimum, TotalMinimum);

        public int FreeBase => Math.Min(ImprovementManager.ValueOf(_objCharacter, Improvement.ImprovementType.Attributelevel, false, Abbrev), MetatypeMaximum - MetatypeMinimum);

        /// <summary>
        /// Current karma value of the CharacterAttribute.
        /// </summary>
        public int Karma
        {
            get => _intKarma;
            set
            {
                if (value != _intKarma)
                {
                    _intKarma = value;
                    OnPropertyChanged(nameof(Karma));
                }
            }
        }

        /// <summary>
        /// Current value of the CharacterAttribute before modifiers are applied.
        /// </summary>
        public int Value => Math.Min(Math.Max(Base + FreeBase + RawMinimum + AttributeValueModifiers, TotalMinimum) + Karma, TotalMaximum);

        /// <summary>
        /// Total Maximum value of the CharacterAttribute before essence modifiers are applied.
        /// </summary>
        public int MaximumNoEssenceLoss
        {
            get
            {
                // If we're looking at MAG and the character is a Cyberzombie, MAG is always 1, regardless of ESS penalties and bonuses.
                if (_objCharacter.MetatypeCategory == "Cyberzombie" && (Abbrev == "MAG" || Abbrev == "MAGAdept"))
                {
                    return 1;
                }

                int intRawMinimum = MetatypeMinimum;
                int intRawMaximum = MetatypeMaximum;
                foreach (Improvement objImprovement in _objCharacter.Improvements)
                {
                    if (objImprovement.ImproveType == Improvement.ImprovementType.Attribute &&
                        (objImprovement.ImprovedName == Abbrev || objImprovement.ImprovedName == Abbrev + "Base") &&
                        objImprovement.ImproveSource != Improvement.ImprovementSource.EssenceLoss && objImprovement.ImproveSource != Improvement.ImprovementSource.EssenceLossChargen &&
                        objImprovement.Enabled)
                    {
                        intRawMinimum += objImprovement.Minimum * objImprovement.Rating;
                        intRawMaximum += objImprovement.Maximum * objImprovement.Rating;
                    }
                }

                int intTotalMinimum = intRawMinimum;
                int intTotalMaximum = intRawMaximum;

                if (intTotalMinimum < 1)
                {
                    if (_objCharacter.IsCritter || _intMetatypeMax == 0 || Abbrev == "EDG" || Abbrev == "MAG" || Abbrev == "MAGAdept" || Abbrev == "RES" || Abbrev == "DEP")
                        intTotalMinimum = 0;
                    else
                        intTotalMinimum = 1;
                }
                if (intTotalMaximum < intTotalMinimum)
                    intTotalMaximum = intTotalMinimum;

                return intTotalMaximum;
            }
        }

        /// <summary>
        /// Formatted Value of the attribute, including the sum of any modifiers in brackets.
        /// </summary>
        public string DisplayValue => HasModifiers ? $"{Value} ({TotalValue})" : $"{Value}";

        /// <summary>
        /// Augmentation modifier value for the CharacterAttribute.
        /// </summary>
        /// <remarks>This value should not be saved with the character information. It should instead be re-calculated every time the character is loaded and augmentations are added/removed.</remarks>
        public int AugmentModifier
        {
            get => _intAugModifier;
            set
            {
                if (value != _intAugModifier)
                {
                    _intAugModifier = value;
                    OnPropertyChanged(nameof(Augmented));
                }
            }
        }

        /// <summary>
        /// The CharacterAttribute's total value including augmentations.
        /// </summary>
        /// <remarks>This value should not be saved with the character information. It should instead be re-calculated every time the character is loaded and augmentations are added/removed.</remarks>
        public int Augmented => Value + AugmentModifier;

        private int _intCachedAttributeModifiers = int.MinValue;
        /// <summary>
        /// The total amount of the modifiers that affect the CharacterAttribute's value without affecting Karma costs.
        /// </summary>
        public int AttributeModifiers
        {
            get
            {
                if (_intCachedAttributeModifiers != int.MinValue)
                    return _intCachedAttributeModifiers;
                HashSet<string> lstUniqueName = new HashSet<string>();
                HashSet<Tuple<string, int>> lstUniquePair = new HashSet<Tuple<string, int>>();
                int intModifier = 0;
                foreach (Improvement objImprovement in _objCharacter.Improvements
                    .Where(objImprovement => objImprovement.Enabled
                    && !objImprovement.Custom && objImprovement.ImproveType == Improvement.ImprovementType.Attribute
                    && objImprovement.ImprovedName == Abbrev && string.IsNullOrEmpty(objImprovement.Condition)))
                {
                    string strUniqueName = objImprovement.UniqueName;
                    if (!string.IsNullOrEmpty(strUniqueName))
                    {
                        // If this has a UniqueName, run through the current list of UniqueNames seen. If it is not already in the list, add it.
                        if (!lstUniqueName.Contains(strUniqueName))
                            lstUniqueName.Add(strUniqueName);

                        // Add the values to the UniquePair List so we can check them later.
                        lstUniquePair.Add(new Tuple<string, int>(strUniqueName, objImprovement.Augmented * objImprovement.Rating));
                    }
                    else
                    {
                        intModifier += objImprovement.Augmented * objImprovement.Rating;
                    }
                }

                if (lstUniqueName.Contains("precedence0"))
                {
                    // Retrieve only the highest precedence0 value.
                    // Run through the list of UniqueNames and pick out the highest value for each one.
                    int intHighest = int.MinValue;
                    foreach (Tuple<string, int> strValues in lstUniquePair)
                    {
                        if (strValues.Item1 == "precedence0")
                        {
                            if (strValues.Item2 > intHighest)
                                intHighest = strValues.Item2;
                        }
                    }
                    if (lstUniqueName.Contains("precedence-1"))
                    {
                        foreach (Tuple<string, int> strValues in lstUniquePair)
                        {
                            if (strValues.Item1 == "precedence-1")
                            {
                                intHighest += strValues.Item2;
                            }
                        }
                    }
                    intModifier = Math.Max(intHighest, intModifier);
                }
                else if (lstUniqueName.Contains("precedence1"))
                {
                    // Retrieve all of the items that are precedence1 and nothing else.
                    int intTmpModifier = 0;
                    foreach (Tuple<string, int> strValues in lstUniquePair)
                    {
                        if (strValues.Item1 == "precedence1" || strValues.Item1 == "precedence-1")
                            intTmpModifier += strValues.Item2;
                    }
                    intModifier = Math.Max(intTmpModifier, intModifier);
                }
                else
                {
                    // Run through the list of UniqueNames and pick out the highest value for each one.
                    foreach (string strName in lstUniqueName)
                    {
                        int intHighest = int.MinValue;
                        foreach (Tuple<string, int> strValues in lstUniquePair)
                        {
                            if (strValues.Item1 == strName)
                            {
                                if (strValues.Item2 > intHighest)
                                    intHighest = strValues.Item2;
                            }
                        }
                        if (intHighest != int.MinValue)
                            intModifier += intHighest;
                    }
                }

                // Factor in Custom Improvements.
                lstUniqueName.Clear();
                lstUniquePair.Clear();
                int intCustomModifier = 0;
                foreach (Improvement objImprovement in _objCharacter.Improvements)
                {
                    if (objImprovement.Enabled && objImprovement.Custom && objImprovement.ImproveType == Improvement.ImprovementType.Attribute && objImprovement.ImprovedName == Abbrev && string.IsNullOrEmpty(objImprovement.Condition))
                    {
                        string strUniqueName = objImprovement.UniqueName;
                        if (!string.IsNullOrEmpty(strUniqueName))
                        {
                            // If this has a UniqueName, run through the current list of UniqueNames seen. If it is not already in the list, add it.
                            if (!lstUniqueName.Contains(strUniqueName))
                                lstUniqueName.Add(strUniqueName);

                            // Add the values to the UniquePair List so we can check them later.
                            lstUniquePair.Add(new Tuple<string, int>(strUniqueName, objImprovement.Augmented * objImprovement.Rating));
                        }
                        else
                        {
                            intCustomModifier += objImprovement.Augmented * objImprovement.Rating;
                        }
                    }
                }

                // Run through the list of UniqueNames and pick out the highest value for each one.
                foreach (string strName in lstUniqueName)
                {
                    int intHighest = int.MinValue;
                    foreach (Tuple<string, int> strValues in lstUniquePair)
                    {
                        if (strValues.Item1 == strName)
                        {
                            if (strValues.Item2 > intHighest)
                                intHighest = strValues.Item2;
                        }
                    }
                    if (intHighest != int.MinValue)
                        intCustomModifier += intHighest;
                }

                return _intCachedAttributeModifiers = intModifier + intCustomModifier;
            }
        }

        private int _intCachedAttributeValueModifiers = int.MinValue;
        /// <summary>
        /// The total amount of the modifiers that raise the actual value of the CharacterAttribute and increase its Karma cost.
        /// </summary>
        public int AttributeValueModifiers
        {
            get
            {
                if (_intCachedAttributeValueModifiers != int.MinValue)
                    return _intCachedAttributeValueModifiers;
                HashSet<string> lstUniqueName = new HashSet<string>();
                HashSet<Tuple<string, int>> lstUniquePair = new HashSet<Tuple<string, int>>();
                int intModifier = 0;
                foreach (Improvement objImprovement in _objCharacter.Improvements)
                {
                    if (objImprovement.Enabled && objImprovement.ImproveType == Improvement.ImprovementType.Attribute && objImprovement.ImprovedName == Abbrev + "Base" && string.IsNullOrEmpty(objImprovement.Condition))
                    {
                        string strUniqueName = objImprovement.UniqueName;
                        if (!string.IsNullOrEmpty(strUniqueName))
                        {
                            // If this has a UniqueName, run through the current list of UniqueNames seen. If it is not already in the list, add it.
                            if (!lstUniqueName.Contains(strUniqueName))
                                lstUniqueName.Add(strUniqueName);

                            // Add the values to the UniquePair List so we can check them later.
                            lstUniquePair.Add(new Tuple<string, int>(strUniqueName, objImprovement.Augmented * objImprovement.Rating));
                        }
                        else
                        {
                            intModifier += objImprovement.Augmented * objImprovement.Rating;
                        }
                    }
                }

                if (lstUniqueName.Contains("precedence0"))
                {
                    // Retrieve only the highest precedence0 value.
                    // Run through the list of UniqueNames and pick out the highest value for each one.
                    int intHighest = int.MinValue;
                    foreach (Tuple<string, int> strValues in lstUniquePair)
                    {
                        if (strValues.Item1 == "precedence0")
                        {
                            if (strValues.Item2 > intHighest)
                                intHighest = strValues.Item2;
                        }
                    }
                    if (lstUniqueName.Contains("precedence-1"))
                    {
                        foreach (Tuple<string, int> strValues in lstUniquePair)
                        {
                            if (strValues.Item1 == "precedence-1")
                            {
                                intHighest += strValues.Item2;
                            }
                        }
                    }
                    intModifier = Math.Max(intHighest, intModifier);
                }
                else if (lstUniqueName.Contains("precedence1"))
                {
                    // Retrieve all of the items that are precedence1 and nothing else.
                    int intTmpModifier = 0;
                    foreach (Tuple<string, int> strValues in lstUniquePair)
                    {
                        if (strValues.Item1 == "precedence1" || strValues.Item1 == "precedence-1")
                            intTmpModifier += strValues.Item2;
                    }
                    intModifier = Math.Max(intTmpModifier, intModifier);
                }
                else
                {
                    // Run through the list of UniqueNames and pick out the highest value for each one.
                    foreach (string strName in lstUniqueName)
                    {
                        int intHighest = int.MinValue;
                        foreach (Tuple<string, int> strValues in lstUniquePair)
                        {
                            if (strValues.Item1 == strName)
                            {
                                if (strValues.Item2 > intHighest)
                                    intHighest = strValues.Item2;
                            }
                        }
                        if (intHighest != int.MinValue)
                            intModifier += intHighest;
                    }
                }

                return _intCachedAttributeValueModifiers = intModifier;
            }
        }

        /// <summary>
        /// Whether or not the CharacterAttribute has any modifiers from Improvements.
        /// </summary>
        public bool HasModifiers
        {
            get
            {
                foreach (Improvement objImprovement in _objCharacter.Improvements)
                {
                    if (objImprovement.ImproveType == Improvement.ImprovementType.Attribute && (objImprovement.ImprovedName == Abbrev || objImprovement.ImprovedName == Abbrev + "Base") && objImprovement.Enabled)
                    {
                        if (objImprovement.Augmented != 0)
                            return true;
                        if ((objImprovement.ImproveSource == Improvement.ImprovementSource.EssenceLoss || objImprovement.ImproveSource == Improvement.ImprovementSource.EssenceLossChargen) &&
                            (_objCharacter.MAGEnabled && (Abbrev == "MAG" || Abbrev == "MAGAdept") ||
                            _objCharacter.RESEnabled && Abbrev == "RES" ||
                            _objCharacter.DEPEnabled && Abbrev == "DEP"))
                            return true;
                    }
                }

                // If this is AGI or STR, factor in any Cyberlimbs.
                if (!_objCharacter.Options.DontUseCyberlimbCalculation && (Abbrev == "AGI" || Abbrev == "STR"))
                {
                    foreach (Cyberware objCyberware in _objCharacter.Cyberware)
                    {
                        if (objCyberware.Category == "Cyberlimb" && !string.IsNullOrEmpty(objCyberware.LimbSlot))
                            return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// The total amount of the modifiers that affect the CharacterAttribute's Minimum value.
        /// </summary>
        public int MinimumModifiers
        {
            get
            {
                int intModifier = 0;
                foreach (Improvement objImprovement in _objCharacter.Improvements)
                {
                    if (objImprovement.ImproveType == Improvement.ImprovementType.Attribute &&
                        (objImprovement.ImprovedName == Abbrev || objImprovement.ImprovedName == Abbrev + "Base") && objImprovement.Enabled)
                    {
                        intModifier += objImprovement.Minimum * objImprovement.Rating;
                    }
                }
                return intModifier;
            }
        }

        /// <summary>
        /// The total amount of the modifiers that affect the CharacterAttribute's Maximum value.
        /// </summary>
        public int MaximumModifiers
        {
            get
            {
                return _objCharacter.Improvements.Where(objImprovement => objImprovement.ImproveType == Improvement.ImprovementType.Attribute && (objImprovement.ImprovedName == Abbrev || objImprovement.ImprovedName == Abbrev + "Base") && objImprovement.Enabled).Sum(objImprovement => objImprovement.Maximum * objImprovement.Rating);
            }
        }

        /// <summary>
        /// The total amount of the modifiers that affect the CharacterAttribute's Augmented Maximum value.
        /// </summary>
        public int AugmentedMaximumModifiers
        {
            get
            {
                int intModifier = 0;
                foreach (Improvement objImprovement in _objCharacter.Improvements)
                {
                    if (objImprovement.ImproveType == Improvement.ImprovementType.Attribute && objImprovement.ImprovedName == Abbrev && objImprovement.Enabled)
                    {
                        intModifier += objImprovement.AugmentedMaximum * objImprovement.Rating;
                    }
                }
                return intModifier;
            }
        }
        /// <summary>
        /// The CharacterAttribute's total value (Value + Modifiers). 
        /// </summary>
        public int CalculatedTotalValue(bool blnIncludeCyberlimbs = true)
        {
            // If we're looking at MAG and the character is a Cyberzombie, MAG is always 1, regardless of ESS penalties and bonuses.
            if (_objCharacter.MetatypeCategory == "Cyberzombie" && (Abbrev == "MAG" || Abbrev == "MAGAdept"))
                return 1;

            int intMeat = Value + AttributeModifiers;
            int intReturn = intMeat;

            //// If this is AGI or STR, factor in any Cyberlimbs.
            if ((Abbrev == "AGI" || Abbrev == "STR") && !_objCharacter.Options.DontUseCyberlimbCalculation && blnIncludeCyberlimbs)
            {
                int intLimbTotal = 0;
                int intLimbCount = 0;
                foreach (Cyberware objCyberware in _objCharacter.Cyberware.Where(objCyberware => objCyberware.Category == "Cyberlimb" && !string.IsNullOrWhiteSpace(objCyberware.LimbSlot) && !_objCharacter.Options.ExcludeLimbSlot.Contains(objCyberware.LimbSlot)))
                {
                    intLimbCount += objCyberware.LimbSlotCount;
                    switch (Abbrev)
                    {
                        case "STR":
                            intLimbTotal += objCyberware.TotalStrength * objCyberware.LimbSlotCount;
                            break;
                        case "AGI":
                            intLimbTotal += objCyberware.TotalAgility * objCyberware.LimbSlotCount;
                            break;
                    }
                }

                if (intLimbCount > 0)
                {
                    int intMaxLimbs = _objCharacter.LimbCount();
                    int intMissingLimbCount = Math.Max(intMaxLimbs - intLimbCount, 0);
                    // Not all of the limbs have been replaced, so we need to place the Attribute in the other "limbs" to get the average value.
                    intLimbTotal += intMeat * intMissingLimbCount;
                    intReturn = (intLimbTotal + intMaxLimbs - 1) / intMaxLimbs;
                }
            }
            // Do not let the CharacterAttribute go above the Metatype's Augmented Maximum.
            if (intReturn > TotalAugmentedMaximum)
                intReturn = TotalAugmentedMaximum;

            // An Attribute cannot go below 1 unless it is EDG, MAG, or RES, the character is a Critter, or the Metatype Maximum is 0.
            if (intReturn < 1)
            {
                if (_objCharacter.CritterEnabled || _intMetatypeMax == 0 || Abbrev == "EDG" || Abbrev == "RES" || Abbrev == "MAG" || Abbrev == "MAGAdept" || (_objCharacter.MetatypeCategory != "A.I." && Abbrev == "DEP"))
                    return 0;
                else
                    return 1;
            }
            return intReturn;
        }

        /// <summary>
        /// The CharacterAttribute's total value (Value + Modifiers).
        /// </summary>
        public int TotalValue => CalculatedTotalValue();

        /// <summary>
        /// The CharacterAttribute's combined Minimum value (Metatype Minimum + Modifiers), uncapped by its zero.
        /// </summary>
        public int RawMinimum => MetatypeMinimum + MinimumModifiers;

        /// <summary>
        /// The CharacterAttribute's combined Minimum value (Metatype Minimum + Modifiers).
        /// </summary>
        public int TotalMinimum
        {
            get
            {
                // If we're looking at MAG and the character is a Cyberzombie, MAG is always 1, regardless of ESS penalties and bonuses.
                if (_objCharacter.MetatypeCategory == "Cyberzombie" && (Abbrev == "MAG" || Abbrev == "MAGAdept"))
                    return 1;

                int intReturn = RawMinimum;
                if (intReturn < 1)
                {
                    if (_objCharacter.IsCritter || _intMetatypeMax == 0 || Abbrev == "EDG" || Abbrev == "MAG" || Abbrev == "MAGAdept" || Abbrev == "RES" || Abbrev == "DEP")
                        intReturn = 0;
                    else
                        intReturn = 1;
                }
                return intReturn;
            }
        }

        /// <summary>
        /// The CharacterAttribute's combined Maximum value (Metatype Maximum + Modifiers).
        /// </summary>
        public int TotalMaximum
        {
            get
            {
                // If we're looking at MAG and the character is a Cyberzombie, MAG is always 1, regardless of ESS penalties and bonuses.
                if (_objCharacter.MetatypeCategory == "Cyberzombie" && (Abbrev == "MAG" || Abbrev == "MAGAdept"))
                    return 1;

                int intReturn = MetatypeMaximum + MaximumModifiers;

                if (intReturn < 0)
                    intReturn = 0;

                return intReturn;
            }
        }

        /// <summary>
        /// The CharacterAttribute's combined Augmented Maximum value (Metatype Augmented Maximum + Modifiers).
        /// </summary>
        public int TotalAugmentedMaximum
        {
            get
            {
                // If we're looking at MAG and the character is a Cyberzombie, MAG is always 1, regardless of ESS penalties and bonuses.
                if (_objCharacter.MetatypeCategory == "Cyberzombie" && (Abbrev == "MAG" || Abbrev == "MAGAdept"))
                    return 1;

                int intReturn;
                if (Abbrev == "EDG" || Abbrev == "MAG" || Abbrev == "MAGAdept" || Abbrev == "RES" || Abbrev == "DEP")
                    intReturn = TotalMaximum + AugmentedMaximumModifiers;
                else
                    intReturn = TotalMaximum + 4 + AugmentedMaximumModifiers;
                // intReturn = TotalMaximum + Convert.ToInt32(Math.Floor((Convert.ToDecimal(TotalMaximum, GlobalOptions.CultureInfo) / 2))) + AugmentedMaximumModifiers;

                if (intReturn < 0)
                    intReturn = 0;

                return intReturn;
            }
        }

        /// <summary>
        /// CharacterAttribute abbreviation.
        /// </summary>
        public string Abbrev => _strAbbrev;

        public string DisplayNameShort(string strLanguage)
        {
            if (Abbrev == "MAGAdept")
                return LanguageManager.GetString("String_AttributeMAGShort", strLanguage) + " (" + LanguageManager.GetString("String_DescAdept", strLanguage) + ')';

            return LanguageManager.GetString("String_Attribute" + Abbrev + "Short", strLanguage);
        }

        public string DisplayNameLong(string strLanguage)
        {
            if (Abbrev == "MAGAdept")
                return LanguageManager.GetString("String_AttributeMAGLong", strLanguage) + " (" + LanguageManager.GetString("String_DescAdept", strLanguage) + ')';

            return LanguageManager.GetString("String_Attribute" + Abbrev + "Long", strLanguage);
        }

        public string DisplayNameFormatted => GetDisplayNameFormatted(GlobalOptions.Language);

        public string GetDisplayNameFormatted(string strLanguage)
        {
            if (Abbrev == "MAGAdept")
                return LanguageManager.GetString("String_AttributeMAGLong", strLanguage) + " (" + LanguageManager.GetString("String_AttributeMAGShort", strLanguage) + ") (" + LanguageManager.GetString("String_DescAdept", strLanguage) + ')';

            return DisplayNameLong(strLanguage) + " (" + DisplayNameShort(strLanguage) + ')';
        }

        /// <summary>
        /// Is it possible to place points in Base or is it prevented by their build method?
        /// </summary>
        public bool BaseUnlocked => _objCharacter.BuildMethodHasSkillPoints;

        /// <summary>
        /// CharacterAttribute Limits
        /// </summary>
        public string MetatypeLimits => string.Format("{0} / {1} ({2})", MetatypeMinimum, MetatypeMaximum, MetatypeAugmentedMaximum);

        /// <summary>
        /// CharacterAttribute Limits
        /// </summary>
        public string AugmentedMetatypeLimits => string.Format("{0} / {1} ({2})", TotalMinimum, TotalMaximum, TotalAugmentedMaximum);

        #endregion

        #region Methods
        /// <summary>
        /// Set the minimum, maximum, and augmented values for the CharacterAttribute based on string values from the Metatype XML file.
        /// </summary>
        /// <param name="strMin">Metatype's minimum value for the CharacterAttribute.</param>
        /// <param name="strMax">Metatype's maximum value for the CharacterAttribute.</param>
        /// <param name="strAug">Metatype's maximum augmented value for the CharacterAttribute.</param>
        public void AssignLimits(string strMin, string strMax, string strAug)
        {
            MetatypeMinimum = Convert.ToInt32(strMin);
            MetatypeMaximum = Convert.ToInt32(strMax);
            MetatypeAugmentedMaximum = Convert.ToInt32(strAug);
        }

        public string UpgradeToolTip => string.Format(LanguageManager.GetString("Tip_ImproveItem", GlobalOptions.Language), (Value + 1), UpgradeKarmaCost);

        /// <summary>
        /// ToolTip that shows how the CharacterAttribute is calculating its Modified Rating.
        /// </summary>
        public string ToolTip
        {
            get
            {
                string strModifier = string.Empty;

                HashSet<string> lstUniqueName = new HashSet<string>();
                List<Tuple<string, int, string>> lstUniquePair = new List<Tuple<string, int, string>>();
                int intBaseValue = 0;
                foreach (Improvement objImprovement in _objCharacter.Improvements)
                {
                    if (objImprovement.Enabled && !objImprovement.Custom && objImprovement.ImproveType == Improvement.ImprovementType.Attribute && objImprovement.ImprovedName == Abbrev && string.IsNullOrEmpty(objImprovement.Condition))
                    {
                        string strUniqueName = objImprovement.UniqueName;
                        if (!string.IsNullOrEmpty(strUniqueName) && strUniqueName != "enableattribute" && objImprovement.ImproveType == Improvement.ImprovementType.Attribute && objImprovement.ImprovedName == Abbrev)
                        {
                            // If this has a UniqueName, run through the current list of UniqueNames seen. If it is not already in the list, add it.
                            if (!lstUniqueName.Contains(strUniqueName))
                                lstUniqueName.Add(strUniqueName);

                            // Add the values to the UniquePair List so we can check them later.
                            lstUniquePair.Add(new Tuple<string, int, string>(strUniqueName, objImprovement.Augmented * objImprovement.Rating, _objCharacter.GetObjectName(objImprovement, GlobalOptions.Language)));
                        }
                        else if (!(objImprovement.Value == 0 && objImprovement.Augmented == 0))
                        {
                            strModifier += " + " + _objCharacter.GetObjectName(objImprovement, GlobalOptions.Language) + " (" +
                                           (objImprovement.Augmented * objImprovement.Rating).ToString() + ')';
                            intBaseValue += objImprovement.Augmented * objImprovement.Rating;
                        }
                    }
                }

                if (lstUniqueName.Contains("precedence0"))
                {
                    // Retrieve only the highest precedence0 value.
                    // Run through the list of UniqueNames and pick out the highest value for each one.
                    int intHighest = int.MinValue;

                    string strNewModifier = string.Empty;
                    foreach (Tuple<string, int, string> strValues in lstUniquePair)
                    {
                        if (strValues.Item1 == "precedence0")
                        {
                            if (strValues.Item2 > intHighest)
                            {
                                intHighest = strValues.Item2;
                                strNewModifier = " + " + strValues.Item3 + " (" + strValues.Item2.ToString() + ')';
                            }
                        }
                    }
                    if (lstUniqueName.Contains("precedence-1"))
                    {
                        foreach (Tuple<string, int, string> strValues in lstUniquePair)
                        {
                            if (strValues.Item1 == "precedence-1")
                            {
                                intHighest += strValues.Item2;
                                strNewModifier += " + " + strValues.Item3 + " (" + strValues.Item2.ToString() + ')';
                            }
                        }
                    }

                    if (intHighest > intBaseValue)
                        strModifier = strNewModifier;
                }
                else if (lstUniqueName.Contains("precedence1"))
                {
                    // Retrieve all of the items that are precedence1 and nothing else.
                    int intHighest = int.MinValue;
                    string strNewModifier = string.Empty;
                    foreach (Tuple<string, int, string> strValues in lstUniquePair)
                    {
                        if (strValues.Item1 == "precedence1" || strValues.Item1 == "precedence-1")
                        {
                            strNewModifier += " + " + strValues.Item3 + " (" + strValues.Item2.ToString() + ')';
                            intHighest += strValues.Item2;
                        }
                    }
                    if (intHighest > intBaseValue)
                        strModifier = strNewModifier;
                }
                else
                {
                    // Run through the list of UniqueNames and pick out the highest value for each one.
                    foreach (string strName in lstUniqueName)
                    {
                        int intHighest = int.MinValue;
                        foreach (Tuple<string, int, string> strValues in lstUniquePair)
                        {
                            if (strValues.Item1 == strName)
                            {
                                if (strValues.Item2 > intHighest)
                                {
                                    intHighest = strValues.Item2;
                                    strModifier += " + " + strValues.Item3 + " (" + strValues.Item2.ToString() + ')';
                                }
                            }
                        }
                    }
                }

                // Factor in Custom Improvements.
                lstUniqueName.Clear();
                lstUniquePair.Clear();
                foreach (Improvement objImprovement in _objCharacter.Improvements)
                {
                    if (objImprovement.Enabled && objImprovement.Custom && objImprovement.ImproveType == Improvement.ImprovementType.Attribute && objImprovement.ImprovedName == Abbrev && string.IsNullOrEmpty(objImprovement.Condition))
                    {
                        string strUniqueName = objImprovement.UniqueName;
                        if (!string.IsNullOrEmpty(strUniqueName))
                        {
                            // If this has a UniqueName, run through the current list of UniqueNames seen. If it is not already in the list, add it.
                            if (!lstUniqueName.Contains(strUniqueName))
                                lstUniqueName.Add(strUniqueName);

                            // Add the values to the UniquePair List so we can check them later.
                            lstUniquePair.Add(new Tuple<string, int, string>(strUniqueName, objImprovement.Augmented * objImprovement.Rating, _objCharacter.GetObjectName(objImprovement, GlobalOptions.Language)));
                        }
                        else
                        {
                            strModifier += " + " + _objCharacter.GetObjectName(objImprovement, GlobalOptions.Language) + " (" +
                                               (objImprovement.Augmented * objImprovement.Rating).ToString() + ')';
                        }
                    }
                }

                // Run through the list of UniqueNames and pick out the highest value for each one.
                foreach (string strName in lstUniqueName)
                {
                    int intHighest = int.MinValue;
                    foreach (Tuple<string, int, string> strValues in lstUniquePair)
                    {
                        if (strValues.Item1 == strName)
                        {
                            if (strValues.Item2 > intHighest)
                            {
                                intHighest = strValues.Item2;
                                strModifier += " + " + strValues.Item3 + " (" + strValues.Item2.ToString() + ')';
                            }
                        }
                    }
                }

                //// If this is AGI or STR, factor in any Cyberlimbs.
                StringBuilder strCyberlimb = new StringBuilder();
                if ((Abbrev == "AGI" || Abbrev == "STR") && !_objCharacter.Options.DontUseCyberlimbCalculation)
                {
                    foreach (Cyberware objCyberware in _objCharacter.Cyberware)
                    {
                        if (objCyberware.Category == "Cyberlimb")
                        {
                            strCyberlimb.Append("\n");
                            strCyberlimb.Append(objCyberware.DisplayName(GlobalOptions.Language) + " (");
                            strCyberlimb.Append(Abbrev == "AGI" ? objCyberware.TotalAgility.ToString() : objCyberware.TotalStrength.ToString());
                            strCyberlimb.Append(')');
                        }
                    }
                    strModifier += strCyberlimb;
                }
                /*
                if ((Abbrev == "RES" || Abbrev == "MAG" || Abbrev == "MAGAdept" || Abbrev == "DEP") && _objCharacter.EssencePenalty != 0)
                {
                    strModifier += $" + -{_objCharacter.EssencePenalty} ({LanguageManager.GetString("String_AttributeESSLong")})";
                }
                */

                return DisplayAbbrev + " (" + Value.ToString() + ')' + strModifier;
            }
        }

        public int SpentPriorityPoints
        {
            get
            {
                int intBase = Base;
                int intReturn = intBase;

                int intExtra = 0;
                decimal decMultiplier = 1.0m;
                foreach (Improvement objLoopImprovement in _objCharacter.Improvements)
                {
                    if ((objLoopImprovement.ImprovedName == Abbrev || string.IsNullOrEmpty(objLoopImprovement.ImprovedName)) &&
                        (string.IsNullOrEmpty(objLoopImprovement.Condition) || (objLoopImprovement.Condition == "career") == _objCharacter.Created || (objLoopImprovement.Condition == "create") != _objCharacter.Created) &&
                        objLoopImprovement.Minimum <= intBase && objLoopImprovement.Enabled)
                    {
                        if (objLoopImprovement.ImproveType == Improvement.ImprovementType.AttributePointCost)
                            intExtra += objLoopImprovement.Value * (Math.Min(intBase, objLoopImprovement.Maximum == 0 ? int.MaxValue : objLoopImprovement.Maximum) - objLoopImprovement.Minimum);
                        else if (objLoopImprovement.ImproveType == Improvement.ImprovementType.AttributePointCostMultiplier)
                            decMultiplier *= objLoopImprovement.Value / 100.0m;
                    }
                }
                if (decMultiplier != 1.0m)
                    intReturn = decimal.ToInt32(decimal.Ceiling(intReturn * decMultiplier));
                intReturn += intExtra;

                return Math.Max(intReturn, 0);
            }
        }

        public bool AtMetatypeMaximum => Value == TotalMaximum && TotalMinimum > 0;

        public int KarmaMaximum => TotalMaximum - TotalBase;
        public int PriorityMaximum => TotalMaximum - Karma - FreeBase - RawMinimum;
        /// <summary>
        /// Karma price to upgrade. Returns negative if impossible
        /// </summary>
        /// <returns>Price in karma</returns>
        public int UpgradeKarmaCost
        {
            get
            {
                int intValue = Value;
                int upgrade;
                int intOptionsCost = _objCharacter.Options.KarmaAttribute;
                if (intValue >= TotalMaximum)
                {
                    return -1;
                }
                else if (intValue == 0)
                {
                    upgrade = intOptionsCost;
                }
                else
                {
                    upgrade = (intValue + 1) * intOptionsCost;
                }
                if (_objCharacter.Options.AlternateMetatypeAttributeKarma)
                    upgrade -= (MetatypeMinimum - 1) * intOptionsCost;

                int intExtra = 0;
                decimal decMultiplier = 1.0m;
                foreach (Improvement objLoopImprovement in _objCharacter.Improvements)
                {
                    if ((objLoopImprovement.ImprovedName == Abbrev || string.IsNullOrEmpty(objLoopImprovement.ImprovedName)) &&
                        (string.IsNullOrEmpty(objLoopImprovement.Condition) || (objLoopImprovement.Condition == "career") == _objCharacter.Created || (objLoopImprovement.Condition == "create") != _objCharacter.Created) &&
                            (objLoopImprovement.Maximum == 0 || intValue <= objLoopImprovement.Maximum) && objLoopImprovement.Minimum <= intValue && objLoopImprovement.Enabled)
                    {
                        if (objLoopImprovement.ImproveType == Improvement.ImprovementType.AttributeKarmaCost)
                            intExtra += objLoopImprovement.Value;
                        else if (objLoopImprovement.ImproveType == Improvement.ImprovementType.AttributeKarmaCostMultiplier)
                            decMultiplier *= objLoopImprovement.Value / 100.0m;
                    }
                }
                if (decMultiplier != 1.0m)
                    upgrade = decimal.ToInt32(decimal.Ceiling(upgrade * decMultiplier));
                upgrade += intExtra;

                return Math.Max(upgrade, Math.Min(1, intOptionsCost));
            }
        }

        public int TotalKarmaCost
        {
            get
            {
                if (Karma == 0)
                    return 0;

                int intValue = Value;
                int intRawTotalBase = _objCharacter.Options.ReverseAttributePriorityOrder ? Math.Max(FreeBase + RawMinimum, TotalMinimum) : TotalBase;
                int intTotalBase = intRawTotalBase;
                if (_objCharacter.Options.AlternateMetatypeAttributeKarma)
                {
                    int intHumanMinimum = _objCharacter.Options.ReverseAttributePriorityOrder ? FreeBase + 1 + MinimumModifiers : Base + FreeBase + 1 + MinimumModifiers;
                    if (intHumanMinimum < 1)
                    {
                        if (_objCharacter.IsCritter || _intMetatypeMax == 0 || Abbrev == "EDG" || Abbrev == "MAG" || Abbrev == "MAGAdept" || Abbrev == "RES" || Abbrev == "DEP")
                            intHumanMinimum = 0;
                        else
                            intHumanMinimum = 1;
                    }
                    intTotalBase = intHumanMinimum;
                }

                // The expression below is a shortened version of n*(n+1)/2 when applied to karma costs. n*(n+1)/2 is the sum of all numbers from 1 to n.
                // I'm taking n*(n+1)/2 where n = Base + Karma, then subtracting n*(n+1)/2 from it where n = Base. After removing all terms that cancel each other out, the expression below is what remains.
                int intCost = (2 * intTotalBase + Karma + 1) * Karma / 2 * _objCharacter.Options.KarmaAttribute;

                int intExtra = 0;
                decimal decMultiplier = 1.0m;
                foreach (Improvement objLoopImprovement in _objCharacter.Improvements)
                {
                    if ((objLoopImprovement.ImprovedName == Abbrev || string.IsNullOrEmpty(objLoopImprovement.ImprovedName)) &&
                        (string.IsNullOrEmpty(objLoopImprovement.Condition) || (objLoopImprovement.Condition == "career") == _objCharacter.Created || (objLoopImprovement.Condition == "create") != _objCharacter.Created) &&
                            objLoopImprovement.Minimum <= intValue && objLoopImprovement.Enabled)
                    {
                        if (objLoopImprovement.ImproveType == Improvement.ImprovementType.AttributeKarmaCost)
                            intExtra += objLoopImprovement.Value * (Math.Min(intValue, objLoopImprovement.Maximum == 0 ? int.MaxValue : objLoopImprovement.Maximum) - Math.Max(intRawTotalBase, objLoopImprovement.Minimum - 1));
                        else if (objLoopImprovement.ImproveType == Improvement.ImprovementType.AttributeKarmaCostMultiplier)
                            decMultiplier *= objLoopImprovement.Value / 100.0m;
                    }
                }
                if (decMultiplier != 1.0m)
                    intCost = decimal.ToInt32(decimal.Ceiling(intCost * decMultiplier));
                intCost += intExtra;

                return Math.Max(intCost, 0);
            }
        }

        public bool CanUpgradeCareer => _objCharacter.Karma >= UpgradeKarmaCost && TotalMaximum > Value;

        // Caching the value prevents calling the event multiple times. 
        private bool _oldUpgrade;
        private void OnCharacterChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName != nameof(Character.Karma))
                return;
            if (_oldUpgrade != CanUpgradeCareer)
            {
                _oldUpgrade = CanUpgradeCareer;
                OnPropertyChanged(nameof(CanUpgradeCareer));
            }
        }

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            foreach (string s in DependencyTree.Find(propertyName))
            {
                var v = new PropertyChangedEventArgs(s);
                PropertyChanged?.Invoke(this, v);
            }
        }

		/// <summary>
		/// Convert a string to an Attribute Category.
		/// </summary>
		/// <param name="strAbbrev">Linked attribute abbreviation.</param>
		public static AttributeCategory ConvertToAttributeCategory(string strAbbrev)
		{
			switch (strAbbrev)
			{
				case "DEP":
				case "EDG":
				case "ESS":
				case "MAG":
                case "MAGAdept":
				case "RES":
					return AttributeCategory.Special;
				default:
					return AttributeCategory.Standard;
			}
		}

		/// <summary>
		/// Convert a string to an Attribute Category.
		/// </summary>
		/// <param name="strValue">String value to convert.</param>
		public static AttributeCategory ConvertToMetatypeAttributeCategory(string strValue)
		{
			//If a value does exist, test whether it belongs to a shapeshifter form.
			switch (strValue)
			{
				case "Shapeshifter":
					return AttributeCategory.Shapeshifter;
				default:
					return AttributeCategory.Standard;
			}
		}
		#endregion

        #region static
        //A tree of dependencies. Once some of the properties are changed, 
        //anything they depend on, also needs to raise OnChanged
        //This tree keeps track of dependencies
        private static readonly ReverseTree<string> DependencyTree =
            new ReverseTree<string>(nameof(ToolTip),
                new ReverseTree<string>(nameof(DisplayValue),
                    new ReverseTree<string>(nameof(Augmented),
                        new ReverseTree<string>(nameof(TotalValue),
                            new ReverseTree<string>(nameof(AttributeModifiers)),
                            new ReverseTree<string>(nameof(Karma)),
                            new ReverseTree<string>(nameof(Base)),
                            new ReverseTree<string>(nameof(AugmentedMetatypeLimits),
                                new ReverseTree<string>(nameof(TotalMinimum)),
                                new ReverseTree<string>(nameof(TotalMaximum)),
                                new ReverseTree<string>(nameof(TotalAugmentedMaximum)))))));

        public string UpgradeKarmaCostString => LanguageManager.GetString("Message_ConfirmKarmaExpense", GlobalOptions.Language).Replace("{0}", Abbrev.Replace("{1}", (Value + 1).ToString()).Replace("{2}", UpgradeKarmaCost.ToString()));

        /// <summary>
        /// Translated abbreviation of the attribute.
        /// </summary>
        public string DisplayAbbrev => GetDisplayAbbrev(GlobalOptions.Language);

        public string GetDisplayAbbrev(string strLanguage)
        {
            if (Abbrev == "MAGAdept")
                return LanguageManager.GetString("String_AttributeMAGShort", strLanguage) + " (" + LanguageManager.GetString("String_DescAdept", strLanguage) + ')';

            return LanguageManager.GetString($"String_Attribute{Abbrev}Short", strLanguage);
        }

        public void Upgrade(int intAmount = 1)
        {
            for (int i = 0; i < intAmount; ++i)
            {
                if (!CanUpgradeCareer)
                    return;

                int intPrice = UpgradeKarmaCost;
                int intValue = Value;
                string strUpgradetext = $"{LanguageManager.GetString("String_ExpenseAttribute", GlobalOptions.Language)} {Abbrev} {intValue} 🡒 {intValue + 1}";

                ExpenseLogEntry objEntry = new ExpenseLogEntry(_objCharacter);
                objEntry.Create(intPrice * -1, strUpgradetext, ExpenseType.Karma, DateTime.Now);
                objEntry.Undo = new ExpenseUndo().CreateKarma(KarmaExpenseType.ImproveAttribute, Abbrev);

                _objCharacter.ExpenseEntries.AddWithSort(objEntry);

                Karma += 1;
                _objCharacter.Karma -= intPrice;
            }
        }

        public void Degrade(int intAmount)
        {
            for (int i = intAmount; i > 0; --i)
            {
                if (Karma > 0)
                {
                    Karma -= 1;
                }
                else if (Base > 0)
                {
                    Base -= 1;
                }
                else if (Abbrev == "EDG" && TotalMinimum > 0)
                {
                    //Edge can reduce the metatype minimum below zero. 
                    MetatypeMinimum -= 1;
                }
                else
                    return;
            }
        }

        [Obsolete("Refactor this method away once improvementmanager gets outbound events")]
        private void OnImprovementEvent(ICollection<Improvement> improvements)
        {
            bool blnHasAugmented = false;
            if (improvements.Any(imp => imp.ImproveType == Improvement.ImprovementType.Attribute && (imp.ImprovedName == Abbrev || imp.ImprovedName == Abbrev + "Base") && imp.Augmented != 0))
            {
                blnHasAugmented = true;
                _intCachedAttributeModifiers = int.MinValue;
                _intCachedAttributeValueModifiers = int.MinValue;
            }
            if (improvements.Any(imp => imp.ImproveType == Improvement.ImprovementType.Attribute && (imp.ImprovedName == Abbrev || imp.ImprovedName == Abbrev + "Base") && imp.AugmentedMaximum != 0 || imp.Maximum != 0 || imp.Minimum != 0))
            {
                OnPropertyChanged(nameof(AugmentedMetatypeLimits));
            }
            else if (improvements.Any(imp => imp.ImproveType == Improvement.ImprovementType.ReplaceAttribute && imp.ImprovedName == Abbrev))
            {
                OnPropertyChanged(nameof(AugmentedMetatypeLimits));
            }
            else if (improvements.Any(imp => imp.ImproveType == Improvement.ImprovementType.Attributelevel))
            {
                OnPropertyChanged(nameof(Base));
            }
            else if (improvements.Any(imp => imp.ImproveSource == Improvement.ImprovementSource.Cyberware))
            {
                OnPropertyChanged(nameof(AttributeModifiers));
            }
            else if (improvements.Any(imp => imp.ImproveType == Improvement.ImprovementType.Attribute && imp.ImprovedName == Abbrev && imp.Value != 0))
            {
                OnPropertyChanged(nameof(TotalValue));
            }
            else if (blnHasAugmented)
            {
                OnPropertyChanged(nameof(Augmented));
            }
        }

        /// <summary>
        /// Forces a particular event to fire.
        /// </summary>
        /// <param name="property"></param>
        public void ForceEvent(string property)
        {
            foreach (string s in DependencyTree.Find(property))
            {
                var v = new PropertyChangedEventArgs(s);
                PropertyChanged?.Invoke(this, v);
            }
        }
        #endregion
    }
}
