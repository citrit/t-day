using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CommandLine;
using TDayLib;


namespace checkaddress
{
    class MainClass
    {
        public class Options
        {
            [Option('x', "excelfile", Required = true, HelpText = "Excel file with raw addresses.")]
            public string excelFile { get; set; }
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
            Console.WriteLine("Lets check some addresses!");

            var goodAddr = new ConcurrentBag<TDayAddress>();
            var badAddr = new ConcurrentBag<TDayAddress>();

            Stopwatch sw = new Stopwatch();
            sw.Start();
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            TDayAddress.ReadExcelFile(opts.excelFile, Path.GetDirectoryName(opts.excelFile), goodAddr, badAddr, new MsgOut(DebugOut));
            sw.Stop();
            string drcty = Path.GetDirectoryName(opts.excelFile);
            Console.WriteLine($"\nValidate addresses time: {sw.Elapsed}  with {goodAddr.Count} good and {badAddr.Count} failed addresses");
            Console.WriteLine($"\nData written to {drcty + "/GoodAddresses.csv"} and {drcty + "/Badddresses.csv"}");
        }
    }
}
