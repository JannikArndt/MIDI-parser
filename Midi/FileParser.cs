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
using TracksIEnumerable = System.Collections.Generic.IEnumerable<Midi.Chunks.TrackChunk>;
using HeaderChunk = Midi.Chunks.HeaderChunk;
using TrackChunk = Midi.Chunks.TrackChunk;
using StringEncoder = System.Text.UTF7Encoding;
using ArgumentException = System.ArgumentException;
using TrackEventsIEnumerable = System.Collections.Generic.IEnumerable<Midi.Events.MidiEvent>;
using TrackEventsList = System.Collections.Generic.List<Midi.Events.MidiEvent>;
using ByteList = System.Collections.Generic.List<byte>;
using ByteEnumerable = System.Collections.Generic.IEnumerable<byte>;
using MidiEvent = Midi.Events.MidiEvent;
using MIDIEvent_Length_Tuple = System.Tuple<Midi.Util.Option.Option<Midi.Events.MidiEvent>, int, byte>;
using Tuple = System.Tuple;
using SomeMidiEvent = Midi.Util.Option.Some<Midi.Events.MidiEvent>;
using NoMidiEvent = Midi.Util.Option.None<Midi.Events.MidiEvent>;
using BoolList = System.Collections.Generic.List<bool>;
using BoolEnumerable = System.Collections.Generic.IEnumerable<bool>;
using Math = System.Math;
using System;

// Meta Events
using Midi.Events.MetaEvents;

using SysexEvent = Midi.Events.SysexEvent;

// Channel Events
using Midi.Events.ChannelEvents;

namespace Midi
{
    public class FileParser
    {
        private readonly static StringEncoder stringEncoder = new StringEncoder();

        public static MidiData parse(FileStream input_file_stream)
        {
            BinaryReader input_binary_reader = new BinaryReader(input_file_stream);

            HeaderChunk header_chunk;
            ushort number_of_tracks;
            {
                string header_chunk_ID = stringEncoder.GetString(input_binary_reader.ReadBytes(4));
                int header_chunk_size = BitConverter.ToInt32(input_binary_reader.ReadBytes(4).Reverse().ToArray<byte>(), 0);
                byte[] header_chunk_data = input_binary_reader.ReadBytes(header_chunk_size);

                ushort format_type = BitConverter.ToUInt16(header_chunk_data.Take(2).Reverse().ToArray<byte>(), 0);
                number_of_tracks = BitConverter.ToUInt16(header_chunk_data.Skip(2).Take(2).Reverse().ToArray<byte>(), 0);
                ushort time_division = BitConverter.ToUInt16(header_chunk_data.Skip(4).Take(2).Reverse().ToArray<byte>(), 0);

                header_chunk = new HeaderChunk(format_type, time_division);
            }

            TracksIEnumerable tracks =
                Enumerable.Range(0, number_of_tracks)
                .Select(track_number =>
                {
                    string track_chunk_ID = stringEncoder.GetString(input_binary_reader.ReadBytes(4));
                    int track_chunk_size = BitConverter.ToInt32(input_binary_reader.ReadBytes(4).Reverse().ToArray<byte>(), 0);
                    byte[] track_chunk_data = input_binary_reader.ReadBytes(track_chunk_size);

                    return new TrackChunk(parse_events(track_chunk_data, track_chunk_size));
                });

            return new MidiData(header_chunk, tracks);
        }

        private static TrackEventsIEnumerable parse_events(ByteEnumerable track_data, int chunk_size)
        {
            int i = 0;
            byte last_midi_channel = 0x00;

            while (i < chunk_size)
            {
                MIDIEvent_Length_Tuple tuple = next_event(track_data, i, last_midi_channel);
                i += tuple.Item2;
                last_midi_channel = tuple.Item3;

                switch (tuple.Item1.GetType() == typeof(SomeMidiEvent))
                {
                    case true:
                        yield return ((SomeMidiEvent)tuple.Item1).value;
                        break;
                }
            }

            yield break;
        }

