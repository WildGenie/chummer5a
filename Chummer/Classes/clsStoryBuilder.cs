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
using System.Collections.Generic;
 using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Chummer
{
    public sealed class StoryBuilder
    {
        private readonly Dictionary<string, string> persistenceDictionary = new Dictionary<string, string>();
        private readonly Character _objCharacter;
        private readonly Random _objRandom = MersenneTwister.SfmtRandom.Create();
        private int _intModuloTemp;
        public StoryBuilder(Character objCharacter)
        {
            _objCharacter = objCharacter;
            persistenceDictionary.Add("metatype", _objCharacter.Metatype.ToLower());
            persistenceDictionary.Add("metavariant", _objCharacter.Metavariant.ToLower());
        }

        public string GetStory(string strLanguage)
        {
            //Little bit of data required for following steps
            XmlDocument xdoc = XmlManager.Load("lifemodules.xml", strLanguage);

            //Generate list of all life modules (xml, we don't save required data to quality) this character has
            List<XmlNode> modules = new List<XmlNode>();

            foreach (Quality quality in _objCharacter.Qualities)
            {
                if (quality.Type == QualityType.LifeModule)
                {
                    modules.Add(Quality.GetNodeOverrideable(quality.QualityId, xdoc));
                }
            }

            //Sort the list (Crude way, but have to do)
            for (int i = 0; i < modules.Count; i++)
            {
                string stageName = xdoc.SelectSingleNode(i <= 4 ? "chummer/stages/stage[@order = \"" + (i + 1).ToString() + "\"]" : "chummer/stages/stage[@order = \"5\"]")?.InnerText;
                int j;
                for (j = i; j < modules.Count; j++)
                {
                    if (modules[j]["stage"] != null && modules[j]["stage"].InnerText == stageName)
                        break;
                }
                if (j != i && j < modules.Count)
                {
                    XmlNode tmp = modules[i];
                    modules[i] = modules[j];
                    modules[j] = tmp;
                }
            }

            string[] story = new string[modules.Count];
            object storyLock = new object();
            //Actually "write" the story
            Parallel.For(0, modules.Count, i =>
            {
                XmlNode objStoryModule = modules[i];
                StringBuilder objModuleString = new StringBuilder();
                Write(objModuleString, objStoryModule["story"]?.InnerText ?? string.Empty, 5, xdoc);
                lock (storyLock)
                    story[i] = objModuleString.ToString();
            });

            return string.Join(Environment.NewLine + Environment.NewLine, story);
        }
        
        private void Write(StringBuilder story, string innerText, int levels, XmlDocument xmlDoc)
        {
            if (levels <= 0) return;

            int startingLength = story.Length;

            string[] words;
            if (innerText.StartsWith('$') && innerText.IndexOf(' ') < 0)
            {
                words = Macro(innerText, xmlDoc).Split(' ', '\n', '\r', '\t');
            }
            else
            {
                words = innerText.Split(' ', '\n', '\r', '\t');
            }

            bool mfix = false;
            foreach (string word in words)
            {
                string trim = word.Trim();
                if (trim.StartsWith("$DOLLAR"))
                {
                    story.Append('$');
                    mfix = true;
                }
                else if (trim.StartsWith('$'))
                {
                    //if (story.Length > 0 && story[story.Length - 1] == ' ') story.Length--;
                    Write(story, trim, --levels, xmlDoc);
                    mfix = true;
                }
                else
                {
                    if (story.Length != startingLength && !mfix)
                    {
                        story.Append(' ');
                    }
                    else
                    {
                        mfix = false;
                    }
                    int slenght = story.Length;
                    story.AppendFormat(trim);
                    if (story.Length != slenght)
                    {
                        
                    }
                }
            }
        }
        
        public string Macro(string innerText, XmlDocument xmlDoc)
        {
            if (string.IsNullOrEmpty(innerText))
                return string.Empty;
            string endString = innerText.ToLower().Substring(1).TrimEnd(',', '.');
            string macroName, macroPool;
            if (endString.Contains('_'))
            {
                string[] split = endString.Split('_');
                macroName = split[0];
                macroPool = split[1];
            }
            else
            {
                macroName = macroPool = endString;
            }

            //$DOLLAR is defined elsewhere to prevent recursive calling
            if (macroName == "street")
            {
                if (!string.IsNullOrEmpty(_objCharacter.Alias))
                {
                    return _objCharacter.Alias;
                }
                return "Alias ";
            }
            if(macroName == "real")
            {
                if (!string.IsNullOrEmpty(_objCharacter.Name))
                {
                    return _objCharacter.Name;
                }
                return "Unnamed John Doe ";
            }
            if (macroName == "year")
            {
                if (int.TryParse(_objCharacter.Age, out int year))
                {
                    if (int.TryParse(macroPool, out int age))
                    {
                        return (DateTime.UtcNow.Year + 62 + age - year).ToString();
                    }
                    return (DateTime.UtcNow.Year + 62 - year).ToString();
                }
                return string.Format("(ERROR PARSING \"{0}\")", _objCharacter.Age);
            }

            //Did not meet predefined macros, check user defined
            
            string searchString = "/chummer/storybuilder/macros/" + macroName;

            XmlNode userMacro = xmlDoc?.SelectSingleNode(searchString);

            if (userMacro != null)
            {
                if (userMacro.FirstChild != null)
                {
                    //Already defined, no need to do anything fancy
                    if (!persistenceDictionary.TryGetValue(macroPool, out string selected))
                    {
                        if (userMacro.FirstChild.Name == "random")
                        {
                            //Any node not named 
                            XmlNodeList possible = userMacro.FirstChild.SelectNodes("./*[not(self::default)]");
                            if (possible != null && possible.Count > 0)
                            {
                                if (possible.Count > 1)
                                {
                                    do
                                    {
                                        _intModuloTemp = _objRandom.Next();
                                    }
                                    while (_intModuloTemp >= int.MaxValue - int.MaxValue % possible.Count); // Modulo bias removal
                                }
                                else
                                    _intModuloTemp = 1;
                                selected = possible[_intModuloTemp % possible.Count].Name;
                            }
                        }
                        else if (userMacro.FirstChild.Name == "persistent")
                        {
                            //Any node not named 
                            XmlNodeList possible = userMacro.FirstChild.SelectNodes("./*[not(self::default)]");
                            if (possible != null && possible.Count > 0)
                            {
                                if (possible.Count > 1)
                                {
                                    do
                                    {
                                        _intModuloTemp = _objRandom.Next();
                                    }
                                    while (_intModuloTemp >= int.MaxValue - int.MaxValue % possible.Count); // Modulo bias removal
                                }
                                else
                                    _intModuloTemp = 1;
                                selected = possible[_intModuloTemp % possible.Count].Name;
                                persistenceDictionary.Add(macroPool, selected);
                            }
                        }
                        else
                        {
                            return string.Format("(Formating error in  $DOLLAR{0} )", macroName);
                        }
                    }

                    if (!string.IsNullOrEmpty(selected) && userMacro.FirstChild[selected] != null)
                    {
                        return userMacro.FirstChild[selected].InnerText;
                    }
                    else if (userMacro.FirstChild["default"] != null)
                    {
                        return userMacro.FirstChild["default"].InnerText;
                    }
                    else
                    {
                        return string.Format("(Unknown key {0} in  $DOLLAR{1} )", macroPool, macroName);
                    }
                }
                else
                {
                    return userMacro.InnerText;
                }
            }
            return string.Format("(Unknown Macro  $DOLLAR{0} )", innerText.Substring(1));
        }
    }
}
