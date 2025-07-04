﻿using System.Runtime.InteropServices;
using System.Globalization;
using System.Drawing;

namespace Somno.Native.WinUSER;

[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left, Top, Right, Bottom;

    public RECT(int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public RECT(Rectangle r) : this(r.Left, r.Top, r.Right, r.Bottom) { }

    public int X {
        get => Left;
        set { Right -= (Left - value); Left = value; }
    }

    public int Y {
        get => Top;
        set { Bottom -= (Top - value); Top = value; }
    }

    public int Height {
        get => Bottom - Top;
        set { Bottom = value + Top; }
    }

    public int Width {
        get => Right - Left;
        set { Right = value + Left; }
    }

    public Point Location {
        get => new(Left, Top);
        set { X = value.X; Y = value.Y; }
    }

    public Size Size {
        get => new(Width, Height);
        set { Width = value.Width; Height = value.Height; }
    }

    public static implicit operator Rectangle(RECT r) => new(r.Left, r.Top, r.Width, r.Height);

    public static implicit operator RECT(Rectangle r) => new(r);

    public static bool operator ==(RECT r1, RECT r2) => r1.Equals(r2);

    public static bool operator !=(RECT r1, RECT r2) => !r1.Equals(r2);

    public bool Equals(RECT r)
        => r.Left == Left
        && r.Top == Top
        && r.Right == Right
        && r.Bottom == Bottom;

    public override bool Equals(object obj)
    {
        if (obj is RECT rect)
            return Equals(rect);

        else if (obj is Rectangle dotnetRect)
            return Equals(new RECT(dotnetRect));

        return false;
    }

    public override int GetHashCode() => ((Rectangle)this).GetHashCode();

    public override string ToString()
        => string.Format(CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
}
