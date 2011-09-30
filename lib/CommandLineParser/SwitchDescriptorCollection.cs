﻿#region License
//Ntreev CommandLineParser for .Net 
//https://github.com/NtreevSoft/CommandLineParser

//Released under the MIT License.

//Copyright (c) 2010 Ntreev Soft co., Ltd.

//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
//documentation files (the "Software"), to deal in the Software without restriction, including without limitation the 
//rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit 
//persons to whom the Software is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the 
//Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE 
//WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR 
//COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR 
//OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections;
using System.Text.RegularExpressions;

using Trace = System.Diagnostics.Trace;

namespace Ntreev.Library
{
    class SwitchDescriptorCollection : ICollection
    {
        List<SwitchDescriptor> internalDescriptors = null;

        delegate bool Filter(SwitchAttribute obj);

        static bool ByRequired(SwitchAttribute obj)
        {
            return obj.Required == true;
        }

        static int NameComparison(SwitchDescriptor x, SwitchDescriptor y)
        {
            return x.Name.CompareTo(y.Name);
        }

        static int ShortNameComparison(SwitchDescriptor x, SwitchDescriptor y)
        {
            return x.ShortName.CompareTo(y.ShortName);
        }

        static SwitchDescriptorCollection GetSwitches(object instance, SwitchAttributeCollection extenedSwitchAttributes, Filter filter)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(instance);
            List<SwitchDescriptor> validProperties = new List<SwitchDescriptor>();

            foreach (PropertyDescriptor item in properties)
            {
                if (item.IsBrowsable == false || item.IsReadOnly == true)
                    continue;

                if (item.Converter.CanConvertFrom(typeof(string)) == false)
                    continue;

                SwitchAttribute optionAttribute = item.Attributes[typeof(SwitchAttribute)] as SwitchAttribute;

                if (optionAttribute == null)
                {
                    if (extenedSwitchAttributes != null && extenedSwitchAttributes.ContainsKey(item.Name) == true)
                        optionAttribute = extenedSwitchAttributes[item.Name];
                    else
                        optionAttribute = SwitchAttribute.DefaultValue;
                }

                if (filter == null || filter(optionAttribute) == true)
                    validProperties.Add(new SwitchDescriptor(item, optionAttribute));
            }
            return new SwitchDescriptorCollection(validProperties);
        }

        public static SwitchDescriptorCollection GetSwitches(object instance, SwitchAttributeCollection extenedSwitchAttributes)
        {
            return GetSwitches(instance, extenedSwitchAttributes, (Filter)null);
        }

        public static SwitchDescriptorCollection GetRequiredSwitches(object instance, SwitchAttributeCollection extenedSwitchAttributes)
        {
            return GetSwitches(instance, extenedSwitchAttributes, SwitchDescriptorCollection.ByRequired);
        }

        void SplitUnusedArgs(string args, List<string> unusedList)
        {
            string groupName = "arg";
            string pattern = string.Format(@"(""(?<{0}>.*)"")|(?<{0}>(\S)+)", groupName);
            Regex regex = new Regex(pattern, RegexOptions.ExplicitCapture);
            Match match = regex.Match(args);

            while (match.Success == true)
            {
                string matchedString = match.Groups[groupName].ToString();
                unusedList.Add(matchedString);
                Trace.WriteLine("unused arg : " + matchedString);
                match = match.NextMatch();
            }
        }

        Dictionary<string, SwitchDescriptor> DescriptorsForParsing(List<SwitchDescriptor> sourceList)
        {
            Dictionary<string, SwitchDescriptor> list = new Dictionary<string, SwitchDescriptor>(sourceList.Count * 2);
            
            foreach (SwitchDescriptor item in sourceList)
            {
                list.Add(item.Name, item);
                if(item.ShortName != string.Empty)
                    list.Add(item.ShortName, item);
            }

            return list;
        }

        public SwitchDescriptorCollection(List<SwitchDescriptor> descriptors)
        {
            this.internalDescriptors = descriptors;
        }

        public void AssertRequired()
        {
            foreach (SwitchDescriptor item in this.internalDescriptors)
            {
                if (item.Required == true && item.Parsed == false)
                    throw new MissingSwitchException(Resource.SwitchIsMissing, item.Name);
            }
        }

        public void AssertValidation()
        {
            HashSet<string> hashSet = new HashSet<string>();

            foreach (SwitchDescriptor item in this)
            {
                hashSet.Add(item.Name);
            }

            foreach (SwitchDescriptor item in this)
            {
                string shortName = item.ShortName;
                if (shortName == string.Empty)
                    continue;

                if (hashSet.Contains(shortName) == true)
                    throw new SwitchException(Resource.SwitchWasAlreadyRegistered, shortName);
                hashSet.Add(shortName);
            }
        }

