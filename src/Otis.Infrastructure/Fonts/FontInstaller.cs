// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Otis.Core.TrueType;

namespace Otis.Infrastructure.Fonts;

public sealed partial class FontInstaller(FontLocations locations)
{
    private const string FontsKey = @"Software\Microsoft\Windows NT\CurrentVersion\Fonts";
    private const int FontChange = 0x001D;
    private const nint Broadcast = 0xFFFF;
    private const int DelayUntilReboot = 0x4;

    public static FontInstaller ForCurrentUser() => new(new FontLocations(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Fonts"),
        FontsKey,
        FontsKey,
        AnnounceChanges: true));

    public FontInstallation Find(string family)
    {
        using (var machine = Registry.LocalMachine.OpenSubKey(locations.MachineKey))
        {
            if (machine?.GetValue(ValueName(family)) is string machinePath)
            {
                return new FontInstallation(InstallScope.AllUsers, machinePath, VersionOf(ResolveMachine(machinePath)));
            }
        }

        using var user = Registry.CurrentUser.OpenSubKey(locations.UserKey);
        return user?.GetValue(ValueName(family)) is string userPath
            ? new FontInstallation(InstallScope.CurrentUser, userPath, VersionOf(userPath))
            : FontInstallation.None;
    }

    // A machine-wide copy wins over a per-user one in Windows, so Otis refuses rather than install a font nobody sees.
    public FontInstallation Install(string fontFile, string family)
    {
        var existing = this.Find(family);
        if (existing.Scope == InstallScope.AllUsers)
        {
            throw new InvalidOperationException(
                $"\"{family}\" is installed for all users. Remove it in Windows Settings › Personalization › Fonts first.");
        }

        Directory.CreateDirectory(locations.FontsFolder);
        var target = Path.Combine(locations.FontsFolder, Path.GetFileName(fontFile));
        if (!string.Equals(Path.GetFullPath(fontFile), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(fontFile, target, overwrite: true);
        }

        this.Remove(existing, keep: target);
        using (var key = Registry.CurrentUser.CreateSubKey(locations.UserKey))
        {
            key.SetValue(ValueName(family), target);
        }

        if (locations.AnnounceChanges)
        {
            _ = AddFontResourceW(target);
            Announce();
        }

        return new FontInstallation(InstallScope.CurrentUser, target, VersionOf(target));
    }

    public void Uninstall(string family)
    {
        var existing = this.Find(family);
        if (existing.Scope == InstallScope.CurrentUser)
        {
            this.Remove(existing, keep: null);
            if (locations.AnnounceChanges)
            {
                Announce();
            }
        }
    }

    private static string ValueName(string family) => $"{family} (TrueType)";

    private static string ResolveMachine(string path) =>
        Path.IsPathRooted(path) ? path : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), path);

    private static Core.Projects.FontVersion? VersionOf(string path)
    {
        try
        {
            return File.Exists(path) ? TrueTypeNames.OtisVersion(File.ReadAllBytes(path)) : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static void Announce() => _ = SendMessageTimeoutW(Broadcast, FontChange, 0, 0, 0x0002, 1000, out _);

    [LibraryImport("gdi32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int AddFontResourceW(string file);

    [LibraryImport("gdi32.dll", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RemoveFontResourceW(string file);

    [LibraryImport("user32.dll")]
    private static partial nint SendMessageTimeoutW(nint window, int message, nint wParam, nint lParam, int flags, int timeout, out nint result);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool MoveFileExW(string existing, string? replacement, int flags);

    // A file still loaded by a running Word cannot be deleted; it goes away at the next restart.
    private void Remove(FontInstallation existing, string? keep)
    {
        if (existing.Scope != InstallScope.CurrentUser || existing.Path is null)
        {
            return;
        }

        using (var key = Registry.CurrentUser.OpenSubKey(locations.UserKey, writable: true))
        {
            var names = key?.GetValueNames().Where(n => string.Equals(key.GetValue(n) as string, existing.Path, StringComparison.OrdinalIgnoreCase)) ?? [];
            foreach (var name in names.ToList())
            {
                key!.DeleteValue(name, throwOnMissingValue: false);
            }
        }

        if (string.Equals(existing.Path, keep, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (locations.AnnounceChanges)
        {
            _ = RemoveFontResourceW(existing.Path);
        }

        try
        {
            File.Delete(existing.Path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            _ = MoveFileExW(existing.Path, null, DelayUntilReboot);
        }
    }
}
