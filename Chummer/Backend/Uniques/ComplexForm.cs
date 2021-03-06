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
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

namespace Chummer
{
    /// <summary>
    /// A Technomancer Program or Complex Form.
    /// </summary>
    public class ComplexForm : IHasInternalId, IHasName, IHasXmlNode
    {
        private Guid _guiID;
        private string _strName = string.Empty;
        private string _strTarget = string.Empty;
        private string _strDuration = string.Empty;
        private string _strFV = string.Empty;
        private string _strSource = string.Empty;
        private string _strPage = string.Empty;
        private string _strNotes = string.Empty;
        private string _strExtra = string.Empty;
        private int _intGrade;
        private readonly Character _objCharacter;

        #region Constructor, Create, Save, Load, and Print Methods
        public ComplexForm(Character objCharacter)
        {
            // Create the GUID for the new Complex Form.
            _guiID = Guid.NewGuid();
            _objCharacter = objCharacter;
        }

        /// Create a Complex Form from an XmlNode.
        /// <param name="objXmlComplexFormNode">XmlNode to create the object from.</param>
        /// <param name="strExtra">Value to forcefully select for any ImprovementManager prompts.</param>
        public void Create(XmlNode objXmlComplexFormNode, string strExtra = "")
        {
            if (objXmlComplexFormNode.TryGetStringFieldQuickly("name", ref _strName))
                _objCachedMyXmlNode = null;
            objXmlComplexFormNode.TryGetStringFieldQuickly("target", ref _strTarget);
            objXmlComplexFormNode.TryGetStringFieldQuickly("source", ref _strSource);
            objXmlComplexFormNode.TryGetStringFieldQuickly("page", ref _strPage);
            objXmlComplexFormNode.TryGetStringFieldQuickly("duration", ref _strDuration);
            objXmlComplexFormNode.TryGetStringFieldQuickly("fv", ref _strFV);
            if (!objXmlComplexFormNode.TryGetStringFieldQuickly("altnotes", ref _strNotes))
                objXmlComplexFormNode.TryGetStringFieldQuickly("notes", ref _strNotes);
            _strExtra = strExtra;

            /*
            if (string.IsNullOrEmpty(_strNotes))
            {
                _strNotes = CommonFunctions.GetTextFromPDF($"{_strSource} {_strPage}", _strName);
                if (string.IsNullOrEmpty(_strNotes))
                {
                    _strNotes = CommonFunctions.GetTextFromPDF($"{Source} {Page(GlobalOptions.Language)}", DisplayName(GlobalOptions.Language));
                }
            }*/
        }

        /// <summary>
        /// Save the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        public void Save(XmlTextWriter objWriter)
        {
            objWriter.WriteStartElement("complexform");
            objWriter.WriteElementString("guid", _guiID.ToString("D"));
            objWriter.WriteElementString("name", _strName);
            objWriter.WriteElementString("target", _strTarget);
            objWriter.WriteElementString("duration", _strDuration);
            objWriter.WriteElementString("fv", _strFV);
            objWriter.WriteElementString("extra", _strExtra);
            objWriter.WriteElementString("source", _strSource);
            objWriter.WriteElementString("page", _strPage);
            objWriter.WriteElementString("notes", _strNotes);
            objWriter.WriteElementString("grade", _intGrade.ToString(GlobalOptions.InvariantCultureInfo));
            objWriter.WriteEndElement();
            _objCharacter.SourceProcess(_strSource);
        }

        /// <summary>
        /// Load the Complex Form from the XmlNode.
        /// </summary>
        /// <param name="objNode">XmlNode to load.</param>
        public void Load(XmlNode objNode)
        {
            objNode.TryGetField("guid", Guid.TryParse, out _guiID);
            if (objNode.TryGetStringFieldQuickly("name", ref _strName))
                _objCachedMyXmlNode = null;
            objNode.TryGetStringFieldQuickly("target", ref _strTarget);
            objNode.TryGetStringFieldQuickly("source", ref _strSource);
            objNode.TryGetStringFieldQuickly("page", ref _strPage);
            objNode.TryGetStringFieldQuickly("duration", ref _strDuration);
            objNode.TryGetStringFieldQuickly("extra", ref _strExtra);
            objNode.TryGetStringFieldQuickly("fv", ref _strFV);
            objNode.TryGetStringFieldQuickly("notes", ref _strNotes);
            objNode.TryGetInt32FieldQuickly("grade", ref _intGrade);
        }

