using System.Collections.Generic;
using System.Linq;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace PEUtility
{
    public class ExportEntry
    {
        public string Name { get; }
        public long Address { get; }

        public ExportEntry(string name, long address)
        {
            Name = name;
            Address = address;
        }

        public override string ToString()
        {
            return $"{Address} {Name}";
        }
    }

    class ExportDirectoryReader
    {
        private Executable _executable;

        private long _exportTableAddress;
        private long _namesSectionRawData;

        private MemoryMappedViewAccessor _nameSectionAccessor;
        private MemoryMappedViewAccessor _namesAccessor;
        private MemoryMappedViewAccessor _functionsAccessor;

        public ExportDirectoryReader(Executable executable)
        {
            _executable = executable;
        }

        public ExportEntry[] Read()
        {
            _exportTableAddress = _executable.GetExportTableAddress();
            if (_exportTableAddress == 0)
            {
                return new ExportEntry[0];
            }

            ImageExportDirectory exportDirectory;
            ReadExportDirectory(out exportDirectory);

            uint numNames = exportDirectory.NumberOfNames;
            uint numFunctions = exportDirectory.NumberOfFunctions;

            // Names section
            int namesSection = _executable.GetRvaSection(exportDirectory.AddressOfNames);
            _nameSectionAccessor = _executable.GetSectionAccessor(namesSection);
            _namesSectionRawData = _executable.Sections[namesSection].PointerToRawData;

            // Name addresses
            var addressOfNames = _executable.DecodeRva(exportDirectory.AddressOfNames, namesSection);
            _namesAccessor = _executable.GetFileAccessor(addressOfNames, numNames * sizeof(uint));

            // Funtions section
            var functionsSection = _executable.GetRvaSection(exportDirectory.AddressOfFunctions);
            var addressOfFunctions = _executable.DecodeRva(exportDirectory.AddressOfFunctions, functionsSection);
            _functionsAccessor = _executable.GetFileAccessor(addressOfFunctions, numFunctions * sizeof(uint));

            var exportEntries = new ExportEntry[numNames];
            for (int i = 0; i < numNames; i++)
            {
                var entry = ReadExportEntry(i);
                exportEntries[i] = entry;
            }

            _nameSectionAccessor.Dispose();
            _namesAccessor.Dispose();
            _functionsAccessor.Dispose();

            return exportEntries;
        }

        private void ReadExportDirectory(out ImageExportDirectory exportDirectory)
        {
            using (var accessor = _executable.GetFileAccessor(_executable.DecodeRva(_exportTableAddress),
                Marshal.SizeOf(typeof(ImageExportDirectory))))
            {
                accessor.Read(0, out exportDirectory);
            }
        }

        private ExportEntry ReadExportEntry(int index)
        {
            uint nameRva = _namesAccessor.ReadUInt32(index * sizeof(uint));
            long nameAddress = _executable.DecodeRva(nameRva) - _namesSectionRawData;
            string name = _executable.ReadStringFromFile(_nameSectionAccessor, nameAddress);

            uint functionRva = _functionsAccessor.ReadUInt32(index * sizeof(uint));
            //long functionAddress = _executable.DecodeRva(functionRva);

            return new ExportEntry(name, functionRva);
        }

        // https://en.wikiversity.org/wiki/Visual_C%2B%2B_name_mangling
        private string UnmangleName(string name)
        {
            if (name[0] != '?') return name;

            List<string> fragments = new List<string>();
            string fragment = "";
            string result = "";

            bool nameFragment = true;
            bool specialName = false;
            bool hasTemplateArgs = false;
            for (int i = 1; i < name.Length; i++)
            {
                char c = name[i];

                if (nameFragment)
                {
                    if (c == '?')
                    {
                        specialName = true;
                        nameFragment = false;
                    }
                    else if (c == '@')
                    {
                        if (fragment.Length != 0)
                        {
                            fragments.Add(fragment);
                            //if (!hasTemplateArgs)
                            {
                                fragment = "";
                            }
                        }
                        else
                        {
                            if (hasTemplateArgs)
                            {
                                var templateArgs = fragments.Skip(1).Reverse();
                                result += fragments.First() + '<' + string.Join("::", templateArgs) + '>';
                            }
                            else
                            {
                                fragments.Reverse();
                                result += string.Join("::", fragments);
                            }
                            hasTemplateArgs = false;
                            nameFragment = false;
                        }
                    }
                    else
                    {
                        fragment += c;
                    }
                }
                else if (specialName)
                {
                    if (c == '$')
                    {
                        hasTemplateArgs = true;
                    }

                    specialName = false;
                    nameFragment = true;
                }
                else
                {
                    result += c;
                }
            }
            return result;
        }
    }
}
