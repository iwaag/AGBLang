using System;
using System.Collections.Generic;
using System.Text;
using AGDev;
namespace AGBLang.StdUtil {
	public class StdGrammarAnalyzer : GrammarAnalyzer {
		public IncrementalGAnalyzer incrGAnalyzer;
		void GrammarAnalyzer.AnalyzeGrammar(GAnlysInput input, Taker<GrammarBlock> listener) {
			var result = new ExpansiveMutableGBlock { metaForCluster = StdMetaInfos.sentenceCluster };
			Action<GrammarBlock> adder = (gBlock) => result.AddBlock(gBlock);
			var nextInput = input;
			while (true) {
				var easyLis = new EasyIncrGAnalysListener {};
				incrGAnalyzer.Analyze(nextInput, easyLis);
				if (!easyLis.didMatch) {
					break;
				}
				nextInput = easyLis.nextInput;
				easyLis.listener.OnResultRequested(adder);
			}
			if (result.content != null) {
				listener.Take(result.content);
			}
			else {
				listener.None();
			}
		}
	}
	public class EasyIncrGAnalysListener : IncrGAnalysisListener {
		public bool didMatch = false;
		public GAnlysInput nextInput;
		public AfterMatchListener listener;
		public AlternativeIncrGAnalyzer alternative;
		void IncrGAnalysisListener.OnMatch(GAnlysInput _nextInput, AfterMatchListener _listener, AlternativeIncrGAnalyzer _alternative) {
			didMatch = true;
			nextInput = _nextInput;
			listener = _listener;
			alternative = _alternative;
		}
	}
	#region IncrementalGAnalyzer impls
	public class IGAnlys_ModifyBlock : IncrementalGAnalyzer {
		public IGAnlys_ModifyBlock(IncrementalGAnalyzer _baseAnlys, IncrementalGAnalyzer _preMod, IncrementalGAnalyzer _postMod) {
			baseAnlys = _baseAnlys;
			preMod = _preMod;
			postMod = _postMod;
			if (preMod != null) {
				anaylzers.Add(new IGAnlys_Optional { baseAnalyzer = preMod });
			}
			anaylzers.Add(baseAnlys);
			if (postMod != null) {
				anaylzers.Add(new IGAnlys_Optional { baseAnalyzer = postMod });
			}
		}
		IncrementalGAnalyzer baseAnlys;
		IncrementalGAnalyzer preMod;
		IncrementalGAnalyzer postMod;
		public List<IncrementalGAnalyzer> anaylzers = new List<IncrementalGAnalyzer>();
		void IncrementalGAnalyzer.Analyze(GAnlysInput input, IncrGAnalysisListener listener) {
			anaylzers[0].Analyze( input,
				new ChainListener {
					index = 0,
					results = new List<IndexAndResult>(),
					analyzers = anaylzers,
					finalListener = listener,
					afterListenerFactory = CreateAfterListener
				}
			);
		}
		AfterMatchListener CreateAfterListener(List<IndexAndResult> updatedMatches) {
			return new PrvtAfterMatchListener {
				parent = this,
				updatedMatches = updatedMatches
			};
			
		}
		public class PrvtAfterMatchListener : AfterMatchListener {
			public IGAnlys_ModifyBlock parent = null;
			public List<IndexAndResult> updatedMatches;
			void AfterMatchListener.OnResultRequested(Action<MutableGrammarBlock> blockTaker) {
				MutableGrammarBlock baseBlock = null;
				List<GrammarBlock> modifiers = new List<GrammarBlock>();
				foreach (var afLIs in updatedMatches) {
					if (afLIs.index == 0 && parent.preMod != null)
						afLIs.result.listener.OnResultRequested((mod) => modifiers.Add(mod));
					else if ( (afLIs.index == 0 && parent.preMod == null) || (afLIs.index == 1 && parent.preMod != null)) {
						afLIs.result.listener.OnResultRequested((mgBlock) => baseBlock = mgBlock);
					}
					else if ((afLIs.index == 1 && parent.preMod == null) || (afLIs.index == 2 && parent.preMod != null)) {
						afLIs.result.listener.OnResultRequested((mod) => modifiers.Add(mod));
					}
				}
				if (baseBlock != null) {
					foreach (var mod in modifiers) {
						baseBlock.AddModifier(mod);
					}
					blockTaker(baseBlock);
				}
			}
		}
	}
	public abstract class ResultChangingIGAnalyzer : IncrementalGAnalyzer {
		public IncrementalGAnalyzer baseAnalyzer;
		void IncrementalGAnalyzer.Analyze(GAnlysInput input, IncrGAnalysisListener listener) {
			baseAnalyzer.Analyze(input, new ResultChangingIGAListener { baseListener = listener, parent = this });
		}
		public abstract AfterMatchListener ChangeAfterListener(AfterMatchListener sourceAfterListener);
		public class ResultChangingIGAListener : IncrGAnalysisListener {
			public IncrGAnalysisListener baseListener;
			public ResultChangingIGAnalyzer parent;
			void IncrGAnalysisListener.OnMatch(GAnlysInput _nextInput, AfterMatchListener listener, AlternativeIncrGAnalyzer alternative) {
				if (alternative != null)
					baseListener.OnMatch(_nextInput, parent.ChangeAfterListener(listener), new ResultAltIncrGAnalyzer { baseAltAnalyzer = alternative, parent = parent });
				else
					baseListener.OnMatch(_nextInput, parent.ChangeAfterListener(listener));
			}
			public class ResultAltIncrGAnalyzer : AlternativeIncrGAnalyzer {
				public AlternativeIncrGAnalyzer baseAltAnalyzer;
				public ResultChangingIGAnalyzer parent;
				void AlternativeIncrGAnalyzer.AnalyzeAgain(IncrGAnalysisListener listener) {
					baseAltAnalyzer.AnalyzeAgain(new ResultChangingIGAListener { parent = parent, baseListener = listener });
				}
			}
		}
	}
	public class IGAnlys_IgnoreBlock : ResultChangingIGAnalyzer {
		static IgnoreAfterMatchListener ignore = new IgnoreAfterMatchListener {};
		public override AfterMatchListener ChangeAfterListener(AfterMatchListener sourceAfterListener) {
			//return ignore;
			return new IgnoreAfterMatchListener { original = sourceAfterListener };
		}
		public class IgnoreAfterMatchListener : AfterMatchListener {
			public AfterMatchListener original;//debug
			void AfterMatchListener.OnResultRequested(Action<MutableGrammarBlock> blockTaker) {
			}
		}
	}
	public class IGAnlys_Optional : IncrementalGAnalyzer {
		public IncrementalGAnalyzer baseAnalyzer;
		void IncrementalGAnalyzer.Analyze(GAnlysInput input, IncrGAnalysisListener listener) {
			var recLis = new RecordingIncrGAnalysListener { baseListener = listener};
			baseAnalyzer.Analyze(input, recLis);
			if (! recLis.didMatch) {
				listener.OnMatch(input, StbAfterLis.instance);
			}
		}
	}
	public class StbAfterLis : AfterMatchListener {
		public static StbAfterLis instance { get; } = new StbAfterLis();
		void AfterMatchListener.OnResultRequested(Action<MutableGrammarBlock> blockTaker) {}
	}
	public class IGAnlys_AddMeta : ResultChangingIGAnalyzer {
		public List<GrammarBlock> metas = new List<GrammarBlock>();
		public override AfterMatchListener ChangeAfterListener(AfterMatchListener sourceAfterListener) {
			return new AddMetaAMatchListener { baseListener = sourceAfterListener, parent = this };
		}
		public class AddMetaAMatchListener : AfterMatchListener {
			public IGAnlys_AddMeta parent;
			public AfterMatchListener baseListener;
			void AfterMatchListener.OnResultRequested(Action<MutableGrammarBlock> blockTaker) {
				baseListener.OnResultRequested(
					(gBlock) => {
						foreach (var meta in parent.metas) {
							gBlock.AddMetaInfo(meta);
						}
						blockTaker(gBlock);
					}
				);
			}
		}
	}
	public class IGAnlys_ResultClusterizer : ResultChangingIGAnalyzer {
		public override AfterMatchListener ChangeAfterListener(AfterMatchListener sourceAfterListener) {
			return new ClusteringAfterMatchListener { baseAMatchLis = sourceAfterListener };
		}
	}
	public class ClusteringAfterMatchListener : AfterMatchListener {
		public AfterMatchListener baseAMatchLis;
		void AfterMatchListener.OnResultRequested(Action<MutableGrammarBlock> blockTaker) {
			var mClusterGBlock = new StdMutableClusterGBlock();
			MutableGrammarBlock lastBlock = null;
			baseAMatchLis.OnResultRequested(
				(gBlock) => mClusterGBlock.subBlocks.Add(lastBlock = gBlock)
			);
			if (mClusterGBlock.subBlocks.Count == 1)
				blockTaker(lastBlock);
			else if (mClusterGBlock.subBlocks.Count > 1)
				blockTaker(mClusterGBlock);
		}
	}
	public class IGAnlys_Word : IncrementalGAnalyzer, AfterMatchListener {
		public List<string> words = new List<string>();
		public string integrated = "";
		public void AddMorphemeText(string morphemeWord) {
			words.Add(morphemeWord);
			if (integrated.Length != 0)
				integrated += " ";
			integrated += morphemeWord;
		}
		public bool IsMatching(GAnlysInput input) {
			int i = 0;
			foreach (var sourceWord in input.followings) {
				if (!words[i].Equals(sourceWord.word, StringComparison.CurrentCultureIgnoreCase)) {
					return false;
				}
				if (i == words.Count - 1) {
					return true;
				}
				i++;
			}
			//text end before match
			return false;
		} 
		void IncrementalGAnalyzer.Analyze(GAnlysInput input, IncrGAnalysisListener listener) {
			if(IsMatching(input))
				listener.OnMatch(input.GetAdvanced(words.Count), this);
		}
		void AfterMatchListener.OnResultRequested(Action<MutableGrammarBlock> blockTaker) {
			blockTaker(new StdMutableGUnit { word = integrated });
		}
	}
	public class IGAnlys_PolymorphicWord : IncrementalGAnalyzer {
		public List<IGAnlys_Word> wordAnalyzers = new List<IGAnlys_Word>();
		void IncrementalGAnalyzer.Analyze(GAnlysInput input, IncrGAnalysisListener listener) {
			UnitProcess(input, 0, listener);
		}
		public void UnitProcess(GAnlysInput input, int fromIndex, IncrGAnalysisListener listener) {
			for (int index = fromIndex; index < wordAnalyzers.Count; index++) {
				if (wordAnalyzers[index].IsMatching(input)) {
					if (index < wordAnalyzers.Count - 1) {
						listener.OnMatch(input.GetAdvanced(wordAnalyzers[index].words.Count), wordAnalyzers[0], new PrvtAltGAnlys {
							parent = this, index = index + 1, input = input
						});
						return;
					}
					else {
						listener.OnMatch(input.GetAdvanced(wordAnalyzers[index].words.Count), wordAnalyzers[0], null);
						return;
					}
				}
			}
		}
		class PrvtAltGAnlys : AlternativeIncrGAnalyzer {
			public IGAnlys_PolymorphicWord parent;
			public int index;
			public GAnlysInput input;
			void AlternativeIncrGAnalyzer.AnalyzeAgain(IncrGAnalysisListener listener) {
				parent.UnitProcess(input, index, listener);
			}
		}
	}
	public class IGAnlys_RepeatableBlock : IncrementalGAnalyzer {
		public IncrementalGAnalyzer baseAnalyzer;
		public IncrementalGAnalyzer conjectionAnalyzer;
		public bool isConjectionOptional = false;
		void IncrementalGAnalyzer.Analyze(GAnlysInput input, IncrGAnalysisListener listener) {
			var easyLis = new EasyIncrGAnalysListener();
			baseAnalyzer.Analyze(input, easyLis);
			if (easyLis.didMatch) {
				var alt = new PrvtAltIGAnlys { input = input, parent = this, previousAlt = easyLis.alternative, analyzers = new List<IncrementalGAnalyzer> { baseAnalyzer } };
				listener.OnMatch(easyLis.nextInput, easyLis.listener, alt);
			}
		}
		public class PrvtAltIGAnlys : AlternativeIncrGAnalyzer {
			public GAnlysInput input;
			public IGAnlys_RepeatableBlock parent;
			public AlternativeIncrGAnalyzer previousAlt;
			public List<IncrementalGAnalyzer> analyzers;
			void AlternativeIncrGAnalyzer.AnalyzeAgain(IncrGAnalysisListener listener) {
				#region try past alternatives
				if (previousAlt != null){
					var altLis = new EasyIncrGAnalysListener();
					previousAlt.AnalyzeAgain(altLis);
					if (altLis.didMatch) {
						var alt = new PrvtAltIGAnlys { parent = parent, previousAlt = altLis.alternative, analyzers = analyzers, input = input };
						listener.OnMatch(altLis.nextInput, altLis.listener, alt);
						return;
					}
				}
				#endregion
				#region try repeat
				var newAnalyzers = new List<IncrementalGAnalyzer>(analyzers);
				if (parent.conjectionAnalyzer != null) {
					if (parent.isConjectionOptional)
						newAnalyzers.Add(new IGAnlys_IgnoreBlock { baseAnalyzer = new IGAnlys_Optional { baseAnalyzer = parent.conjectionAnalyzer } });
					else
						newAnalyzers.Add(new IGAnlys_IgnoreBlock { baseAnalyzer = parent.conjectionAnalyzer });
				}
				newAnalyzers.Add(parent.baseAnalyzer);
				var repeatLis = new EasyIncrGAnalysListener();
				newAnalyzers[0].Analyze(
					input,
					new ChainListener {
						index = 0,
						results = new List<IndexAndResult>(),
						analyzers = newAnalyzers,
						finalListener = repeatLis,
						afterListenerFactory = DefaultAfterListener
					}
				);
				if (repeatLis.didMatch) {
					listener.OnMatch(repeatLis.nextInput, repeatLis.listener,
						new PrvtAltIGAnlys { analyzers = newAnalyzers, input = input, parent = parent, previousAlt = repeatLis.alternative}
					);
				}
				#endregion

			}
			static AfterMatchListener DefaultAfterListener(List<IndexAndResult> updatedMatches) {
				return new ClusterAfterListener { afterListeners = updatedMatches };
			}
		}
	}
	public class IGAnlys_RepeatableBlockOld2 : IncrementalGAnalyzer {
		public IncrementalGAnalyzer baseAnalyzer;
		public IncrementalGAnalyzer conjectionAnalyzer;
		public bool isConjectionOptional = true;
		void IncrementalGAnalyzer.Analyze(GAnlysInput input, IncrGAnalysisListener listener) {
			var easyLis = new EasyIncrGAnalysListener();
			baseAnalyzer.Analyze(input, easyLis);
			if (easyLis.didMatch) {
				var alt = new PrvtAltIGAnlys { parent = this, previousResult = new List<EasyIncrGAnalysListener> { easyLis } };
				listener.OnMatch(easyLis.nextInput, easyLis.listener, alt);
			}
		}
		public class PrvtAltIGAnlys : AlternativeIncrGAnalyzer {
			public IGAnlys_RepeatableBlockOld2 parent;
			public List<EasyIncrGAnalysListener> previousResult;
			void AlternativeIncrGAnalyzer.AnalyzeAgain(IncrGAnalysisListener listener) {
				var nextInput = previousResult[previousResult.Count-1].nextInput;
				var newResultStack = new List<EasyIncrGAnalysListener>(previousResult);
				#region try past alternatives
				while (newResultStack.Count != 0) {
					var lastResult = newResultStack[newResultStack.Count - 1];
					if (lastResult.alternative != null) {
						var altLis = new EasyIncrGAnalysListener();
						lastResult.alternative.AnalyzeAgain(altLis);
						if (altLis.didMatch) {
							newResultStack.RemoveAt(newResultStack.Count - 1);
							newResultStack.Add(lastResult);
							listener.OnMatch(altLis.nextInput, new ClusterAMListener { listeners = newResultStack }, new PrvtAltIGAnlys { parent = parent, previousResult = newResultStack });
							return;
						}
					}
				}
				#endregion
				#region try repeat
				var conjLis = new EasyIncrGAnalysListener();
				parent.conjectionAnalyzer.Analyze(nextInput, conjLis);
				if (conjLis.didMatch) {
					nextInput = conjLis.nextInput;
				}
				if (conjLis.didMatch || parent.isConjectionOptional) {
					var mainLis = new EasyIncrGAnalysListener();
					parent.baseAnalyzer.Analyze(nextInput, mainLis);
					if (mainLis.didMatch) {
						newResultStack.Add(mainLis);
						listener.OnMatch(mainLis.nextInput, new ClusterAMListener { listeners = newResultStack }, new PrvtAltIGAnlys { parent = parent, previousResult = newResultStack });
						return;
					}
				}
				#endregion
				
			}
		}
	}
	public class ClusterAMListener : AfterMatchListener {
		public IEnumerable<EasyIncrGAnalysListener> listeners;
		void AfterMatchListener.OnResultRequested(Action<MutableGrammarBlock> blockTaker) {
			foreach (var listener in listeners) {
				listener.listener.OnResultRequested(blockTaker);
			}
		}
	}
	#if false
	public class IGAnlys_RepeatableBlockOld : IncrementalGAnalyzer {
		public IncrementalGAnalyzer baseAnalyzer;
		public IncrementalGAnalyzer conjectionAnalyzer;
		public bool isConjectionOptional = true;
		void IncrementalGAnalyzer.Analyze(GAnlysInput input, IncrGAnalysisListener listener) {
			Stack<EasyIncrGAnalysListener> resultArchives = new Stack<EasyIncrGAnalysListener>();
			while (true) {
				var currentInput = resultArchives.Count == 0 ? input : resultArchives.Peek().nextInput;
				if (resultArchives.Count > 0 && conjectionAnalyzer != null) {
					var conjLis = new EasyIncrGAnalysListener();
					conjectionAnalyzer.Analyze(currentInput, conjLis);
					if (conjLis.didMatch) {
						currentInput = conjLis.nextInput;
					}
					if (!conjLis.didMatch && !isConjectionOptional)
						break;
				}
				var easyLis = new EasyIncrGAnalysListener();
				baseAnalyzer.Analyze(currentInput, easyLis);
				if (!easyLis.didMatch) {
					if (resultArchives.Count == 0)
						return;
					else
						break;
				}
				else {
					resultArchives.Push(easyLis);
				}
			}
			List<AfterMatchListener> afLiss = new List<AfterMatchListener>();
			foreach (var result in resultArchives) {
				afLiss.Add(result.listener);
			}
			bool hasAlt = false;
			foreach (var result in resultArchives) {
				if (result.alternative != null)
					hasAlt = true;
			}
			AlternativeIncrGAnalyzer altLis = null;
			if (hasAlt) {
				altLis = new PrvtAltIGAnlys { givenResultArchive = resultArchives, parent = this };
			}
			listener.OnMatch(resultArchives.Peek().nextInput, new PrvtLis { listeners = afLiss }, altLis);
		}
		public class PrvtLis : AfterMatchListener {
			public IEnumerable<AfterMatchListener> listeners;
			void AfterMatchListener.OnResultRequested(Action<MutableGrammarBlock> blockTaker) {
				foreach (var listener in listeners) {
					listener.OnResultRequested(blockTaker);
				}
			}
		}
		public class PrvtAltIGAnlys : AlternativeIncrGAnalyzer {
			public IGAnlys_RepeatableBlock parent;
			public Stack<EasyIncrGAnalysListener> givenResultArchive;
			void AlternativeIncrGAnalyzer.AnalyzeAgain(IncrGAnalysisListener listener) {
				Stack<EasyIncrGAnalysListener> resultArchives = new Stack<EasyIncrGAnalysListener>(givenResultArchive);
				EasyIncrGAnalysListener lastLis = null;
				while (true) {
					if (resultArchives.Count <= 0) {
						return;
					}
					var analyzer = resultArchives.Pop();
					if (analyzer.alternative == null)
						continue;
					var easyLis = new EasyIncrGAnalysListener();
					analyzer.alternative.AnalyzeAgain(easyLis);
					if (easyLis.didMatch) {
						resultArchives.Push(easyLis);
						lastLis = easyLis;
						break;
					}
				}
				while (true) {
					if (parent.conjectionAnalyzer != null) {
						var conjLis = new EasyIncrGAnalysListener { };
						parent.conjectionAnalyzer.Analyze(lastLis.nextInput, conjLis);
						if (conjLis.didMatch) {
							lastLis = conjLis;
						}
						else if (!parent.isConjectionOptional) {
							return;
						}
					}
					var easyLis = new EasyIncrGAnalysListener();
					parent.baseAnalyzer.Analyze(lastLis.nextInput, easyLis);
					if (!easyLis.didMatch) {
						if (resultArchives.Count == 0)
							return;
						else
							break;
					}
					else {
						resultArchives.Push(easyLis);
					}
				}
				List<AfterMatchListener> afLiss = new List<AfterMatchListener>();
				foreach (var result in resultArchives) {
					afLiss.Add(result.listener);
				}
				bool hasAlt = false;
				foreach (var result in resultArchives) {
					if (result.alternative != null)
						hasAlt = true;
				}
				AlternativeIncrGAnalyzer altAnlys = null;
				if (hasAlt) {
					altAnlys = new PrvtAltIGAnlys { givenResultArchive = resultArchives, parent = parent };
				}
				listener.OnMatch(resultArchives.Peek().nextInput, new PrvtLis { listeners = afLiss }, altAnlys);
			}
		}
	}
	#endif
	public class IGAnlys_Quote : IncrementalGAnalyzer {
		public int morphemeID;
		void IncrementalGAnalyzer.Analyze(GAnlysInput input, IncrGAnalysisListener listener) {
			foreach (var sourceFUnit in input.followings) {
				if (sourceFUnit.id == morphemeID) {
					listener.OnMatch(input.GetAdvanced(1), new QuoteAfterMatchListener {unit = sourceFUnit });
				}
				break;
			}
			
		}
		public class QuoteAfterMatchListener : AfterMatchListener {
			public Morpheme unit;
			void AfterMatchListener.OnResultRequested(Action<MutableGrammarBlock> blockTaker) {
				blockTaker(new StdMutableGUnit { word = unit.word });
			}
		}
	}
	public class IGAnlys_Unreadable : IncrementalGAnalyzer {
		public IncrementalGAnalyzer lasyAnlys;
		void IncrementalGAnalyzer.Analyze(GAnlysInput input, IncrGAnalysisListener listener) {
			int offset = 0;
			var afterLis = new PrvtALis { };
			foreach (var morpheme in input.followings) {
				EasyIncrGAnalysListener easyLis = new EasyIncrGAnalysListener();
				var nextInput = input.GetAdvanced(offset);
				lasyAnlys.Analyze(nextInput, easyLis);
				if (easyLis.didMatch) {
					listener.OnMatch(nextInput, afterLis);
					return;
				}
				else {
					afterLis.gBlocks.Add(new StdMutableGUnit { word = morpheme.word});
				}
				offset++;
			}
		}
		public class PrvtALis : AfterMatchListener {
			public List<MutableGrammarBlock> gBlocks = new List<MutableGrammarBlock>();
			void AfterMatchListener.OnResultRequested(Action<MutableGrammarBlock> blockTaker) {
				if(gBlocks.Count == 1) {
					gBlocks[0].AddMetaInfo(StdMetaInfos.unreadable);
					blockTaker(gBlocks[0]);
				}
				else if (gBlocks.Count > 1) {
					var cluster = new StdMutableClusterGBlock();
					foreach (var block in gBlocks) {
						cluster.subBlocks.Add(block);
					}
					(cluster as MutableGrammarBlock).AddMetaInfo(StdMetaInfos.unreadable);
					blockTaker(cluster);
				}
			}
		}
	}
	public class IGAnlys_Candidates : IncrementalGAnalyzer {
		public class SubListener : IncrGAnalysisListener {
			public IGAnlys_Candidates parent;
			public int nextAnalyzerIndex;
			public IncrGAnalysisListener clientListner;
			public GAnlysInput originalInput;
			public bool didHit = false;
			void IncrGAnalysisListener.OnMatch(GAnlysInput nextInput, AfterMatchListener listener, AlternativeIncrGAnalyzer alternative) {
				didHit = true;
				if (alternative != null) {
					clientListner.OnMatch(nextInput, listener,
						new UnitAltAnlys { parent = parent, nextIndex = nextAnalyzerIndex, subAltAnalyzer = alternative, originalInput = originalInput }
					);
				}
				else {
					if (nextAnalyzerIndex < parent.analyzers.Count) {
						clientListner.OnMatch(nextInput, listener, new UnitAltAnlys { parent = parent, nextIndex = nextAnalyzerIndex, originalInput = originalInput });
					}
					else {
						clientListner.OnMatch(nextInput, listener);
					}
				}
			}
		}
		public class UnitAltAnlys : AlternativeIncrGAnalyzer {
			public IGAnlys_Candidates parent;
			public int nextIndex;
			public GAnlysInput originalInput;

