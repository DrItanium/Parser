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
//#define STATS
//#define WALK
//#define CLOSURE_STATS
using System;
using System.Reflection;
using System.Text;
using System.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Libraries.Starlight;
using Libraries.LexicalAnalysis;
using Libraries.Extensions;

namespace Libraries.Parsing
{
	public class LR1Parser : AbstractMemoizedLR1Parser<Rule,int>
	{
		public const string DEFAULT_TERMINATE_SYMBOL = "$";
		private LR1ParsingTable actionTable;
		private LR1GotoTable gotoTable;
		public LR1ParsingTable ActionTable { get { return actionTable; } }
		public LR1GotoTable GotoTable { get { return gotoTable; } }

		public LR1Parser(Grammar g, string terminateSymbol, SemanticRule r, bool supressMessages)
			: base(g, terminateSymbol, r, supressMessages, true) { }
		public LR1Parser(Grammar g, string terminateSymbol, SemanticRule r) : this(g, terminateSymbol, r, false) { }
		public LR1Parser(Grammar g, string terminateSymbol) : this(g, terminateSymbol, (x) => x[0]) { }
		public LR1Parser(Grammar g) : this(g, DEFAULT_TERMINATE_SYMBOL) { }
		public LR1Parser(Grammar g, string terminateSymbol, LR1ParsingTable table, LR1GotoTable gotoTable, SemanticRule r)
			: base(g, terminateSymbol, r, true, false)
		{
			this.actionTable = table;
			this.gotoTable = gotoTable;	
			this.onAccept = r;
			baseToken = new Token<string>(TerminateSymbol, TerminateSymbol, TerminateSymbol.Length);
			stateStack = new Stack<object>();
			initial = new LookaheadRule(TerminateSymbol, TargetGrammar[0]);
		}
		public LR1Parser(Grammar g, LR1ParsingTable table, LR1GotoTable gotoTable, SemanticRule r) : this(g, DEFAULT_TERMINATE_SYMBOL, table, gotoTable, r) { }
		public override string RetrieveTables(Dictionary<string, string> symbolTable)
		{
			//C# representation of these tables	
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("public static partial class Tables {");
			foreach(var v in symbolTable)
			{
				sb.AppendFormat("public static readonly string {0} = \"{1}\";\n", 
						v.Value, v.Key);
			}
			sb.Append(actionTable.MakeQuine(symbolTable));
			sb.AppendLine();
			sb.AppendLine();
			sb.Append(gotoTable.MakeQuine(symbolTable));
			sb.AppendLine();
			sb.AppendLine();
			sb.AppendLine("}");
			return sb.ToString();
		}
		protected override void SetupExtraParserElements()
		{
			base.SetupExtraParserElements();
			actionTable = new LR1ParsingTable();
			gotoTable = new LR1GotoTable();
		}
		protected override void PreTableConstruction()
		{
			var nonTerminals = TargetGrammar.NonTerminalSymbols.ToArray();
			var terminals = TargetGrammar.TerminalSymbols.ToArray();
			for(int i = 0; i < cPrime.Count; i++)
				gotoTable.AddRange(i, nonTerminals);
			for(int i = 0; i < cPrime.Count; i++)
			{
				actionTable.Add(i);
				actionTable.AddRange(i,terminals);
				actionTable.Add(i,TerminateSymbol);
			}
		}
		protected override void AddToGotoTable(int index, string current, int state)
		{
			gotoTable[state][current] = index;
		}
		protected override void MakeTable_SetAcceptState(int state, string symbol)
		{
			actionTable[state][symbol] = new LR1ParsingTableCell(TableCellAction.Accept, 0);
		}
		protected override TableCellAction MakeTable_ExtractAction(int state, string symbol)
		{
			return actionTable[state][symbol].Action;
		}
		protected override bool MakeTable_Reduce_Condition(int revision, int state, string lookaheadSymbol)
		{
			return revision != actionTable[state][lookaheadSymbol].TargetState;
		}
		protected override void MakeTable_SetReduceState(int state, string symbol, int rev)
		{
			actionTable[state][symbol] = new LR1ParsingTableCell(TableCellAction.Reduce, rev);
		}
		protected override bool MakeTable_Shift_Condition(int targetValue, int state, string lookaheadSymbol)
		{
			return targetValue != actionTable[state][lookaheadSymbol].TargetState;
		}
		protected override void MakeTable_SetShiftState(int state, string symbol, int target)
		{
			actionTable[state][symbol] = new LR1ParsingTableCell(TableCellAction.Shift, target);
		}
		public override object Parse(IEnumerable<Token<string>> tokens, string input)
		{
			return CoreParse(tokens, input, true, true);
		}
		protected object CoreParse(IEnumerable<Token<string>> tokens, string input, 
				bool printErrorMessages, bool invokeRules)
		{
			var _input = tokens.Concat( new [] { baseToken });
			stateStack.Clear();
			stateStack.Push(baseToken);
			stateStack.Push(0);
			Queue<Token<string>> _i = new Queue<Token<string>>(_input);
			var a = _i.Dequeue();
			int s = 0;
			var result = actionTable[s,a.TokenType];
			while(true)
			{
				s = (int)stateStack.Peek();
				result = actionTable[s,a.TokenType];
				switch(result.Action)
				{
					case TableCellAction.Shift:
						stateStack.Push(a);
						stateStack.Push(result.TargetState);
						a = _i.Dequeue();
						break;
					case TableCellAction.Reduce:
						var rule = TargetGrammar.LookupRule(result.TargetState);
						var firstRule = rule[0];
						int count = firstRule.Count; //not the rule's length but the rule's first production's length
						object[] elements = new object[count];	
						for(int i = 0; i < count; i++)
						{
							stateStack.Pop(); //targetState
							elements[i] = stateStack.Pop();
						}
						var element = (int)stateStack.Peek();
						var next = gotoTable[element, rule.Name];
						if(invokeRules)
						{
							var quantity = firstRule.Rule(elements.Reverse().ToArray());
							stateStack.Push(quantity);
						}
						else
						{
							stateStack.Push(new object());
						}
						stateStack.Push(next);
						break;
					case TableCellAction.Accept:
						if(!SupressMessages)
						{
							Console.WriteLine("State Stack Contents before accept");
							foreach(var v in stateStack)
								Console.WriteLine(v);
						}
						stateStack.Pop();
						if(invokeRules)
							return onAccept(new object[] { stateStack.Pop() } );
						else
							return true;
					case TableCellAction.Error:
						if(printErrorMessages)
						{
							Console.WriteLine("Error Occurred at position {0}, {1}", a.Start, a.Value);
							Console.WriteLine("{0}", input.Substring(0, a.Start + 1));
						}
						return false;

				}
			}	
		}
		private IEnumerable<Token<string>> Translate0(IEnumerable<string> input)
		{
			List<Token<string>> toks = new List<Token<string>>();
			foreach(var i in input)
				toks.Add(new Token<string>(i,i,i.Length));
			return toks;
		}
		public override bool IsValid(IEnumerable<string> input)
		{
			return (bool)CoreParse(Translate0(input), string.Empty, false, false);
		}
	}
}
