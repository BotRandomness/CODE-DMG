#pragma warning disable  // Disable all warnings

using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using ImGuiNET;

// C# Port of Mini memory editor for Dear ImGui - http://www.github.com/ocornut/imgui_club
// Port based on ImGui.NET C# memory editor, re-ported and updated for QuickImGUI.NET (https://github.com/Vawlpe/QuickImGui.NET/blob/main/src/QuickImGuiNET/Widgets/MemoryEditor.cs)
// re-updated for ImGui.NET
// Orignal based on ocornet's Dear ImGui "imgui_memory_editor.h"

public class MemoryEditor {
    private string AddrInputBuf = string.Empty;
    public int base_display_addr;
    public int Cols = 16; // number of columns to display.

    // [Internal State]
    private bool ContentsWidthChanged;

    // Settings
    public byte[] Data;
    private int DataEditingAddr = -1;
    private bool DataEditingTakeFocus;
    private string DataInputBuf = string.Empty;
    private int DataPreviewAddr = -1;
    private int GotoAddr = -1;
    public uint HighlightColor; // background color of highlighted bytes.

    public Func<byte[], int, bool>?
        HighlightFn; // optional handler to return Highlight property (to support non-contiguous highlighting).

    private int HighlightMax = -1;

    private int HighlightMin = -1;

    // number of addr digits to display (default calculated based on maximum displayed addr).
    public int OptAddrDigitsCount;
    public float OptFooterExtraHeight; // space to reserve at the bottom of the widget to add custom widgets
    public bool OptGreyOutZeroes; // display null/zero bytes using the TextDisabled color.
    public int OptMidColsCount; // set to 0 to disable extra spacing between every mid-cols.

    public bool OptShowAscii = true; // display ASCII representation on the right side.

    // display a footer previewing the decimal/binary/hex/float representation of the currently selected bytes.
    public bool OptShowDataPreview = true;

    // display options button/context menu. when disabled, options will be locked unless you provide your own UI for them.
    public bool OptShowOptions;
    public bool OptUpperCaseHex = true; // display hexadecimal values as "FF" instead of "ff".
    private ImGuiDataType PreviewDataType = ImGuiDataType.U8;
    public Func<byte[], int, byte>? ReadFn; // optional handler to read bytes.
    public bool ReadOnly; // disable any editing.
    private Sizes s;
    public Action<byte[], int, byte>? WriteFn; // optional handler to write bytes.

    public MemoryEditor()
    {

    }

    public void GotoAddrAndHighlight(int addr_min, int addr_max)
    {
        GotoAddr = addr_min;
        HighlightMin = addr_min;
        HighlightMax = addr_max;
    }

    private void CalcSizes()
    {
        var style = ImGui.GetStyle();
        s.AddrDigitsCount = OptAddrDigitsCount;
        if (s.AddrDigitsCount == 0)
            for (var n = base_display_addr + Data.Length - 1; n > 0; n >>= 4)
                s.AddrDigitsCount++;
        s.LineHeight = ImGui.GetTextLineHeight();
        s.GlyphWidth = ImGui.CalcTextSize("F").X + 1; // We assume the font is mono-space
        s.HexCellWidth =
            (int)(s.GlyphWidth *
                  2.5f); // "FF " we include trailing space in the width to easily catch clicks everywhere
        s.SpacingBetweenMidCols =
            (int)(s.HexCellWidth * 0.25f); // Every OptMidColsCount columns we add a bit of extra spacing
        s.PosHexStart = (s.AddrDigitsCount + 2) * s.GlyphWidth;
        s.PosHexEnd = s.PosHexStart + s.HexCellWidth * Cols;
        s.PosAsciiStart = s.PosAsciiEnd = s.PosHexEnd;
        if (OptShowAscii)
        {
            s.PosAsciiStart = s.PosHexEnd + s.GlyphWidth * 1;
            if (OptMidColsCount > 0)
                s.PosAsciiStart += (Cols + OptMidColsCount - 1) / OptMidColsCount * s.SpacingBetweenMidCols;
            s.PosAsciiEnd = s.PosAsciiStart + Cols * s.GlyphWidth;
        }

        s.WindowWidth = s.PosAsciiEnd + style.ScrollbarSize + style.WindowPadding.X * 2 + s.GlyphWidth;
    }

    public unsafe void RenderContent()
    {
        if (ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows) &&
            ImGui.IsMouseReleased(ImGuiMouseButton.Right))
            ImGui.OpenPopup("context");

