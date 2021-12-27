using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.BobClient.InteractiveTests
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
        public RunMode RunMode { get; set; } = RunMode.Get | RunMode.Exists;
        public ulong StartId { get; set; } = 0;
        public ulong? EndId { get; set; } = null;
        public uint Count { get; set; } = 1;
        public bool RandomMode { get; set; } = false;
        public VerbosityLevel Verbosisty { get; set; } = VerbosityLevel.Normal;
        public int Timeout { get; set; } = 60;
        public uint ThreadCount { get; set; } = 1;
        public uint ExistsPackageSize { get; set; } = 100;
        public uint KeySize { get; set; } = sizeof(ulong);
        public int? DataLength { get; set; } = null;
        public string DataPatternHex { get; set; } = null;
        public bool ValidateGet { get; set; } = false;
        public string PutFileSourcePattern { get; set; } = null;
        public string GetFileTargetPattern { get; set; } = null;
        public int ProgressIntervalMs { get; set; } = 1000;
        public List<string> Nodes { get; set; } = new List<string>();
    }


    public static class CommandLineParametersParser
    {
        public static void PrintHelp()
        {
            Console.WriteLine($"Bob client tests (version: v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version})");
            Console.WriteLine("Arguments:");
            Console.WriteLine("  --mode   | -m     : Work mode combined by comma. Possible values: 'Get,Put,Exists'. Default: 'Get,Exists'");
            Console.WriteLine("  --length | -l     : Set size of the single record. Default: 1024");
            Console.WriteLine("  --start  | -s     : Start Id. Default: 0");
            Console.WriteLine("  --end    | -e     : End Id (optional)");
            Console.WriteLine("  --count  | -c     : Count of ids to process. Default: 1");
            Console.WriteLine("  --nodes  | -n     : Comma separated node addresses. Example: '127.0.0.1:20000, 127.0.0.2:20000'");
            Console.WriteLine("  --keySize         : Target key size in bytes. Default: 8");
            Console.WriteLine("  --threads         : Number of threads. Default: 1");
            Console.WriteLine("  --timeout         : Operation and connection timeout in seconds. Default: 60");
            Console.WriteLine("  --random          : Random read/write mode. Default: false");
            Console.WriteLine("  --verbosity       : Enable verbose output for errors (Min, Normal, Max). Default: Normal");
            Console.WriteLine("  --packageSize     : Exists package size. Default: 100");
            Console.WriteLine("  --validateGet     : Validates data received by Get. Default: false");
            Console.WriteLine("  --hexDataPattern  : Data pattern as hex string (optional)");
            Console.WriteLine("  --putFileSource   : Path to the file with source data. Supports '{key}' as pattern (optional)");
            Console.WriteLine("  --getFileTarget   : Path to the file to store data from Get or to validate. Supports '{key}' as pattern (optional)");
            Console.WriteLine("  --progressPeriod  : Progress printing period in milliseconds. Default: 1000");
            Console.WriteLine("  --help            : Print help");
            Console.WriteLine();
        }

        public static ExecutionConfig ParseConfigFromArgs(string[] args)
        {
            var result = new ExecutionConfig();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "--mode":
                    case "-m":
                        result.RunMode = (RunMode)Enum.Parse(typeof(RunMode), args[i + 1], ignoreCase: true);
                        i++;
                        break;
                    case "--length":
                    case "-l":
                        result.DataLength = int.Parse(args[i + 1]);
                        i++;
                        break;
                    case "--start":
                    case "-s":
                        result.StartId = ulong.Parse(args[i + 1]);
                        i++;
                        break;
                    case "--end":
                    case "-e":
                        result.EndId = ulong.Parse(args[i + 1]);
                        i++;
                        break;
                    case "--count":
                    case "-c":
                        result.Count = uint.Parse(args[i + 1]);
                        i++;
                        break;
                    case "--timeout":
                        result.Timeout = int.Parse(args[i + 1]);
                        i++;
                        break;
                    case "--threads":
                        result.ThreadCount = uint.Parse(args[i + 1]);
                        i++;
                        break;
                    case "--packagesize":
                        result.ExistsPackageSize = uint.Parse(args[i + 1]);
                        i++;
                        break;
                    case "--keysize":
                        result.KeySize = uint.Parse(args[i + 1]);
                        i++;
                        break;
                    case "--random":
                        if (i + 1 < args.Length && bool.TryParse(args[i + 1], out bool randomMode))
                        {
                            result.RandomMode = randomMode;
                            i++;
                        }
                        else
                        {
                            result.RandomMode = true;
                        }
                        break;
                    case "--verbosity":
                        result.Verbosisty = (VerbosityLevel)Enum.Parse(typeof(VerbosityLevel), args[i + 1], true);
                        i++;
                        break;
                    case "--hexdatapattern":
                        result.DataPatternHex = args[i + 1];
                        i++;
                        break;
                    case "--validateget":
                        if (i + 1 < args.Length && bool.TryParse(args[i + 1], out bool validateGet))
                        {
                            result.ValidateGet = validateGet;
                            i++;
                        }
                        else
                        {
                            result.ValidateGet = true;
                        }
                        break;
                    case "--putfilesource":
                        result.PutFileSourcePattern = args[i + 1];
                        i++;
                        break;
                    case "--getfiletarget":
                        result.GetFileTargetPattern = args[i + 1];
                        i++;
                        break;
                    case "--progressperiod":
                        result.ProgressIntervalMs = (int)uint.Parse(args[i + 1]);
                        i++;
                        break;
                    case "--nodes":
                    case "-n":
                        result.Nodes = args[i + 1].Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).ToList();
                        i++;
                        break;
                    case "--help":
                        PrintHelp();
                        break;
                    default:
                        Console.WriteLine($"Unknown argument: {args[i]}. Use '--help' to get help");
                        break;
                }
            }

            if (result.DataPatternHex != null && (result.GetFileTargetPattern != null || result.PutFileSourcePattern != null))
                Console.WriteLine("HexDataPattern cannot be combined with GetFileTarget or PutFileSource");

            return result;
        }
    }
}
