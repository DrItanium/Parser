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
using System.Runtime;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.IO;
using System.Linq;
using Libraries.LexicalAnalysis;
using Libraries.Starlight;

namespace Libraries.Parsing
{
	public abstract class Parser<R,Encoding>
		where Encoding : struct
		where R : Rule
	{
		private AbstractGrammar<R,Encoding> g;
		public AbstractGrammar<R,Encoding> TargetGrammar { get { return g; } set { g = value; } }
		private string terminateSymbol;
		public string TerminateSymbol { get { return terminateSymbol; } set { terminateSymbol = value; } }
		public bool SupressMessages { get; set; }
		public bool SetupRequired { get; set; }
		public Parser(AbstractGrammar<R,Encoding> g, string terminateSymbol, bool supressMessages, bool setupRequired)
		{
			this.g = g;
			this.terminateSymbol = terminateSymbol;
			SupressMessages = supressMessages;
			SetupRequired = setupRequired;
			if(SetupRequired)
			  SetupParser();
		}
		public Parser(AbstractGrammar<R,Encoding> g, string terminateSymbol, bool supressMessages)
			: this(g, terminateSymbol, supressMessages, true)
		{

		}
		public Parser(AbstractGrammar<R,Encoding> g, string terminateSymbol)
			: this(g, terminateSymbol, false)
		{

		}
		protected abstract void SetupParser();
		public virtual object Parse(IEnumerable<Token<string>> tokens)
		{
			return Parse(tokens, string.Empty);
		}
		public abstract object Parse(IEnumerable<Token<string>> tokens, string input);
		public abstract bool IsValid(IEnumerable<string> input);
		
	}
}