        private static MIDIEvent_Length_Tuple next_event(ByteEnumerable track_data, int start_index, byte last_midi_channel)
        {
            int i = start_index - 1;

            MidiEvent midi_event = null;
            {
                int delta_time = 0;
                {
                    ByteList length_temp = new ByteList();
                    do
                    {
                        i += 1;
                        length_temp.Add(track_data.ElementAt(i));
                    } while (track_data.ElementAt(i) > 0x7F);

                    switch (length_temp.Count < 4)
                    {
                        case true:
                            int diff = 4 - length_temp.Count;
                            length_temp.InsertRange(0, Enumerable.Range(0, diff).Select(x => (byte)0x00));
                            break;
                    }

                    length_temp = decode_variable_length(length_temp).ToList();

                    length_temp.Reverse();
                    delta_time = BitConverter.ToInt32(length_temp.ToArray(), 0);
                }

                i += 1;

                byte event_type_value = track_data.ElementAt(i);

                // MIDI Channel Events
                if ((event_type_value & 0xF0) < 0xF0)
                {

                    byte midi_channel_event_type = (byte)(event_type_value & 0xF0);
                    byte midi_channel = (byte)(event_type_value & 0x0F);
                    i += 1;
                    byte parameter_1 = track_data.ElementAt(i);
                    byte parameter_2 = 0x00;

                    // One or two parameter type
                    switch (midi_channel_event_type)
                    {
                        // One parameter types
                        case 0xC0:
                            midi_event = new ProgramChangeEvent(delta_time, midi_channel, parameter_1);
                            last_midi_channel = midi_channel;
                            break;
                        case 0xD0:
                            midi_event = new ChannelAftertouchEvent(delta_time, midi_channel, parameter_1);
                            last_midi_channel = midi_channel;
                            break;

                        // Two parameter types
                        case 0x80:
                            i += 1;
                            parameter_2 = track_data.ElementAt(i);
                            midi_event = new NoteOffEvent(delta_time, midi_channel, parameter_1, parameter_2);
                            last_midi_channel = midi_channel;
                            break;
                        case 0x90:
                            i += 1;
                            parameter_2 = track_data.ElementAt(i);
                            midi_event = new NoteOnEvent(delta_time, midi_channel, parameter_1, parameter_2);
                            last_midi_channel = midi_channel;
                            break;
                        case 0xA0:
                            i += 1;
                            parameter_2 = track_data.ElementAt(i);
                            midi_event = new NoteAftertouchEvent(delta_time, midi_channel, parameter_1, parameter_2);
                            last_midi_channel = midi_channel;
                            break;
                        case 0xB0:
                            i += 1;
                            parameter_2 = track_data.ElementAt(i);
                            midi_event = new ControllerEvent(delta_time, midi_channel, parameter_1, parameter_2);
                            last_midi_channel = midi_channel;
                            break;
                        case 0xE0:
                            i += 1;
                            parameter_2 = track_data.ElementAt(i);
                            midi_event = new PitchBendEvent(delta_time, midi_channel, parameter_1, parameter_2);
                            last_midi_channel = midi_channel;
                            break;
                        // Might be a Control Change Messages LSB
                        default:
                            midi_event = new ControllerEvent(delta_time, last_midi_channel, event_type_value, parameter_1);
                            break;
                    }

                    i += 1;
                }
                // Meta Events
                else if (event_type_value == 0xFF)
                {
                    i += 1;
                    byte meta_event_type = track_data.ElementAt(i);
                    i += 1;
                    byte meta_event_length = track_data.ElementAt(i);
                    i += 1;
                    byte[] meta_event_data = Enumerable.Range(i, meta_event_length).Select(b => track_data.ElementAt(b)).ToArray();

                    switch (meta_event_type)
                    {
                        case 0x00:
                            midi_event = new SequenceNumberEvent(BitConverter.ToUInt16(meta_event_data.Reverse().ToArray<byte>(), 0));
                            break;
                        case 0x01:
                            midi_event = new TextEvent(delta_time, stringEncoder.GetString(meta_event_data));
                            break;
                        case 0x02:
                            midi_event = new CopyrightNoticeEvent(stringEncoder.GetString(meta_event_data));
                            break;
                        case 0x03:
                            midi_event = new SequenceOrTrackNameEvent(stringEncoder.GetString(meta_event_data));
                            break;
                        case 0x04:
                            midi_event = new InstrumentNameEvent(delta_time, stringEncoder.GetString(meta_event_data));
                            break;
                        case 0x05:
                            midi_event = new LyricsEvent(delta_time, stringEncoder.GetString(meta_event_data));
                            break;
                        case 0x06:
                            midi_event = new MarkerEvent(delta_time, stringEncoder.GetString(meta_event_data));
                            break;
                        case 0x07:
                            midi_event = new CuePointEvent(delta_time, stringEncoder.GetString(meta_event_data));
                            break;
                        case 0x20:
                            midi_event = new MIDIChannelPrefixEvent(delta_time, meta_event_data[0]);
                            break;
                        case 0x2F:
                            midi_event = new EndOfTrackEvent(delta_time);
                            break;
                        case 0x51:
                            int tempo =
                                (meta_event_data[2] & 0x0F) +
                                ((meta_event_data[2] & 0xF0) * 16) +
                                ((meta_event_data[1] & 0x0F) * 256) +
                                ((meta_event_data[1] & 0xF0) * 4096) +
                                ((meta_event_data[0] & 0x0F) * 65536) +
                                ((meta_event_data[0] & 0xF0) * 1048576);
                            midi_event = new SetTempoEvent(delta_time, tempo);
                            break;
                        case 0x54:
                            midi_event = new SMPTEOffsetEvent(delta_time, meta_event_data[0], meta_event_data[1], meta_event_data[2], meta_event_data[3], meta_event_data[4]);
                            break;
                        case 0x58:
                            midi_event = new TimeSignatureEvent(delta_time, meta_event_data[0], meta_event_data[1], meta_event_data[2], meta_event_data[3]);
                            break;
                        case 0x59:
                            midi_event = new KeySignatureEvent(delta_time, meta_event_data[0], meta_event_data[1]);
                            break;
                        case 0x7F:
                            midi_event = new SequencerSpecificEvent(delta_time, meta_event_data);
                            break;
                    }

                    i += meta_event_length;
                }
                // System Exclusive Events
                else if (event_type_value == 0xF0 || event_type_value == 0xF7)
                {

                    int event_length = 0;
                    {
                        ByteList length_temp = new ByteList();
                        do
                        {
                            i += 1;
                            length_temp.Add(track_data.ElementAt(i));
                        } while (track_data.ElementAt(i) > 0x7F);

                        switch (length_temp.Count < 4)
                        {
                            case true:
                                int diff = 4 - length_temp.Count;
                                length_temp.InsertRange(0, Enumerable.Range(0, diff).Select(x => (byte)0x00));
                                break;
                        }

                        length_temp = decode_variable_length(length_temp).ToList();

                        length_temp.Reverse();
                        event_length = BitConverter.ToInt32(length_temp.ToArray(), 0);
                    }

                    i += 1;

                    ByteEnumerable event_data = Enumerable.Range(i, event_length).Select(b => track_data.ElementAt(b));

                    midi_event = new SysexEvent(delta_time, event_type_value, event_data);

                    i += event_length;
                }
            }

            switch (midi_event != null)
            {
                case true:
                    return new MIDIEvent_Length_Tuple(new SomeMidiEvent(midi_event), i - start_index, last_midi_channel);
            }

            return new MIDIEvent_Length_Tuple(new NoMidiEvent(), i - start_index, last_midi_channel);
        }

