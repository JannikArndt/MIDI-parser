/*
Copyright (c) 2013 Christoph Fabritz

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
using System.Linq;
using FileStream = System.IO.FileStream;
using BinaryReader = System.IO.BinaryReader;
using BitConverter = System.BitConverter;
using Tracks = System.Collections.Generic.List<Midi.Chunks.TrackChunk>;
using HeaderChunk = Midi.Chunks.HeaderChunk;
using TrackChunk = Midi.Chunks.TrackChunk;
using StringEncoder = System.Text.UTF7Encoding;
using ArgumentException = System.ArgumentException;
using TrackEvents = System.Collections.Generic.List<Midi.Events.MidiEvent>;

// Meta Events
using Midi.Events.MetaEvents;

// Channel Events
using ChannelAftertouchEvent = Midi.Events.ChannelEvents.ChannelAftertouchEvent;
using ControllerEvent = Midi.Events.ChannelEvents.ControllerEvent;
using NoteAftertouchEvent = Midi.Events.ChannelEvents.NoteAftertouchEvent;
using NoteOffEvent = Midi.Events.ChannelEvents.NoteOffEvent;
using NoteOnEvent = Midi.Events.ChannelEvents.NoteOnEvent;
using PitchBendEvent = Midi.Events.ChannelEvents.PitchBendEvent;
using ProgramChangeEvent = Midi.Events.ChannelEvents.ProgramChangeEvent;

namespace Midi
{
	public class FileParser
	{

		public static MidiData parse (FileStream input_file_stream)
		{
			BinaryReader input_binary_reader = new BinaryReader (input_file_stream);
			StringEncoder stringEncoder = new StringEncoder ();

			HeaderChunk header_chunk;
			ushort number_of_tracks;
			{
				string header_chunk_ID = stringEncoder.GetString (input_binary_reader.ReadBytes (4));
				int header_chunk_size = BitConverter.ToInt32 (input_binary_reader.ReadBytes (4).Reverse ().ToArray<byte> (), 0);
				byte[] header_chunk_data = input_binary_reader.ReadBytes (header_chunk_size);

				ushort format_type = BitConverter.ToUInt16 (header_chunk_data.Take (2).Reverse ().ToArray<byte> (), 0);
				number_of_tracks = BitConverter.ToUInt16 (header_chunk_data.Skip (2).Take (2).Reverse ().ToArray<byte> (), 0);
				ushort time_division = BitConverter.ToUInt16 (header_chunk_data.Skip (4).Take (2).Reverse ().ToArray<byte> (), 0);

				header_chunk = new HeaderChunk (format_type, time_division);
			}

			Tracks tracks =
				Enumerable.Range (0, number_of_tracks)
				.Select (track_number => {
					string track_chunk_ID = stringEncoder.GetString (input_binary_reader.ReadBytes (4));
					int track_chunk_size = BitConverter.ToInt32 (input_binary_reader.ReadBytes (4).Reverse ().ToArray<byte> (), 0);
					byte[] track_chunk_data = input_binary_reader.ReadBytes (track_chunk_size);

					TrackEvents events = parse_events (track_chunk_data);

					return new TrackChunk (events);
				}).ToList ();

			return new MidiData (header_chunk, tracks);
		}
		
		private static TrackEvents parse_events (byte[] track_data)
		{
			int chunk_size = track_data.Count ();
			TrackEvents events = new TrackEvents ();
			StringEncoder stringEncoder = new StringEncoder ();
			
			int i = 0;
			
			int delta_time = 0;
			while (i < chunk_size) {
				
				switch (track_data [i] < 0x80) {
					
				// End of delta time reached
				case true:
					
					delta_time += track_data [i];
					
					i += 1;
					byte event_type_value = track_data [i];
					
					// MIDI Channel Events
					if ((event_type_value & 0xF0) < 0xF0) {
						
						byte midi_channel_event_type = (byte)(event_type_value & 0xF0);
						byte midi_channel = (byte)(event_type_value & 0x0F);
						i += 1;
						byte parameter_1 = track_data [i];
						byte parameter_2 = 0x00;
						
						// One or two parameter type
						switch (midi_channel_event_type) {
							
						// One parameter types
						case 0xC0:
							events.Add (new ProgramChangeEvent (delta_time, midi_channel, parameter_1));
							break;
						case 0xD0:
							events.Add (new ChannelAftertouchEvent (delta_time, midi_channel, parameter_1));
							break;
							
						// Two parameter types
						case 0x80:
							i += 1;
							parameter_2 = track_data [i];
							events.Add (new NoteOffEvent (delta_time, midi_channel, parameter_1, parameter_2));
							break;
						case 0x90:
							i += 1;
							parameter_2 = track_data [i];
							events.Add (new NoteOnEvent (delta_time, midi_channel, parameter_1, parameter_2));
							break;
						case 0xA0:
							i += 1;
							parameter_2 = track_data [i];
							events.Add (new NoteAftertouchEvent (delta_time, midi_channel, parameter_1, parameter_2));
							break;
						case 0xB0:
							i += 1;
							parameter_2 = track_data [i];
							events.Add (new ControllerEvent (delta_time, midi_channel, parameter_1, parameter_2));
							break;
						case 0xE0:
							i += 1;
							parameter_2 = track_data [i];
							events.Add (new PitchBendEvent (delta_time, midi_channel, parameter_1, parameter_2));
							break;
						}
						
						//events.Add (new ChannelEvent (delta_time, midi_channel_event_type, midi_channel, parameter_1, parameter_2));
						
						i += 1;
					}
					// Meta Events
					else if (event_type_value == 0xFF) {
						i += 1;
						byte meta_event_type = track_data [i];
						i += 1;
						byte meta_event_length = track_data [i];
						i += 1;
						byte[] meta_event_data = Enumerable.Range (i, meta_event_length).Select (b => track_data [b]).ToArray<byte> ();

						switch (meta_event_type) {
						case 0x00:
							events.Add (new SequenceNumberEvent(delta_time));
							break;
						case 0x01:
							events.Add (new TextEvent(delta_time));
							break;
						case 0x02:
							events.Add (new CopyrightNoticeEvent());
							break;
						case 0x03:
							events.Add (new SequenceOrTrackNameEvent(delta_time));
							break;
						case 0x04:
							events.Add (new InstrumentNameEvent(delta_time));
							break;
						case 0x05:
							events.Add (new LyricsEvent(delta_time));
							break;
						case 0x06:
							events.Add (new MarkerEvent(delta_time));
							break;
						case 0x07:
							events.Add (new CuePointEvent(delta_time));
							break;
						case 0x20:
							events.Add (new MIDIChannelPrefixEvent(delta_time));
							break;
						case 0x2F:
							events.Add (new EndOfTrackEvent(delta_time));
							break;
						case 0x51:
							events.Add (new SetTempoEvent(delta_time));
							break;
						case 0x54:
							events.Add (new SMPTEOffsetEvent(delta_time));
							break;
						case 0x58:
							events.Add (new TimeSignatureEvent(delta_time));
							break;
						case 0x59:
							events.Add (new KeySignatureEvent(delta_time));
							break;
						case 0x7F:
							events.Add (new SequencerSpecificEvent(delta_time));
							break;
						}
						
						i += meta_event_length;
					}
					// System Exclusive Events
					else if (event_type_value == 0xF0) {
						throw new ArgumentException ("System Exclusive Events not yet supported");
					}
					
					delta_time = 0;
					
					break;
				// End of delta time not yet reached
				case false:
					delta_time += track_data [i];
					i += 1;
					break;
				}
			}
			
			return events;
		}
	}
}
