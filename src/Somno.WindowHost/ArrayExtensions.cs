using System;

namespace Somno.WindowHost;

internal static class ArrayExtensions
{
    public static T[] Fill<T>(this T[] self, Func<T> func)
    {
        for (int i = 0; i < self.Length; i++) {
            self[i] = func();
        }

        return self;
    }

    public static T[] Transform<T>(this T[] self, Func<T, T> transform)
    {
        for (int i = 0; i < self.Length; i++) {
            self[i] = transform(self[i]);
        }

        return self;
    }

    public static string CharsToString(this char[] self) => new(self);
}
