//Copyright 2012 Joshua Scoggins. All rights reserved.
//
//Redistribution and use in source and binary forms, with or without modification, are
//permitted provided that the following conditions are met:
//
//   1. Redistributions of source code must retain the above copyright notice, this list of
//      conditions and the following disclaimer.
//
//   2. Redistributions in binary form must reproduce the above copyright notice, this list
//      of conditions and the following disclaimer in the documentation and/or other materials
//      provided with the distribution.
//
//THIS SOFTWARE IS PROVIDED BY Joshua Scoggins ``AS IS'' AND ANY EXPRESS OR IMPLIED
//WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
//FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL Joshua Scoggins OR
//CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
//CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
//SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
//ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
//NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
//ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
//The views and conclusions contained in the software and documentation are those of the
//authors and should not be interpreted as representing official policies, either expressed
//or implied, of Joshua Scoggins. 
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Libraries.Parsing
{
	//	public static class Program
	//	{
	//		public static void Main(string[] args)
	//		{
	//			CellEncoder ce = new CellEncoder();
	//			uint[] elements = new uint[] { 255, 16384, 6561 };
	//			foreach(var v in elements)
	//				Console.WriteLine(v);
	//			ulong value = ce.Encode(elements);
	//			Console.WriteLine("Encoded Value is: {0}", value);
	//			var decoded = ce.Decode(value);
	//			foreach(var v in decoded)
	//				Console.WriteLine(v);
	//		}
	//	}
	public class CellEncoder : IEncoder<uint, ulong>
	{
		public ulong Encode(IEnumerable<uint> decoding)
		{
			ulong value = 0L;
			ulong v0 = (ulong)decoding.First();
			ulong v1 = (ulong)decoding.ElementAt(1);
			ulong v2 = (ulong)decoding.ElementAt(2);
			value = (v0 << 56);
			value = value + v2;
			value = value + (v1 << 28);
			return value;
		}
		public IEnumerable<uint> Decode(ulong encoding)
		{
			yield return (uint)(encoding >> 56);
			yield return (uint)((encoding << 8) >> 36);
			yield return (uint)((encoding << 36) >> 36);
		}
	}
}
