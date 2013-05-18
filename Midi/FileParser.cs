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
using TrackData = System.Collections.Generic.Dictionary<string, object>;

namespace Midi
{
	public class FileParser
	{

		public static MidiFile parse (FileStream input_file_stream)
		{
			BinaryReader input_binary_reader = new BinaryReader (input_file_stream);
			StringEncoder stringEncoder = new StringEncoder ();

			HeaderChunk header_chunk;
			{
				string header_chunk_ID = stringEncoder.GetString (input_binary_reader.ReadBytes (4));
				int header_chunk_size = BitConverter.ToInt32 (input_binary_reader.ReadBytes (4).Reverse ().ToArray<byte> (), 0);
				byte[] header_chunk_data = input_binary_reader.ReadBytes (header_chunk_size);
				header_chunk = new HeaderChunk (header_chunk_ID, header_chunk_size, header_chunk_data);
			}

			Tracks tracks =
				Enumerable.Range (0, header_chunk.number_of_tracks)
				.Select (track_number => {
				string track_chunk_ID = stringEncoder.GetString (input_binary_reader.ReadBytes (4));
				int track_chunk_size = BitConverter.ToInt32 (input_binary_reader.ReadBytes (4).Reverse ().ToArray<byte> (), 0);
				byte[] track_chunk_data = input_binary_reader.ReadBytes (track_chunk_size);

				return new TrackChunk (track_chunk_ID, track_chunk_size, track_chunk_data);
			}).ToList ();

			return new MidiFile (header_chunk, tracks);
		}
	}
}
