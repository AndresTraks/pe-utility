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

    public sealed class Executable : IDisposable
    {
        private ImageNtHeaders32 _ntHeaders32;
        private ImageNtHeaders64 _ntHeaders64;

        public ImportEntry[] ImportEntries { get; private set; }
        public ExportEntry[] ExportEntries { get; private set; }
        public ImageCor20Header ClrHeader { get; private set; }

        public bool IsValid { get; }
        public string Filename { get; }
        public ExecutableReader Reader { get; }
        public ImageSectionHeader[] Sections { get; private set; }

        public string Type
        {
            get
            {
                if ((ClrHeader.Flags & ComImageFlags.ILOnly) != 0)
                {
                    return "Any CPU";
                }
                return Is64Bit ? "64-bit" : "32-bit";
            }
        }

        public bool Is64Bit => _ntHeaders32.Is64Bit;

        public MemoryMappedViewAccessor GetSectionAccessor(int section)
        {
            return Reader.GetAccessor(Sections[section].PointerToRawData, Sections[section].SizeOfRawData);
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

        public Executable(string fileName)
        {
            Filename = fileName;
            Reader = new ExecutableReader(fileName);

            var header = Reader.ReadStruct<ImageDosHeader>(0);
            if (!header.IsValid)
            {
                MessageBox.Show("Invalid PE header for " + fileName, "Invalid PE header");
                return;
            }

            _ntHeaders32 = Reader.ReadStruct<ImageNtHeaders32>(header.LfaNewHeader);
            if (!_ntHeaders32.IsValid)
            {
                MessageBox.Show("Invalid PE header for " + fileName, "Invalid PE header");
                return;
            }

            if (_ntHeaders32.Is64Bit)
            {
                _ntHeaders64 = Reader.ReadStruct<ImageNtHeaders64>(header.LfaNewHeader);
            }

            // Read sections
            if (Is64Bit)
            {
                Sections = Reader.ReadStructArray<ImageSectionHeader>(
                    header.LfaNewHeader + Marshal.SizeOf(typeof(ImageNtHeaders64)),
                    _ntHeaders64.FileHeader.NumberOfSections);
            }
            else
            {
                Sections = Reader.ReadStructArray<ImageSectionHeader>(
                    header.LfaNewHeader + Marshal.SizeOf(typeof(ImageNtHeaders32)),
                    _ntHeaders32.FileHeader.NumberOfSections);
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

            Reader.Close();
            IsValid = true;
        }

        private void ReadImportTable()
        {
            long importTable;
            if (Is64Bit)
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
                ImageImportDescriptor importDescriptor = Reader.ReadStruct<ImageImportDescriptor>(importTable);

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

                    ulong thunk = Is64Bit ? Reader.ReadUInt64(entryAddress) : Reader.ReadUInt32(entryAddress);
                    while (thunk != 0)
                    {
                        ushort hint = Reader.ReadUInt16(entryAddress);

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
                        
                        entryAddress += Is64Bit ? sizeof(ulong) : sizeof(uint);
                        thunk = Is64Bit ? Reader.ReadUInt64(entryAddress) : Reader.ReadUInt32(entryAddress);
                    }

                    importTable += (uint)Marshal.SizeOf(typeof(ImageImportDescriptor));
                    importDescriptor = Reader.ReadStruct<ImageImportDescriptor>(importTable);
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
            if (Is64Bit)
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
            ClrHeader = Reader.ReadStruct<ImageCor20Header>(clrRuntimeHeader);
        }

        public uint GetExportTableAddress()
        {
            if (Is64Bit)
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
            for (int i = 0; i < Sections.Length; i++)
            {
                if (rva >= Sections[i].VirtualAddress &&
                    rva < Sections[i].VirtualAddress + Sections[i].SizeOfRawData)
                {
                    return i;
                }
            }

            throw new ArgumentOutOfRangeException($"Unknown relative virtual address: {rva}");
        }

        // Decode Relative Virtual Address
        public long DecodeRva(long rva)
        {
            int section = GetRvaSection(rva);
            return DecodeRva(rva, section);
        }

        public long DecodeRva(long rva, int section)
        {
            return Sections[section].PointerToRawData + (rva - Sections[section].VirtualAddress);
        }

        public void Dispose()
        {
            Reader.Dispose();
        }
    }
}
