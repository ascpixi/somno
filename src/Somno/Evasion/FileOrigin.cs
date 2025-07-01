using System;
using System.IO;

namespace Somno.Evasion;

internal static class FileOrigin
{
    public static void RandomizeFileSystemTime(params string[] paths)
    {
        var time = DateTime.Now
            - TimeSpan.FromDays(Random.Shared.Next(30, 365))
            - TimeSpan.FromSeconds(Random.Shared.Next(120, 28800));

        foreach (var path in paths) {
            if(File.Exists(path)) {
                File.SetCreationTime(path, time);
                File.SetLastWriteTime(path, time);
                File.SetLastAccessTime(path, time);
            } else if (Directory.Exists(path)) {
                Directory.SetCreationTime(path, time);
                Directory.SetLastWriteTime(path, time);
                Directory.SetLastAccessTime(path, time);
            } else {
                throw new FileNotFoundException($"Cannot find file/directory '{path}'.");
            }
        }
    }
}
