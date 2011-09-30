﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Ntreev.Library
{
    /// <summary>
    /// 스위치의 정보를 담고 있는 클래스입니다.
    /// </summary>
    public class SwitchDescriptor
    {
        #region private variables

        readonly PropertyDescriptor propertyDescriptor;
        readonly SwitchAttribute switchAttribute;
        bool parsed = false;
        UsageProvider usageProvider;

        #endregion

        #region internal methods

        internal void Parse(string arg, object instance)
        {
            Type type = this.propertyDescriptor.PropertyType;
            object value = null;

            if (type == typeof(bool) && this.switchAttribute.GetArgSeperator() == null) /// 한가지 예외. 이것은 도저히 방법이 없다.
            {
                value = true;
            }
            else
            {
                TypeConverter typeConverter = this.propertyDescriptor.Converter;
                if (typeConverter.CanConvertFrom(typeof(string)) == false)
                    throw new NotSupportedException("타입컨버터에서 문자열에 의한 변환이 지원되질 않습니다.");

                try
                {
                    value = typeConverter.ConvertFrom(arg);
                }
                catch (Exception e)
                {
                    throw new SwitchException("잘못된 인수 형식입니다.", this.Name, e);
                }
            }

            if (value != null)
                this.propertyDescriptor.SetValue(instance, value);
            this.parsed = true;
        }

        internal string GetPattern(string switchGroupName, string argGroupName)
        {
            string quotes = string.Format(@"(""(?<{0}>.*)"")", argGroupName);
            string normal = string.Format(@"(?<{0}>(\S)+)", argGroupName);

            string pattern;
            if(this.ShortName == string.Empty)
                pattern = string.Format(@"^{0}(?<{1}>{2})", SwitchAttribute.SwitchDelimiter, switchGroupName, this.Name);
            else
                pattern = string.Format(@"^{0}(?<{1}>({2}|{3}))", SwitchAttribute.SwitchDelimiter, switchGroupName, this.Name, this.ShortName);

            char? argSeperator = this.switchAttribute.GetArgSeperator();
            if (this.ArgType != typeof(bool) || argSeperator != null)
            {
                if (argSeperator == null)
                {
                    pattern += string.Format(@"(((\s+)({0}|{1}))|($))", quotes, normal);
                }
                else
                {
                    if (argSeperator != char.MinValue)
                        pattern += argSeperator;
                    pattern += string.Format(@"(({0}|{1})|$)", quotes, normal);
                }
            }
            else
            {
                pattern += @"((\s+)|$)";
            }

            return pattern;
        }

        internal SwitchDescriptor(PropertyDescriptor propertyDescriptor, SwitchAttribute optionAttribute)
        {
            this.propertyDescriptor = propertyDescriptor;
            this.switchAttribute = optionAttribute;

            if (optionAttribute.UsageProvider == null)
            {
                this.usageProvider = new InternalUsageProvider(this);
            }
            else
            {
                this.usageProvider = TypeDescriptor.CreateInstance(
                    null, 
                    optionAttribute.UsageProvider,
                    new Type[] { typeof(SwitchDescriptor), },
                    new object[] { this, }) as UsageProvider;
            }

        }

        #endregion

        #region public properties

        /// <summary>
        /// 인자와 스위치가 한 문자열내에 포함되어 있을때 이를 구분하는 문자를 가져옵니다.
        /// </summary>
        /// <remarks>
        /// 이 속성의 값이 null이면 인자와 스위치는 공백으로 구분되어져 있습니다.
        /// <seealso cref="Char.MinValue"/>일 경우에는 스위치와 인자가 하나의 단어로 이루어져 있는 상태입니다.
        /// 그외에는 <see cref="Char.IsPunctuation"/>의 값이 true인 문자로 구분되어집니다.
        /// </remarks>
        public char? ArgSeperator
        {
            get { return this.switchAttribute.GetArgSeperator(); }
        }

        /// <summary>
        /// 스위치의 사용 방법을 제공하는 제공자의 인스턴스를 간져옵니다.
        /// </summary>
        public UsageProvider UsageProvider
        {
            get { return this.usageProvider; }
        }

        /// <summary>
        /// 스위치의 짧은 이름을 가져옵니다.
        /// </summary>
        public string ShortName
        {
            get
            {
                return this.switchAttribute.ShortName;
            }
        }

        /// <summary>
        /// 스위치의 이름을 가져옵니다.
        /// </summary>
        public string Name
        {
            get { return this.propertyDescriptor.Name; }
        }

        /// <summary>
        /// 스위치의 표시 이름을 가져옵니다.
        /// </summary>
        public string DisplayName
        {
            get { return this.propertyDescriptor.DisplayName; }
        }

        /// <summary>
        /// 스위치의 부가적인 설명을 가져옵니다.
        /// </summary>
        public string Description
        {
            get
            {
                return this.propertyDescriptor.Description;
            }
        }

        /// <summary>
        /// 파싱할때 해당 스위치가 꼭 필요한지에 대한 여부를 가져옵니다.
        /// </summary>
        public bool Required
        {
            get { return this.switchAttribute.Required; }
        }

        public string MutuallyExclusive
        {
            get { return this.switchAttribute.MutuallyExclusive; }
        }

        /// <summary>
        /// 파싱후 해당 스위치가 파싱에 성공했는지에 대한 여부를 가져옵니다.
        /// </summary>
        public bool Parsed
        {
            get { return this.parsed; }
        }

        /// <summary>
        /// 해당 스위치가 가지고 있는 인자의 타입을 가져옵니다.
        /// </summary>
        public Type ArgType
        {
            get { return this.propertyDescriptor.PropertyType; }
        }

        #endregion
    }
}
    

