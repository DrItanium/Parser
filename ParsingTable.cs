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
using Libraries.Extensions;
using Libraries.Starlight;

namespace Libraries.Parsing
{
	public static partial class LR1ParsingTableEntryActionExtensions
	{
		public static TableCellAction Parse(string input)
		{
			if(input.StartsWith("err"))
				return TableCellAction.Error;
			else if(input.StartsWith("acc"))
				return TableCellAction.Accept;
			else if(input[0] == 's')
				return TableCellAction.Shift;
			else if(input[0] == 'r')
				return TableCellAction.Reduce;
			else
				throw new Exception("Invalid input given");
		}
		public static bool TryParse(string input, out TableCellAction action)
		{
			try
			{
				action = Parse(input);
				return true;
			}
			catch(Exception)
			{
				action = TableCellAction.Error;
				return false;
			}
		}
	}	
	public class LR1ParsingTableCell
	{
		public static readonly LR1ParsingTableCell DEFAULT_ERROR = 
			new LR1ParsingTableCell(TableCellAction.Error);
		public int TargetState { get; set; }
		public TableCellAction Action { get; set; } 
		public static LR1ParsingTableCell Parse(string input)
		{
			var result = LR1ParsingTableEntryActionExtensions.Parse(input);
			switch(result)
			{
				case TableCellAction.Shift:
				case TableCellAction.Reduce:
					return new LR1ParsingTableCell(result, int.Parse(input.Substring(1)));
				default:
					return new LR1ParsingTableCell(result);
			}

		}
		public LR1ParsingTableCell(string input)
		{
			Action = LR1ParsingTableEntryActionExtensions.Parse(input);
			switch(Action)
			{
				case TableCellAction.Shift:
				case TableCellAction.Reduce:
					TargetState = int.Parse(input.Substring(1));
					break;
				default:
					TargetState = 0;
					break;

			}
		}
		public LR1ParsingTableCell() { }
		public LR1ParsingTableCell(TableCellAction action)
			: this(action, 0)
		{

		}
		public LR1ParsingTableCell(TableCellAction action,
				int targetState) 
		{
			Action = action;
			TargetState = targetState;
		}
		public override bool Equals(object other)
		{
			LR1ParsingTableCell cell0 = (LR1ParsingTableCell)other;
			return cell0.Action.Equals(Action) && TargetState == cell0.TargetState;
		}
		public override int GetHashCode()
		{
			return Action.GetHashCode() + TargetState.GetHashCode();
		}