        public void AssertMutuallyExclusive()
        {
            Dictionary<string, bool> keys = new Dictionary<string, bool>();

            foreach (SwitchDescriptor item in this)
            {
                string key = item.MutuallyExclusive;
                if (key == string.Empty)
                    continue;

                if (keys.ContainsKey(key) == false)
                {
                    keys.Add(key, item.Required);
                }
                else
                {
                    bool required = keys[key];

                    if (required != item.Required)
                        throw new SwitchException("MutuallyExclusive가 같은 스위치는 Required 속성값이 모두 true거나 false여야 합니다.", item.ShortName);
                }
            }
        }

        public void Sort(Comparison<SwitchDescriptor> comparison)
        {
            this.internalDescriptors.Sort(comparison);
        }

        public SwitchDescriptor[] ToArray()
        {
            return this.internalDescriptors.ToArray();
        }

        public void Parse(string[] switchLines, object instance, ParsingOptions parsingOptions, out string[] unused)
        {
            using (new CommandLineParser.Tracer("Parsing switches"))
            {
                const string switchGroupName = "switch";
                const string argGourpName = "arg";
                HashSet<string> mutuallyExclusive = new HashSet<string>();
                List<string> unusedList = new List<string>();

                RegexOptions regexOptions = RegexOptions.ExplicitCapture;
                if ((parsingOptions & ParsingOptions.CaseSensitive) != ParsingOptions.CaseSensitive)
                {
                    regexOptions |= RegexOptions.IgnoreCase;
                }

                foreach (string switchLine in switchLines)
                {
                    SwitchDescriptor matchedSwitch = null;

                    Trace.WriteLine("finding switch : " + switchLine);

                    foreach (SwitchDescriptor switchDescriptor in this.internalDescriptors)
                    {
                        if (switchDescriptor.Parsed == true)
                            continue;

                        string pattern = switchDescriptor.GetPattern(switchGroupName, argGourpName);
                        Regex regex = new Regex(pattern, regexOptions);
                        Match match = regex.Match(switchLine);

                        string s = match.Groups[switchGroupName].ToString();
                        string a = match.Groups[argGourpName].ToString();

                        if (match.Success == true)
                        {
                            Trace.WriteLine("found matched switch : " + s + ", " + a );
                            if (switchDescriptor.MutuallyExclusive != string.Empty &&
                                mutuallyExclusive.Contains(switchDescriptor.MutuallyExclusive) == true)
                            {
                                throw new SwitchException("같은 값의 상호배타적 스위치가 이미 설정되었습니다.", switchDescriptor.ShortName);
                            }

                            switchDescriptor.Parse(a, instance);
                            matchedSwitch = switchDescriptor;
                            mutuallyExclusive.Add(switchDescriptor.MutuallyExclusive);

                            if (switchLine.Length != match.Length)
                            {
                                string sub = switchLine.Substring(match.Length).Trim();
                                SplitUnusedArgs(sub, unusedList);
                            }
                            break;
                        }
                    }

                    if (matchedSwitch == null)
                    {
                        Trace.WriteLine("no switch matched");
                        string pattern = string.Format(@"(?<switchName>{0}\S+)", SwitchAttribute.SwitchDelimiter);
                        Regex regex = new Regex(pattern, RegexOptions.ExplicitCapture);
                        Match match = regex.Match(switchLine);
                        if (match.Success == true)
                        {
                            string matchedString = match.Groups["switchName"].ToString();
                            throw new SwitchException(Resource.InvalidSwitchWasIncluded +"\r\n  - " + switchLine, matchedString);
                        }
                        else
                        {
                            throw new SwitchException(Resource.InvalidSwitchWasIncluded +"\r\n  - " + switchLine);
                        }
                    }
                }

                unused = unusedList.ToArray();
            }
        }

        public int Count
        {
            get { return (this.internalDescriptors as ICollection).Count; }
        }

        public SwitchDescriptor this[string switchName]
        {
            get
            {
                foreach (SwitchDescriptor item in this.internalDescriptors)
                {
                    if (item.Name == switchName)
                        return item;
                }
                return null;
            }
        }

        #region ICollection 멤버

        void ICollection.CopyTo(Array array, int index)
        {
            (this.internalDescriptors as ICollection).CopyTo(array, index);
        }

        bool ICollection.IsSynchronized
        {
            get { return (this.internalDescriptors as ICollection).IsSynchronized; }
        }

        object ICollection.SyncRoot
        {
            get { return (this.internalDescriptors as ICollection).SyncRoot; }
        }

        #endregion

        #region IEnumerable 멤버

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.internalDescriptors.GetEnumerator();
        }

        #endregion
    }
}