        /// <summary>
        /// Print the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        /// <param name="strLanguageToPrint">Language in which to print</param>
        public void Print(XmlTextWriter objWriter, string strLanguageToPrint)
        {
            objWriter.WriteStartElement("complexform");
            objWriter.WriteElementString("name", DisplayNameShort(strLanguageToPrint));
            objWriter.WriteElementString("name_english", Name);
            objWriter.WriteElementString("duration", Duration);
            objWriter.WriteElementString("fv", FV);
            objWriter.WriteElementString("target", Target);
            objWriter.WriteElementString("source", CommonFunctions.LanguageBookShort(Source, strLanguageToPrint));
            objWriter.WriteElementString("page", Page(strLanguageToPrint));
            if (_objCharacter.Options.PrintNotes)
                objWriter.WriteElementString("notes", Notes);
            objWriter.WriteEndElement();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Internal identifier which will be used to identify this Complex Form in the Improvement system.
        /// </summary>
        public string InternalId => _guiID.ToString("D");

        /// <summary>
        /// Complex Form's name.
        /// </summary>
        public string Name
        {
            get => _strName;
            set
            {
                if (_strName != value)
                    _objCachedMyXmlNode = null;
                _strName = value;
            }
        }

        /// <summary>
        /// Complex Form's extra info.
        /// </summary>
        public string Extra
        {
            get => _strExtra;
            set => _strExtra = LanguageManager.ReverseTranslateExtra(value, GlobalOptions.Language);
        }

        /// <summary>
        /// Complex Form's grade.
        /// </summary>
        public int Grade
        {
            get => _intGrade;
            set => _intGrade = value;
        }

        /// <summary>
        /// The name of the object as it should be displayed on printouts (translated name only).
        /// </summary>
        public string DisplayNameShort(string strLanguage)
        {
            string strReturn = _strName;
            // Get the translated name if applicable.
            if (strLanguage != GlobalOptions.DefaultLanguage)
                strReturn = GetNode(strLanguage)?["translate"]?.InnerText ?? _strName;

            if (!string.IsNullOrEmpty(_strExtra))
            {
                string strExtra = _strExtra;
                if (strLanguage != GlobalOptions.DefaultLanguage)
                    strExtra = LanguageManager.TranslateExtra(_strExtra, strLanguage);
                strReturn += " (" + strExtra + ')';
            }
            return strReturn;
        }

        /// <summary>
        /// The name of the object as it should be displayed in lists. Name (Extra).
        /// </summary>
        public string DisplayName => DisplayNameShort(GlobalOptions.Language);

        /// <summary>
        /// Complex Form's Duration.
        /// </summary>
        public string Duration
        {
            get => _strDuration;
            set => _strDuration = value;
        }

        /// <summary>
        /// The Complex Form's FV.
        /// </summary>
        public string FV
        {
            get => _strFV;
            set => _strFV = value;
        }

        /// <summary>
        /// The Complex Form's Target.
        /// </summary>
        public string Target
        {
            get => _strTarget;
            set => _strTarget = value;
        }

        /// <summary>
        /// Complex Form's Source.
        /// </summary>
        public string Source
        {
            get => _strSource;
            set => _strSource = value;
        }

        /// <summary>
        /// Sourcebook Page Number.
        /// </summary>
        public string Page(string strLanguage)
        {
            // Get the translated name if applicable.
            if (strLanguage == GlobalOptions.DefaultLanguage)
                return _strPage;

            return GetNode(strLanguage)?["altpage"]?.InnerText ?? _strPage;
        }

        /// <summary>
        /// Notes.
        /// </summary>
        public string Notes
        {
            get => _strNotes;
            set => _strNotes = value;
        }

        private XmlNode _objCachedMyXmlNode;
        private string _strCachedXmlNodeLanguage = string.Empty;

        public XmlNode GetNode()
        {
            return GetNode(GlobalOptions.Language);
        }

        public XmlNode GetNode(string strLanguage)
        {
            if (_objCachedMyXmlNode == null || strLanguage != _strCachedXmlNodeLanguage || GlobalOptions.LiveCustomData)
            {
                _objCachedMyXmlNode = XmlManager.Load("complexforms.xml", strLanguage).SelectSingleNode("/chummer/complexforms/complexform[name = \"" + Name + "\"]");
                _strCachedXmlNodeLanguage = strLanguage;
            }
            return _objCachedMyXmlNode;
        }
        #endregion

        #region Methods
        public TreeNode CreateTreeNode(ContextMenuStrip cmsComplexForm)
        {
            TreeNode objNode = new TreeNode
            {
                Name = InternalId,
                Text = DisplayName,
                Tag = InternalId,
                ContextMenuStrip = cmsComplexForm
            };
            if (!string.IsNullOrEmpty(Notes))
            {
                objNode.ForeColor = Color.SaddleBrown;
            }
            else if (Grade != 0)
            {
                objNode.ForeColor = SystemColors.GrayText;
            }
            objNode.ToolTipText = Notes.WordWrap(100);
            return objNode;
        }
        #endregion
    }
}
