using System;
using FileStream = System.IO.FileStream;
using System.Linq;

namespace MidiTest
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			System.Console.Out.WriteLine ("START");
			DateTime start = System.DateTime.Now;

			FileStream file = System.IO.File.OpenRead ("D:\\Daten\\vvvv\\123\\8641_test - Copy.mid");
			Midi.MidiFile midi_file = Midi.FileParser.parse (file);
			//System.Console.Out.WriteLine (midi_file);

			//System.Console.Out.WriteLine (255 & 0xF0);
			//System.Console.Out.WriteLine (255 & 0x0F);
			//Enumerable.Range (4, 0).ToList().ForEach(x => System.Console.Out.WriteLine (255 & 0x0F));
			//System.Console.Out.WriteLine ("0");
			//midi_file.tracks.First().events.ForEach(x => System.Console.Out.WriteLine (x));

			System.Console.Out.WriteLine (midi_file.tracks.ElementAt (1).events.Count);
			System.Console.Out.WriteLine (midi_file.tracks.ElementAt (0).events.Count);
			System.Console.Out.WriteLine (midi_file.tracks.ElementAt (2).events.Count);
			System.Console.Out.WriteLine (midi_file.tracks.ElementAt (3).events.Count);
			//System.Console.Out.WriteLine (midi_file.tracks.ElementAt (3).events.Count);
			//System.Console.Out.WriteLine (midi_file.tracks.ElementAt (3).events.Count);
			midi_file.tracks.ElementAt (2).events.ForEach((Midi.Events.MidiEvent x) => System.Console.Out.WriteLine (x));

			DateTime end = System.DateTime.Now;
			System.Console.Out.WriteLine ("END: " + (end - start) + " Milliseconds");
			System.Console.In.ReadLine ();
		}
	}
}