			public AlternativeIncrGAnalyzer subAltAnalyzer;

			void AlternativeIncrGAnalyzer.AnalyzeAgain(IncrGAnalysisListener listener) {
				var index = nextIndex;
				var innerListener = new SubListener { clientListner = listener, parent = parent, nextAnalyzerIndex = index, originalInput = originalInput };
				if (subAltAnalyzer != null)
					subAltAnalyzer.AnalyzeAgain(innerListener);
				while (!innerListener.didHit && index < parent.analyzers.Count) {
					innerListener.nextAnalyzerIndex++;
					parent.analyzers[index].Analyze(originalInput, innerListener);
					index++;
				}
			}
		}
		public List<IncrementalGAnalyzer> analyzers = new List<IncrementalGAnalyzer>();
		void IncrementalGAnalyzer.Analyze(GAnlysInput input, IncrGAnalysisListener listener) {
			var innerListener = new SubListener { clientListner = listener, parent = this, nextAnalyzerIndex = 1, originalInput = input };
			while (!innerListener.didHit && innerListener.nextAnalyzerIndex-1 < analyzers.Count) {
				analyzers[innerListener.nextAnalyzerIndex-1].Analyze(input, innerListener);
				innerListener.nextAnalyzerIndex++;

			}
		}
	}
	public class RecordingIncrGAnalysListener : IncrGAnalysisListener {
		public IncrGAnalysisListener baseListener;
		public bool didMatch = false;
		void IncrGAnalysisListener.OnMatch(GAnlysInput result, AfterMatchListener listener, AlternativeIncrGAnalyzer alternative) {
			didMatch = true;
			baseListener.OnMatch(result, listener, alternative);
		}
	}
	public struct IndexedAfterMatchListener {
		public int index;
		public AfterMatchListener afterListener;
		public IndexedAfterMatchListener(int _index, AfterMatchListener _afterListener) {
			index = _index;
			afterListener = _afterListener;
		}
	}
	public struct IndexAndResult {
		public int index;
		public EasyIncrGAnalysListener result;
	}
	public class ChainListener : IncrGAnalysisListener {
		public class PrvtAltAnlys : AlternativeIncrGAnalyzer {
			public List<IndexAndResult> results;
			public List<IncrementalGAnalyzer> analyzers;
			public Func<List<IndexAndResult>, AfterMatchListener> afterListenerFactory;
			void AlternativeIncrGAnalyzer.AnalyzeAgain(IncrGAnalysisListener listener) {
				var newResults = new List<IndexAndResult>(results);
				while (newResults.Count > 0) {
					var lastResult = newResults[newResults.Count-1];
					newResults.RemoveAt(newResults.Count - 1);
					var altLis = new ChainListener {
						finalListener = listener,
						index = lastResult.index,
						analyzers = analyzers,
						afterListenerFactory = afterListenerFactory,
						results = newResults
					};
					lastResult.result.alternative?.AnalyzeAgain(altLis);
					if (altLis.didFinalMatch) {
						return;
					}
				}
			}
		}
		public List<IndexAndResult> results;
		public int index;
		public IncrGAnalysisListener finalListener;
		public bool didFinalMatch = false;
		public List<IncrementalGAnalyzer> analyzers;
		public Func<List<IndexAndResult>, AfterMatchListener> afterListenerFactory;
		void IncrGAnalysisListener.OnMatch(GAnlysInput nextInput, AfterMatchListener afterListener, AlternativeIncrGAnalyzer alternative) {
			results.Add(new IndexAndResult { 
				index = index,
				result = new EasyIncrGAnalysListener { alternative = alternative, didMatch = true, listener = afterListener, nextInput = nextInput}
			});
			//last analyzer
			if (index == analyzers.Count - 1) {
				didFinalMatch = true;
				finalListener.OnMatch(nextInput, afterListenerFactory(new List<IndexAndResult>(results)), new PrvtAltAnlys { results = new List<IndexAndResult>(results), afterListenerFactory = afterListenerFactory, analyzers = analyzers});
			}
			//go to next analyzer
			else{
				var nextLis = new ChainListener { afterListenerFactory = afterListenerFactory, analyzers = analyzers, finalListener = finalListener, index = index + 1, results = results };
				analyzers[index+1].Analyze(nextInput, nextLis);
				didFinalMatch = nextLis.didFinalMatch;
				if (!nextLis.didFinalMatch){
					bool hasAlternative = false;
					foreach (var result in results) {
						if(result.result.alternative != null){
							hasAlternative = true;
							break;
						}
					}
					if (hasAlternative) {
						AlternativeIncrGAnalyzer alt = new PrvtAltAnlys { afterListenerFactory = afterListenerFactory, analyzers = analyzers, results = new List<IndexAndResult>(results) };
						alt.AnalyzeAgain(finalListener);
					}
				}
			}

		}
	}
	public class ChainListenerOld : IncrGAnalysisListener {
		public IncrGAnalysisListener rootListener;
		public List<IncrementalGAnalyzer> analyzers;
		public List<IndexedAfterMatchListener> previoudMatches;
		public Func<List<IndexedAfterMatchListener>, AfterMatchListener> afterListenerFactory;
		public int nextIndex = 0;
		void IncrGAnalysisListener.OnMatch(GAnlysInput nextInput, AfterMatchListener afterListener, AlternativeIncrGAnalyzer alternative) {
			List<IndexedAfterMatchListener> updatedMatches = new List<IndexedAfterMatchListener>(previoudMatches);
			updatedMatches.Add(new IndexedAfterMatchListener(nextIndex-1, afterListener));
			if (nextIndex < analyzers.Count) {
				var recordingListener = new RecordingIncrGAnalysListener {
					baseListener = new ChainListenerOld {
						analyzers = analyzers, nextIndex = nextIndex + 1, rootListener = rootListener, previoudMatches = updatedMatches, afterListenerFactory = afterListenerFactory
					}
				};
				analyzers[nextIndex].Analyze(nextInput, recordingListener);
				if (!recordingListener.didMatch && alternative != null) {
					updatedMatches.RemoveAt(updatedMatches.Count - 1);
					AlternativeIncrGAnalyzer currentAlternative = alternative;
					var altListener = new ChainListenerOld {
						analyzers = analyzers, nextIndex = nextIndex, rootListener = rootListener, previoudMatches = updatedMatches, afterListenerFactory = afterListenerFactory
					};
					alternative.AnalyzeAgain(altListener);
				}

			}
			else {
				//rootListener.OnMatch(nextInput, new ClusterAfterListener { afterListeners = updatedMatches });
				rootListener.OnMatch(nextInput, afterListenerFactory(updatedMatches), alternative);
			}
		}
	}
	public class ClusterAfterListener : AfterMatchListener {
		public List<IndexAndResult> afterListeners;
		void AfterMatchListener.OnResultRequested(Action<MutableGrammarBlock> blockTaker) {
			foreach (var afterListener in afterListeners) {
				afterListener.result.listener.OnResultRequested(blockTaker);
			}
		}
	}
	public class IGAnlys_Sequence : IncrementalGAnalyzer {
		public int sequenceCount => analyzers.Count;
		public List<IncrementalGAnalyzer> analyzers = new List<IncrementalGAnalyzer>();
		void IncrementalGAnalyzer.Analyze(GAnlysInput input, IncrGAnalysisListener listener) {
			analyzers[0].Analyze(
				input,
				new ChainListener {
					index = 0,
					results = new List<IndexAndResult>(),
					analyzers = analyzers,
					finalListener = listener,
					afterListenerFactory = DefaultAfterListener
				}
			);
		}
		static AfterMatchListener DefaultAfterListener(List<IndexAndResult> updatedMatches) {
			return new ClusterAfterListener { afterListeners = updatedMatches };
		}
	}
	#endregion
}
