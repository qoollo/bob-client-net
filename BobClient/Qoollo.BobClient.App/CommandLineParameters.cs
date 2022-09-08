using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ByteSizeLib;
using CommandLine;

namespace Qoollo.BobClient.App
{
    [Flags]
    public enum RunMode
    {
        None = 0,
        Get = 1,
        Put = 2,
        Exists = 4
    }

    public enum VerbosityLevel
    {
        Min = 0,
        Normal = 1,
        Max = 2
    }


    public class ExecutionConfig
    {
        [Option('m', "mode", Required = true, HelpText = "Work mode combined by comma. Possible values: 'Get,Put,Exists'")]
        public RunMode RunMode { get; set; } = RunMode.Get | RunMode.Exists;

        [Option('n', "nodes", Required = true, Default = null, HelpText = "Comma separated node addresses. Example: '127.0.0.1:20000, 127.0.0.2:20000'", Separator = ',')]
        public IEnumerable<string> Nodes { get; set; } = Array.Empty<string>();

        [Option('s', "start", Required = true, HelpText = "Start Id")]
        public ulong StartId { get; set; } = 0;

        [Option('e', "end", Required = false, Default = null, HelpText = "End Id")]
        public ulong? EndId { get; set; } = null;

        [Option('c', "count", Required = false, Default = (uint)1, HelpText = "Count of ids to process")]
        public uint Count { get; set; } = 1;

        [Option('l', "length", Required = false, Default = null, HelpText = "(Default: 1024) Size of the single record (support size specifiers: kb, mb, gb)")]
        public string DataLengthString
        {
            get { return DataLength?.ToString(); }
            set
            {
                if (value != null)
                {
                    if (uint.TryParse(value, out uint bytesVal))
                    {
                        DataLength = ByteSize.FromBytes(bytesVal);
                    }
                    else
                    {
                        value = value
                            .ToUpperInvariant()
                            .Replace(ByteSize.KiloByteSymbol, ByteSize.KibiByteSymbol)
                            .Replace(ByteSize.MegaByteSymbol, ByteSize.MebiByteSymbol)
                            .Replace(ByteSize.GigaByteSymbol, ByteSize.GibiByteSymbol)
                            .Replace(ByteSize.TeraByteSymbol, ByteSize.TebiByteSymbol)
                            .Replace(ByteSize.PetaByteSymbol, ByteSize.PebiByteSymbol);
                        DataLength = ByteSize.Parse(value, CultureInfo.InvariantCulture);
                    }

                    if (DataLength.Value.Bytes < 0)
                        throw new ArgumentOutOfRangeException(nameof(DataLength), "Length cannot be negative");
                    if (DataLength.Value.Bytes > int.MaxValue)
                        throw new ArgumentOutOfRangeException(nameof(DataLength), "Length is too large");
                }
                else
                {
                    DataLength = null;
                }
            }
        }
        public ByteSize? DataLength { get; set; } = null;


        [Option("random", Required = false, Default = false, HelpText = "Random read/write mode")]
        public bool RandomMode { get; set; } = false;

        [Option("verbosity", Required = false, Default = VerbosityLevel.Normal, HelpText = "Enable verbose output for errors (Min, Normal, Max)")]
        public VerbosityLevel Verbosisty { get; set; } = VerbosityLevel.Normal;

        [Option("timeout", Required = false, Default = (int)60, HelpText = "Operation and connection timeout in seconds")]
        public int Timeout
        {
            get { return _timeout; }
            set
            {
                if (value < 0) 
                    throw new ArgumentOutOfRangeException(nameof(Timeout), "Timeout cannot be negative");
                _timeout = value;
            }
        }
        private int _timeout = 60;

        [Option("threads", Required = false, Default = (uint)1, HelpText = "Number of threads")]
        public uint ThreadCount
        {
            get { return _threadCount; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(ThreadCount), "Number of threads should be at least 1");
                _threadCount = value;
            }
        }
        private uint _threadCount = 1;

        [Option("package-size", Required = false, Default = (uint)100, HelpText = "Exists package size")]
        public uint ExistsPackageSize
        {
            get { return _existsPackageSize; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(ExistsPackageSize), "Package size for exists cannot be less than 1");
                _existsPackageSize = value;
            }
        }
        private uint _existsPackageSize = 100;

        [Option("key-size", Required = false, Default = (uint)8, HelpText = "Target key size in bytes")]
        public uint KeySize
        {
            get { return _keySize; }
            set
            {
                if (value < 1) 
                    throw new ArgumentOutOfRangeException(nameof(KeySize), "Key size cannot be less than 1");
                _keySize = value;
            }
        }
        private uint _keySize  = sizeof(ulong);

        [Option("hex-data-pattern", Required = false, Default = null, HelpText = "Data pattern as hex string")]
        public string DataPatternHex { get; set; } = null;

        [Option("validate-get", Required = false, Default = false, HelpText = "Validates data received by Get")]
        public bool ValidateGet { get; set; } = false;

        [Option("put-file-source", Required = false, Default = null, HelpText = "Path to the file with source data. Supports '{key}' as pattern")]
        public string PutFileSourcePattern { get; set; } = null;

        [Option("get-file-target", Required = false, Default = null, HelpText = "Path to the file to store data from Get or to validate. Supports '{key}' as pattern")]
        public string GetFileTargetPattern { get; set; } = null;

        [Option("progress-period", Required = false, Default = 1000, HelpText = "Progress printing period in milliseconds")]
        public int ProgressPeriodMs 
        { 
            get { return _progressPeriodMs; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(ProgressPeriodMs), "Progress period cannot be less than 1");
                _progressPeriodMs = value;
            }
        } 
        private int _progressPeriodMs = 1000;
    }


    public static class CommandLineParametersParser
    {
        private static readonly Parser _commandLineParser = new Parser(s =>
        {
            s.AutoHelp = true;
            s.AutoVersion = true;
            s.ParsingCulture = CultureInfo.InvariantCulture;
            s.GetoptMode = false;
            s.CaseInsensitiveEnumValues = true;
            s.HelpWriter = System.IO.TextWriter.Synchronized(Console.Out);
        });

        public static ExecutionConfig ParseConfigFromArgs(string[] args)
        {
            var result = _commandLineParser.ParseArguments<ExecutionConfig>(args).Value;
            if (result == null)
                return result;

            if (result.Nodes == null || !result.Nodes.Any())
            {
                Console.WriteLine("Nodes list cannot be empty");
                return null;
            }

            if (result.DataPatternHex != null && (result.GetFileTargetPattern != null || result.PutFileSourcePattern != null))
            {
                Console.WriteLine("HexDataPattern cannot be combined with GetFileTarget or PutFileSource");
                return null;
            }

            result.Nodes = result.Nodes.Select(o => o.Trim()).ToArray();
            return result;
        }
    }
}
