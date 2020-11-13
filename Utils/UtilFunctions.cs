﻿using System;
using System.IO;
using System.Security.Cryptography;
using static System.Array;
using static System.IntPtr;
using static System.Math;
using static System.IO.Directory;
using static System.Security.Cryptography.Aes;
using static TEKLauncher.Data.Settings;
using static TEKLauncher.Utils.WinAPI;

namespace TEKLauncher.Utils
{
    internal static class UtilFunctions
    {
        static UtilFunctions()
        {
            AES = Create();
            AES.BlockSize = 128;
            AES.KeySize = 256;
        }
        private static readonly Aes AES;
        private static void ExecuteAsUser(string FilePath, string Parameters)
        {
            IntPtr ShellWindow = GetShellWindow();
            if (ShellWindow.ToInt64() != 0L)
            {
                IntPtr PrimaryToken = Zero, ShellProcess = Zero, ShellProcessToken = Zero;
                try
                {
                    GetWindowProcessID(ShellWindow, out int ShellWindowProcessID);
                    if ((ShellProcess = OpenProcess(1024, false, ShellWindowProcessID)) != Zero)
                    {
                        OpenProcessToken(ShellProcess, 2, out ShellProcessToken);
                        DuplicateToken(ShellProcessToken, 395, Zero, 2, 1, out PrimaryToken);
                        StartupInfo StartupInfo = new StartupInfo();
                        CreateProcessWithToken(PrimaryToken, 0, Zero, $@"""{FilePath}"" {Parameters}", 0, Zero, Zero, ref StartupInfo, out ProcessInfo _);
                    }
                }
                finally
                {
                    CloseHandle(ShellProcessToken);
                    CloseHandle(PrimaryToken);
                    CloseHandle(ShellProcess);
                }
            }
        }
        internal static void DeleteDirectory(string Path)
        {
            foreach (string Subirectory in EnumerateDirectories(Path))
                DeleteDirectory(Subirectory);
            foreach (string FilePath in EnumerateFiles(Path))
                File.Delete(FilePath);
            Delete(Path);
        }
        internal static void DeletePath(string Path)
        {
            if (Exists(Path))
                DeleteDirectory(Path);
            else if (FileExists(Path))
                File.Delete(Path);
        }
        internal static void Execute(string Path, string Parameters)
        {
            if (RunAsAdmin)
                WinAPI.Execute(Zero, "open", Path, Parameters, null, 1);
            else
                ExecuteAsUser(Path, Parameters);
        }
        internal static void Execute(string URI) => WinAPI.Execute(Zero, "open", URI, null, null, 1);
        internal static bool FileExists(string FilePath) => GetFileSize(FilePath) != -1L;
        internal static bool IsConnectionAvailable() => GetConnectionState(out int Flags) && ((Flags & 1) | (Flags & 2)) != 0;
        internal static byte[] AESDecrypt(byte[] Input, byte[] Key)
        {
            byte[] Cipher = new byte[Input.Length - 16], EncryptedIV = new byte[16], IV, Output;
            Copy(Input, EncryptedIV, 16);
            Copy(Input, 16, Cipher, 0, Cipher.Length);
            AES.Mode = CipherMode.ECB;
            AES.Padding = PaddingMode.None;
            using (ICryptoTransform Transform = AES.CreateDecryptor(Key, null))
                IV = Transform.TransformFinalBlock(EncryptedIV, 0, 16);
            AES.Mode = CipherMode.CBC;
            AES.Padding = PaddingMode.PKCS7;
            using (ICryptoTransform Transform = AES.CreateDecryptor(Key, IV))
            using (MemoryStream CipherStream = new MemoryStream(Cipher))
            using (CryptoStream CryptoStream = new CryptoStream(CipherStream, Transform, CryptoStreamMode.Read))
            {
                byte[] Text = new byte[Cipher.Length];
                Output = new byte[CryptoStream.Read(Text, 0, Text.Length)];
                Copy(Text, Output, Output.Length);
            }
            return Output;
        }
        internal static uint ComputeAdlerHash(byte[] Chunk)
        {
            uint A = 0U, B = 0U;
            for (int Iterator = 0; Iterator < Chunk.Length; Iterator++)
                B = (B + (A = (A + Chunk[Iterator]) % 65521U)) % 65521U;
            return A | (B << 16);
        }
        internal static long GetFileSize(string FilePath)
        {
            IntPtr FileHandle = OpenFile(FilePath, 0x80000000U, 7, Zero, 3, 128, Zero);
            if (FileHandle.ToInt64() == -1L)
                return -1L;
            WinAPI.GetFileSize(FileHandle, out long Size);
            CloseHandle(FileHandle);
            return Size;
        }
        internal static long GetFreeSpace(string Path)
        {
            GetDiskFreeSpace(Path, out long FreeSpace, Zero, Zero);
            return FreeSpace;
        }
        internal static string ConvertBytes(long Bytes) => Bytes >= 1073741824L ? $"{Round(Bytes / 1073741824D, 2)} GB" : Bytes >= 1048576L ? $"{Round(Bytes / 1048576D, 1)} MB" : $"{Round(Bytes / 1024D)} KB";
    }
}