		public override string ToString()
		{
			string fmt = (TargetState == -1) ? "{0}" : "{0}{1}";
			return string.Format(fmt, TranslateAction(Action), TargetState);
		}
		private string TranslateAction(TableCellAction act)
		{
			switch(act)
			{
				case TableCellAction.Shift:
					return "s";
				case TableCellAction.Reduce:
					return "r";
				case TableCellAction.Accept:
					return "acc";
				case TableCellAction.Error:
					return "err";
				default:
					return "";
			}
		}
		public static explicit operator LR1ParsingTableCell(string input)
		{
			return LR1ParsingTableCell.Parse(input);
		}
	}
	//TODO: Rewrite this to use a Dictionary<string, List<T>> instead
	//of the current List<Dictionary<string, T>>
	public abstract class GenericTable<T> : List<Dictionary<string, T>>
	{
		public T this[int i, string a]
		{
			get
			{
				return this[i][a];
			}
		}
		public void Add(int state)
		{
			if(state >= Count)
				Add(new Dictionary<string, T>());
		}
		public void AddRange(int state, IEnumerable<string> symbols)
		{
			foreach(var v in symbols)
				Add(state, v);
		}
		protected virtual T GetDefaultValue()
		{
			return default(T);
		}
		public void Add(int state, string symbol)
		{
			Add(state, symbol, GetDefaultValue());	
		}
		public void Add(int state, string symbol, T cell)
		{
			Add(state);
			var st = this[state];
			st[symbol] = cell;
		}
		public void CleanUp()
		{
			int oldCount = Count;
			var rev = GetSimplistics();
			Clear();
			Dictionary<string, T>[] collec = new Dictionary<string, T>[oldCount];
			foreach(var v in rev)
			{
				var dict = v.Key;
				var list = v.Value;
				foreach(var l in list)
					collec[l] = dict;
			}
			AddRange(collec);
			Console.WriteLine("rev.Count = {0}", rev.Count);
		}
		public Dictionary<Dictionary<string, T>, List<int>> GetSimplistics()
		{
			Dictionary<Dictionary<string, T>, List<int>> rev = 
				new Dictionary<Dictionary<string, T>, List<int>>(
						new DictionaryEqualityComparison());
			for(int i = 0; i < Count; i++)
			{
				var dict = this[i];
				if(rev.ContainsKey(dict))
					rev[dict].Add(i);
				else
					rev[dict] = new List<int> { i };
			}
			return rev;
		}
		public abstract string MakeQuine(Dictionary<string,string> symbolTable);
		class DictionaryEqualityComparison : EqualityComparer<Dictionary<string, T>>
		{
			public override bool Equals(Dictionary<string, T> a, Dictionary<string, T> b)
			{
				if(a.Count == b.Count)
				{
					var total = from x in a
						join y in b on x.Key equals y.Key
						where x.Value.Equals(y.Value)
						select x;
					return total.Count() == a.Count;
				}
				else
					return false;
			}	
			public override int GetHashCode(Dictionary<string, T> dict)
			{
				long value = 0L;
				foreach(var v in dict)
					value += v.Key.GetHashCode() + v.Value.GetHashCode();
				return (int)value;
			}
		}
	}
	public class LR1ParsingTable : GenericTable<LR1ParsingTableCell>
	{
		protected override LR1ParsingTableCell GetDefaultValue()
		{
			return LR1ParsingTableCell.DEFAULT_ERROR;
		}
		public override string MakeQuine(Dictionary<string,string> symbolTable)
		{
			int size = Count;
			var simp = GetSimplistics();
			Dictionary<string, string> cellNames = new Dictionary<string, string>();
			Dictionary<Dictionary<string, LR1ParsingTableCell>, string> binding =
				new Dictionary<Dictionary<string, LR1ParsingTableCell>, string>(); 

			StringBuilder cellCreation = new StringBuilder();
			StringBuilder initCells = new StringBuilder();
			initCells.AppendLine("static void InitCells()");
			initCells.AppendLine("{");
			foreach(var r in this)
			{
				foreach(var v in r)
				{
					string oName = v.Value.ToString();
					string name = string.Format("cell_{0}", oName);
					if(!cellNames.ContainsKey(v.Value.ToString()))
					{
					cellNames[v.Value.ToString()] = name;
					cellCreation.AppendFormat("static LR1ParsingTableCell {0};", name);
					initCells.AppendFormat("{0} = new LR1ParsingTableCell(\"{1}\");\n", name, oName);
					}
				}
			}
			initCells.AppendLine("}");
			//TODO: Break up each table into a separte function
			//      to get around the "method too complex" error
			StringBuilder methodMaker = new StringBuilder();
			StringBuilder tableCreation = new StringBuilder();
			StringBuilder initGroupsFunction = new StringBuilder();
			initGroupsFunction.AppendLine("static void InitGroups()");
			initGroupsFunction.AppendLine("{");
			int index = 0;
			foreach(var v in simp)
			{
				var dict = v.Key;
				string name = string.Format("tab{0}", index);
				binding[dict] = name; 
				tableCreation.AppendFormat("static Dictionary<string, LR1ParsingTableCell> {0};\n ", name);
				initGroupsFunction.AppendLine(
						MakeMethod(name, 
							methodMaker,
							symbolTable,
							cellNames,
							dict));
				index++;
			}	
			initGroupsFunction.AppendLine("}");
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("static Tables()");
			sb.AppendLine("{");
			sb.AppendLine("InitCells();");
			sb.AppendLine("InitGroups();");
			sb.AppendLine("InitParseTable();");
			sb.AppendLine("}");
			sb.Append(methodMaker.ToString());
			sb.Append(initGroupsFunction.ToString());
			sb.Append(initCells.ToString());
			sb.Append(cellCreation.ToString());
			sb.Append(tableCreation.ToString());
			sb.Append("public static LR1ParsingTable parseTable;\n");
			sb.AppendLine("static void InitParseTable() {\n");
			sb.Append("parseTable = new LR1ParsingTable \n{\n");

			string[] result = new string[size];

			//for(int i = 0; i < simp.Count; i++)
			foreach(var s in simp)
			{
				string name = binding[s.Key];
				foreach(var v in s.Value)
					result[v] = name;
			}
			for(int j = 0; j < result.Length; j++)
			{
				sb.AppendFormat("{0},\n",result[j]);
			}
			sb.Append("\n};\n");
			sb.Append("}\n");
			return sb.ToString();
		}
		private string MakeMethod(
				string name,
			 	StringBuilder fn,
			 	Dictionary<string,string> symbolTable,
				Dictionary<string,string> cellNames,
				Dictionary<string, LR1ParsingTableCell> dict)
		{
				fn.AppendFormat("static void init{0}() ",  name);
				fn.Append("{ \n");
				fn.AppendFormat("{0} = new Dictionary<string, LR1ParsingTableCell>", name);
				fn.Append("{\n");
				foreach(var q in dict)
				{
					string _name = symbolTable[q.Key];
					//Console.WriteLine("Looking for {0}", q.Value.ToString());
					string val = cellNames[q.Value.ToString()];
					fn.AppendFormat("{0} {1}, {2} {3},\n", "{", _name, val, "}");
				}
				fn.Append("};\n}\n");
				return string.Format("init{0}();", name);
		}
	}
	