        if (Cols < 1)
            Cols = 1;

        CalcSizes();
        var style = ImGui.GetStyle();

        //Added this to top
        if (OptShowDataPreview)
        {
            DrawPreviewLine();
            ImGui.Separator();
        }

        // We begin into our scrolling region with the 'ImGuiWindowFlags_NoMove' in order to prevent click from moving the window.
        // This is used as a facility since our main click detection code doesn't assign an ActiveId so the click would normally be caught as a window-move.
        var height_separator = style.ItemSpacing.Y;
        var footer_height = OptFooterExtraHeight;

        if (OptShowOptions)
            footer_height += height_separator + ImGui.GetFrameHeightWithSpacing();
        if (OptShowDataPreview)
            footer_height += height_separator + ImGui.GetFrameHeightWithSpacing() +
                             ImGui.GetTextLineHeightWithSpacing() * 3;

        if (ImGui.BeginChild("##scrolling"))
        {
            var draw_list = ImGui.GetWindowDrawList();

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

            // We are not really using the clipper API correctly here, because we rely on visible_start_addr/visible_end_addr for our scrolling function.
            var line_total_count = (Data.Length + Cols - 1) / Cols;
            ImGuiListClipper clipper = new();
            ImGuiNative.ImGuiListClipper_Begin(&clipper, line_total_count, s.LineHeight);

            var data_next = false;

            if (ReadOnly || DataEditingAddr >= Data.Length)
                DataEditingAddr = -1;
            if (DataPreviewAddr >= Data.Length)
                DataPreviewAddr = -1;

            var preview_data_type_size = OptShowDataPreview ? DataTypeGetSize(PreviewDataType) : 0;
            var data_editing_addr_next = -1;

            if (DataEditingAddr != -1)
            {
                // Move cursor but only apply on next frame so scrolling with be synchronized (because currently we can't change the scrolling while the window is being rendered)
                if (ImGui.IsKeyPressed(ImGuiKey.UpArrow) && DataEditingAddr >= Cols)
                    data_editing_addr_next = DataEditingAddr - Cols;
                else if (ImGui.IsKeyPressed(ImGuiKey.DownArrow) && DataEditingAddr < Data.Length - Cols)
                    data_editing_addr_next = DataEditingAddr + Cols;
                else if (ImGui.IsKeyPressed(ImGuiKey.LeftArrow) && DataEditingAddr > 0)
                    data_editing_addr_next = DataEditingAddr - 1;
                else if (ImGui.IsKeyPressed(ImGuiKey.RightArrow) && DataEditingAddr < Data.Length - 1)
                    data_editing_addr_next = DataEditingAddr + 1;
            }

            // Draw vertical separator
            var window_pos = ImGui.GetWindowPos();
            if (OptShowAscii)
                draw_list.AddLine(new Vector2(window_pos.X + s.PosAsciiStart - s.GlyphWidth, window_pos.Y),
                    new Vector2(window_pos.X + s.PosAsciiStart - s.GlyphWidth, window_pos.Y + 9999),
                    ImGui.GetColorU32(ImGuiCol.Border));

            var color_text = ImGui.GetColorU32(ImGuiCol.Text);
            var color_disabled = OptGreyOutZeroes ? ImGui.GetColorU32(ImGuiCol.TextDisabled) : color_text;
            var format_data = (int n, int a) =>
                OptUpperCaseHex ? $"{$"{a:X}".PadLeft(n)}" : $"{$"{a:x}".PadLeft(n)}";
            var format_address = format_data;
            var format_byte = (byte b) => OptUpperCaseHex ? $"{b:X2}" : $"{b:x2}";
            var format_byte_space = (byte b) => $"{format_byte(b)} ";

            while (ImGuiNative.ImGuiListClipper_Step(&clipper) > 0)
                for (var line_i = clipper.DisplayStart; line_i < clipper.DisplayEnd; line_i++)
                {
                    var addr = line_i * Cols;
                    ImGui.Text(format_address(s.AddrDigitsCount, base_display_addr + addr));
                    // Draw Hexadecimal
                    for (var n = 0; n < Cols && addr < Data.Length; n++, addr++)
                    {
                        var byte_pos_x = s.PosHexStart + s.HexCellWidth * n;
                        if (OptMidColsCount > 0)
                            byte_pos_x += n / OptMidColsCount * s.SpacingBetweenMidCols;
                        ImGui.SameLine(byte_pos_x);

                        // Draw highlight
                        var is_highlight_from_user_range = addr >= HighlightMin && addr < HighlightMax;
                        var is_highlight_from_user_func = HighlightFn is not null && HighlightFn(Data, addr);
                        var is_highlight_from_preview = addr >= DataPreviewAddr &&
                                                        addr < DataPreviewAddr + preview_data_type_size;
                        if (is_highlight_from_user_range || is_highlight_from_user_func ||
                            is_highlight_from_preview)
                        {
                            var cpos = ImGui.GetCursorScreenPos();
                            var highlight_width = s.GlyphWidth * 2;
                            var is_next_byte_highlighted = addr + 1 < Data.Length &&
                                                           ((HighlightMax != -1 && addr + 1 < HighlightMax) ||
                                                            (HighlightFn is not null &&
                                                             HighlightFn(Data, addr + 1)));
                            if (is_next_byte_highlighted || n + 1 == Cols)
                            {
                                highlight_width = s.HexCellWidth;
                                if (OptMidColsCount > 0 && n > 0 && n + 1 < Cols &&
                                    (n + 1) % OptMidColsCount == 0)
                                    highlight_width += s.SpacingBetweenMidCols;
                            }

                            draw_list.AddRectFilled(cpos,
                                new Vector2(cpos.X + highlight_width, cpos.Y + s.LineHeight), HighlightColor);
                        }

                        if (DataEditingAddr == addr)
                        {
                            // Display text input on current byte
                            var data_write = false;
                            ImGui.PushID(addr);
                            if (DataEditingTakeFocus)
                                ImGui.SetKeyboardFocusHere(0);

                            UserData user_data = new();
                            user_data.CursorPos = -1;
                            var flags = ImGuiInputTextFlags.CharsHexadecimal |
                                        ImGuiInputTextFlags.EnterReturnsTrue |
                                        ImGuiInputTextFlags.AutoSelectAll |
                                        ImGuiInputTextFlags.NoHorizontalScroll |
                                        ImGuiInputTextFlags.CallbackAlways |
                                        ImGuiInputTextFlags.AlwaysOverwrite;
                            ImGui.SetNextItemWidth(s.GlyphWidth * 2);
                            if (ImGui.InputText("##data", ref DataInputBuf, 32, flags, UserData.Callback,
                                    new IntPtr(&user_data)))
                                data_write = data_next = true;
                            else if (!DataEditingTakeFocus && !ImGui.IsItemActive())
                                DataEditingAddr = data_editing_addr_next = -1;
                            DataEditingTakeFocus = false;
                            if (user_data.CursorPos >= 2)
                                data_write = data_next = true;
                            if (data_editing_addr_next != -1)
                                data_write = data_next = false;
                            if (data_write && int.TryParse(DataInputBuf, NumberStyles.HexNumber, null,
                                    out var data_input_value))
                            {
                                if (WriteFn is not null)
                                    WriteFn(Data, addr, (byte)data_input_value);
                                else
                                    Data[addr] = (byte)data_input_value;
                            }

                            ImGui.PopID();
                        }
                        else
                        {
                            // NB: The trailing space is not visible but ensure there's no gap that the mouse cannot click on.
                            var b = ReadFn?.Invoke(Data, addr) ?? Data[addr];
                            {
                                if (b == 0 && OptGreyOutZeroes)
                                    ImGui.TextDisabled("00 ");
                                else
                                    ImGui.Text(format_byte_space(b));
                            }

                            if (ReadOnly || !ImGui.IsItemHovered() || !ImGui.IsMouseClicked(0)) continue;
                            DataEditingTakeFocus = true;
                            data_editing_addr_next = addr;
                        }
                    }

                    // Draw ASCII values
                    if (!OptShowAscii) continue;

                    ImGui.SameLine(s.PosAsciiStart);
                    var pos = ImGui.GetCursorScreenPos();
                    addr = line_i * Cols;
                    ImGui.PushID(line_i);
                    if (ImGui.InvisibleButton("ascii", new Vector2(s.PosAsciiEnd - s.PosAsciiStart, s.LineHeight)))
                    {
                        DataEditingAddr = DataPreviewAddr =
                            addr + (int)((ImGui.GetIO().MousePos.X - pos.X) / s.GlyphWidth);
                        DataEditingTakeFocus = true;
                    }

                    ImGui.PopID();
                    for (var n = 0; n < Cols && addr < Data.Length; n++, addr++)
                    {
                        if (addr == DataEditingAddr)
                        {
                            draw_list.AddRectFilled(pos, new Vector2(pos.X + s.GlyphWidth, pos.Y + s.LineHeight),
                                ImGui.GetColorU32(ImGuiCol.FrameBg));
                            draw_list.AddRectFilled(pos, new Vector2(pos.X + s.GlyphWidth, pos.Y + s.LineHeight),
                                ImGui.GetColorU32(ImGuiCol.TextSelectedBg));
                        }

                        var c = ReadFn?.Invoke(Data, addr) ?? Data[addr];
                        var display_c = c is < 32 or >= 128 ? '.' : Encoding.ASCII.GetChars(new[] { c })[0];
                        draw_list.AddText(pos, display_c != '.' ? color_text : color_disabled, $"{display_c}");
                        pos.X += s.GlyphWidth;
                    }
                }

            ImGui.PopStyleVar(2);
            ImGui.EndChild();

            // Notify the main window of our ideal child content size (FIXME: we are missing an API to get the contents size from the child)
            ImGui.SetCursorPosX(s.WindowWidth);

            if (data_next && DataEditingAddr + 1 < Data.Length)
            {
                DataEditingAddr = DataPreviewAddr = DataEditingAddr + 1;
                DataEditingTakeFocus = true;
            }
            else if (data_editing_addr_next != -1)
            {
                DataEditingAddr = DataPreviewAddr = data_editing_addr_next;
                DataEditingTakeFocus = true;
            }

            var lock_show_data_preview = OptShowDataPreview;
            if (OptShowOptions)
            {
                ImGui.Separator();
                DrawOptionsLine();
            }

            //if (lock_show_data_preview)
            //{
            //    ImGui.Separator();
            //    DrawPreviewLine();
            //}
        }

