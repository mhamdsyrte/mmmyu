namespace FileManagerPro.Services;

public class FileEntry
{
    public string Name { get; set; } = "";
    public string FullPath { get; set; } = "";
    public bool IsDirectory { get; set; }
    public long SizeBytes { get; set; }
    public DateTime Modified { get; set; }
}

public class ClipboardState
{
    public List<string> Paths { get; private set; } = new();
    public bool IsCut { get; private set; }

    public void SetCopy(IEnumerable<string> paths)
    {
        Paths = paths.ToList();
        IsCut = false;
    }

    public void SetCut(IEnumerable<string> paths)
    {
        Paths = paths.ToList();
        IsCut = true;
    }

    public bool HasContent => Paths.Count > 0;

    public void Clear() => Paths = new();
}

public class FileService
{
    /// <summary>
    /// Root folders shown when the explorer first opens.
    /// </summary>
    public List<FileEntry> GetRoots()
    {
        var roots = new List<FileEntry>();

        void TryAdd(string path, string label)
        {
            if (Directory.Exists(path))
            {
                roots.Add(new FileEntry
                {
                    Name = label,
                    FullPath = path,
                    IsDirectory = true,
                    Modified = Directory.GetLastWriteTime(path)
                });
            }
        }

        TryAdd("/storage/emulated/0", "التخزين الداخلي");
        TryAdd("/storage/emulated/0/Download", "التنزيلات");
        TryAdd("/storage/emulated/0/DCIM", "الكاميرا");
        TryAdd("/storage/emulated/0/Documents", "المستندات");

        // Any additional mounted volumes (SD cards, USB) show up under /storage
        try
        {
            foreach (var dir in Directory.GetDirectories("/storage"))
            {
                var name = Path.GetFileName(dir);
                if (name is "emulated" or "self") continue;
                TryAdd(dir, $"تخزين خارجي ({name})");
            }
        }
        catch { /* ignore if /storage isn't readable */ }

        return roots;
    }

    public (List<FileEntry> entries, string? error) ListEntries(string path)
    {
        var result = new List<FileEntry>();
        try
        {
            foreach (var dir in Directory.GetDirectories(path))
            {
                var info = new DirectoryInfo(dir);
                result.Add(new FileEntry
                {
                    Name = info.Name,
                    FullPath = dir,
                    IsDirectory = true,
                    Modified = info.LastWriteTime
                });
            }

            foreach (var file in Directory.GetFiles(path))
            {
                var info = new FileInfo(file);
                result.Add(new FileEntry
                {
                    Name = info.Name,
                    FullPath = file,
                    IsDirectory = false,
                    SizeBytes = info.Length,
                    Modified = info.LastWriteTime
                });
            }

            return (result.OrderByDescending(e => e.IsDirectory).ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToList(), null);
        }
        catch (UnauthorizedAccessException)
        {
            return (result, "لا تملك صلاحية الوصول لهذا المجلد");
        }
        catch (Exception ex)
        {
            return (result, ex.Message);
        }
    }

    public string? CreateFolder(string parent, string name)
    {
        try
        {
            var path = Path.Combine(parent, name);
            if (Directory.Exists(path) || File.Exists(path))
                return "يوجد عنصر بنفس الاسم بالفعل";
            Directory.CreateDirectory(path);
            return null;
        }
        catch (Exception ex) { return ex.Message; }
    }

    public string? CreateTextFile(string parent, string name)
    {
        try
        {
            if (!name.Contains('.')) name += ".txt";
            var path = Path.Combine(parent, name);
            if (Directory.Exists(path) || File.Exists(path))
                return "يوجد عنصر بنفس الاسم بالفعل";
            File.WriteAllText(path, "");
            return null;
        }
        catch (Exception ex) { return ex.Message; }
    }

    public string? Delete(FileEntry entry)
    {
        try
        {
            if (entry.IsDirectory)
                Directory.Delete(entry.FullPath, true);
            else
                File.Delete(entry.FullPath);
            return null;
        }
        catch (Exception ex) { return ex.Message; }
    }

    public string? Rename(FileEntry entry, string newName)
    {
        try
        {
            var newPath = Path.Combine(Path.GetDirectoryName(entry.FullPath)!, newName);
            if (Directory.Exists(newPath) || File.Exists(newPath))
                return "يوجد عنصر بنفس الاسم بالفعل";

            if (entry.IsDirectory)
                Directory.Move(entry.FullPath, newPath);
            else
                File.Move(entry.FullPath, newPath);
            return null;
        }
        catch (Exception ex) { return ex.Message; }
    }

    /// <summary>Copies (or moves, when <paramref name="move"/> is true) each path into destDir.</summary>
    public string? PasteInto(IEnumerable<string> sourcePaths, string destDir, bool move)
    {
        try
        {
            foreach (var source in sourcePaths)
            {
                var name = Path.GetFileName(source.TrimEnd('/'));
                var dest = GetAvailableName(destDir, name);

                if (Directory.Exists(source))
                {
                    if (move) Directory.Move(source, dest);
                    else CopyDirectory(source, dest);
                }
                else if (File.Exists(source))
                {
                    if (move) File.Move(source, dest);
                    else File.Copy(source, dest);
                }
            }
            return null;
        }
        catch (Exception ex) { return ex.Message; }
    }

    string GetAvailableName(string destDir, string name)
    {
        var candidate = Path.Combine(destDir, name);
        if (!Directory.Exists(candidate) && !File.Exists(candidate)) return candidate;

        var ext = Path.GetExtension(name);
        var baseName = Path.GetFileNameWithoutExtension(name);
        int i = 1;
        string newCandidate;
        do
        {
            newCandidate = Path.Combine(destDir, $"{baseName} ({i}){ext}");
            i++;
        } while (Directory.Exists(newCandidate) || File.Exists(newCandidate));
        return newCandidate;
    }

    void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.GetFiles(sourceDir))
            File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)));
        foreach (var dir in Directory.GetDirectories(sourceDir))
            CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
    }

    public (string content, string? error) ReadText(string path)
    {
        try { return (File.ReadAllText(path), null); }
        catch (Exception ex) { return ("", ex.Message); }
    }

    public string? WriteText(string path, string content)
    {
        try { File.WriteAllText(path, content); return null; }
        catch (Exception ex) { return ex.Message; }
    }

    public static bool LooksLikeText(string fileName)
    {
        var textExt = new[] { ".txt", ".md", ".json", ".xml", ".csv", ".log", ".cs", ".js", ".ts",
                               ".html", ".css", ".yml", ".yaml", ".ini", ".config", ".sh", ".py", ".java" };
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return textExt.Contains(ext);
    }

    public static string FormatSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        int unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return unit == 0 ? $"{size:0} {units[unit]}" : $"{size:0.0} {units[unit]}";
    }
}
