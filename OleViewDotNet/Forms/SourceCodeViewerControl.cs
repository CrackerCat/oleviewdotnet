﻿//    This file is part of OleViewDotNet.
//    Copyright (C) James Forshaw 2014
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

using ICSharpCode.TextEditor.Document;
using NtApiDotNet.Ndr;
using OleViewDotNet.Database;
using OleViewDotNet.Utilities.Format;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace OleViewDotNet.Forms;

internal partial class SourceCodeViewerControl : UserControl
{
    private COMRegistry m_registry;
    private object m_selected_obj;
    private ICOMSourceCodeFormattable m_formattable_obj;
    private COMSourceCodeBuilderType m_output_type;
    private bool m_hide_comments;
    private bool m_interfaces_only;
    private bool m_hide_parsing_options;

    private class NdrTextMarker : TextMarker
    {
        public INdrNamedObject Tag { get; }

        public NdrTextMarker(int offset, int length, TextMarkerType textMarkerType, INdrNamedObject tag)
            : base(offset, length, textMarkerType)
        {
            Tag = tag;
        }
    }

    public SourceCodeViewerControl()
    {
        InitializeComponent();
        textEditor.SetHighlighting("C#");
        textEditor.IsReadOnly = true;
        m_hide_comments = true;
        toolStripMenuItemHideComments.Checked = true;
        m_interfaces_only = true;
        toolStripMenuItemInterfacesOnly.Checked = true;
        toolStripMenuItemIDLOutputType.Checked = true;
        m_output_type = COMSourceCodeBuilderType.Idl;
        SetText(string.Empty, Array.Empty<NdrFormatterNameTag>());
    }

    private void SetText(string text, IEnumerable<NdrFormatterNameTag> tags)
    {
        textEditor.Text = text.TrimEnd();
        foreach (var tag in tags)
        {
            textEditor.Document.MarkerStrategy.AddMarker(new NdrTextMarker(tag.Offset, tag.Length, TextMarkerType.Underlined, tag.Entry));
        }
        textEditor.Refresh();
    }

    internal void SetRegistry(COMRegistry registry)
    {
        m_registry = registry;
    }

    private static bool IsParsed(ICOMSourceCodeFormattable obj)
    {
        if (obj is ICOMSourceCodeParsable parsable)
        {
            return parsable.IsSourceCodeParsed;
        }
        return true;
    }

    internal void Format(COMSourceCodeBuilder builder, ICOMSourceCodeFormattable formattable)
    {
        if (!IsParsed(formattable))
        {
            builder.AppendLine($"'{m_selected_obj}' needs to be parsed before it can be shown.");
        }
        else
        {
            formattable.Format(builder);
        }
    }

    internal void Format()
    {
        COMSourceCodeBuilder builder = new(m_registry)
        {
            InterfacesOnly = m_interfaces_only,
            HideComments = m_hide_comments,
            OutputType = m_output_type
        };

        if (m_formattable_obj?.IsFormattable == true)
        {
            Format(builder, m_formattable_obj);
        }
        else
        {
            builder.AppendLine(m_selected_obj is null ?
                "No formattable object selected"
                : $"'{m_selected_obj}' is not formattable.");
        }
        SetText(builder.ToString(), builder.Tags);
    }

    internal object SelectedObject
    {
        get => m_selected_obj;
        set
        {
            m_selected_obj = value;
            if (value is IEnumerable<ICOMSourceCodeFormattable> list)
            {
                m_formattable_obj = new SourceCodeFormattableList(list);
            }
            else if (value is ICOMSourceCodeFormattable formattable)
            {
                m_formattable_obj = formattable;
            }
            else
            {
                m_formattable_obj = null;
            }

            if (!IsParsed(m_formattable_obj) && AutoParse)
            {
                ParseSourceCode();
            }

            parseSourceCodeToolStripMenuItem.Enabled = m_formattable_obj is not null && !IsParsed(m_formattable_obj);
            Format();
        }
    }

    internal bool InterfacesOnly
    {
        get => m_interfaces_only;
        set => m_interfaces_only = value;
    }

    private void OnHideParsingOptionsChanged()
    {
        parseSourceCodeToolStripMenuItem.Visible = !m_hide_parsing_options;
        autoParseToolStripMenuItem.Visible = !m_hide_parsing_options;
    }

