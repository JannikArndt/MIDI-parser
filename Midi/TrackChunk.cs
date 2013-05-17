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
using TrackEvents = System.Collections.Generic.List<Midi.Event.MidiEvent>;
using StringEncoder = System.Text.UTF7Encoding;
using ByteList = System.Collections.Generic.List<byte>;
using BitConverter = System.BitConverter;
using MidiEvent = Midi.Event.MidiEvent;
using MetaEvent = Midi.Event.MetaEvent;
using ChannelEvent = Midi.Event.ChannelEvent;
using ArgumentException = System.ArgumentException;

namespace Midi
{
	public class TrackChunk : Chunk
	{
		private readonly byte[] track_data;
		// Lazy loading of the events list
		private TrackEvents _events = null;

		public TrackEvents events {
			get {
				switch (_events == null) {
				case true:
					_events = parse_events (this.chunk_size, this.track_data);
					return _events;
				default:
					return _events;
				}
			}
			private set {}
		}

		public TrackChunk (string chunk_ID, int chunk_size, byte[] track_data) : base(chunk_ID, chunk_size)
		{
			this.track_data = track_data;
		}

		private static TrackEvents parse_events (int chunk_size, byte[] track_data)
		{
			TrackEvents events = new TrackEvents ();
			StringEncoder stringEncoder = new StringEncoder ();

			int i = 0;
			ByteList delta_time_bytes = new ByteList ();
			while (i < chunk_size) {
				delta_time_bytes.Add (track_data [i]);
				
				switch (track_data [i] == 0x00) {
					
				// End of delta time reached
				case true:
					i += 1;
					
					byte event_type_value = track_data [i];
					int delta_time = 0;
					try {
						delta_time = BitConverter.ToInt16 (delta_time_bytes.ToArray<byte> ().Reverse ().ToArray<byte> (), 0);
					} catch (ArgumentException e) {
						delta_time = 0;
					}
					
					// MIDI Channel Events
					if ((event_type_value & 0xF0) < 0xF0) {
						
						byte midi_channel = (byte)(event_type_value & 0x0F);
						i += 1;
						byte parameter_1 = track_data [i];
						i += 1;
						byte parameter_2 = track_data [i];
						events.Add (new ChannelEvent (delta_time, event_type_value, midi_channel, parameter_1, parameter_2));
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
					
					i += 1;
					
					delta_time_bytes = new ByteList ();
					break;
				// End of delta time not yet reached
				case false:
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
