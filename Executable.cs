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

        private String ReadStringFromFile(uint rva)
        {
            int section = GetRvaSection(rva);
            var accessor = GetSectionAccessor(section);
            var position = DecodeRva(rva) - _sections[section].PointerToRawData;
            string value = ReadStringFromFile(accessor, position);
            accessor.Dispose();
            return value;
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
            uint importTable;
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
            ImageImportDescriptor importDescriptor;
            try
            {
                var accessor = _file.CreateViewAccessor(importTable, Marshal.SizeOf(typeof(ImageImportDescriptor)), MemoryMappedFileAccess.Read);
                accessor.Read(0, out importDescriptor);
                accessor.Dispose();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Import Section Read Exception");
                throw;
            }

            var importEntries = new List<ImportEntry>();
            while (importDescriptor.Characteristics != 0 || importDescriptor.FirstThunk != 0 ||
                   importDescriptor.ForwarderChain != 0 && importDescriptor.Name != 0 ||
                   importDescriptor.TimeDateStamp != 0)
            {
                var name = ReadStringFromFile(importDescriptor.Name);
                var importEntry = new ImportEntry(name);
                importEntries.Add(importEntry);

                var entryAddress = (importDescriptor.Characteristics != 0) ? importDescriptor.Characteristics : importDescriptor.FirstThunk;
                entryAddress = DecodeRva(entryAddress);
                importEntry.Entries.Add(entryAddress.ToString());

                importTable += (uint)Marshal.SizeOf(typeof (ImageImportDescriptor));
                try
                {
                    var accessor = _file.CreateViewAccessor(importTable, Marshal.SizeOf(typeof(ImageImportDescriptor)), MemoryMappedFileAccess.Read);
                    accessor.Read(0, out importDescriptor);
                    accessor.Dispose();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Import Section Read Exception");
                    throw;
                }
            }
            ImportEntries = importEntries.ToArray();
        }

        private void ReadExportTable()
        {
            uint exportTable;
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

            var addressOfNames = DecodeRva(exportDirectory.AddressOfNames);
            var namesSection = GetRvaSection(addressOfNames);
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

        private int GetRvaSection(uint rva)
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
        private uint DecodeRva(uint rva)
        {
            int section = GetRvaSection(rva);
            if (section == -1)
                return 0;
            return _sections[section].PointerToRawData + rva - +_sections[section].VirtualAddress;
        }

        internal void Close()
        {
            _file.Dispose();
        }
    }
}
