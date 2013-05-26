using System;
using FileStream = System.IO.FileStream;
using System.Linq;
using ByteList = System.Collections.Generic.List<byte>;
using BoolList = System.Collections.Generic.List<bool>;
using BoolEnumerable = System.Collections.Generic.IEnumerable<bool>;

namespace MidiTest
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            System.Console.Out.WriteLine("START");
            DateTime start = System.DateTime.Now;

            FileStream file = System.IO.File.OpenRead("D:\\Downloads\\8641_test - Copy.mid");
            Midi.MidiData midi_file = Midi.FileParser.parse(file);

            System.Console.Out.WriteLine(midi_file.header);
            System.Console.Out.WriteLine(midi_file.tracks.Count);
            midi_file.tracks.ToList().ForEach(t => System.Console.Out.WriteLine("count: " + t.events.ToList().Count));
            midi_file.tracks.ToList().ForEach(t => System.Console.Out.WriteLine("count: " + t.events.ToList().Count));
            midi_file.tracks.ToList().ForEach(t => System.Console.Out.WriteLine("count: " + t.events.ToList().Count));
            midi_file.tracks.ToList().ForEach(t => System.Console.Out.WriteLine("count: " + t.events.ToList().Count));
            midi_file.tracks.ToList().ForEach(t => System.Console.Out.WriteLine("count: " + t.events.ToList().Count));
            midi_file.tracks.ToList().ForEach(t => System.Console.Out.WriteLine("count: " + t.events.ToList().Count));
            //midi_file.tracks.ToList().ElementAt(3).events.ToList().ForEach(t => System.Console.Out.WriteLine("e: " + t));

           /*System.Collections.Generic.Dictionary<ByteList, ByteList> test_and_results = new System.Collections.Generic.Dictionary<ByteList, ByteList>() {
				{new byte[] {0x00, 0x00, 0x00, 0x00}.ToList(), new byte[] {0x00}.ToList()},
				{new byte[] {0x00, 0x00, 0x00, 0x40}.ToList(), new byte[] {0x40}.ToList()},
				{new byte[] {0x00, 0x00, 0x00, 0x7F}.ToList(), new byte[] {0x7F}.ToList()},
				{new byte[] {0x00, 0x00, 0x00, 0x80}.ToList(), new byte[] {0x81, 0x00}.ToList()},
				{new byte[] {0x00, 0x00, 0x20, 0x00}.ToList(), new byte[] {0xC0, 0x00}.ToList()},
				{new byte[] {0x00, 0x00, 0x3F, 0xFF}.ToList(), new byte[] {0xFF, 0x7F}.ToList()},
				{new byte[] {0x00, 0x00, 0x40, 0x00}.ToList(), new byte[] {0x81, 0x80, 0x00}.ToList()},
				{new byte[] {0x00, 0x10, 0x00, 0x00}.ToList(), new byte[] {0xC0, 0x80, 0x00}.ToList()},
				{new byte[] {0x00, 0x1F, 0xFF, 0xFF}.ToList(), new byte[] {0xFF, 0xFF, 0x7F}.ToList()},
				{new byte[] {0x00, 0x20, 0x00, 0x00}.ToList(), new byte[] {0x81, 0x80, 0x80, 0x00}.ToList()},
				{new byte[] {0x08, 0x00, 0x00, 0x00}.ToList(), new byte[] {0xC0, 0x80, 0x80, 0x00}.ToList()},
				{new byte[] {0x0F, 0xFF, 0xFF, 0xFF}.ToList(), new byte[] {0xFF, 0xFF, 0xFF, 0x7F}.ToList()},
			};

            ByteList a1 = new byte[] { 0x00, 0x00, 0x00, 0x00 }.ToList();
            ByteList a2 = new byte[] { 0x00, 0x00, 0x00, 0x40 }.ToList();
            ByteList a3 = new byte[] { 0x00, 0x00, 0x00, 0x7F }.ToList();
            ByteList a4 = new byte[] { 0x00, 0x00, 0x00, 0x80 }.ToList();
            ByteList a5 = new byte[] { 0x00, 0x00, 0x20, 0x00 }.ToList();
            ByteList a6 = new byte[] { 0x00, 0x00, 0x3F, 0xFF }.ToList();
            ByteList a7 = new byte[] { 0x00, 0x00, 0x40, 0x00 }.ToList();
            ByteList a8 = new byte[] { 0x00, 0x10, 0x00, 0x00 }.ToList();
            ByteList a9 = new byte[] { 0x00, 0x1F, 0xFF, 0xFF }.ToList();
            ByteList a10 = new byte[] { 0x00, 0x20, 0x00, 0x0 }.ToList();
            ByteList a11 = new byte[] { 0x08, 0x00, 0x00, 0x00 }.ToList();
            ByteList a12 = new byte[] { 0x0F, 0xFF, 0xFF, 0xFF }.ToList();

            ByteList e1 = decode_variable_length(new byte[] { 0x00 }.ToList());
            ByteList e2 = decode_variable_length(new byte[] { 0x40 }.ToList());
            ByteList e3 = decode_variable_length(new byte[] { 0x7F }.ToList());
            ByteList e4 = decode_variable_length(new byte[] { 0x81, 0x00 }.ToList());
            ByteList e5 = decode_variable_length(new byte[] { 0xC0, 0x00 }.ToList());
            ByteList e6 = decode_variable_length(new byte[] { 0xFF, 0x7F }.ToList());
            ByteList e7 = decode_variable_length(new byte[] { 0x81, 0x80, 0x00 }.ToList());
            ByteList e8 = decode_variable_length(new byte[] { 0xC0, 0x80, 0x00 }.ToList());
            ByteList e9 = decode_variable_length(new byte[] { 0xFF, 0xFF, 0x7F }.ToList());
            ByteList e10 = decode_variable_length(new byte[] { 0x81, 0x80, 0x80, 0x00 }.ToList());
            ByteList e11 = decode_variable_length(new byte[] { 0xC0, 0x80, 0x80, 0x00 }.ToList());
            ByteList e12 = decode_variable_length(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }.ToList());

            System.Console.Out.WriteLine("");
            System.Console.Out.Write("a1: ");
            a1.ForEach(x => System.Console.Out.Write(x + ", "));
            System.Console.Out.WriteLine("");
            System.Console.Out.Write("e1: ");
            e1.ForEach(x => System.Console.Out.Write(x + ", "));
            System.Console.Out.WriteLine("");
            System.Console.Out.WriteLine("");
            System.Console.Out.Write("a2: ");
            a2.ForEach(x => System.Console.Out.Write(x + ", "));
            System.Console.Out.WriteLine("");
            System.Console.Out.Write("e2: ");
            e2.ForEach(x => System.Console.Out.Write(x + ", "));
            System.Console.Out.WriteLine("");
            System.Console.Out.WriteLine("");
            System.Console.Out.Write("a3: ");
            a3.ForEach(x => System.Console.Out.Write(x + ", "));
            System.Console.Out.WriteLine("");
            System.Console.Out.Write("e3: ");
            e3.ForEach(x => System.Console.Out.Write(x + ", "));
            System.Console.Out.WriteLine("");
            System.Console.Out.WriteLine("");
            System.Console.Out.Write("a4: ");
            a4.ForEach(x => System.Console.Out.Write(x + ", "));
            System.Console.Out.WriteLine("");
            System.Console.Out.Write("e4: ");
            e4.ForEach(x => System.Console.Out.Write(x + ", "));
            System.Console.Out.WriteLine("");
            System.Console.Out.WriteLine("");
            System.Console.Out.Write("a5: ");
            a5.ForEach(x => System.Console.Out.Write(x + ", "));
            System.Console.Out.WriteLine("");
            System.Console.Out.Write("e5: ");
            e5.ForEach(x => System.Console.Out.Write(x + ", "));
            System.Console.Out.WriteLine("");
            System.Console.Out.WriteLine("");
            System.Console.Out.Write("a6: ");
            a6.ForEach(x => System.Console.Out.Write(x + ", "));
            System.Console.Out.WriteLine("");
            System.Console.Out.Write("e6: ");
            e6.ForEach(x => System.Console.Out.Write(x + ", "));
            System.Console.Out.WriteLine("");
            System.Console.Out.WriteLine("");
            System.Console.Out.Write("a7: ");
            a7.ForEach(x => System.Console.Out.Write(x + ", "));
            System.Console.Out.WriteLine("");
            System.Console.Out.Write("e7: ");
            e7.ForEach(x => System.Console.Out.Write(x + ", "));
            System.Console.Out.WriteLine("");
            System.Console.Out.WriteLine("");
            System.Console.Out.Write("a8: ");
            a8.ForEach(x => System.Console.Out.Write(x + ", "));
            System.Console.Out.WriteLine("");
            System.Console.Out.Write("e8: ");
            e8.ForEach(x => System.Console.Out.Write(x + ", "));
            System.Console.Out.WriteLine("");
            System.Console.Out.WriteLine("");
            System.Console.Out.Write("a9: ");
            a9.ForEach(x => System.Console.Out.Write(x + ", "));
            System.Console.Out.WriteLine("");
            System.Console.Out.Write("e9: ");
            e9.ForEach(x => System.Console.Out.Write(x + ", "));
            System.Console.Out.WriteLine("");
            System.Console.Out.WriteLine("");
            System.Console.Out.Write("a10: ");
            a10.ForEach(x => System.Console.Out.Write(x + ", "));
            System.Console.Out.WriteLine("");
            System.Console.Out.Write("e10: ");
            e10.ForEach(x => System.Console.Out.Write(x + ", "));
            System.Console.Out.WriteLine("");
            System.Console.Out.WriteLine("");
            System.Console.Out.Write("a11: ");
            a11.ForEach(x => System.Console.Out.Write(x + ", "));
            System.Console.Out.WriteLine("");
            System.Console.Out.Write("e11: ");
            e11.ForEach(x => System.Console.Out.Write(x + ", "));
            System.Console.Out.WriteLine("");
            System.Console.Out.WriteLine("");
            System.Console.Out.Write("a12: ");
            a12.ForEach(x => System.Console.Out.Write(x + ", "));
            System.Console.Out.WriteLine("");
            System.Console.Out.Write("e12: ");
            e12.ForEach(x => System.Console.Out.Write(x + ", "));*/


            System.Console.Out.WriteLine("");
            System.Console.Out.WriteLine("");

            DateTime end = System.DateTime.Now;
            System.Console.Out.WriteLine("");
            System.Console.Out.WriteLine("");
            System.Console.Out.WriteLine("END: " + (end - start) + " Milliseconds");
            System.Console.In.ReadLine();
        }
    }
}
