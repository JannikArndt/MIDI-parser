using Midi.Chunks;
using Midi.Events;
using Midi.Events.ChannelEvents;
using Midi.Events.MetaEvents;
using Midi.Util;
using Midi.Util.Option;
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Midi
{
    public class FileParser
    {
        private static readonly UTF7Encoding StringEncoder = new UTF7Encoding();

        public static MidiData Parse(FileStream inputFileStream)
        {
            var inputBinaryReader = new BinaryReader(inputFileStream);


            // Order of ReadBytes() ist critical!
            var headerChunkId = StringEncoder.GetString(inputBinaryReader.ReadBytes(4));
            var headerChunkSize = BitConverter.ToInt32(inputBinaryReader.ReadBytes(4).Reverse().ToArray(), 0);
            var headerChunkData = inputBinaryReader.ReadBytes(headerChunkSize);

            var formatType = BitConverter.ToUInt16(headerChunkData.Take(2).Reverse().ToArray(), 0);
            var numberOfTracks = BitConverter.ToUInt16(headerChunkData.Skip(2).Take(2).Reverse().ToArray(), 0);
            var timeDivision = BitConverter.ToUInt16(headerChunkData.Skip(4).Take(2).Reverse().ToArray(), 0);

            var headerChunk = new HeaderChunk(formatType, timeDivision) { ChunkId = headerChunkId };

            var tracks =
                Enumerable.Range(0, numberOfTracks).Select(trackNumber =>
                {
                    // ReSharper disable once UnusedVariable
                    var trackChunkId = StringEncoder.GetString(inputBinaryReader.ReadBytes(4));
                    var trackChunkSize = BitConverter.ToInt32(inputBinaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                    var trackChunkData = inputBinaryReader.ReadBytes(trackChunkSize);

                    return Tuple.Create(trackChunkSize, trackChunkData);
                }).ToList()
                    .Select(rawTrack => new TrackChunk(ParseEvents(rawTrack.Item2.ToList(), rawTrack.Item1))).ToList();

            return new MidiData(headerChunk, tracks);
        }

        private static List<MidiEvent> ParseEvents(List<byte> trackData, int chunkSize)
        {
            var i = 0;
            var lastMidiChannel = (byte)0x00;
            var results = new List<MidiEvent>();

            while (i < chunkSize)
            {
                var tuple = NextEvent(trackData, i, lastMidiChannel);
                i += tuple.Item2;
                lastMidiChannel = tuple.Item3;

                if (tuple.Item1 is Some<MidiEvent>)
                    results.Add((tuple.Item1 as Some<MidiEvent>).value);
            }
            return results;
        }

        private static Tuple<Option<MidiEvent>, int, byte> NextEvent(List<byte> trackData, int startIndex, byte lastMidiChannel)
        {
            var i = startIndex - 1;

            MidiEvent midiEvent = null;
            {
                int deltaTime;
                {
                    var lengthTemp = new List<byte>();
                    do
                    {
                        i += 1;
                        lengthTemp.Add(trackData.ElementAt(i));
                    } while (trackData.ElementAt(i) > 0x7F);

                    deltaTime = VariableLengthUtil.decode_to_int(lengthTemp);
                }

                i += 1;

                var eventTypeValue = trackData.ElementAt(i);

                // MIDI Channel Events
                if ((eventTypeValue & 0xF0) < 0xF0)
                {
                    var midiChannelEventType = (byte)(eventTypeValue & 0xF0);
                    var midiChannel = (byte)(eventTypeValue & 0x0F);
                    i += 1;
                    var parameter1 = trackData.ElementAt(i);
                    byte parameter2;

                    // One or two parameter type
                    switch (midiChannelEventType)
                    {
                        // One parameter types
                        case 0xC0:
                            midiEvent = new ProgramChangeEvent(deltaTime, midiChannel, parameter1);
                            lastMidiChannel = midiChannel;
                            break;
                        case 0xD0:
                            midiEvent = new ChannelAftertouchEvent(deltaTime, midiChannel, parameter1);
                            lastMidiChannel = midiChannel;
                            break;

                        // Two parameter types
                        case 0x80:
                            i += 1;
                            parameter2 = trackData.ElementAt(i);
                            midiEvent = new NoteOffEvent(deltaTime, midiChannel, parameter1, parameter2);
                            lastMidiChannel = midiChannel;
                            break;
                        case 0x90:
                            i += 1;
                            parameter2 = trackData.ElementAt(i);
                            midiEvent = new NoteOnEvent(deltaTime, midiChannel, parameter1, parameter2);
                            lastMidiChannel = midiChannel;
                            break;
                        case 0xA0:
                            i += 1;
                            parameter2 = trackData.ElementAt(i);
                            midiEvent = new NoteAftertouchEvent(deltaTime, midiChannel, parameter1, parameter2);
                            lastMidiChannel = midiChannel;
                            break;
                        case 0xB0:
                            i += 1;
                            parameter2 = trackData.ElementAt(i);
                            midiEvent = new ControllerEvent(deltaTime, midiChannel, parameter1, parameter2);
                            lastMidiChannel = midiChannel;
                            break;
                        case 0xE0:
                            i += 1;
                            parameter2 = trackData.ElementAt(i);
                            midiEvent = new PitchBendEvent(deltaTime, midiChannel, parameter1, parameter2);
                            lastMidiChannel = midiChannel;
                            break;
                        // Might be a Control Change Messages LSB
                        default:
                            midiEvent = new ControllerEvent(deltaTime, lastMidiChannel, eventTypeValue, parameter1);
                            break;
                    }

                    i += 1;
                }
                // Meta Events
                else if (eventTypeValue == 0xFF)
                {
                    i += 1;
                    var metaEventType = trackData.ElementAt(i);
                    i += 1;
                    var metaEventLength = trackData.ElementAt(i);
                    i += 1;
                    var metaEventData = Enumerable.Range(i, metaEventLength).Select(trackData.ElementAt).ToArray();

                    switch (metaEventType)
                    {
                        case 0x00:
                            midiEvent = new SequenceNumberEvent(BitConverter.ToUInt16(metaEventData.Reverse().ToArray(), 0));
                            break;
                        case 0x01:
                            midiEvent = new TextEvent(deltaTime, StringEncoder.GetString(metaEventData));
                            break;
                        case 0x02:
                            midiEvent = new CopyrightNoticeEvent(StringEncoder.GetString(metaEventData));
                            break;
                        case 0x03:
                            midiEvent = new SequenceOrTrackNameEvent(StringEncoder.GetString(metaEventData));
                            break;
                        case 0x04:
                            midiEvent = new InstrumentNameEvent(deltaTime, StringEncoder.GetString(metaEventData));
                            break;
                        case 0x05:
                            midiEvent = new LyricsEvent(deltaTime, StringEncoder.GetString(metaEventData));
                            break;
                        case 0x06:
                            midiEvent = new MarkerEvent(deltaTime, StringEncoder.GetString(metaEventData));
                            break;
                        case 0x07:
                            midiEvent = new CuePointEvent(deltaTime, StringEncoder.GetString(metaEventData));
                            break;
                        case 0x20:
                            midiEvent = new MIDIChannelPrefixEvent(deltaTime, metaEventData[0]);
                            break;
                        case 0x2F:
                            midiEvent = new EndOfTrackEvent(deltaTime);
                            break;
                        case 0x51:
                            var tempo =
                                (metaEventData[2] & 0x0F) +
                                ((metaEventData[2] & 0xF0) * 16) +
                                ((metaEventData[1] & 0x0F) * 256) +
                                ((metaEventData[1] & 0xF0) * 4096) +
                                ((metaEventData[0] & 0x0F) * 65536) +
                                ((metaEventData[0] & 0xF0) * 1048576);
                            midiEvent = new SetTempoEvent(deltaTime, tempo);
                            break;
                        case 0x54:
                            midiEvent = new SMPTEOffsetEvent(deltaTime, metaEventData[0], metaEventData[1], metaEventData[2], metaEventData[3], metaEventData[4]);
                            break;
                        case 0x58:
                            midiEvent = new TimeSignatureEvent(deltaTime, metaEventData[0], metaEventData[1], metaEventData[2], metaEventData[3]);
                            break;
                        case 0x59:
                            midiEvent = new KeySignatureEvent(deltaTime, metaEventData[0], metaEventData[1]);
                            break;
                        case 0x7F:
                            midiEvent = new SequencerSpecificEvent(deltaTime, metaEventData);
                            break;
                    }

                    i += metaEventLength;
                }
                // System Exclusive Events
                else if (eventTypeValue == 0xF0 || eventTypeValue == 0xF7)
                {
                    var lengthTemp = new List<byte>();
                    do
                    {
                        i += 1;
                        lengthTemp.Add(trackData.ElementAt(i));
                    } while (trackData.ElementAt(i) > 0x7F);

                    var eventLength = VariableLengthUtil.decode_to_int(lengthTemp);

                    i += 1;

                    var eventData = Enumerable.Range(i, eventLength).Select(trackData.ElementAt);

                    midiEvent = new SysexEvent(deltaTime, eventTypeValue, eventData);

                    i += eventLength;
                }
            }

            return midiEvent != null
                ? new Tuple<Option<MidiEvent>, int, byte>(new Some<MidiEvent>(midiEvent), i - startIndex, lastMidiChannel)
                : new Tuple<Option<MidiEvent>, int, byte>(new None<MidiEvent>(), i - startIndex, lastMidiChannel);
        }
    }
}