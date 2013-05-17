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
using BitConverter = System.BitConverter;

namespace Midi
{
	public class HeaderChunk : Chunk
	{
		public readonly int format_type;
		public readonly int number_of_tracks;
		public readonly int time_division;

		public HeaderChunk (string chunk_ID, int chunk_size, byte[] header_data) : base(chunk_ID, chunk_size)
		{
			this.format_type = BitConverter.ToInt16 (header_data.Take (2).Reverse ().ToArray<byte> (), 0);
			this.number_of_tracks = BitConverter.ToInt16 (header_data.Skip (2).Take (2).Reverse ().ToArray<byte> (), 0);
			this.time_division = BitConverter.ToInt16 (header_data.Skip (4).Take (2).Reverse ().ToArray<byte> (), 0);
		}

		override public string ToString ()
		{
			return "HeaderChunk(" + base.ToString () + ", format_type: " + format_type + ", number_of_tracks: " + number_of_tracks + ", time_division: " + time_division + ")";
		}
	}
}