    internal bool AutoParse
    {
        get => autoParseToolStripMenuItem.Checked;
        set => autoParseToolStripMenuItem.Checked = value;
    }

    internal bool HideParsingOptions
    {
        get => m_hide_parsing_options;
        set
        {
            m_hide_parsing_options = value;
            OnHideParsingOptionsChanged();
        }
    }

    private void toolStripMenuItemIDLOutputType_Click(object sender, EventArgs e)
    {
        m_output_type = COMSourceCodeBuilderType.Idl;
        toolStripMenuItemIDLOutputType.Checked = true;
        toolStripMenuItemCppOutputType.Checked = false;
        toolStripMenuItemGenericOutputType.Checked = false;
        Format();
    }

    private void toolStripMenuItemCppOutputType_Click(object sender, EventArgs e)
    {
        m_output_type = COMSourceCodeBuilderType.Cpp;
        toolStripMenuItemIDLOutputType.Checked = false;
        toolStripMenuItemCppOutputType.Checked = true;
        toolStripMenuItemGenericOutputType.Checked = false;
        Format();
    }

    private void toolStripMenuItemGenericOutputType_Click(object sender, EventArgs e)
    {
        m_output_type = COMSourceCodeBuilderType.Generic;
        toolStripMenuItemIDLOutputType.Checked = false;
        toolStripMenuItemCppOutputType.Checked = false;
        toolStripMenuItemGenericOutputType.Checked = true;
        Format();
    }

    private void toolStripMenuItemHideComments_Click(object sender, EventArgs e)
    {
        m_hide_comments = !m_hide_comments;
        toolStripMenuItemHideComments.Checked = m_hide_comments;
        Format();
    }

    private void toolStripMenuItemInterfacesOnly_Click(object sender, EventArgs e)
    {
        m_interfaces_only = !m_interfaces_only;
        toolStripMenuItemInterfacesOnly.Checked = m_interfaces_only;
        Format();
    }

    private void exportToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using SaveFileDialog dlg = new();
        dlg.Filter = "All Files (*.*)|*.*";
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                File.WriteAllText(dlg.FileName, textEditor.Text);
            }
            catch (Exception ex)
            {
                EntryPoint.ShowError(this, ex);
            }
        }
    }

    private void parseSourceCodeToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ParseSourceCode();
        Format();
    }

    private void ParseSourceCode()
    {
        try
        {
            if (m_formattable_obj is ICOMSourceCodeParsable parsable && !parsable.IsSourceCodeParsed)
            {
                parsable.ParseSourceCode();
                parseSourceCodeToolStripMenuItem.Enabled = false;
            }
        }
        catch (Exception ex)
        {
            m_formattable_obj = new SourceCodeFormattableText($"ERROR: {ex.Message}");
        }
    }

    private void autoParseToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (AutoParse)
        {
            SelectedObject = m_selected_obj;
        }
    }

    private NdrTextMarker GetTagAtCaret()
    {
        var tags = textEditor.Document.MarkerStrategy.GetMarkers(textEditor.ActiveTextAreaControl.Caret.Position);
        if (tags.Count > 0)
        {
            return (NdrTextMarker)tags[0];
        }
        return null;
    }

    private void contextMenuStrip_Opening(object sender, CancelEventArgs e)
    {
        editNameToolStripMenuItem.Enabled = GetTagAtCaret() is not null;
    }

    private void editNameToolStripMenuItem_Click(object sender, EventArgs e)
    {
        NdrTextMarker tag = GetTagAtCaret();
        if (tag is null)
        {
            return;
        }

        using GetTextForm frm = new(tag.Tag.Name);
        frm.Text = "Edit Proxy Name";
        if (frm.ShowDialog(this) == DialogResult.OK)
        {
            tag.Tag.Name = frm.Data;
            textEditor.Document.ReadOnly = false;
            textEditor.Document.Replace(tag.Offset, tag.Length, frm.Data);
            textEditor.Document.MarkerStrategy.AddMarker(new NdrTextMarker(tag.Offset, frm.Data.Length, TextMarkerType.Underlined, tag.Tag));
            textEditor.Document.ReadOnly = true;
            textEditor.Refresh();
            if (m_selected_obj is ICOMSourceCodeEditable editable)
            {
                editable.Update();
            }
        }
    }

    private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Format();
    }
}
