﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ntreev.Library.Commands
{
    //public sealed class CommandCustomPropertyDescriptor : CommandMemberDescriptor
    //{
    //    private readonly PropertyInfo propertyInfo;
    //    private readonly string summary;
    //    private readonly string description;

    //    public CommandCustomPropertyDescriptor(string propertyName)
    //        : base(propertyInfo.GetCommandPropertyAttribute(), propertyInfo.Name)
    //    {
    //        var provider = CommandDescriptor.GetUsageDescriptionProvider(propertyInfo.DeclaringType);
    //        this.propertyInfo = propertyInfo;
    //        this.summary = provider.GetSummary(propertyInfo);
    //        this.description = provider.GetDescription(propertyInfo);
    //    }

    //    public override string DisplayName
    //    {
    //        get { return this.propertyInfo.GetDisplayName(); }
    //    }

    //    public override Type MemberType
    //    {
    //        get { return this.propertyInfo.PropertyType; }
    //    }

    //    public override string Summary
    //    {
    //        get { return this.summary; }
    //    }

    //    public override string Description
    //    {
    //        get { return this.description; }
    //    }

    //    public override object DefaultValue
    //    {
    //        get { return this.propertyInfo.GetDefaultValue(); }
    //    }

    //    public override bool IsToggle
    //    {
    //        get
    //        {
    //            if (this.IsRequired == false && this.MemberType == typeof(bool))
    //                return true;
    //            return base.IsToggle;
    //        }
    //    }

    //    public override IEnumerable<Attribute> Attributes
    //    {
    //        get
    //        {
    //            foreach (Attribute item in this.propertyInfo.GetCustomAttributes(true))
    //            {
    //                yield return item;
    //            }
    //        }
    //    }

    //    public override TypeConverter Converter
    //    {
    //        get { return this.propertyInfo.GetConverter(); }
    //    }

    //    protected override void SetValue(object instance, object value)
    //    {
    //        this.propertyInfo.SetValue(instance, value, null);
    //    }

    //    protected override object GetValue(object instance)
    //    {
    //        return this.propertyInfo.GetValue(instance, null);
    //    }
    //}
}