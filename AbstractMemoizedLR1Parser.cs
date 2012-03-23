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
			protected Token<string> baseToken;	
			protected Stack<object> stateStack;
			protected int size;
			protected LookaheadRule initial;
			protected Dictionary<AdvanceableProduction,AdvanceableProduction> advancementDict;
			protected Dictionary<string, AdvanceableRule> skipList;
			protected Dictionary<string, IEnumerable<string>> firstCache;
			protected Dictionary<Rule, AdvanceableRule> initialListing;
			protected Dictionary<Production, AdvanceableProduction> advProds;
			protected Dictionary<AdvanceableRule, Dictionary<string, LookaheadRule>> memo;
			protected Dictionary<AdvanceableProduction, AdvanceableRule> terminates;
			protected Dictionary<List<LookaheadRule>, int> indexList = new Dictionary<List<LookaheadRule>, int>(new ListOfListsLookaheadComparer());
			protected Dictionary<string, Dictionary<string, Dictionary<AdvanceableProduction, LookaheadRule>>> singles;
			public int Size { get { return size; } }
			protected AbstractMemoizedLR1Parser(AbstractGrammar<R,Encoding> g,
					string terminateSymbol, SemanticRule r,
					bool suppressMessages, bool setupRequired) 
				: base(g, terminateSymbol, r, suppressMessages, setupRequired)
			{
			}

			protected override void SetupExtraParserElements()
			{
				terminates = new Dictionary<AdvanceableProduction, AdvanceableRule>();
				memo = new Dictionary<AdvanceableRule, Dictionary<string, LookaheadRule>>();
				singles = new Dictionary<string, Dictionary<string, Dictionary<AdvanceableProduction, LookaheadRule>>>();
				baseToken = new Token<string>(TerminateSymbol, TerminateSymbol, TerminateSymbol.Length);
				stateStack = new Stack<object>();
				initial = new LookaheadRule(TerminateSymbol, TargetGrammar[0]);
				cPrime = new List<List<LookaheadRule>>();
				advProds = new Dictionary<Production, AdvanceableProduction>();
				skipList = new Dictionary<string, AdvanceableRule>();
				firstCache = new Dictionary<string, IEnumerable<string>>();
				advancementDict = new Dictionary<AdvanceableProduction, AdvanceableProduction>();
				initialListing = new Dictionary<Rule, AdvanceableRule>();
				//	actionTable = new LR1ParsingTable();
				//	gotoTable = new LR1GotoTable();

				PopulateAdvProds();
				PopulateInitialListings();
				PopulateSkipList();
				PopulateFirstCache();
				PopulateNextCache();
				PopulateMemoCache();
				PopulateSinglesCache();
			}

			protected override void PreTableConstruction()
			{
				PopulateCPrimeIndex();
				size = cPrime.Count;
			}
			protected override void PostTableConstruction() { }
			protected virtual void PopulateCPrimeIndex()
			{
				for(int i = 0; i < cPrime.Count; i++)
				{
					var curr = cPrime[i];
					indexList[curr] = i;
				}
			}
			protected virtual void PopulateSinglesCache()
			{
				var symbs = TargetGrammar.TerminalSymbols.Concat(new string[] {TerminateSymbol }).ToArray();
				for(int i = 0; i < TargetGrammar.Count; i++)
				{
					var v = TargetGrammar[i];
					singles[v.Name] = new Dictionary<string, Dictionary<AdvanceableProduction, LookaheadRule>>();
					var ii = initialListing[v];
					for(int j = 0; j < ii.Count; j++)
					{
						var p = ii[j];
						for(int k = 0; k < symbs.Length; k++)
							//foreach(var symb in TargetGrammar.TerminalSymbols.Concat(new string[] { TerminateSymbol } ))
						{
							var symb = symbs[k];
							if(!singles[v.Name].ContainsKey(symb))
								singles[v.Name][symb] = new Dictionary<AdvanceableProduction, LookaheadRule>();
							var iter = p;
							while(iter.HasNext)
							{
								var tmp = advancementDict[iter];
								LookaheadRule lr = new LookaheadRule();
								lr.Repurpose(v.Name);
								lr.LookaheadSymbol = symb;
								lr.Add(tmp);
								singles[v.Name][symb][iter] = lr;
								iter = tmp;
							}
							LookaheadRule lr2 = new LookaheadRule();
							lr2.Repurpose(v.Name);
							lr2.LookaheadSymbol = symb;
							lr2.Add(iter);
							singles[v.Name][symb][iter] = lr2;
						}
					}
				}
			}
			protected virtual void PopulateMemoCache()
			{
				foreach(var v in initialListing)
				{
					var dict = new Dictionary<string, LookaheadRule>();
					dict[TerminateSymbol] = new LookaheadRule(TerminateSymbol, v.Value);
					foreach(var q in TargetGrammar.TerminalSymbols)
					{
						dict[q] = new LookaheadRule(q, v.Value);
					}
					memo[v.Value] = dict;
				}
			}
			protected virtual void PopulateAdvProds()
			{
				for(int i = 0; i < TargetGrammar.Count; i++)
				{
					Rule r = TargetGrammar[i];
					for(int j = 0; j < r.Count; j++)
						if(!advProds.ContainsKey(r[j]))
							advProds[r[j]] = r[j].MakeAdvanceable();
				}
			}
			protected virtual void PopulateInitialListings()
			{
				for(int i = 0; i < TargetGrammar.Count; i++)
				{
					Rule r = TargetGrammar[i];
					if(!initialListing.ContainsKey(r))
					{
						var tmp = new AdvanceableRule(r.Name);
						for(int j = 0; j < r.Count; j++)
							tmp.Add(advProds[r[j]]);
						initialListing.Add(r, tmp);
					}
				}
			}
			protected virtual void PopulateNextCache()
			{
				for(int i = 0; i < TargetGrammar.Count; i++)
				{
					var r = initialListing[TargetGrammar[i]];
					for(int j =0; j < r.Count; j++)
					{
						AdvanceableProduction prod = r[j]; 
						while(prod.HasNext)
						{
							var tmp = prod.FunctionalNext();
							advancementDict[prod] = tmp;
							prod = tmp;
						}
						advancementDict[prod] = prod;
						terminates[prod] = new AdvanceableRule(r.Name);
						terminates[prod].Add(prod);
					}
				}
			}
			protected virtual void PopulateFirstCache()
			{
				LookaheadRule temp = new LookaheadRule();
				foreach(var v in skipList)
				{
					temp.Repurpose(v.Key);
					temp.Repurpose(v.Value);
					First(temp, 0);
				}
			}
			protected virtual void PopulateSkipList()
			{
				for(int i = 0; i < TargetGrammar.Count; i++)
				{
					Rule r = TargetGrammar[i];
					AdvanceableRule rul = new AdvanceableRule(r.Name);
					for(int j = 0; j < r.Count; j++)
						if(!r[j][0].Equals(r.Name))
							rul.Add(advProds[r[j]]);
					skipList.Add(r.Name, rul);
				}	
			}

			private List<LookaheadRule> currentRules = new List<LookaheadRule>();
			private List<AdvanceableRule> nullTerminators = new List<AdvanceableRule>();
			public override IEnumerable<LookaheadRule> Closure(
					IEnumerable<LookaheadRule> rules, 
					string terminateSymbol)
			{
				//Closure is more refined. 
				//If [A → α • B β, a] belongs to the set of items, 
				//and B → γ is a production of the grammar, 
				//then we add the item [B → • γ, b] for all b in FIRST(β a).
				HashSet<LookaheadRule> rr = new HashSet<LookaheadRule>(rules); 
				int size = currentRules.Count;
				do
				{
					size = rr.Count;
					//get rid of the 
					foreach(var rule in rr)
					{
						var a = rule.LookaheadSymbol;
						var container = singles[rule.Name][rule.LookaheadSymbol];
						for(int j = 0; j < rule.Count; j++)
						{
							var p = rule[j];
							string symbol = p.Current;
							if(!p.HasNext)
								nullTerminators.Add(terminates[p]);
							else 
							{
								if(TargetGrammar.Exists(symbol))
								{
									AdvanceableProduction betaProd = advancementDict[p]; //create beta
									var target = initialListing[TargetGrammar[symbol]]; //grab the B-Production
									var actualTarget = memo[target];
									if(!betaProd.HasNext && a.Equals(terminateSymbol)) 
										currentRules.Add(actualTarget[terminateSymbol]);
									else
									{
										if(betaProd.HasNext)
										{
											var next = container[p]; 

											var ff = First(next, betaProd.Position);
											foreach(var v in ff)
												currentRules.Add(actualTarget[v]);
										}
										currentRules.Add(actualTarget[a]);
									}
								}
							}
						}
					}
					rr.UnionWith(currentRules);
					currentRules.Clear();
				}while(rr.Count != size);
				for(int n = 0; n < nullTerminators.Count; n++)
				{
					var v = nullTerminators[n];
					string name = v.Name;
					foreach(var s in rr)
					{
						if(s.Name.Equals(name))
							foreach(var qq in v)
								if(!s.Contains(qq))
									s.Add(qq);
					}
				}
				if(nullTerminators.Count > 0)
					nullTerminators.Clear();
				if(currentRules.Count > 0)
					currentRules.Clear();
				return rr;
			}
			private long ComputeFakeHash(IEnumerable<LookaheadRule> i)
			{
				long total = 0L;
				foreach(var v in i)
					total += v.GetHashCode();
				return total;

			}
			private Dictionary<long, IEnumerable<LookaheadRule>> closureCache = new Dictionary<long, IEnumerable<LookaheadRule>>();
			private HashSet<LookaheadRule> gotoStorage = new HashSet<LookaheadRule>();
			public override IEnumerable<LookaheadRule> ComputeGoto(IEnumerable<LookaheadRule> i, string x)
			{
				gotoStorage.Clear();
				foreach(var r in i)
				{
					for(int j = 0; j < r.Count; j++)
					{
						var p = r[j];
						if(p.Current == null)
							continue;	
						else if(p.Current.Equals(x))
							gotoStorage.Add(singles[r.Name][r.LookaheadSymbol][p]);
					}
				}
				//get the precomputed value.
				long hash = ComputeFakeHash(gotoStorage);
				if(closureCache.ContainsKey(hash)) //if it already exists then return the cached value
					return closureCache[hash];
				else
				{
					//otherwise compute it and then cache it.
					var result = Closure(gotoStorage);
					closureCache.Add(hash, result); 
					return result;
				}
			}
			private HashSet<IEnumerable<LookaheadRule>> lr = new HashSet<IEnumerable<LookaheadRule>>(SetOfSetsLookaheadComparer.DefaultComparer);
			public override IEnumerable<IEnumerable<LookaheadRule>> Items()
			{
				if(lr.Count > 0)
					lr.Clear();
				var set = new HashSet<IEnumerable<LookaheadRule>>(SetOfSetsLookaheadComparer.DefaultComparer);
				set.Add(Closure(new LookaheadRule[] { initial }));
				int size = -1;
				string x = null;
				HashSet<LookaheadRule> result = null;
				var symbolTable = TargetGrammar.SymbolTable.ToArray();
				do
				{
					size = set.Count;
					foreach(var i in set)
					{
						for(int j = 0; j < symbolTable.Length; j++)
						{
							x = symbolTable[j];
							result = (HashSet<LookaheadRule>)ComputeGoto(i, x);
							if(result.Count > 0 && !lr.Contains(result))
								lr.Add(result);
						}	
					}
					set.UnionWith(lr);
					lr.Clear();
				} while(set.Count != size);
				return set;
			}



			public override IEnumerable<string> First(LookaheadRule rule, int lookahead)
			{
				HashSet<string> s = new HashSet<string>();
				LookaheadRule lr = null;
				//LookaheadRule lr = new LookaheadRule();
				for(int i = 0; i < rule.Count; i++)
				{
					var v = rule[i];
					if(v.Count == 0) //empty rule
					{
						s.Add("<empty>");
						continue;
					}
					for(int j = lookahead; j < v.Count; j++)
					{
						var q = v[j];
						//this is some ugly fucking code.
						//my better idea is to store a dictionary that 
						//ties a ruleIndex to a list of valid productions
						//-------
						//More succintly it lists the values that do not
						//start with the current rule symbol. It is a far smaller
						//list and doesn't chew through tons of space. In fact it 
						//doesn't have to be a int but a string to make things more
						//convienient since we are referring to a rule
						//------
						//Plus it is also precomputed at Parser creation time because it is not
						//going to change.
						if(TargetGrammar.Exists(q))
						{
							if(firstCache.ContainsKey(q))
							{
								s.UnionWith(firstCache[q]);	
								break;
							}
							else
							{
								if(lr == null)
									lr = new LookaheadRule();
								if(q.Equals(rule.Name)) //prevent an infinite loop
									lr.Repurpose(rule.LookaheadSymbol, skipList[rule.Name]);
								else
									lr.Repurpose(rule.LookaheadSymbol, TargetGrammar[q]);
								IEnumerable<string> result = First(lr, 0);
								s.UnionWith(result);
								if(!firstCache.ContainsKey(q))
									firstCache.Add(q, result);
								if(result.Contains("<empty>"))
									continue;
								else
									break;
							}
						}
						else //assume non-terminal
						{
							s.Add(q);
							break;
						}
					}
				}	
				return s;
			}

			protected int GetIndex(List<LookaheadRule> rules)
			{
				if(indexList.ContainsKey(rules))
					return indexList[rules];
				else
				{
					var cmp = ListOfListsLookaheadComparer.DefaultComparer;
					for(int i = 0; i < cPrime.Count; i++)
					{
						if(cmp.Equals(rules, cPrime[i]))
						{
							indexList[rules] = i;
							return i;
						}
					}
					throw new ArgumentException("Given list of rules doesn't correspond to anything in C-prime");
				}
			}

			protected override void MakeGotoTable()
			{
				List<LookaheadRule> g = new List<LookaheadRule>();
				int state = 0;
				for(int j = 0; j < cPrime.Count; j++)
				{
					var Ii = cPrime[j];
					for(int k = 0; k < Ii.Count; k++)
					{
						var rule = Ii[k];
						for(int z = 0; z < rule.Count; z++)
						{
							var prod = rule[z];
							var curr = prod.Current;
							if(curr == null)
								continue;
							if(TargetGrammar.Exists(curr))
							{
								g.AddRange(ComputeGoto(Ii, curr));
								AddToGotoTable((int)GetIndex(g), curr, state);
								g.Clear();
							}
						}
					}
					state++;
				}
			}
			protected abstract void AddToGotoTable(int index, string current, int state);




			//BEGIN CLASSES
		}
	public class ListOfListsLookaheadComparer : EqualityComparer<List<LookaheadRule>>
	{
		public static readonly ListOfListsLookaheadComparer DefaultComparer = new ListOfListsLookaheadComparer();
		private HashSet<LookaheadRule> hs = new HashSet<LookaheadRule>();
		public override bool Equals(List<LookaheadRule> a, List<LookaheadRule> b)
		{
			bool result = (a.Count == b.Count);
			if(result)
			{
				hs.Clear();
				hs.UnionWith(a);
				hs.UnionWith(b);
				result = result && (hs.Count == a.Count);
			}
			return result;
		}
		public override int GetHashCode(List<LookaheadRule> r)
		{
			long hashCode = 0L;
			for(int i =0 ; i < r.Count; i++)
				hashCode += r[i].GetHashCode();
			return (int)hashCode;
		}
	}
	public class SetOfSetsLookaheadComparer : EqualityComparer<IEnumerable<LookaheadRule>>
	{
		public static readonly SetOfSetsLookaheadComparer DefaultComparer = new SetOfSetsLookaheadComparer();
		private HashSet<LookaheadRule> hs = new HashSet<LookaheadRule>(); //potential threading issue
		public override bool Equals(IEnumerable<LookaheadRule> a, IEnumerable<LookaheadRule> b)
		{
			int aSize = a.Count();
			int bSize = b.Count();
			bool result = aSize == bSize;
			if(result)
			{
				hs.Clear();
				hs.UnionWith(a);
				hs.UnionWith(b);
				result = result && hs.Count == aSize;
			}
			return result;
		}
		public override int GetHashCode(IEnumerable<LookaheadRule> r)
		{
			int sum = 0;
			foreach(var v in r)
				sum += v.GetHashCode();
			return sum;
		}
	}
	public class CoreCheckLookaheadComparer : EqualityComparer<List<LookaheadRule>>
	{
		public override bool Equals(List<LookaheadRule> a, List<LookaheadRule> b)
		{
			if(a.Count == b.Count)
			{
				var stringsA = (from x in a
						select x.LookaheadSymbol).ToArray();
				var stringsB = (from x in b
						select x.LookaheadSymbol).ToArray();
				for(int i = 0; i < b.Count; i++)
				{
					a[i].LookaheadSymbol = string.Empty;
					b[i].LookaheadSymbol = string.Empty;
				}
				for(int i = 0; i < a.Count; i++)
				{
					if(!b.Contains(a[i]))
						return false;
					//lets modify the lookahead symbols temporarily
				}
				for(int i = 0; i < a.Count; i++)
				{
					a[i].LookaheadSymbol = stringsA[i];
					b[i].LookaheadSymbol = stringsB[i];
				}
				return true;
			}
			else
				return false;
		}
		public override int GetHashCode(List<LookaheadRule> a)
		{
			long l = 0L;
			for(int i =0; i < a.Count; i++)
				l += (a[i].GetHashCode() - a[i].LookaheadSymbol.GetHashCode());
			return (int)l;
		}
	}
}
