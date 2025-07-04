﻿using System;
using System.Text;

namespace Somno;

/// <summary>
/// Provides extended random object generation methods.
/// </summary>
internal static class RandomProvider
{
    public static string GenerateWord(int length)
    {
        var rand = new Random();
        string vowels = "aeiou";
        string consonants = "bcdfghjklmnpqrstvwxyz";
        string word = "";

        for (int i = 0; i < length; i++) {
            char c;

            if (i % 2 == 0) {
                // Add a consonant in even positions
                c = consonants[rand.Next(consonants.Length)];
            }
            else {
                // Add a vowel in odd positions
                c = vowels[rand.Next(vowels.Length)];
            }

            if (i == 0) {
                c = char.ToUpperInvariant(c);
            }

            word += c;
        }

        return word;
    }

    public static string GenerateString(int length)
    {
        var sb = new StringBuilder();
        var rand = new Random();

        for (int i = 0; i < length; i++) {
            char c = (char)rand.Next('A', 'Z');
            if (rand.Next(2) == 1) {
                c = char.ToLowerInvariant(c);
            }

            sb.Append(c);
        }

        return sb.ToString();
    }
}