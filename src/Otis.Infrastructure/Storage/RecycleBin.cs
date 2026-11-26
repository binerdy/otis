// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.InteropServices;

namespace Otis.Infrastructure.Storage;

public sealed partial class RecycleBin : IFileRemover
{
    private const uint Delete = 3;
    private const ushort AllowUndo = 0x40;
    private const ushort NoConfirmation = 0x10;
    private const ushort Silent = 0x4;
    private const ushort NoErrorUi = 0x400;

    public void Remove(IReadOnlyList<string> files)
    {
        if (files.Count == 0)
        {
            return;
        }

        // The shell expects the paths separated by NUL and ended by two NULs.
        var list = Marshal.StringToHGlobalUni(string.Join('\0', files) + "\0\0");
        try
        {
            var operation = new FileOperation
            {
                Function = Delete,
                From = list,
                Flags = AllowUndo | NoConfirmation | Silent | NoErrorUi,
            };
            var result = SHFileOperationW(ref operation);
            if (result != 0 || operation.AnyOperationsAborted != 0)
            {
                throw new IOException($"Windows could not move the files to the Recycle Bin (code {result}).");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(list);
        }
    }

    [LibraryImport("shell32.dll")]
    private static partial int SHFileOperationW(ref FileOperation operation);

    [StructLayout(LayoutKind.Sequential)]
    private record struct FileOperation
    {
        public nint Window;
        public uint Function;
        public nint From;
        public nint To;
        public ushort Flags;
        public int AnyOperationsAborted;
        public nint NameMappings;
        public nint ProgressTitle;
    }
}
