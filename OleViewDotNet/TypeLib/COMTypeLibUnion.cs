﻿//    This file is part of OleViewDotNet.
//    Copyright (C) James Forshaw 2014, 2016
//
//    OleViewDotNet is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    OleViewDotNet is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with OleViewDotNet.  If not, see <http://www.gnu.org/licenses/>.

using OleViewDotNet.TypeLib.Instance;
using OleViewDotNet.Utilities.Format;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;

namespace OleViewDotNet.TypeLib;

public sealed class COMTypeLibUnion : COMTypeLibComplexType
{
    internal COMTypeLibUnion(COMTypeDocumentation doc, TYPEATTR attr, IEnumerable<COMTypeCustomDataItem> custom_data)
       : base(doc, attr, custom_data)
    {
    }

    internal override void FormatInternal(COMSourceCodeBuilder builder)
    {
        builder.AppendLine($"typedef {GetTypeAttributes().FormatAttrs()}union {{");
        using (builder.PushIndent(4))
        {
            foreach (var v in Fields)
            {
                builder.AppendLine($"{v.Type.FormatType()} {v.Name}{v.Type.FormatPostName()};");
            }
        }
        builder.AppendLine($"}} {Name};");
    }
}

