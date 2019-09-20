using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using AGBLang;
using System.Text;

namespace AGDev.StdUtil {
	public interface GenericReader {
		void ReadBool(string name, bool value);
		GenericReader GetSubReader(string name);
		void ReadString(string name, string value);
		void Conclude();
	}
	public class FormToGAnlys {
		public StdMAnalyzer analyzer;
		public IncrGAnlysDictionary dict;
		public IncrementalGAnalyzer PickBestElement(string key) {
			var morphemes = analyzer.AnalyzeImmediate(key);
			bool doIgnoreNext = false;
			bool isUnreadableNext = false;
			List<IncrementalGAnalyzer> analyzers = new List<IncrementalGAnalyzer>();
			IGAnlys_Word wordBeingBuilt = null;
			foreach (var morpheme in morphemes) {
				if (morpheme.id == 0) {
					if (wordBeingBuilt == null)
						wordBeingBuilt = new IGAnlys_Word();
					wordBeingBuilt.AddMorphemeText(morpheme.word);
				} else if (wordBeingBuilt != null) {
					analyzers.Add(ModifyAnalyzer(wordBeingBuilt, ref doIgnoreNext, ref isUnreadableNext));
					wordBeingBuilt = null;
				}
				if (morpheme.id == 1) {
					if (int.TryParse(morpheme.word, out int morphemeID)) {
						analyzers.Add(ModifyAnalyzer(new IGAnlys_Quote { morphemeID = morphemeID }, ref doIgnoreNext, ref isUnreadableNext));
					} else {
						if(!doIgnoreNext)
							analyzers.Add(dict.dict[morpheme.word]);
						else
							analyzers.Add(new IGAnlys_IgnoreBlock { baseAnalyzer = dict.dict[morpheme.word] });
						doIgnoreNext = false;
					}
				} else if (morpheme.id == 2) {
					doIgnoreNext = true;
				} else if (morpheme.id == 3) {
					isUnreadableNext = true;
				}
			}
			if (wordBeingBuilt != null) {
				analyzers.Add(ModifyAnalyzer(wordBeingBuilt, ref doIgnoreNext, ref isUnreadableNext));
			}
			if (analyzers.Count == 1) {
				return analyzers[0];
			}
			else if (analyzers.Count > 1) {
				var sequence = new IGAnlys_Sequence();
				foreach (var analyzer in analyzers) {
					sequence.analyzers.Add(analyzer);
				}
				return new IGAnlys_ResultClusterizer { baseAnalyzer = sequence };
			}
			return null;
		}
		public IGAnlys_Word ForceWordANlys(string key) {
			var morphemes = analyzer.AnalyzeImmediate(key);
			IGAnlys_Word wordBeingBuilt = null;
			foreach (var morpheme in morphemes) {
				if (wordBeingBuilt == null)
					wordBeingBuilt = new IGAnlys_Word();
				wordBeingBuilt.AddMorphemeText(morpheme.word);
			}
			return wordBeingBuilt;
		}
		public IncrementalGAnalyzer ModifyAnalyzer(IncrementalGAnalyzer anlys, ref bool doIgnoreNext, ref bool isUnreadableNext) {
			var result = anlys;
			if (doIgnoreNext) {
				result = new IGAnlys_IgnoreBlock { baseAnalyzer = anlys };
			} else if (isUnreadableNext) {
				result = new IGAnlys_Unreadable { lasyAnlys = anlys };
			}
			doIgnoreNext = false;
			isUnreadableNext = false;
			return result;
		}
	}
	public class FormatReader : GenericReader {
		public static IncrementalGAnalyzer cojunctionIGAnlys;
		public static IncrGAnlysDictionary gAnlysDict;
		public static FormToGAnlys fReader;
		public static Dictionary<string, GrammarBlock> metaInfos;
		public List<string> forms = new List<string>();
		public List<string> names = new List<string>();
		public List<string> categoriess = new List<string>();
		public List<string> attributes = new List<string>();
		public List<string> preModifiers = new List<string>();
		public List<string> postModifiers = new List<string>();
		public bool isPolymorphicWord = false;
		public bool isEnumerable = false;
		public bool isConjunctionOptional = false;
		public IncrementalGAnalyzer FormsToAnlys(List<string> forms, bool setting_isPolymorphicWord) {
			if (forms.Count == 0) {
				return fReader.PickBestElement(forms[0]);
			}
			if (setting_isPolymorphicWord) {
				IGAnlys_PolymorphicWord poly = new IGAnlys_PolymorphicWord();
				foreach (var form in forms) {
					poly.wordAnalyzers.Add(fReader.ForceWordANlys(form));
				}
				return poly;
			}
			if (forms.Count > 1) {
				IGAnlys_Candidates candidates = new IGAnlys_Candidates { };
				foreach (var form in forms) {
					candidates.analyzers.Add(fReader.PickBestElement(form));
				}
				return candidates;
			}
			return fReader.PickBestElement(forms[0]);

		}
		void GenericReader.Conclude() {
			IncrementalGAnalyzer analyzer = FormsToAnlys(forms, isPolymorphicWord);
			if (preModifiers.Count > 0 || postModifiers.Count > 0) {
				IncrementalGAnalyzer premod = null;
				IncrementalGAnalyzer postmod = null;
				if (preModifiers.Count > 0)
					//premod = new IGAnlys_RepeatableBlock { baseAnalyzer = FormsToAnlys(preModifiers), conjectionAnalyzer = cojunctionIGAnlys };
					premod = FormsToAnlys(preModifiers, false);
				if (postModifiers.Count > 0)
					//postmod = new IGAnlys_RepeatableBlock { baseAnalyzer = FormsToAnlys(postModifiers), conjectionAnalyzer = cojunctionIGAnlys };
					postmod = FormsToAnlys(postModifiers, false);
				var modBlock = new IGAnlys_ModifyBlock(analyzer, premod, postmod);
				analyzer = modBlock;
			}
			if (isEnumerable) {
				analyzer = new IGAnlys_RepeatableBlock { baseAnalyzer = analyzer, conjectionAnalyzer = cojunctionIGAnlys, isConjectionOptional = isConjunctionOptional };
				analyzer = new IGAnlys_ResultClusterizer { baseAnalyzer = analyzer };
			}
			if (attributes.Count > 0) {
				var metaAnlys = new IGAnlys_AddMeta { baseAnalyzer = analyzer };
				foreach (var attribute in attributes) {
					if (metaInfos.TryGetValue(attribute, out GrammarBlock gBlock)) {
						metaAnlys.metas.Add(gBlock);
					}
				};
				analyzer = metaAnlys;
			}
			foreach (var category in categoriess) {
				gAnlysDict.categories[category].analyzers.Add(analyzer);
			};
			foreach (var name in names) {
				gAnlysDict.dict[name] = analyzer;
			};

		}

