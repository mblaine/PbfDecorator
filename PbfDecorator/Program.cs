using System;
using System.IO;
using CommandLine;
using OsmSharp;
using OsmSharp.Streams;

namespace PbfDecorator
{
    class Program
    {
        class Options
        {
            [Option('i', "input", Required = true, HelpText = "Input *.pbf file name")]
            public string Input { get; set; }
            [Option('o', "output", Required = true, HelpText = "Output *.pbf file name")]
            public string Output { get; set; }
        }

        static void Main(string[] args)
        {
            string input = null, output = null;

            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       if (!string.IsNullOrWhiteSpace(o.Input))
                           input = o.Input;
                       if (!string.IsNullOrWhiteSpace(o.Output))
                           output = o.Output;
                   });

            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(output))
                return;

            Console.WriteLine($"Input: {input}");
            Console.WriteLine($"Output: {output}");

            using (var fsIn = File.OpenRead(input))
            using (var fsOut = new FileInfo(output).Open(FileMode.Create, FileAccess.ReadWrite))
            {
                var source = new PBFOsmStreamSource(fsIn);
                var target = new PBFOsmStreamTarget(fsOut, false);//false = compression disabled
                target.Initialize();

                int count = 0;
                
                foreach (OsmGeo element in source)
                {
                    count++;
                    if (count % 10000 == 0)
                        Console.Write($"\r{count:#,##0} {element.Type}       ");

                    if (element.Type != OsmGeoType.Node)
                    {
                        //element.Tags.Add("osmid", $"{element.Type.ToString().ToLower()}/{element.Id}");
                        //if (element.Version.HasValue)
                            //element.Tags.Add("osmver", element.Version.Value.ToString());
                        if (element.TimeStamp.HasValue)
                        {
                            //element.Tags.Add("osmdate", element.TimeStamp.Value.ToString("yyyy-MM-dd"));
                            DateTime d1 = element.TimeStamp.Value.Date;
                            DateTime d2 = DateTime.Today;
                            //https://stackoverflow.com/a/4639057
                            int months = (d2.Year - d1.Year) * 12 + d2.Month - d1.Month + (d2.Day >= d1.Day ? 0 : -1);
                            element.Tags.Add("osmage", months.ToString());
                        }
                    }

                    switch(element.Type)
                    {
                        case OsmGeoType.Node:
                            target.AddNode((Node)element);
                            break;
                        case OsmGeoType.Way:
                            target.AddWay((Way)element);
                            break;
                        case OsmGeoType.Relation:
                            target.AddRelation((Relation)element);
                            break;
                    }
                    
                }

                target.Flush();
                target.Close();                
            }
        }
    }
}
