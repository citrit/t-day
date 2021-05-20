using CommandLine;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using TDayLib;

namespace gen_routes
{
    public class Program
    {
        public class Options
        {
            [Option('i', "inputFile", Required = true, HelpText = "CSV File with validated addresses.")]
            public string inputFile { get; set; }
        }

        public static void Main(string[] args)
        {

            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptions)
                .WithNotParsed(HandleParseError);
        }

        private static void HandleParseError(IEnumerable<Error> errs)
        {
        }

        private static void DebugOut(string msg)
        {
            Console.Out.Write(msg);
        }

        private static void RunOptions(Options opts)
        {
            using (var reader = new StreamReader(opts.inputFile))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<TDayAddress>();

                TDayAddress.GenerateRoutes(records, Path.GetDirectoryName(opts.inputFile), new MsgOut(DebugOut));
            }
        }

   }
}
