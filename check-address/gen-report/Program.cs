using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CommandLine;
using RTFExporter;

namespace gen_report
{
    class Program
    {

        public class Options
        {
            [Option('d', "deliveriesFile", Required = true, HelpText = "File with output deliveries.")]
            public string deliveriesFile { get; set; }
        }

        public static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptions)
                .WithNotParsed(HandleParseError);

        }

        private static void RunOptions(Options opts)
        {
            System.IO.StreamReader file = new System.IO.StreamReader(opts.deliveriesFile);
            string fname = Path.Combine(Path.GetDirectoryName(opts.deliveriesFile), "DeliveriesReport.rtf");
            string line;
            int delCnt = 0;
            int stpCnt = 0;
            using (RTFDocument doc = new RTFDocument(fname))
            {
                doc.SetMargin(0.5F, 0.5F, 1.0F, 1.0F);
                while ((line = file.ReadLine()) != null)
                {
                    string[] vals = line.Split(" ");
                    if (vals[0] == "DeliverID:" && vals[3] != "0")
                    {
                        delCnt++;
                        var p = doc.AppendParagraph();

                        p.style.alignment = Alignment.Left;
                        p.style.spaceAfter = 400;

                        var t = p.AppendText($"DeliveryID: {vals[1]} with {vals[3]} stops.\n");
                        t.style.bold = true;
                        t.style.color = new Color(0, 0, 0);
                        t.style.fontFamily = "Courier New";
                        int cnt = int.Parse(vals[3]);
                        int rsp = 45;
                        int numMeals = 0;
                        int totMeals = 0;
                        var rand = new Random();
                        for (int ii = 0; ii < cnt; ii++)
                        {
                            t = p.AppendText("_".PadLeft(65, '_') + "\n");
                            t.style.fontFamily = "Courier New";
                            stpCnt++;
                            vals = file.ReadLine().Split(":")[1].Split("|");
                            totMeals += numMeals = int.Parse(vals[11]);
                            t.content += $"{vals[0]} {vals[1]}"+sp(vals[0] + vals[1],rsp);
                            t.content += $"<b># of Meals: {numMeals}</b>\n";
                            t.content += $"  {vals[2]} {vals[3]}" + sp(vals[2] + vals[3], rsp-1)+ "Phone Numbers:\n";
                            string pn = vals[9] != "" ? String.Format("{0:(###) ###-####}", Int64.Parse(vals[9])) : "";
                            t.content += $"  {vals[4]}" + sp(vals[4], rsp) + $"Home: {pn}\n";
                            pn = vals[8] != "" ? String.Format("{0:(###)###-####}", Int64.Parse(vals[8])) : "";
                            t.content += $"  {vals[5]}, {vals[6]} {vals[7]}" + sp(vals[5]+ vals[6]+ vals[7], rsp-3) + $"Cell: {pn}\n";
                            t.content += $"  Delivery Notes: {vals[12]}\n";
                        }
                        t = p.AppendText("\n".PadRight(15, '=') + $" Meal Total: {totMeals} " + "\n\n".PadLeft(15, '='));
                        t.style.fontFamily = "Courier New";
                        t.style.fontSize += 4;
                        p.PageBreak = true;
                    }
                }
            }
            Console.Out.WriteLine($"Processed {delCnt} deliveries with a total of {stpCnt} stops written to {fname}");
        }

        private static string sp(string inStr, int cnt)
        {
            string ret = "";
            int pads = cnt - inStr.Length;
            for (int ii = 0; ii < (pads); ii++)
                ret += "\t";

            return "".PadLeft(pads>0?pads:0);
        }


        private static void HandleParseError(IEnumerable<Error> errs)
        {
        }

        private static void DebugOut(string msg)
        {
            Console.Out.Write(msg);
        }
    }
}