        if (!ContentsWidthChanged) return;
        CalcSizes();
        ImGui.SetWindowSize(new Vector2(s.WindowWidth, ImGui.GetWindowSize().Y));
    }

    private void DrawOptionsLine()
    {
        var style = ImGui.GetStyle();

        // Options menu
        if (ImGui.Button("Options"))
            ImGui.OpenPopup("context");
        if (ImGui.BeginPopup("context"))
        {
            ImGui.SetNextItemWidth(s.GlyphWidth * 7 + style.FramePadding.X * 2.0f);
            if (ImGui.DragInt("##cols", ref Cols, 0.2f, 4, 32, "%d cols"))
            {
                ContentsWidthChanged = true;
                if (Cols < 1) Cols = 1;
            }

            ImGui.Checkbox("Show Data Preview", ref OptShowDataPreview);
            if (ImGui.Checkbox("Show Ascii", ref OptShowAscii))
                ContentsWidthChanged = true;

            ImGui.Checkbox("Grey out zeroes", ref OptGreyOutZeroes);
            ImGui.Checkbox("Uppercase Hex", ref OptUpperCaseHex);

            ImGui.EndPopup();
        }

        ImGui.SameLine();
        ImGui.Text(OptUpperCaseHex
            ? $"Range {$"{base_display_addr:X}".PadLeft(s.AddrDigitsCount, '0')}"
              + $"..{$"{base_display_addr + Data.Length - 1:X}".PadLeft(s.AddrDigitsCount, '0')}"
            : $"Range {$"{base_display_addr:x}".PadLeft(s.AddrDigitsCount, '0')}"
              + $"..{$"{base_display_addr + Data.Length - 1:x}".PadLeft(s.AddrDigitsCount, '0')}");
        ImGui.SameLine();
        ImGui.SetNextItemWidth((s.AddrDigitsCount + 1) * s.GlyphWidth + style.FramePadding.X * 2.0f);
        if (ImGui.InputText("##addr", ref AddrInputBuf, 32,
                ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.EnterReturnsTrue))
        {
            int goto_addr;
            if (int.TryParse(AddrInputBuf, NumberStyles.HexNumber, null, out goto_addr))
            {
                GotoAddr = goto_addr - base_display_addr;
                HighlightMin = HighlightMax = -1;
            }
        }

        if (GotoAddr == -1) return;
        if (GotoAddr < Data.Length)
        {
            if (ImGui.BeginChild("##scrolling"))
            {
                ImGui.SetScrollFromPosY(ImGui.GetCursorStartPos().Y + GotoAddr / Cols * ImGui.GetTextLineHeight());
                ImGui.EndChild();
            }

            DataEditingAddr = DataPreviewAddr = GotoAddr;
            DataEditingTakeFocus = true;
        }

        GotoAddr = -1;
    }

    private void DrawPreviewLine()
    {
        var style = ImGui.GetStyle();
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Preview as:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(s.GlyphWidth * 10.0f + style.FramePadding.X * 2.0f + style.ItemInnerSpacing.X);
        if (ImGui.BeginCombo("##combo_type", DataTypeGetDesc(PreviewDataType), ImGuiComboFlags.HeightLargest))
        {
            for (var n = 0; n < (int)ImGuiDataType.COUNT; n++)
                if (ImGui.Selectable(DataTypeGetDesc((ImGuiDataType)n), (int)PreviewDataType == n))
                    PreviewDataType = (ImGuiDataType)n;
            ImGui.EndCombo();
        }

        var buf = string.Empty;
        var x = s.GlyphWidth * 6.0f;
        var has_value = DataPreviewAddr != -1;
        if (has_value)
            DrawPreviewData(PreviewDataType, DataFormat.Dec, out buf);
        ImGui.Text("Dec");
        ImGui.SameLine(x);
        ImGui.TextUnformatted(has_value ? buf : "N/A");
        if (has_value)
            DrawPreviewData(PreviewDataType, DataFormat.Hex, out buf);
        ImGui.Text("Hex");
        ImGui.SameLine(x);
        ImGui.TextUnformatted(has_value ? buf : "N/A");
        if (has_value)
            DrawPreviewData(PreviewDataType, DataFormat.Bin, out buf);
        ImGui.Text("Bin");
        ImGui.SameLine(x);
        ImGui.TextUnformatted(has_value ? buf : "N/A");
    }

    // Utilities for Data Preview
    private string DataTypeGetDesc(ImGuiDataType data_type)
    {
        var descs = new[]
            { "Int8", "Uint8", "Int16", "Uint16", "Int32", "Uint32", "Int64", "Uint64", "Float", "Double" };
        return descs[(int)data_type];
    }

    private int DataTypeGetSize(ImGuiDataType data_type)
    {
        var sizes = new[] { 1, 1, 2, 2, 4, 4, 8, 8, sizeof(float), sizeof(double) };
        return sizes[(int)data_type];
    }

    private string FormatBinary(ref byte[] buf, int width)
    {
        var out_buf = string.Empty;
        var n = width / 8;
        for (var j = n - 1; j >= 0; --j)
        {
            for (var i = 0; i < 8; ++i)
                //out_buf += (buf[j] & (1 << (7 - i))) == 0 ? '1' : '0';
                out_buf += (buf[j] & (1 << (7 - i))) == 0 ? '0' : '1';
            out_buf += ' ';
        }

        return out_buf;
    }

    // [Internal]
    private void DrawPreviewData(ImGuiDataType data_type, DataFormat data_format, out string out_buf)
    {
        var buf = new byte[8];
        var elem_size = DataTypeGetSize(data_type);
        var size = DataPreviewAddr + elem_size > Data.Length ? Data.Length - DataPreviewAddr : elem_size;
        if (ReadFn is not null)
            for (var i = 0; i < size; ++i)
                buf[i] = ReadFn(Data, DataPreviewAddr + i);
        else
            buf = Data.Skip(DataPreviewAddr).Take(size).ToArray();

        if (DataTypeGetSize(data_type) != buf.Length)
        {
            out_buf = string.Empty;
            return;
        }

        if (data_format == DataFormat.Bin)
        {
            var binbuf = new byte[8];
            Array.Copy(buf, binbuf, size);
            out_buf = FormatBinary(ref binbuf, size * 8);
            return;
        }

        //out_buf[0] = 0;
        out_buf = string.Empty;
        switch (data_type)
        {
            case ImGuiDataType.S8:
                var int8 = (sbyte)buf.First();
                out_buf = data_format switch
                {
                    DataFormat.Dec => $"{int8:D}",
                    DataFormat.Hex => $"0x{int8:x2}"
                };
                break;
            case ImGuiDataType.U8:
                var uint8 = buf.First();
                out_buf = data_format switch
                {
                    DataFormat.Dec => $"{uint8:D}",
                    DataFormat.Hex => $"0x{uint8:x2}"
                };
                break;
            case ImGuiDataType.S16:
                var int16 = BitConverter.ToInt16(buf);
                out_buf = data_format switch
                {
                    DataFormat.Dec => $"{int16:D}",
                    DataFormat.Hex => $"0x{int16:x4}"
                };
                break;
            case ImGuiDataType.U16:
                var uint16 = BitConverter.ToUInt16(buf);
                out_buf = data_format switch
                {
                    DataFormat.Dec => $"{uint16:D}",
                    DataFormat.Hex => $"0x{uint16:x4}"
                };
                break;
            case ImGuiDataType.S32:
                var int32 = BitConverter.ToInt32(buf);
                out_buf = data_format switch
                {
                    DataFormat.Dec => $"{int32:D}",
                    DataFormat.Hex => $"0x{int32:x8}"
                };
                break;
            case ImGuiDataType.U32:
                var uint32 = BitConverter.ToUInt32(buf);
                out_buf = data_format switch
                {
                    DataFormat.Dec => $"{uint32:D}",
                    DataFormat.Hex => $"0x{uint32:x8}"
                };
                break;
            case ImGuiDataType.S64:
                var int64 = BitConverter.ToInt64(buf);
                out_buf = data_format switch
                {
                    DataFormat.Dec => $"{int64:D}",
                    DataFormat.Hex => $"0x{int64:x16}"
                };
                break;
            case ImGuiDataType.U64:
                var uint64 = BitConverter.ToUInt64(buf);
                out_buf = data_format switch
                {
                    DataFormat.Dec => $"{uint64:D}",
                    DataFormat.Hex => $"0x{uint64:x16}"
                };
                break;
            case ImGuiDataType.Float:
                var float32 = BitConverter.ToSingle(buf);
                var bits_f32 = BitConverter.SingleToInt32Bits(float32);
                out_buf = data_format switch
                {
                    DataFormat.Dec => $"{float32:N}".PadRight(128, '\0')[..128],
                    DataFormat.Hex => $"{((bits_f32 & (1L << 31)) != 0 ? "-" : string.Empty)}" + //Negative
                                      $"1.{bits_f32 & 0x7FFFFF:X}" + // Mantissa in hex
                                      $"+{(bits_f32 >> 23) & 0xFF:X}" // Exponent in hex
                };
                break;
            case ImGuiDataType.Double:
                var float64 = BitConverter.ToDouble(buf);
                var bits_f64 = BitConverter.DoubleToInt64Bits(float64);
                out_buf = data_format switch
                {
                    DataFormat.Dec => $"{float64:N}".PadRight(128, '\0')[..128],
                    DataFormat.Hex => $"{((bits_f64 & (1L << 63)) != 0 ? "-" : string.Empty)}" + //Negative
                                      $"1.{bits_f64 & 0xFFFFFFFFFFFFF:X}" + // Mantissa in hex
                                      $"+{(int)((bits_f64 >> 52) & 0x7FF):X}" // Exponent in hex
                };
                break;
        }
    }

    private enum DataFormat
    {
        Bin = 0,
        Dec = 1,
        Hex = 2
    }

    private struct Sizes
    {
        public int AddrDigitsCount;
        public float LineHeight;
        public float GlyphWidth;
        public float HexCellWidth;
        public float SpacingBetweenMidCols;
        public float PosHexStart;
        public float PosHexEnd;
        public float PosAsciiStart;
        public float PosAsciiEnd;
        public float WindowWidth;
    }

    private unsafe struct UserData
    {
        public UserData()
        {
            CursorPos = 0;
        }

        // FIXME: We should have a way to retrieve the text edit cursor position more easily in the API, this is rather tedious. This is such a ugly mess we may be better off not using InputText() at all here.
        public static int Callback(ImGuiInputTextCallbackData* _data)
        {
            var data = new ImGuiInputTextCallbackDataPtr(_data);
            var user_data = (UserData*)data.UserData;
            if (data.HasSelection())
                user_data->CursorPos = data.CursorPos;

            if (data.SelectionStart != 0 || data.SelectionEnd != data.BufTextLen) return 0;
            // When not editing a byte, always refresh its InputText content pulled from underlying memory data
            // (this is a bit tricky, since InputText technically "owns" the master copy of the buffer we edit it in there)
            data.DeleteChars(0, data.BufTextLen);
            data.InsertChars(0, Marshal.PtrToStringAuto(new IntPtr(user_data->CurrentBufOverwrite)));
            data.SelectionStart = 0;
            data.SelectionEnd = 2;
            data.CursorPos = 0;

            return 0;
        }

        public fixed byte CurrentBufOverwrite[3]; // Input
        public int CursorPos; // Output
    }
}