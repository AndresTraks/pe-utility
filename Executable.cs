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

    public class ExportEntry
    {
        public string Name { get; set; }

        public ExportEntry(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    class Executable
    {
        private MemoryMappedFile _file;
        private ImageNtHeaders32 _ntHeaders32;
        private ImageNtHeaders64 _ntHeaders64;
        private ImageSectionHeader[] _sections;

        public ImportEntry[] ImportEntries;
        public ExportEntry[] ExportEntries;
        public bool IsValid { get; set; }
        public string Filename { get; set; }

        private MemoryMappedViewAccessor GetSectionAccessor(int section)
        {
            return _file.CreateViewAccessor(_sections[section].PointerToRawData, _sections[section].SizeOfRawData, MemoryMappedFileAccess.Read);
        }

        private String ReadStringFromFile(long rva)
        {
            int section = GetRvaSection(rva);
            using (var accessor = GetSectionAccessor(section))
            {
                var position = DecodeRva(rva) - _sections[section].PointerToRawData;
                return ReadStringFromFile(accessor, position);
            }
        }

        private String ReadStringFromFile(MemoryMappedViewAccessor accessor, long position)
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
            return new String(bytes.ToArray());
        }

        private UInt32 ReadUInt32(long address)
        {
            using (var accessor = _file.CreateViewAccessor(address, sizeof (UInt32), MemoryMappedFileAccess.Read))
            {
                return accessor.ReadUInt32(0);
            }
        }

        private long ReadInt64(long address)
        {
            using (var accessor = _file.CreateViewAccessor(address, sizeof (long), MemoryMappedFileAccess.Read))
            {
                return accessor.ReadInt64(0);
            }
        }

        private ulong ReadUInt64(long address)
        {
            using (var accessor = _file.CreateViewAccessor(address, sizeof(ulong), MemoryMappedFileAccess.Read))
            {
                return accessor.ReadUInt64(0);
            }
        }

        public Executable(string filename)
        {
            Filename = filename;

            try
            {
                _file = MemoryMappedFile.CreateFromFile(filename, FileMode.Open);
            }
            catch (Exception e)
            {
                MessageBox.Show("Couldn't open " + filename + "\n" + e.Message, "Error");
                return;
            }

            ImageDosHeader header;
            try
            {
                var accessor = _file.CreateViewAccessor(0, Marshal.SizeOf(typeof(ImageDosHeader)), MemoryMappedFileAccess.Read);
                accessor.Read(0, out header);
                accessor.Dispose();
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
                var accessor = _file.CreateViewAccessor(header.LfaNewHeader, Marshal.SizeOf(typeof(ImageNtHeaders32)), MemoryMappedFileAccess.Read);
                accessor.Read(0, out _ntHeaders32);
                accessor.Dispose();
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
                    var accessor = _file.CreateViewAccessor(header.LfaNewHeader, Marshal.SizeOf(typeof(ImageNtHeaders64)), MemoryMappedFileAccess.Read);
                    accessor.Read(0, out _ntHeaders64);
                    accessor.Dispose();
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
                _sections = new ImageSectionHeader[_ntHeaders64.FileHeader.NumberOfSections];
                try
                {
                    var accessor = _file.CreateViewAccessor(header.LfaNewHeader + Marshal.SizeOf(typeof(ImageNtHeaders64)),
                        _ntHeaders64.FileHeader.NumberOfSections * Marshal.SizeOf(typeof(ImageSectionHeader)), MemoryMappedFileAccess.Read);
                    accessor.ReadArray(0, _sections, 0, _ntHeaders64.FileHeader.NumberOfSections);
                    accessor.Dispose();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Section Read Exception");
                    throw;
                }
            }
            else
            {
                _sections = new ImageSectionHeader[_ntHeaders32.FileHeader.NumberOfSections];
                try
                {
                    var accessor = _file.CreateViewAccessor(header.LfaNewHeader + Marshal.SizeOf(typeof(ImageNtHeaders32)),
                        _ntHeaders32.FileHeader.NumberOfSections * Marshal.SizeOf(typeof(ImageSectionHeader)), MemoryMappedFileAccess.Read);
                    accessor.ReadArray(0, _sections, 0, _ntHeaders32.FileHeader.NumberOfSections);
                    accessor.Dispose();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Section Read Exception");
                    throw;
                }
            }

            ReadImportTable();
            ReadExportTable();

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
                var accessor = _file.CreateViewAccessor(importTable, Marshal.SizeOf(typeof(ImageImportDescriptor)), MemoryMappedFileAccess.Read);
                ImageImportDescriptor importDescriptor;
                accessor.Read(0, out importDescriptor);
                accessor.Dispose();

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
                        var hintAccessor = _file.CreateViewAccessor(entryAddress, sizeof(UInt16), MemoryMappedFileAccess.Read);
                        UInt16 hint = hintAccessor.ReadUInt16(0);
                        hintAccessor.Dispose();

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
                            var importName = ReadStringFromFile((long)thunk + sizeof(UInt16));
                            importEntry.Entries.Add(importName);
                        }
                        
                        entryAddress += _ntHeaders32.Is64Bit ? sizeof(UInt64) : sizeof(UInt32);
                        thunk = _ntHeaders32.Is64Bit ? ReadUInt64(entryAddress) : ReadUInt32(entryAddress);
                    }

                    importTable += (uint)Marshal.SizeOf(typeof(ImageImportDescriptor));
                    var descAccessor = _file.CreateViewAccessor(importTable, Marshal.SizeOf(typeof(ImageImportDescriptor)), MemoryMappedFileAccess.Read);
                    descAccessor.Read(0, out importDescriptor);
                    descAccessor.Dispose();
                }
                ImportEntries = importEntries.ToArray();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Import Section Read Exception");
                throw;
            }
        }

        private void ReadExportTable()
        {
            long exportTable;
            if (_ntHeaders32.Is64Bit)
            {
                exportTable = _ntHeaders64.OptionalHeader.ExportTable.VirtualAddress;
            }
            else
            {
                exportTable = _ntHeaders32.OptionalHeader.ExportTable.VirtualAddress;
            }
            if (exportTable == 0)
            {
                ExportEntries = new ExportEntry[0];
                return;
            }

            exportTable = DecodeRva(exportTable);
            ImageExportDirectory exportDirectory;
            try
            {
                var accessor = _file.CreateViewAccessor(exportTable, Marshal.SizeOf(typeof(ImageExportDirectory)), MemoryMappedFileAccess.Read);
                accessor.Read(0, out exportDirectory);
                accessor.Dispose();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Export Section Read Exception");
                throw;
            }

            var namesSection = GetRvaSection(exportDirectory.AddressOfNames);
            var addressOfNames = DecodeRva(exportDirectory.AddressOfNames, namesSection);
            uint numNames = exportDirectory.NumberOfNames;
            var exportEntries = new List<ExportEntry>();
            try
            {
                var namesSectionAccessor = GetSectionAccessor(namesSection);
                var accessor = _file.CreateViewAccessor(addressOfNames, numNames * sizeof(uint), MemoryMappedFileAccess.Read);
                int i;
                for (i = 0; i < numNames; i++)
                {
                    var nameAddress = DecodeRva(accessor.ReadUInt32(i * sizeof(uint))) - _sections[namesSection].PointerToRawData;
                    var name = ReadStringFromFile(namesSectionAccessor, nameAddress);
                    exportEntries.Add(new ExportEntry(name));
                }
                accessor.Dispose();
                namesSectionAccessor.Dispose();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Export Section Read Exception");
                throw;
            }

            ExportEntries = exportEntries.ToArray();
        }

        private int GetRvaSection(long rva)
        {
            int i;
            for (i = 0; i < _sections.Length; i++)
            {
                if (rva >= _sections[i].VirtualAddress &&
                    rva < _sections[i].VirtualAddress + _sections[i].SizeOfRawData)
                {
                    return i;
                }
            }
            return -1;
        }

        // Decode Relative Virtual Address
        private long DecodeRva(long rva)
        {
            int section = GetRvaSection(rva);
            if (section == -1)
                return 0;
            return DecodeRva(rva, section);
        }

        private long DecodeRva(long rva, int section)
        {
            return (long)_sections[section].PointerToRawData + (rva - _sections[section].VirtualAddress);
        }

        internal void Close()
        {
            _file.Dispose();
        }
    }
}
