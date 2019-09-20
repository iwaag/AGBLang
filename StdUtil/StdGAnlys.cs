using System;
using System.Collections.Generic;
using System.Text;
using AGBLang;
namespace AGDev.StdUtil {
	public class StdGrammarAnalyzer : GrammarAnalyzer {
		public IncrementalGAnalyzer incrGAnalyzer;
		void GrammarAnalyzer.AnalyzeGrammar(GAnlysInput input, AsyncCollector<GrammarBlock> listener) {
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
				listener.Collect(result.content);
			}
			
		}
	}
	#region grammar block
	public interface GrammarBlockVisitor {
		void IfHasMetaInfo(GrammarBlock meta);
		void IfGrammarUnit(GrammarUnit unit);
		void IfClusterGrammarBlock(ClusterGrammarBlock cluster);
		void IfHasModifier(GrammarBlock mod);
	}
	static class StdMetaInfos {
		public static readonly GrammarUnit sentenceCluster = new MinimumGBUnit { word = "SentenceCluster" };
		public static readonly GrammarUnit nominalBlock = new MinimumGBUnit { word = "NominalBlock" };
		public static readonly GrammarUnit verbalBlock = new MinimumGBUnit { word = "VerbalBlock" };
		public static readonly GrammarUnit quoteBlock = new MinimumGBUnit { word = "QuoteBlockMarker" };
		public static readonly GrammarUnit sv = new MinimumGBUnit { word = "SV" };
		public static readonly GrammarUnit conditionSV = new MinimumGBUnit { word = "ConditionSV" };
		public static readonly GrammarUnit negated = new MinimumGBUnit { word = "Negated" };
		public static readonly GrammarUnit title = new MinimumGBUnit { word = "Title" };
		public static readonly GrammarUnit clusterExtractable = new MinimumGBUnit { word = "ClusterExtractable" };

		public static readonly GrammarUnit metaCluster = new MinimumGBUnit { word = "MetaCluster" };
		public static readonly GrammarUnit anonymousCommand = new MinimumGBUnit { word = "AnonymousCommand" };
		public static readonly GrammarUnit modifierCluster = new MinimumGBUnit { word = "ModifierCluster" };
		public static readonly GrammarUnit quoteSV = new MinimumGBUnit { word = "QuoteSV" };
		public static readonly GrammarUnit pronoun = new MinimumGBUnit { word = "Pronoun" };
		public static readonly GrammarUnit plural = new MinimumGBUnit { word = "Plural" };
		public static readonly GrammarUnit unreadable = new MinimumGBUnit { word = "Unreadable" };
	}
	public class MinimumGBUnit : GrammarUnit {
		public string word;
		string GrammarUnit.word => word;
		GrammarUnit GrammarBlock.unit => this;
		ClusterGrammarBlock GrammarBlock.cluster => null;
		GrammarBlock GrammarBlock.modifier => null;
		GrammarBlock GrammarBlock.metaInfo => null;
	}
	public class StdBehaviorExpression : BehaviorExpression {
		public StdBehaviorExpression(GrammarBlock _subject, GrammarUnit _verb) {
			clusterGBlock = new StdMutableClusterGBlock();
			clusterGBlock.subBlocks.Add(_subject);
			clusterGBlock.subBlocks.Add(_verb);
			(clusterGBlock as MutableClusterGrammarBlock).AddModifier(StdMetaInfos.sv);
		}
		public StdMutableClusterGBlock clusterGBlock;
		GrammarBlock BehaviorExpression.subject { get { return clusterGBlock.subBlocks[0]; } }
		GrammarUnit BehaviorExpression.verb { get { return clusterGBlock.subBlocks[1].unit; } }
		GrammarBlock BehaviorExpression.asGBlock { get { return clusterGBlock; } }
	}
	public class StdMutableGUnit : MutableGrammarUnit {
		public string word;
		public StdMutableGUnit(string _word) => word = _word;
		public StdMutableGUnit() => word = "";
		ExpansiveMutableGBlock modifier;
		ExpansiveMutableGBlock metaInfo;
		MutableGrammarUnit MutableGrammarBlock.mUnit => this;
		MutableClusterGrammarBlock MutableGrammarBlock.mCluster => null;
		string GrammarUnit.word => word;
		GrammarUnit GrammarBlock.unit => this;
		ClusterGrammarBlock GrammarBlock.cluster => null;
		GrammarBlock GrammarBlock.modifier => modifier;
		GrammarBlock GrammarBlock.metaInfo => metaInfo;
		void MutableGrammarBlock.AddMetaInfo(GrammarBlock block) {
			if (metaInfo == null) {
				metaInfo = new ExpansiveMutableGBlock { metaForCluster = StdMetaInfos.metaCluster };
			}
			metaInfo.AddBlock(block);
		}
		void MutableGrammarBlock.AddModifier(GrammarBlock block) {
			if (modifier == null) {
				modifier = new ExpansiveMutableGBlock { metaForCluster = StdMetaInfos.modifierCluster };
			}
			modifier.AddBlock(block);
		}
		void MutableGrammarUnit.SetWord(string _word) {
			word = _word;
		}
	}
	public class StdMutableClusterGBlock : MutableClusterGrammarBlock {
		public List<GrammarBlock> subBlocks = new List<GrammarBlock>();
		ExpansiveMutableGBlock modifier;
		ExpansiveMutableGBlock metaInfo;
		MutableGrammarUnit MutableGrammarBlock.mUnit => null;
		MutableClusterGrammarBlock MutableGrammarBlock.mCluster => this;
		GrammarUnit GrammarBlock.unit => null;
		ClusterGrammarBlock GrammarBlock.cluster => this;
		GrammarBlock GrammarBlock.modifier => modifier;

		GrammarBlock GrammarBlock.metaInfo => metaInfo;

		IList<GrammarBlock> ClusterGrammarBlock.blocks => subBlocks;

		void MutableClusterGrammarBlock.AddBlock(GrammarBlock grammarBlock) {
			subBlocks.Add(grammarBlock);
		}

		void MutableGrammarBlock.AddMetaInfo(GrammarBlock block) {
			if (metaInfo == null) {
				metaInfo = new ExpansiveMutableGBlock { metaForCluster = StdMetaInfos .metaCluster };
			}
			metaInfo.AddBlock(block);
		}

		void MutableGrammarBlock.AddModifier(GrammarBlock block) {
			if (modifier == null) {
				modifier = new ExpansiveMutableGBlock { metaForCluster = StdMetaInfos.modifierCluster };
			}
			modifier.AddBlock(block);
		}
	}
	public class ExpansiveMutableGBlock : GrammarBlock {
		public GrammarBlock content {
			get {
				if (unit != null)
					return unit;
				else
					return cluster;
			}
		}
		public GrammarBlock metaForCluster;
		GrammarBlock modifier;
		GrammarBlock metaInfo;
		StdMutableClusterGBlock myCluster;
		List<GrammarBlock> blocks = new List<GrammarBlock>();
		ClusterGrammarBlock cluster;
		GrammarUnit unit;

		GrammarBlock GrammarBlock.modifier => modifier;

		GrammarBlock GrammarBlock.metaInfo => metaInfo;

		GrammarUnit GrammarBlock.unit => unit;

		ClusterGrammarBlock GrammarBlock.cluster => cluster;
		public void AddBlock(GrammarBlock gramarBlock) {
			if (GrammarBlockUtils.HasMetaInfo(gramarBlock, StdMetaInfos.metaCluster.word) || GrammarBlockUtils.HasMetaInfo(gramarBlock, StdMetaInfos.clusterExtractable.word)) {
				foreach (var subBlock in gramarBlock.cluster.blocks) {
					blocks.Add(subBlock);
				}
			}
			else
				blocks.Add(gramarBlock);
			if (blocks.Count == 1) {
				if (gramarBlock.unit != null)
					unit = gramarBlock.unit;
				else {
					cluster = gramarBlock.cluster;
				}
				modifier = gramarBlock.modifier;
				metaInfo = gramarBlock.metaInfo;
			}
			else {
				if (myCluster == null) {	
					myCluster = new StdMutableClusterGBlock();
					myCluster.subBlocks = blocks;
					cluster = myCluster;
					unit = null;
				}
				modifier = null;
				metaInfo = metaForCluster;

			}
		}
	}
	#endregion
	public class GrammarBlockUtils {
		public static void VisitGrammarBlock(GrammarBlock gBlock, GrammarBlockVisitor visitor) {
			if (gBlock.metaInfo != null) {
				visitor.IfHasMetaInfo(gBlock.metaInfo);
			}
			if (gBlock.unit != null) {
				visitor.IfGrammarUnit(gBlock.unit);
			} else if (gBlock.cluster != null) {
				visitor.IfClusterGrammarBlock(gBlock.cluster);
			}
			if (gBlock.modifier != null) {
				visitor.IfHasModifier(gBlock.modifier);
			}
		}
		public static BehaviorExpression GBlockToBExpression(GrammarBlock block) {
			return new StdBehaviorExpression(block.cluster.blocks[0], block.cluster.blocks[1].unit);
		}
		public static bool IsUnit(GrammarBlock block, string word) {
			if (block == null)
				return false;
			if (block.unit != null) {
				return string.Compare(block.unit.word, word, true) == 0;
			}
			return false;
		}
		public static bool IsUnit(GrammarBlock block, System.Func<string, bool> checkUnit) {
			if (block == null)
				return false;
			if (block.unit != null) {
				return checkUnit(block.unit.word);
			}
			return false;
		}
		public static GrammarBlock ShallowSeek(GrammarBlock block, System.Func<string, bool> checkUnit) {
			if (block == null)
				return null;
			if (IsUnit(block, checkUnit))
				return block;
			else {
				if (block.cluster != null) {
					foreach (var element in block.cluster.blocks) {
						if (IsUnit(element, checkUnit))
							return element;
					}
				}
			}
			return null;
		}
		public static GrammarBlock ShallowSeek(GrammarBlock block, string word) {
			if (block == null)
				return null;
			if (IsUnit(block, word))
				return block;
			else {
				if (block.cluster != null) {
					foreach (var element in block.cluster.blocks) {
						if (IsUnit(element, word))
							return element;
					}
				}
			}
			return null;
		}
		public static GrammarBlock ShallowSeekByMetaInfo(GrammarBlock block, string meta, string clusterToSeek = null) {
			if (block == null)
				return null;
			if (block.metaInfo != null) {
				var found = ShallowSeek(block.metaInfo, meta);
				if (found != null) {
					return block;
				}
			}
			
			if (block.cluster != null) {
				if (clusterToSeek == null || HasMetaInfo(block, clusterToSeek)) {
					foreach (var element in block.cluster.blocks) {
						if (element.metaInfo != null) {
							var found = ShallowSeek(element.metaInfo, meta);
							if (found != null) {
								return element;
							}
						}
					}
				}
			}
			return null;
		}
		public static bool HasMetaInfo(GrammarBlock gBlock, string metaInfo) {
			if (string.IsNullOrEmpty(metaInfo))
				return false;
			if (gBlock != null ? gBlock.metaInfo != null : false) {
				return ShallowSeek(gBlock.metaInfo, metaInfo) != null;
			}
			return false;
		}
		public static GrammarBlock ShallowSeekModifier(GrammarBlock block, string matchWord) {
			if (block != null ? block.modifier != null : false) {
				return ShallowSeek(block.modifier, matchWord);
			}
			return null;
		}
		public static void DeepForEachUnit(GrammarBlock gBlock, System.Action<GrammarUnit> func, System.Func<GrammarBlock, GrammarBlock, bool> filter, GrammarBlock parent = null) {
			if (gBlock == null)
				return;
			if (filter(gBlock, parent)) {
				if(gBlock.unit != null) {
					func(gBlock.unit);
				}
				else if (gBlock.cluster != null) {
					foreach (var subBlock in gBlock.cluster.blocks) {
						if (filter(subBlock, gBlock)) {
							DeepForEachUnit(subBlock, func, filter, parent);
						}
					}
				}
			}
		}
		public static void DeepForEachBlockUnit(GrammarBlock gBlock, System.Action<GrammarUnit> func, string blockMeta, GrammarBlock parent = null) {
			if (gBlock == null)
				return;
			if (CheckBlock(gBlock, blockMeta, parent)) {
				if (gBlock.unit != null) {
					func(gBlock.unit);
				} else if (gBlock.cluster != null) {
					foreach (var subBlock in gBlock.cluster.blocks) {
						if (CheckBlock(subBlock, blockMeta, gBlock)) {
							DeepForEachBlockUnit(subBlock, func, blockMeta, gBlock);
						}
					}
				}
			}
		}
		public static bool CheckBlock(GrammarBlock gBlock, string blockMeta, GrammarBlock parent = null ) {
			//if(GrammarBlockUtils.HasMetaInfo(gBlock, StdMetaInfos.modifierCluster.word))
			if (gBlock.cluster != null)
				return GrammarBlockUtils.HasMetaInfo(gBlock, blockMeta);
			if (gBlock.unit != null) {
				if (GrammarBlockUtils.HasMetaInfo(gBlock, blockMeta))
					return true;
				if(parent != null) {
					return GrammarBlockUtils.HasMetaInfo(parent, blockMeta);
				}
			}
			return false;
		}
		public static void ShallowForEachUnits(GrammarBlock gBlock, System.Action<GrammarUnit> func, string clusterToSeek = null) {
			if (gBlock == null)
				return;
			if (gBlock.cluster != null) {
				if (clusterToSeek == null || HasMetaInfo(gBlock, clusterToSeek)) {
					foreach (var sub in gBlock.cluster.blocks) {
						if (sub.unit != null)
							func(sub.unit);
					}
				}
			} else {
				func(gBlock.unit);
			}
		}
		public static void ForEachUnitsDeep(GrammarBlock gBlock, System.Action<GrammarUnit> func, string clusterToSeek = null) {
			if (gBlock == null)
				return;
			if (clusterToSeek == null || HasMetaInfo(gBlock, clusterToSeek)) {
				if (gBlock.cluster != null) {
					foreach (var sub in gBlock.cluster.blocks) {
						ForEachUnitsDeep(sub, func, clusterToSeek);
					}
				} else {
					func(gBlock.unit);
				}
			}
		}
		public static void ForEach(GrammarBlock gBlock, string metaInfo, System.Action<GrammarBlock> func, string clusterToSeek = null) {
			if (HasMetaInfo(gBlock, metaInfo)) {
				func(gBlock);
			}
			if (clusterToSeek == null || HasMetaInfo(gBlock, clusterToSeek)) {
				if (gBlock.cluster != null) {
					foreach (var sub in gBlock.cluster.blocks) {
						if (HasMetaInfo(sub, metaInfo)) {
							func(sub);
						}
					}
				}
			}
		}
		public static GrammarBlock GetPrepositoinContent(GrammarBlock block, string preposition) {
			var prepositoinBlock = ShallowSeekModifier(block, preposition);
			return prepositoinBlock.modifier;
		}



		public static void ForEachUnits(GrammarBlock gBlock, System.Action<GrammarUnit> func)
		{
			if (gBlock == null)
				return;
			if (gBlock.cluster != null)
			{
				foreach (var sub in gBlock.cluster.blocks)
				{
					ForEachUnits(sub, func);
				}
			}
			else
			{
				func(gBlock.unit);
			}
		}
		public static void ForEach(GrammarBlock gBlock, string metaInfo, System.Action<GrammarBlock> func)
		{
			if (HasMetaInfo(gBlock, metaInfo))
			{
				func(gBlock);
			}
			if (gBlock.cluster != null)
			{
				foreach (var sub in gBlock.cluster.blocks)
				{
					if (HasMetaInfo(sub, metaInfo))
					{
						func(sub);
					}
				}
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
			void AfterMatchListener.OnResultRequested(Action<MutableGrammarBlock> blockCollector) {
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
					blockCollector(baseBlock);
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
			void AfterMatchListener.OnResultRequested(Action<MutableGrammarBlock> blockCollector) {
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
		void AfterMatchListener.OnResultRequested(Action<MutableGrammarBlock> blockCollector) {}
	}
	public class IGAnlys_AddMeta : ResultChangingIGAnalyzer {
		public List<GrammarBlock> metas = new List<GrammarBlock>();
		public override AfterMatchListener ChangeAfterListener(AfterMatchListener sourceAfterListener) {
			return new AddMetaAMatchListener { baseListener = sourceAfterListener, parent = this };
		}
		public class AddMetaAMatchListener : AfterMatchListener {
			public IGAnlys_AddMeta parent;
			public AfterMatchListener baseListener;
			void AfterMatchListener.OnResultRequested(Action<MutableGrammarBlock> blockCollector) {
				baseListener.OnResultRequested(
					(gBlock) => {
						foreach (var meta in parent.metas) {
							gBlock.AddMetaInfo(meta);
						}
						blockCollector(gBlock);
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
		void AfterMatchListener.OnResultRequested(Action<MutableGrammarBlock> blockCollector) {
			var mClusterGBlock = new StdMutableClusterGBlock();
			MutableGrammarBlock lastBlock = null;
			baseAMatchLis.OnResultRequested(
				(gBlock) => mClusterGBlock.subBlocks.Add(lastBlock = gBlock)
			);
			if (mClusterGBlock.subBlocks.Count == 1)
				blockCollector(lastBlock);
			else if (mClusterGBlock.subBlocks.Count > 1)
				blockCollector(mClusterGBlock);
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
		void AfterMatchListener.OnResultRequested(Action<MutableGrammarBlock> blockCollector) {
			blockCollector(new StdMutableGUnit { word = integrated });
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
			void AfterMatchListener.OnResultRequested(Action<MutableGrammarBlock> blockCollector) {
				foreach (var listener in listeners) {
					listener.OnResultRequested(blockCollector);
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
			void AfterMatchListener.OnResultRequested(Action<MutableGrammarBlock> blockCollector) {
				blockCollector(new StdMutableGUnit { word = unit.word });
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
			void AfterMatchListener.OnResultRequested(Action<MutableGrammarBlock> blockCollector) {
				if(gBlocks.Count == 1) {
					gBlocks[0].AddMetaInfo(StdMetaInfos.unreadable);
					blockCollector(gBlocks[0]);
				}
				else if (gBlocks.Count > 1) {
					var cluster = new StdMutableClusterGBlock();
					foreach (var block in gBlocks) {
						cluster.subBlocks.Add(block);
					}
					(cluster as MutableGrammarBlock).AddMetaInfo(StdMetaInfos.unreadable);
					blockCollector(cluster);
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
		void AfterMatchListener.OnResultRequested(Action<MutableGrammarBlock> blockCollector) {
			foreach (var afterListener in afterListeners) {
				afterListener.afterListener.OnResultRequested(blockCollector);
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
