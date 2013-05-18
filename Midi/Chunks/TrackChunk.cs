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
using TrackEvents = System.Collections.Generic.List<Midi.Events.MidiEvent>;
using StringEncoder = System.Text.UTF7Encoding;
using ByteList = System.Collections.Generic.List<byte>;
using BitConverter = System.BitConverter;
using MidiEvent = Midi.Events.MidiEvent;
using MetaEvent = Midi.Events.MetaEvent;
using ArgumentException = System.ArgumentException;

// Channel Events
using ChannelAftertouchEvent = Midi.Events.ChannelEvents.ChannelAftertouchEvent;
using ControllerEvent = Midi.Events.ChannelEvents.ControllerEvent;
using NoteAftertouchEvent = Midi.Events.ChannelEvents.NoteAftertouchEvent;
using NoteOffEvent = Midi.Events.ChannelEvents.NoteOffEvent;
using NoteOnEvent = Midi.Events.ChannelEvents.NoteOnEvent;
using PitchBendEvent = Midi.Events.ChannelEvents.PitchBendEvent;
using ProgramChangeEvent = Midi.Events.ChannelEvents.ProgramChangeEvent;

namespace Midi.Chunks
{
	public class TrackChunk : Chunk
	{
		public readonly TrackEvents events = new TrackEvents ();

		public TrackChunk (string chunk_ID, int chunk_size, byte[] track_data) : base(chunk_ID, chunk_size)
		{
			this.events = parse_events (this.chunk_size, track_data);
		}

		private static TrackEvents parse_events (int chunk_size, byte[] track_data)
		{
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
						events.Add (new MetaEvent (
							delta_time,
							event_type_value,
							meta_event_type,
							meta_event_length,
							meta_event_data
						));

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
		
		override public string ToString ()
		{
			return "TrackChunk(" + base.ToString () + ", events: '" + events + "')";
		}
	}
}
