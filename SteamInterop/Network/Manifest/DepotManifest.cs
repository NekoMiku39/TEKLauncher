﻿using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using TEKLauncher.ARK;
using static System.BitConverter;
using static System.Convert;
using static System.IO.File;
using static System.Security.Cryptography.Aes;
using static System.Text.Encoding;
using static TEKLauncher.ARK.CreamAPI;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.SteamInterop.Network.ContentDownloader;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.SteamInterop.Network.Manifest
{
    internal class DepotManifest
    {
        static DepotManifest()
        {
            AppIDRelPathHash = AppIDRelPath.GetHashCode();
            for (int Iterator = 0; Iterator < 6; Iterator++)
                ExclusionHashes[Iterator] = Exclusions[Iterator].GetHashCode();
        }
        internal DepotManifest(string ManifestPath, uint DepotID)
        {
            ID = System.IO.Path.GetFileNameWithoutExtension(ManifestPath).Split('-')[1];
            if (DepotID == 346111U)
                IsCAInstalled = IsInstalled;
            if ((Path = ManifestPath).EndsWith("t"))
            {
                Path += "d";
                using (FileStream Stream = OpenRead(ManifestPath))
                using (BinaryReader Reader = new BinaryReader(Stream))
                {
                    if (Stream.Length < 4)
                        throw new ValidatorException(LocString(LocCode.ManifestCorrupted));
                    if (Reader.ReadUInt32() == 0x71F617D0U)
                    {
                        Payload Payload = new Payload();
                        try { Payload.Deserialize(Stream, Reader.ReadUInt32()); }
                        catch (IOException) { throw new ValidatorException(LocString(LocCode.ManifestCorrupted)); }
                        Files = Payload.Files;
                        byte[] DepotKey = DepotKeys[DepotID];
                        Aes Decryptor = Create();
                        Decryptor.BlockSize = 128;
                        Decryptor.KeySize = 256;
                        List<FileEntry> Exclusions = new List<FileEntry>();
                        using (Decryptor)
                            for (int Iterator = 0; Iterator < Files.Count; Iterator++)
                            {
                                FileEntry File = Files[Iterator];
                                byte[] DecryptedName, EncryptedName;
                                try { EncryptedName = FromBase64String(File.Name); }
                                catch { throw new ValidatorException(LocString(LocCode.ManifestCorrupted)); }
                                try { DecryptedName = AESDecrypt(EncryptedName, DepotKey, Decryptor); }
                                catch { throw new ValidatorException(LocString(LocCode.ManifestDecryptFailed)); }
                                File.Name = UTF8.GetString(DecryptedName).TrimEnd('\0');
                                if (DepotID == 346110U && File.Name.EndsWith(".uncompressed_size") || DepotID == 346111U && IsExclusion(File))
                                    Exclusions.Add(File);
                            }
                        foreach (FileEntry File in Exclusions)
                            Files.Remove(File);
                        Files.Sort(Comparator);
                        using (FileStream Writer = File.Create(Path))
                        {
                            Writer.Write(GetBytes(Files.Count), 0, 4);
                            foreach (FileEntry File in Files)
                                File.WriteToFile(Writer);
                        }
                    }
                    else
                        throw new ValidatorException(LocString(LocCode.ManifestCorrupted));
                }
                try { Delete(ManifestPath); }
                catch { }
            }
            else
                using (FileStream Reader = OpenRead(Path))
                {
                    byte[] Buffer = new byte[4];
                    Reader.Read(Buffer, 0, 4);
                    int FilesCount = ToInt32(Buffer, 0);
                    Files = new List<FileEntry>(FilesCount);
                    for (; FilesCount > 0; FilesCount--)
                    {
                        FileEntry File = new FileEntry();
                        File.ReadFromFile(Reader);
                        if (!(DepotID == 346110U && File.Name.EndsWith(".uncompressed_size") || DepotID == 346111U && IsExclusion(File)))
                            Files.Add(File);
                    }
                }
        }
        private readonly bool IsCAInstalled;
        internal readonly string ID, Path;
        internal readonly List<FileEntry> Files;
        private static readonly int AppIDRelPathHash;
        private static readonly int[] ExclusionHashes = new int[6];
        private static readonly string AppIDRelPath = @"ShooterGame\Binaries\Win64\steam_appid.txt";
        private static readonly string[] Exclusions = new[]
        {
            @"Engine\Config\Base.ini",
            @"Engine\Config\BaseEditorLayout.ini",
            @"Engine\Config\BaseScalability.ini",
            @"ShooterGame\Binaries\Win64\officialservers.ini",
            @"ShooterGame\Binaries\Win64\news.ini",
            @"ShooterGame\Binaries\Win64\officialserverstatus.ini"
        };
        private bool IsExclusion(FileEntry File)
        {
            if (IsCAInstalled && File.Name.EndsWith("steam_api64.dll"))
                return true;
            int HashCode = File.Name.GetHashCode();
            if (HashCode == AppIDRelPathHash && File.Name == AppIDRelPath && FileExists($@"{Game.Path}\{AppIDRelPath}"))
                return true;
            bool IsExclusion = false;
            for (int Iterator = 0; Iterator < 6; Iterator++)
                if (HashCode == ExclusionHashes[Iterator])
                    if (File.Name == Exclusions[Iterator])
                    {
                        IsExclusion = true;
                        break;
                    }
            return IsExclusion;
        }
        private int Comparator(FileEntry A, FileEntry B) => A.Name.CompareTo(B.Name);
    }
}