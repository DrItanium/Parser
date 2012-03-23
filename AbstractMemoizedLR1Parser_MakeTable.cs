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
	public abstract partial class AbstractMemoizedLR1Parser<R,Encoding> : AbstractLR1Parser<R,LookaheadRule,Encoding>
		where R : Rule
		where Encoding : struct
		{
			//public abstract void SetState(string target, TableCellAction action, Encoding enc);
			protected abstract void MakeTable_SetAcceptState(int state, string symbol);
			protected abstract TableCellAction MakeTable_ExtractAction(int state, string symbol);
			protected abstract bool MakeTable_Reduce_Condition(Encoding revision, int state, string lookaheadSymbol);
			protected abstract void MakeTable_SetReduceState(int state, string symbol, Encoding rev);
			protected abstract bool MakeTable_Shift_Condition(int targetValue, int state, string lookaheadSymbol);
			protected abstract void MakeTable_SetShiftState(int state, string symbol, int target);

			protected override void MakeTable()
			{
				List<LookaheadRule> Ij = new List<LookaheadRule>();
				int state = 0;
				for(int k = 0; k < cPrime.Count; k++)
				{
					var Ii = cPrime[k];
					for(int q = 0; q < Ii.Count; q++)
					{
						var rule = Ii[q];
						for(int b = 0; b < rule.Count; b++)
						{	
							var prod = rule[b];
							if(!prod.HasNext)
							{
								if(prod.Target.Equals(initial[0].Target))
									MakeTable_SetAcceptState(state, TerminateSymbol);
								else
								{
									Encoding rev = TargetGrammar.ReverseLookup(rule.Name, prod);
									TableCellAction action = MakeTable_ExtractAction(state, rule.LookaheadSymbol);
									switch(action)
									{
										case TableCellAction.Accept:
											if(!SupressMessages && MakeTable_Reduce_Condition(rev,
														state, rule.LookaheadSymbol))
												Console.WriteLine("WTF Mate, attempt to overwrite accept state");
											break;
										case TableCellAction.Reduce:
											if(!SupressMessages && 
													MakeTable_Reduce_Condition(rev, state, rule.LookaheadSymbol))
												Console.WriteLine("Reduce/Reduce Conflict detected with rule {0}", rule.Name);
											break;
										case TableCellAction.Shift:
											if(!SupressMessages && 
													MakeTable_Reduce_Condition(rev, state, rule.LookaheadSymbol))
												Console.WriteLine("Shift/Reduce Conflict detected with rule {0}", rule.Name);
											break;
										case TableCellAction.Goto:
											throw new Exception("This is an error! You shouldn't have gotten here!!!!");
										case TableCellAction.Error:
										default:
											MakeTable_SetReduceState(state, rule.LookaheadSymbol, rev);
											break;
									}
								}
							}
							else
							{
								var a = prod.Current;
								if(TargetGrammar.IsTerminalSymbol(a))
								{
									Ij.AddRange(ComputeGoto(Ii, a));
									int targetState = GetIndex(Ij);
									Ij.Clear();
									var action = MakeTable_ExtractAction(state, a);
									//var original = _st[a];
									switch(action)
									{
										case TableCellAction.Shift:
											if(!SupressMessages &&
													MakeTable_Shift_Condition(targetState,state,a))
												Console.WriteLine("Shift/Shift Conflict detected with rule {0}", rule.Name);
											break;
										case TableCellAction.Reduce:
											if(!SupressMessages &&
													MakeTable_Shift_Condition(targetState,state,a))
												Console.WriteLine("Reduce/Shift Conflict detected with rule {0}", rule.Name);
											break;
										case TableCellAction.Accept:
											if(!SupressMessages &&
													MakeTable_Shift_Condition(targetState,state,a))
												Console.WriteLine("WTF Mate, attempt to overwrite accept state");
											break;
										case TableCellAction.Goto:
											throw new Exception("This is an error! You shouldn't have gotten here!!!!");
										case TableCellAction.Error:
										default:
											MakeTable_SetShiftState(state, a, targetState);
										//	_st[a] = new LR1ParsingTableCell(
										//			LR1ParsingTableEntryAction.Shift, targetState);
											break;
									}
								}
							}
						}
					}
					state++;
				}
#if DEBUG
				Console.WriteLine("Make Table Took {0}", DateTime.Now - start);
#endif
			}
		}
}