		GenericReader GenericReader.GetSubReader(string name) { return null; }
		void GenericReader.ReadString(string name, string value) {
			if (name.Equals("Category", StringComparison.CurrentCultureIgnoreCase) || name.Equals("Categories", StringComparison.CurrentCultureIgnoreCase)) {
				categoriess.Add(value);
			}
			if (name.Equals("Attribute", StringComparison.CurrentCultureIgnoreCase) || name.Equals("Attributes", StringComparison.CurrentCultureIgnoreCase)) {
				attributes.Add(value);
			}
			if (name.Equals("Form", StringComparison.CurrentCultureIgnoreCase) || name.Equals("Forms", StringComparison.CurrentCultureIgnoreCase)) {
				forms.Add(value);
			}
			if (name.Equals("Name", StringComparison.CurrentCultureIgnoreCase) || name.Equals("Names", StringComparison.CurrentCultureIgnoreCase)) {
				names.Add(value);
			}
			if (name.Equals("PreModifier", StringComparison.CurrentCultureIgnoreCase) || name.Equals("PreModifiers", StringComparison.CurrentCultureIgnoreCase)) {
				preModifiers.Add(value);
			}
			if (name.Equals("PostModifier", StringComparison.CurrentCultureIgnoreCase) || name.Equals("PostModifiers", StringComparison.CurrentCultureIgnoreCase)) {
				postModifiers.Add(value);
			}
		}
		public FormatReader Clone() {
			return new FormatReader {
				forms = new List<string>(forms),
				names = new List<string>(names),
				categoriess = new List<string>(categoriess),
				attributes = new List<string>(attributes),
				preModifiers = new List<string>(preModifiers),
				postModifiers = new List<string>(postModifiers),
				//isEnumerable = isEnumerable,
				//isConjunctionOptional = isConjunctionOptional,
				isPolymorphicWord = isPolymorphicWord
			};
		}

		void GenericReader.ReadBool(string name, bool value) {
			if (name.StartsWith("IsPolymorphicWord", StringComparison.CurrentCultureIgnoreCase)) {
				isPolymorphicWord = value;
			}
			if (name.StartsWith("IsEnumerable", StringComparison.CurrentCultureIgnoreCase)) {
				isEnumerable = value;
			}
			if (name.StartsWith("IsConjunctionOptional", StringComparison.CurrentCultureIgnoreCase)) {
				isConjunctionOptional = value;
			}
		}
	}
	public class TemplateReader : GenericReader {
		public Dictionary<string, FormatReader> templateDict;
		public FormatReader template = new FormatReader();
		public List<string> templateNames = new List<string>();
		void GenericReader.Conclude() {
			foreach (var templateName in templateNames) {
				templateDict[templateName] = template;
			}
		}
		GenericReader GenericReader.GetSubReader(string name) {
			return (template as GenericReader).GetSubReader(name);
		}

