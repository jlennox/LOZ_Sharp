﻿namespace z1.UI;

internal static class ZeldaString
{
    public static unsafe string FromBytes(byte[] bytes)
    {
        Span<char> chars = stackalloc char[bytes.Length];
        for (var i = 0; i < bytes.Length; i++)
        {
            chars[i] = CharFromByte(bytes[i]);
        }
        return new string(chars);
    }

    public static IEnumerable<char> EnumerateBytes(IEnumerable<byte> bytes) => bytes.Select(CharFromByte);
    public static IEnumerable<byte> EnumerateText(IEnumerable<char> text) => text.Select(ByteFromChar);

    public static byte ByteFromChar(char c) => c switch {
        ' ' => 0x24,
        ',' => 0x28,
        '!' => 0x29,
        '\'' => 0x2A,
        '&' => 0x2B,
        '.' => 0x2C, // 0xec?
        '"' => 0x2D,
        '?' => 0x2E,
        '-' => 0x62,
        >= '0' and <= '9' => (byte)(c - '0'),
        >= 'a' and <= 'z' => (byte)(c - 'a' + 0x0A),
        >= 'A' and <= 'Z' => (byte)(char.ToLower(c) - 'a' + 0x0A),
        _ => 0,
    };

    public static char CharFromByte(byte b) => b switch {
        0x60 => ' ',
        0x24 => ' ',
        0x28 => ',',
        0x29 => '!',
        0x2A => '\'',
        0x2B => '&',
        0x2C => '.',
        0x2D => '"',
        0x2E => '?',
        0x62 => '-',
        <= 0x09 => (char)(b + '0'),
        _ => (char)(b + 'a' - 0x0A),
    };
}