        private static BoolEnumerable convert_byte_to_bools(byte byte_in)
        {
            Func<byte, BoolEnumerable> convert_hex_to_bools = hex_in =>
            {
                bool[] temp = new bool[0];

                switch (hex_in)
                {
                    case 0x00:
                        temp = new bool[] { false, false, false, false };
                        break;
                    case 0x01:
                        temp = new bool[] { false, false, false, true };
                        break;
                    case 0x02:
                        temp = new bool[] { false, false, true, false };
                        break;
                    case 0x03:
                        temp = new bool[] { false, false, true, true };
                        break;
                    case 0x04:
                        temp = new bool[] { false, true, false, false };
                        break;
                    case 0x05:
                        temp = new bool[] { false, true, false, true };
                        break;
                    case 0x06:
                        temp = new bool[] { false, true, true, false };
                        break;
                    case 0x07:
                        temp = new bool[] { false, true, true, true };
                        break;
                    case 0x08:
                        temp = new bool[] { true, false, false, false };
                        break;
                    case 0x09:
                        temp = new bool[] { true, false, false, true };
                        break;
                    case 0x0A:
                        temp = new bool[] { true, false, true, false };
                        break;
                    case 0x0B:
                        temp = new bool[] { true, false, true, true };
                        break;
                    case 0x0C:
                        temp = new bool[] { true, true, false, false };
                        break;
                    case 0x0D:
                        temp = new bool[] { true, true, false, true };
                        break;
                    case 0x0E:
                        temp = new bool[] { true, true, true, false };
                        break;
                    case 0x0F:
                        temp = new bool[] { true, true, true, true };
                        break;
                }

                return temp;
            };

            return new byte[] { (byte)((byte_in & 0xF0) >> 4), (byte)(byte_in & 0x0F) }
                .SelectMany(bb => convert_hex_to_bools(bb));
        }

