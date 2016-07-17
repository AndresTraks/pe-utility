using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PEUtility
{
    public class ImportEntry
    {
        public string Name { get; set; }
        public List<string> Entries { get; set; }

        public ImportEntry(string name)
        {
            Name = name;
            Entries = new List<string>();
        }

        public override string ToString()
        {
            return Name;
        }
    }

    class Executable
    {
        private ImageNtHeaders32 _ntHeaders32;
        private ImageNtHeaders64 _ntHeaders64;

        public ImportEntry[] ImportEntries;
        public ExportEntry[] ExportEntries;
        public ImageCor20Header ClrHeader;

        public bool IsValid { get; set; }
        public string Filename { get; set; }
        public MemoryMappedFile File { get; set; }
        public ImageSectionHeader[] Sections { get; set; }

        public string Type
        {
            get
            {
                if ((ClrHeader.Flags & ComImageFlags.ILOnly) != 0)
                {
                    return "Any CPU";
                }
                return _ntHeaders32.Is64Bit ? "64-bit" : "32-bit";
            }
        }

        public MemoryMappedViewAccessor GetFileAccessor(long offset, long size)
        {
            return File.CreateViewAccessor(offset, size, MemoryMappedFileAccess.Read);
        }

        public MemoryMappedViewAccessor GetSectionAccessor(int section)
        {
            return GetFileAccessor(Sections[section].PointerToRawData, Sections[section].SizeOfRawData);
        }

        private string ReadStringFromFile(long rva)
        {
            int section = GetRvaSection(rva);
            using (var accessor = GetSectionAccessor(section))
            {
                var position = DecodeRva(rva) - Sections[section].PointerToRawData;
                return ReadStringFromFile(accessor, position);
            }
        }

        public string ReadStringFromFile(UnmanagedMemoryAccessor accessor, long position)
        {
            var bytes = new List<char>();
            while (true)
            {
                var b = accessor.ReadByte(position);
                if (b == 0)
                {
                    break;
                }
                bytes.Add((char)b);
                position++;
            }
            return new string(bytes.ToArray());
        }

        private ushort ReadUInt16(long address)
        {
            using (var accessor = GetFileAccessor(address, sizeof(short)))
            {
                return accessor.ReadUInt16(0);
            }
        }

        private uint ReadUInt32(long address)
        {
            using (var accessor = GetFileAccessor(address, sizeof (uint)))
            {
                return accessor.ReadUInt32(0);
            }
        }

        private long ReadInt64(long address)
        {
            using (var accessor = GetFileAccessor(address, sizeof (long)))
            {
                return accessor.ReadInt64(0);
            }
        }

        private ulong ReadUInt64(long address)
        {
            using (var accessor = GetFileAccessor(address, sizeof(ulong)))
            {
                return accessor.ReadUInt64(0);
            }
        }

        public Executable(string filename)
        {
            Filename = filename;

            try
            {
                File = MemoryMappedFile.CreateFromFile(filename, FileMode.Open);
            }
            catch (Exception e)
            {
                MessageBox.Show("Couldn't open " + filename + "\n" + e.Message, "Error");
                return;
            }

            ImageDosHeader header;
            try
            {
                using (var accessor = GetFileAccessor(0, Marshal.SizeOf(typeof(ImageDosHeader))))
                {
                    accessor.Read(0, out header);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "DOS Header Read Exception");
                return;
            }

            if (!header.IsValid)
            {
                MessageBox.Show("Invalid PE header for " + filename, "Invalid PE header");
                return;
            }

            try
            {
                using (var accessor = GetFileAccessor(header.LfaNewHeader, Marshal.SizeOf(typeof(ImageNtHeaders32))))
                {
                    accessor.Read(0, out _ntHeaders32);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "NT Headers Read Exception");
                return;
            }

            if (!_ntHeaders32.IsValid)
            {
                MessageBox.Show("Invalid PE header for " + filename, "Invalid PE header");
                return;
            }

            if (_ntHeaders32.Is64Bit)
            {
                try
                {
                    using (var accessor = GetFileAccessor(header.LfaNewHeader, Marshal.SizeOf(typeof(ImageNtHeaders64))))
                    {
                        accessor.Read(0, out _ntHeaders64);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "NT Headers Header Exception");
                    return;
                }
            }

            // Read sections
            if (_ntHeaders32.Is64Bit)
            {
                Sections = new ImageSectionHeader[_ntHeaders64.FileHeader.NumberOfSections];
                try
                {
                    using (var accessor = GetFileAccessor(header.LfaNewHeader + Marshal.SizeOf(typeof(ImageNtHeaders64)),
                        _ntHeaders64.FileHeader.NumberOfSections * Marshal.SizeOf(typeof(ImageSectionHeader))))
                    {
                        accessor.ReadArray(0, Sections, 0, _ntHeaders64.FileHeader.NumberOfSections);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Section Read Exception");
                    throw;
                }
            }
            else
            {
                Sections = new ImageSectionHeader[_ntHeaders32.FileHeader.NumberOfSections];
                try
                {
                    using (var accessor = GetFileAccessor(header.LfaNewHeader + Marshal.SizeOf(typeof(ImageNtHeaders32)),
                        _ntHeaders32.FileHeader.NumberOfSections * Marshal.SizeOf(typeof(ImageSectionHeader))))
                    {
                        accessor.ReadArray(0, Sections, 0, _ntHeaders32.FileHeader.NumberOfSections);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Section Read Exception");
                    throw;
                }
            }

            ReadImportTable();

            try
            {
                var exportReader = new ExportDirectoryReader(this);
                ExportEntries = exportReader.Read();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Export Directory Read Exception");
                throw;
            }

            ReadCorHeader();

            IsValid = true;
        }

        private void ReadImportTable()
        {
            long importTable;
            if (_ntHeaders32.Is64Bit)
            {
                importTable = _ntHeaders64.OptionalHeader.ImportTable.VirtualAddress;
            }
            else
            {
                importTable = _ntHeaders32.OptionalHeader.ImportTable.VirtualAddress;
            }
            if (importTable == 0)
            {
                ImportEntries = new ImportEntry[0];
                return;
            }

            importTable = DecodeRva(importTable);
            try
            {
                ImageImportDescriptor importDescriptor;
                using (var accessor = GetFileAccessor(importTable, Marshal.SizeOf(typeof(ImageImportDescriptor))))
                {
                    accessor.Read(0, out importDescriptor);
                }

                var importEntries = new List<ImportEntry>();
                while (importDescriptor.Characteristics != 0 || importDescriptor.FirstThunk != 0 ||
                       importDescriptor.ForwarderChain != 0 && importDescriptor.Name != 0 ||
                       importDescriptor.TimeDateStamp != 0)
                {
                    var name = ReadStringFromFile(importDescriptor.Name);
                    var importEntry = new ImportEntry(name);
                    importEntries.Add(importEntry);

                    long entryAddress = (importDescriptor.Characteristics != 0) ? importDescriptor.Characteristics : importDescriptor.FirstThunk;
                    entryAddress = DecodeRva(entryAddress);

                    ulong thunk = _ntHeaders32.Is64Bit ? ReadUInt64(entryAddress) : ReadUInt32(entryAddress);
                    while (thunk != 0)
                    {
                        ushort hint = ReadUInt16(entryAddress);

                        if ((thunk & 0x8000000000000000) != 0)
                        {
                            thunk &= 0x7FFFFFFFFFFFFFFF;
                            importEntry.Entries.Add("hint: " + hint.ToString() + ", thunk: " + thunk.ToString());
                        }
                        else if ((thunk & 0x80000000) != 0)
                        {
                            thunk &= 0x7FFFFFFF;
                            importEntry.Entries.Add("hint: " + hint.ToString() + ", thunk: " + thunk.ToString());
                        }
                        else
                        {
                            var importName = ReadStringFromFile((long)thunk + sizeof(ushort));
                            importEntry.Entries.Add(importName);
                        }
                        
                        entryAddress += _ntHeaders32.Is64Bit ? sizeof(ulong) : sizeof(uint);
                        thunk = _ntHeaders32.Is64Bit ? ReadUInt64(entryAddress) : ReadUInt32(entryAddress);
                    }

                    importTable += (uint)Marshal.SizeOf(typeof(ImageImportDescriptor));
                    using (var descAccessor = GetFileAccessor(importTable, Marshal.SizeOf(typeof(ImageImportDescriptor))))
                    {
                        descAccessor.Read(0, out importDescriptor);
                    }
                }
                ImportEntries = importEntries.ToArray();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Import Section Read Exception");
                throw;
            }
        }

        private void ReadCorHeader()
        {
            long clrRuntimeHeader;
            if (_ntHeaders32.Is64Bit)
            {
                clrRuntimeHeader = _ntHeaders64.OptionalHeader.CLRRuntimeHeader.VirtualAddress;
            }
            else
            {
                clrRuntimeHeader = _ntHeaders32.OptionalHeader.CLRRuntimeHeader.VirtualAddress;
            }
            if (clrRuntimeHeader == 0)
            {
                return;
            }

            clrRuntimeHeader = DecodeRva(clrRuntimeHeader);
            try
            {
                using (var accessor = GetFileAccessor(clrRuntimeHeader, Marshal.SizeOf(typeof(ImageCor20Header))))
                {
                    accessor.Read(0, out ClrHeader);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "CLR Section Read Exception");
                throw;
            }
        }

        public uint GetExportTableAddress()
        {
            if (_ntHeaders32.Is64Bit)
            {
                return _ntHeaders64.OptionalHeader.ExportTable.VirtualAddress;
            }
            else
            {
                return _ntHeaders32.OptionalHeader.ExportTable.VirtualAddress;
            }
        }

        public int GetRvaSection(long rva)
        {
            int i;
            for (i = 0; i < Sections.Length; i++)
            {
                if (rva >= Sections[i].VirtualAddress &&
                    rva < Sections[i].VirtualAddress + Sections[i].SizeOfRawData)
                {
                    return i;
                }
            }
            return -1;
        }

        // Decode Relative Virtual Address
        public long DecodeRva(long rva)
        {
            int section = GetRvaSection(rva);
            if (section == -1)
                return 0;
            return DecodeRva(rva, section);
        }

        public long DecodeRva(long rva, int section)
        {
            return Sections[section].PointerToRawData + (rva - Sections[section].VirtualAddress);
        }

        internal void Close()
        {
            File.Dispose();
        }
    }
}