	public class LR1GotoTable : GenericTable<int>
	{
		protected override int GetDefaultValue()
		{
			return 0;
		}
		public override string MakeQuine(Dictionary<string,string> symbolTable)
		{
			int size = Count;
			var simp = GetSimplistics();
			Dictionary<string, string> cellNames = new Dictionary<string, string>();
			Dictionary<Dictionary<string, int>, string> binding =
				new Dictionary<Dictionary<string, int>, string>(); 

			StringBuilder cellCreation = new StringBuilder();
			foreach(var r in this)
			{
				foreach(var v in r)
				{
					string oName = (v.Value == -1) ? "NegativeOne" : v.Value.ToString();
					string name = string.Format("id_{0}", oName);
					if(!cellNames.ContainsKey(oName))
					{
					cellNames[oName] = name;
					cellCreation.AppendFormat("public static readonly int {0} = {1};\n", 
							name, oName);
					}
				}
			}
			StringBuilder tableCreation = new StringBuilder();
			int index = 0;
			foreach(var v in simp)
			{
				var dict = v.Key;
				string name = string.Format("gtab{0}", index);
				binding[dict] = name; 
				tableCreation.AppendFormat("public static readonly Dictionary<string, int> {0} = ", name);
				tableCreation.AppendLine("new Dictionary<string, int> {");
				foreach(var q in dict)
				{
					string sName = string.Format("{0}", q.Key);
					if(symbolTable.ContainsKey(q.Key))
						sName = symbolTable[q.Key];
					else
					{
						symbolTable[q.Key] = q.Key;
						cellCreation.AppendFormat("public static readonly string {0} = \"{0}\";\n", q.Key);
					}
					string sValue = cellNames[q.Value.ToString()];
					tableCreation.AppendFormat("{0} {1}, {2} {3},\n", "{", 
							sName, sValue, "}");
				}
				tableCreation.Append("};\n");
				index++;
			}	
			StringBuilder sb = new StringBuilder();
			sb.Append(cellCreation.ToString());
			sb.Append(tableCreation.ToString());
			sb.Append("public static LR1GotoTable gotoTable = new LR1GotoTable\n{\n");
			
			string[] result = new string[size];

			//for(int i = 0; i < simp.Count; i++)
			foreach(var s in simp)
			{
				string name = binding[s.Key];
				foreach(var v in 	s.Value)
					result[v] = name;
			}
			for(int j = 0; j < result.Length; j++)
			{
					sb.AppendFormat("{0},\n",result[j]);
			}
			sb.Append("\n};");
			return sb.ToString();
		}
	}

}