		void GenericReader.ReadBool(string name, bool value) {
			(template as GenericReader).ReadBool(name, value);
		}

		void GenericReader.ReadString(string name, string value) {
			if (name.Equals("TemplateName", StringComparison.CurrentCultureIgnoreCase) || name.Equals("TemplateNames", StringComparison.CurrentCultureIgnoreCase)) {
				templateNames.Add(value);
			} else {
				(template as GenericReader).ReadString(name, value);
			}
		}
	}
	public class GrammarDictRoot : GenericReader {
		public IncrGAnlysDictionary gAnlysDict;
		public Dictionary<string, FormatReader> templateDict = new Dictionary<string, FormatReader>(System.StringComparer.CurrentCultureIgnoreCase);
		void GenericReader.Conclude() { }
		GenericReader GenericReader.GetSubReader(string name) {
			if (name.Equals("GeneralFormat", StringComparison.CurrentCultureIgnoreCase) || name.Equals("GeneralFormats", StringComparison.CurrentCultureIgnoreCase)) {
				return new FormatReader { };
			} else if (name.Equals("Template", StringComparison.CurrentCultureIgnoreCase) || name.Equals("Templates", StringComparison.CurrentCultureIgnoreCase)) {
				return new TemplateReader { templateDict = templateDict };
			} else if (templateDict.TryGetValue(name, out var formatReader)) {
				return formatReader.Clone();
			}
			return null;
		}

		void GenericReader.ReadBool(string name, bool value) { }

		void GenericReader.ReadString(string name, string value) {
			if (name.Equals("Category", StringComparison.CurrentCultureIgnoreCase) || name.Equals("Categories", StringComparison.CurrentCultureIgnoreCase)) {
				if (!gAnlysDict.categories.ContainsKey(name)) {
					gAnlysDict.AddCandidate(value, new IGAnlys_Candidates());
				}
			}
		}
	}
	public class RootReader {
		public Stack<GenericReader> subReader = new Stack<GenericReader>();
		public Stack<string> propertyNames = new Stack<string>();
		public void Read(string jsonText) {
			TextReader textReader = new StringReader(jsonText);
			using (var jsonReader = new JsonTextReader(textReader)) {
				while (jsonReader.Read()) {
					if (jsonReader.TokenType == JsonToken.PropertyName) {
						propertyNames.Pop();
						propertyNames.Push(jsonReader.Value as string);
					} else if (jsonReader.TokenType == JsonToken.StartObject) {
						if (propertyNames.Count > 0) {
							var sub = subReader.Peek().GetSubReader(propertyNames.Peek());
							if (sub != null)
								subReader.Push(sub);
							else {
								subReader.Push(subReader.Peek());
							}
						} else {
							subReader.Push(subReader.Peek());
						}
						propertyNames.Push("");
					} else if (jsonReader.TokenType == JsonToken.EndObject) {
						var last = subReader.Peek();
						subReader.Pop();
						if (last != subReader.Peek())
							last.Conclude();
						propertyNames.Pop();
					} else if (jsonReader.TokenType == JsonToken.String) {
						subReader.Peek().ReadString(propertyNames.Peek(), (string)jsonReader.Value);
					} else if (jsonReader.TokenType == JsonToken.Float) {
						//subReader.Peek().ReadString(propertyNames.Peek(), (float)jsonReader.Value);
					} else if (jsonReader.TokenType == JsonToken.Boolean) {
						subReader.Peek().ReadBool(propertyNames.Peek(), (bool)jsonReader.Value);
					}
				}
			}
		}
	}
	public class IncrGAnlysDictionary : ImmediatePicker<IncrementalGAnalyzer, string> {
		public Dictionary<string, IncrementalGAnalyzer> dict = new Dictionary<string, IncrementalGAnalyzer>(System.StringComparer.CurrentCultureIgnoreCase);
		public Dictionary<string, IGAnlys_Candidates> categories = new Dictionary<string, IGAnlys_Candidates>(System.StringComparer.CurrentCultureIgnoreCase);
		public void AddCandidate(string name, IGAnlys_Candidates candidate) {
			dict[name] = categories[name] = new IGAnlys_Candidates();
		}
		IncrementalGAnalyzer ImmediatePicker<IncrementalGAnalyzer, string>.PickBestElement(string key) {
			dict.TryGetValue(key, out IncrementalGAnalyzer found);
			return found;
		}
	}
}
