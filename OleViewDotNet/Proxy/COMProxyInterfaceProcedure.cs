﻿//    This file is part of OleViewDotNet.
//    Copyright (C) James Forshaw 2018
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

using NtApiDotNet.Ndr;
using System.Collections.Generic;
using System.Linq;

namespace OleViewDotNet.Proxy;

public sealed class COMProxyInterfaceProcedure
{
    private readonly COMProxyInterface m_intf;

    internal COMProxyInterfaceProcedure(COMProxyInterface intf, NdrProcedureDefinition entry)
    {
        m_intf = intf;
        Entry = entry;
        Parameters = entry.Params.Select(p => new COMProxyInterfaceProcedureParameter(intf, p)).ToList().AsReadOnly();
    }

    public string Name
    {
        get => Entry.Name;
        set => Entry.Name = m_intf.CheckName(Entry.Name, value);
    }

    public IReadOnlyList<COMProxyInterfaceProcedureParameter> Parameters { get; }

    public NdrProcedureDefinition Entry { get; }

    public COMProxyInterfaceProcedureParameter ReturnValue => new(m_intf, Entry.ReturnValue);

    public int ProcNum => Entry.ProcNum;
}