        private static byte convert_bools_to_byte(BoolEnumerable bools_in)
        {
            Func<bool[], byte> convert_bools_to_hex = b_in =>
            {
                byte temp = 0x00;

                switch (b_in[0])
                {
                    case true:
                        switch (b_in[1])
                        {
                            case true:
                                switch (b_in[2])
                                {
                                    case true:
                                        switch (b_in[3])
                                        {
                                            case true:
                                                temp = 0x0F;
                                                break;
                                            case false:
                                                temp = 0x0E;
                                                break;
                                        }
                                        break;
                                    case false:
                                        switch (b_in[3])
                                        {
                                            case true:
                                                temp = 0x0D;
                                                break;
                                            case false:
                                                temp = 0x0C;
                                                break;
                                        }
                                        break;
                                }
                                break;
                            case false:
                                switch (b_in[2])
                                {
                                    case true:
                                        switch (b_in[3])
                                        {
                                            case true:
                                                temp = 0x0B;
                                                break;
                                            case false:
                                                temp = 0x0A;
                                                break;
                                        }
                                        break;
                                    case false:
                                        switch (b_in[3])
                                        {
                                            case true:
                                                temp = 0x09;
                                                break;
                                            case false:
                                                temp = 0x08;
                                                break;
                                        }
                                        break;
                                }
                                break;
                        }
                        break;
                    case false:
                        switch (b_in[1])
                        {
                            case true:
                                switch (b_in[2])
                                {
                                    case true:
                                        switch (b_in[3])
                                        {
                                            case true:
                                                temp = 0x07;
                                                break;
                                            case false:
                                                temp = 0x06;
                                                break;
                                        }
                                        break;
                                    case false:
                                        switch (b_in[3])
                                        {
                                            case true:
                                                temp = 0x05;
                                                break;
                                            case false:
                                                temp = 0x04;
                                                break;
                                        }
                                        break;
                                }
                                break;
                            case false:
                                switch (b_in[2])
                                {
                                    case true:
                                        switch (b_in[3])
                                        {
                                            case true:
                                                temp = 0x03;
                                                break;
                                            case false:
                                                temp = 0x02;
                                                break;
                                        }
                                        break;
                                    case false:
                                        switch (b_in[3])
                                        {
                                            case true:
                                                temp = 0x01;
                                                break;
                                            case false:
                                                temp = 0x00;
                                                break;
                                        }
                                        break;
                                }
                                break;
                        }
                        break;
                }

                return temp;
            };

            bool[] in_array = bools_in.ToArray();
            byte result1 = (byte)(
                (convert_bools_to_hex(in_array) << 4)
                +
                 convert_bools_to_hex(in_array.Skip(4).ToArray())
                );

            return result1;
        }

        private static ByteEnumerable decode_variable_length(ByteEnumerable bytes_in)
        {
            // Convert all bytes into a list of booleans and skip MSB
            BoolList bits = bytes_in
                .SelectMany(b => convert_byte_to_bools(b).Skip(1)).ToList();

            // Prepend as many boolean falses as bits were kicked out
            {
                int bools_remaining_to_full_binary = (int)(Math.Ceiling(bits.Count / 8.0) * 8) - bits.Count;
                bits.InsertRange(0, new bool[bools_remaining_to_full_binary]);
            }

            // Partition the list of booleans into groups of eight convert them to bytes
            return Enumerable.Range(0, bits.Count / 8).Select(i => convert_bools_to_byte(bits.Skip(i * 8).Take(8)));
        }
    }
}
