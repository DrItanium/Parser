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
using System.Linq;
using Libraries.Collections;

namespace Libraries.Parsing
{
	/*
		 Lets define an encoding scheme of 64-bits
		 Highest 8-bits for the action
		 56-bits left
		 Depending on that value different encoding occurs
		 For Accept nothing is parsed
		 For Error, 2^56 max error states are allowed
		 For Reduce, divided the remaining bits into two sections (each 28-bits each)
		 upper portion is for the rule number
		 lower portion is for the production number
		 For Goto, the remaining 56 bits are used to denote the state to go to
		 For Shift, the remaining 56 bits are used to denote the state to go to
	 */
	public enum TableCellAction : byte
	{
		Error = 0,
					Accept = 1,
					Reduce = 2,
					Shift = 3,
					Goto = 4,
	}

	public class EnhancedParsingTable : Dictionary<string, CompressionList<ulong>>
	{
		public EnhancedParsingTable(IEnumerable<string> titles, ulong defaultValue)
		{
			foreach(var v in titles)
				this[v] = new CompressionList<ulong>(defaultValue);

		}
		public EnhancedParsingTable(IEnumerable<string> titles)
			: this(titles, 0L)
		{

		}
	}
}
