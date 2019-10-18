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
			if(result.content != null) {
				listener.Take(result.content);
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
					nextIndex = 1,
					previoudMatches = new List<IndexedAfterMatchListener>(),
					analyzers = anaylzers,
					rootListener = listener,
					afterListenerFactory = CreateAfterListener
				}
			);
		}
		AfterMatchListener CreateAfterListener(List<IndexedAfterMatchListener> updatedMatches) {
			return new PrvtAfterMatchListener {
				parent = this,
				updatedMatches = updatedMatches
			};
			
		}
		public class PrvtAfterMatchListener : AfterMatchListener {
			public IGAnlys_ModifyBlock parent = null;
			public List<IndexedAfterMatchListener> updatedMatches;
			void AfterMatchListener.OnResultRequested(Action<MutableGrammarBlock> blockTaker) {
				MutableGrammarBlock baseBlock = null;
				List<GrammarBlock> modifiers = new List<GrammarBlock>();
				foreach (var afLIs in updatedMatches) {
					if (afLIs.index == 0 && parent.preMod != null)
						afLIs.afterListener.OnResultRequested((mod) => modifiers.Add(mod));
					else if ( (afLIs.index == 0 && parent.preMod == null) || (afLIs.index == 1 && parent.preMod != null)) {
						afLIs.afterListener.OnResultRequested((mgBlock) => baseBlock = mgBlock);
					}
					else if ((afLIs.index == 1 && parent.preMod == null) || (afLIs.index == 2 && parent.preMod != null)) {
						afLIs.afterListener.OnResultRequested((mod) => modifiers.Add(mod));
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
			foreach (var wordAnalyzer in wordAnalyzers) {
				if (wordAnalyzer.IsMatching(input)) {
					listener.OnMatch(input.GetAdvanced(wordAnalyzer.words.Count), wordAnalyzers[0]);
					return;
				}
			}
		}
	}
	public class IGAnlys_RepeatableBlock : IncrementalGAnalyzer {
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
				} else {
					resultArchives.Push(easyLis);
				}
			}
			List<AfterMatchListener> afLiss = new List<AfterMatchListener>();
			foreach(var result in resultArchives) {
				afLiss.Add(result.listener);
			}
			bool hasAlt = false;
			foreach (var result in resultArchives) {
				if (result.alternative != null)
					hasAlt = true;
			}
			AlternativeIncrGAnalyzer altLis = null;
			if(hasAlt) {
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
						new UnitAltAnlys { parent = parent, currentIndex = nextAnalyzerIndex, subAltAnalyzer = alternative, originalInput = originalInput });
				}
				else {
					if (nextAnalyzerIndex < parent.analyzers.Count) {
						clientListner.OnMatch(nextInput, listener, new UnitAltAnlys { parent = parent, currentIndex = nextAnalyzerIndex, originalInput = originalInput });
					}
					else {
						clientListner.OnMatch(nextInput, listener);
					}
				}
			}
		}
		public class UnitAltAnlys : AlternativeIncrGAnalyzer {
			public IGAnlys_Candidates parent;
			public int currentIndex;
			public GAnlysInput originalInput;

			public AlternativeIncrGAnalyzer subAltAnalyzer;

			void AlternativeIncrGAnalyzer.AnalyzeAgain(IncrGAnalysisListener listener) {
				var innerListener = new SubListener { clientListner = listener, parent = parent, nextAnalyzerIndex = currentIndex + 1, originalInput = originalInput };
				if (subAltAnalyzer != null)
					subAltAnalyzer.AnalyzeAgain(innerListener);
				while (!innerListener.didHit && currentIndex < parent.analyzers.Count) {
					parent.analyzers[currentIndex].Analyze(originalInput, innerListener);
					currentIndex++;
					innerListener.nextAnalyzerIndex++;
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
	public class ChainListener : IncrGAnalysisListener {
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
					baseListener = new ChainListener {
						analyzers = analyzers, nextIndex = nextIndex + 1, rootListener = rootListener, previoudMatches = updatedMatches, afterListenerFactory = afterListenerFactory
					}
				};
				analyzers[nextIndex].Analyze(nextInput, recordingListener);
				if (!recordingListener.didMatch && alternative != null) {
					updatedMatches.RemoveAt(updatedMatches.Count - 1);
					AlternativeIncrGAnalyzer currentAlternative = alternative;
					var altListener = new ChainListener {
						analyzers = analyzers, nextIndex = nextIndex, rootListener = rootListener, previoudMatches = updatedMatches, afterListenerFactory = afterListenerFactory
					};
					alternative.AnalyzeAgain(altListener);
				}

			}
			else {
				//rootListener.OnMatch(nextInput, new ClusterAfterListener { afterListeners = updatedMatches });
				rootListener.OnMatch(nextInput, afterListenerFactory(updatedMatches));
			}
		}
	}
	public class ClusterAfterListener : AfterMatchListener {
		public List<IndexedAfterMatchListener> afterListeners;
		void AfterMatchListener.OnResultRequested(Action<MutableGrammarBlock> blockTaker) {
			foreach (var afterListener in afterListeners) {
				afterListener.afterListener.OnResultRequested(blockTaker);
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
					nextIndex = 1,
					previoudMatches = new List<IndexedAfterMatchListener>(),
					analyzers = analyzers, rootListener = listener,
					afterListenerFactory = DefaultAfterListener
				}
			);
		}
		static AfterMatchListener DefaultAfterListener(List<IndexedAfterMatchListener> updatedMatches) {
			return new ClusterAfterListener { afterListeners = updatedMatches };
		}
	}
	#endregion
}
