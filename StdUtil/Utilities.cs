using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using AGDev;

namespace AGBLang.StdUtil {
	public class ClusterBehaviorSetter : BehaviorSetter {
		public IEnumerable<BehaviorSetter> bSetters;
		BehaviorTrigger BehaviorSetter.ReadyBehavior(BehaviorExpression gBLock, BehaviorReadySupport support) {
			return BehaviorUtiles.BuildBTrigger(bSetters, gBLock, support);
		}
	}
	public class ClusterBehaviorTrigger : BehaviorTrigger {
		public IList<BehaviorTrigger> triggers;
		void BehaviorTrigger.BeginBehavior(BehaviorListener behaviorListener) {
			var outerListener = new BehaviorListenerForCluster { clientListener = behaviorListener, goalCount = triggers.Count };
			foreach (var trigger in triggers) {
				trigger.BeginBehavior(outerListener);
			}
		}
		class BehaviorListenerForCluster : BehaviorListener {
			public int goalCount;
			public int currentCount = 0;
			public BehaviorListener clientListener;
			void BehaviorListener.OnFinish() {
				currentCount++;
				if (currentCount == goalCount) {
					clientListener.OnFinish();
				}
			}
		}
	}
	public class ClusterBehaviorChecker : BehaviorChecker {
		public IEnumerable<BehaviorChecker> bCheckers;
		BehaviorCheckTrigger BehaviorChecker.ReadyCheckBehavior(BehaviorExpression gBLock, BehaviorReadySupport support) {
			return BehaviorUtiles.BuildBCheckTrigger(bCheckers, gBLock, support);
		}
		
	}
	public class ClusterBehaviorCheckTrigger : BehaviorCheckTrigger {
		public IEnumerable<BehaviorCheckTrigger> triggers;
		void BehaviorCheckTrigger.BeginBehavior(BehaviorCheckListener BehaviorCheckListener) {
			var outerListener = new BehaviorCheckListenerForCluster { clientListener = BehaviorCheckListener };
			foreach (var trigger in triggers) {
				trigger.BeginBehavior(outerListener);
			}
		}
		class BehaviorCheckListenerForCluster : BehaviorCheckListener {
			bool didResult = false;
			public BehaviorCheckListener clientListener;
			void BehaviorCheckListener.OnResultInPositive() {
				if (!didResult) {
					didResult = true;
					clientListener.OnResultInPositive();
				}
			}

			void BehaviorCheckListener.OnResultInNegative() {
				if (!didResult) {
					didResult = true;
					clientListener.OnResultInNegative();
				}
			}
		}
	}
	#region grammar block
	public class JsonGrammarBlockVisitor : GrammarBlockVisitor {
		public StringBuilder builder;
		public bool isFirstElement = true;
		void GrammarBlockVisitor.IfClusterGrammarBlock(ClusterGrammarBlock cluster) {
			if (!isFirstElement)
				builder.Append(",");
			isFirstElement = false;
			builder.Append("\"cluster\":[");
			int clusterIndex = 0;
			foreach (var subBlock in cluster.blocks) {
				if (clusterIndex > 0)
					builder.Append(",");
				builder.Append("{");
				GrammarBlockUtils.VisitGrammarBlock(subBlock, new JsonGrammarBlockVisitor { builder = builder });
				builder.Append("}");
				clusterIndex++;
			}
			builder.Append("]");
		}

		void GrammarBlockVisitor.IfGrammarUnit(GrammarUnit unit) {
			if (!isFirstElement)
				builder.Append(",");
			isFirstElement = false;
			builder.Append("\"unit\":\"" + unit.word + "\"");
		}

		void GrammarBlockVisitor.IfHasMetaInfo(GrammarBlock meta) {
			if (!isFirstElement)
				builder.Append(",");
			isFirstElement = false;
			builder.Append("\"meta\":{");
			GrammarBlockUtils.VisitGrammarBlock(meta, new JsonGrammarBlockVisitor { builder = builder });
			builder.Append("}");
		}

		void GrammarBlockVisitor.IfHasModifier(GrammarBlock mod) {
			if (!isFirstElement)
				builder.Append(",");
			isFirstElement = false;
			builder.Append("\"mod\":{");
			GrammarBlockUtils.VisitGrammarBlock(mod, new JsonGrammarBlockVisitor { builder = builder });
			builder.Append("}");
		}
	}
	public class GrammarBlockUtils {
		public static string ToJson(GrammarBlock gBlock) {
			var resultBuilder = new StringBuilder();
			resultBuilder.Append("{");
			VisitGrammarBlock(gBlock, new JsonGrammarBlockVisitor { builder = resultBuilder });
			resultBuilder.Append("}");
			return resultBuilder.ToString();
		}
		public static void VisitGrammarBlock(GrammarBlock gBlock, GrammarBlockVisitor visitor) {
			if (gBlock == null)
				return;
			if (gBlock.metaInfo != null) {
				visitor.IfHasMetaInfo(gBlock.metaInfo);
			}
			if (gBlock.unit != null) {
				visitor.IfGrammarUnit(gBlock.unit);
			}
			else if (gBlock.cluster != null) {
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
				if (gBlock.unit != null) {
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
				}
				else if (gBlock.cluster != null) {
					foreach (var subBlock in gBlock.cluster.blocks) {
						if (CheckBlock(subBlock, blockMeta, gBlock)) {
							DeepForEachBlockUnit(subBlock, func, blockMeta, gBlock);
						}
					}
				}
			}
		}
		public static bool CheckBlock(GrammarBlock gBlock, string blockMeta, GrammarBlock parent = null) {
			//if(GrammarBlockUtils.HasMetaInfo(gBlock, StdMetaInfos.modifierCluster.word))
			if (gBlock.cluster != null)
				return GrammarBlockUtils.HasMetaInfo(gBlock, blockMeta);
			if (gBlock.unit != null) {
				if (GrammarBlockUtils.HasMetaInfo(gBlock, blockMeta))
					return true;
				if (parent != null) {
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
			}
			else {
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
				}
				else {
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
		public static GrammarBlock GetPrepositoinContent(GrammarBlock block, string preposition, System.Action<GrammarBlock> func) {
			var prepositoinBlock = ShallowSeekModifier(block, preposition);
			return prepositoinBlock.modifier;
		}
		public static void DeepSeek(GrammarBlock block, string meta, System.Action<GrammarBlock> func, bool includeModifier = false) {
			if (HasMetaInfo(block, meta)) {
				func(block);
			}
			if (includeModifier && block.modifier != null) {
				DeepSeek(block.modifier, meta, func, includeModifier);
			}
			if (block.cluster != null){
				foreach (var subBlock in block.cluster.blocks) {
					DeepSeek(subBlock, meta, func, includeModifier);
				}
			}
			
		}


		public static void ForEachUnits(GrammarBlock gBlock, System.Action<GrammarUnit> func) {
			if (gBlock == null)
				return;
			if (gBlock.cluster != null) {
				foreach (var sub in gBlock.cluster.blocks) {
					ForEachUnits(sub, func);
				}
			}
			else {
				func(gBlock.unit);
			}
		}
		public static void ForEach(GrammarBlock gBlock, string metaInfo, System.Action<GrammarBlock> func) {
			if (HasMetaInfo(gBlock, metaInfo)) {
				func(gBlock);
			}
			if (gBlock.cluster != null) {
				foreach (var sub in gBlock.cluster.blocks) {
					if (HasMetaInfo(sub, metaInfo)) {
						func(sub);
					}
				}
			}
		}
	}
	public static class BehaviorUtiles {
		public static BehaviorTrigger BuildBTrigger(IEnumerable<BehaviorSetter> setters, BehaviorExpression expression, BehaviorReadySupport support) {
			List<BehaviorTrigger> triggers = new List<BehaviorTrigger>();
			foreach (var bChecker in setters) {
				var trigger = bChecker.ReadyBehavior(expression, support);
				if (trigger != null) {
					triggers.Add(trigger);
				}
			}
			if (triggers.Count == 1)
				return triggers[0];
			if (triggers.Count > 0)
				return new ClusterBehaviorTrigger { triggers = triggers };
			return null;
		}
		public static BehaviorCheckTrigger BuildBCheckTrigger(IEnumerable<BehaviorChecker> bCheckers, BehaviorExpression expression, BehaviorReadySupport support) {
			List<BehaviorCheckTrigger> triggers = new List<BehaviorCheckTrigger>();
			foreach (var bChecker in bCheckers) {
				var trigger = bChecker.ReadyCheckBehavior(expression, support);
				if (trigger != null) {
					triggers.Add(trigger);
				}
			}
			if (triggers.Count == 1)
				return triggers[0];
			if (triggers.Count > 0)
				return new ClusterBehaviorCheckTrigger { triggers = triggers };
			return null;
		}
	}
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
		public static readonly GrammarUnit passive = new MinimumGBUnit { word = "Passive" };
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
	}
	public class GBlockBuilder
	{
		public string word = "";
		public List<GBlockBuilder> blocks = new List<GBlockBuilder>();
		public List<GrammarUnit> metaInfos = new List<GrammarUnit>();
		public List<GBlockBuilder> modifiers = new List<GBlockBuilder>();
		public MutableGrammarBlock Build() {
			MutableGrammarBlock mainBlock = null;
			if (blocks.Count == 0) {
				if (string.IsNullOrEmpty(word))
					return null;
				mainBlock = new StdMutableGUnit(word);
				
			} else if (blocks.Count == 1) {
				mainBlock = blocks[0].Build();
			} else if (blocks.Count > 1) {
				var mainCluster = new StdMutableClusterGBlock();
				foreach (var subBlock in blocks) {
					(mainCluster as MutableClusterGrammarBlock).AddBlock(subBlock.Build());
				}
				mainBlock = mainCluster;
			}
			foreach (var meta in metaInfos) {
				mainBlock.AddMetaInfo(meta);
			}
			foreach (var mod in modifiers) {
				mainBlock.AddModifier(mod.Build());
			}
			return mainBlock;

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
	[System.Serializable]
	[DataContract]
	public class DeserializedGBlock : GrammarBlock, GrammarUnit, ClusterGrammarBlock {
		[DataMember]
		public SDeserializedGBlock mod;
		[DataMember]
		public SDeserializedGBlock meta;
		[DataMember]
		public List<DeserializedGBlock> cluster;
		public List<GrammarBlock> blocks;
		[DataMember]
		public string unit;
		GrammarUnit GrammarBlock.unit => unit != null ? this : null;
		ClusterGrammarBlock GrammarBlock.cluster => cluster != null ? this : null;
		GrammarBlock GrammarBlock.modifier => mod;
		GrammarBlock GrammarBlock.metaInfo => meta;
		string GrammarUnit.word => unit;
		IList<GrammarBlock> ClusterGrammarBlock.blocks {
			get {
				if (blocks == null)
					blocks = new List<GrammarBlock>(cluster);
				return blocks;
			}
		}
	}
	[System.Serializable]
	public class SDeserializedGBlock : DeserializedGBlock { }
	public class StdGrammarUnit : GrammarUnit {
		public string m_words;
		public GrammarBlock meta;
		public GrammarBlock mod;
		public StdGrammarUnit(string str) {
			m_words = str;
		}
		GrammarUnit GrammarBlock.unit => this;
		ClusterGrammarBlock GrammarBlock.cluster { get { return null; } }
		GrammarBlock GrammarBlock.modifier { get { return mod; } }
		GrammarBlock GrammarBlock.metaInfo { get { return meta; } }
		string GrammarUnit.word { get { return m_words; } }
	}
	public class StdClusterGrammarBlock : ClusterGrammarBlock {
		public List<GrammarBlock> blocks = new List<GrammarBlock>();
		public GrammarBlock meta;
		public GrammarBlock mod;
		GrammarUnit GrammarBlock.unit { get { return null; } }
		ClusterGrammarBlock GrammarBlock.cluster => this;
		GrammarBlock GrammarBlock.modifier { get { return mod; } }
		GrammarBlock GrammarBlock.metaInfo { get { return meta; } }
		IList<GrammarBlock> ClusterGrammarBlock.blocks { get { return blocks; } }
	}
	public class MetaInfoDependentGrammarBlockVisitor : GrammarBlockVisitor {
		public Dictionary<string, GrammarBlockVisitor> metaToVis = new Dictionary<string, GrammarBlockVisitor>();
		public GrammarBlockVisitor subVisitor;
		public bool doDeepSeek = false;
		public bool doDeepSeekModifier = false;
		void GrammarBlockVisitor.IfClusterGrammarBlock(ClusterGrammarBlock cluster) {
			if (subVisitor != null)
				subVisitor.IfClusterGrammarBlock(cluster);
			else if (doDeepSeek) {
				foreach (var block in cluster.blocks) {
					var subVis = new MetaInfoDependentGrammarBlockVisitor { metaToVis = metaToVis, doDeepSeek = doDeepSeek, doDeepSeekModifier = doDeepSeekModifier };
					GrammarBlockUtils.VisitGrammarBlock(block, subVis);
				}
			}
		}

		void GrammarBlockVisitor.IfGrammarUnit(GrammarUnit unit) {
			if (subVisitor != null)
				subVisitor.IfGrammarUnit(unit);

		}

		void GrammarBlockVisitor.IfHasMetaInfo(GrammarBlock meta) {
			foreach (var pair in metaToVis) {
				if (GrammarBlockUtils.ShallowSeek(meta, pair.Key) != null) {
					subVisitor = pair.Value;
					break;
				}
			}
		}

		void GrammarBlockVisitor.IfHasModifier(GrammarBlock mod) {
			if (subVisitor != null)
				subVisitor.IfHasModifier(mod);
			else if (doDeepSeekModifier) {
				var subVis = new MetaInfoDependentGrammarBlockVisitor { metaToVis = metaToVis, doDeepSeek = doDeepSeek, doDeepSeekModifier = doDeepSeekModifier };
				GrammarBlockUtils.VisitGrammarBlock(mod, subVis);
			}
		}
	}
	class SVSentenceVisitor : GrammarBlockVisitor {
		public BehaviorSetCheck behaverSetCheck;
		public Taker<BehaviorTrigger> triggerColl;
		public BehaviorReadySupport support;
		void GrammarBlockVisitor.IfHasMetaInfo(GrammarBlock meta) { }

		void GrammarBlockVisitor.IfGrammarUnit(GrammarUnit unit) { }

		void GrammarBlockVisitor.IfClusterGrammarBlock(ClusterGrammarBlock cluster) {
			var vReader = new VerbalUnitReader { behaverSetCheck = behaverSetCheck, subjectBlock = cluster.blocks[0], bhvrTrgColl = triggerColl, support = support };
			GrammarBlockUtils.VisitGrammarBlock(cluster.blocks[1], vReader);
		}

		void GrammarBlockVisitor.IfHasModifier(GrammarBlock mod) {
		}
		public class VerbalUnitReader : GrammarBlockVisitor {
			public BehaviorSetCheck behaverSetCheck;
			public GrammarBlock subjectBlock;
			public Taker<BehaviorTrigger> bhvrTrgColl;
			public BehaviorReadySupport support;
			void GrammarBlockVisitor.IfHasMetaInfo(GrammarBlock meta) { }
			void GrammarBlockVisitor.IfGrammarUnit(GrammarUnit unit) {
				var behaviorExpression = new StdBehaviorExpression(subjectBlock, unit);
				behaverSetCheck.ReadyBehavior(behaviorExpression, support);
			}

			void GrammarBlockVisitor.IfClusterGrammarBlock(ClusterGrammarBlock cluster) {
				foreach (var block in cluster.blocks) {
					GrammarBlockUtils.VisitGrammarBlock(block, this);
				}
			}

			void GrammarBlockVisitor.IfHasModifier(GrammarBlock mod) { }
		}
	}
	class OR_BehaviorCheckTrigger : BehaviorCheckTrigger, Taker<BehaviorCheckTrigger> {
		public List<BehaviorCheckTrigger> triggers = new List<BehaviorCheckTrigger>();
		void BehaviorCheckTrigger.BeginBehavior(BehaviorCheckListener behaviorListener) {
			var listener = new JustOnceBCheckListener{ listener = behaviorListener };
			foreach (var trigger in triggers) {
				trigger.BeginBehavior(listener);
			}
		}

		void Taker<BehaviorCheckTrigger>.None() {}
		void Taker<BehaviorCheckTrigger>.Take(BehaviorCheckTrigger item) {
			triggers.Add(item);
		}
		public class JustOnceBCheckListener : BehaviorCheckListener {
			public BehaviorCheckListener listener;
			public bool didDecidedResult = false;
			void BehaviorCheckListener.OnResultInNegative() {
				//stub
			}

			void BehaviorCheckListener.OnResultInPositive() {
				if (!didDecidedResult) {
					didDecidedResult = true;
					listener.OnResultInPositive();
				}
			}
		}
	}
	public class ConditionalSVVisitor : GrammarBlockVisitor {
		public BehaviorSetCheck behaverSetCheck;
		public CompositeBehaviorTrigger givenTrigger;
		public CompositeBehaviorTrigger nextCompositTrigger;
		public BehaviorReadySupport support;
		void GrammarBlockVisitor.IfClusterGrammarBlock(ClusterGrammarBlock cluster) {
			var orCheckTrigger = new OR_BehaviorCheckTrigger { };
			var vReader = new ConditionalVerbalUnitReader { behaverSetCheck = behaverSetCheck, subjectBlock = cluster.blocks[0], support = support };
			GrammarBlockUtils.VisitGrammarBlock(cluster.blocks[1], vReader);
			if (orCheckTrigger.triggers.Count > 1) {
				var followingBehavior = new StdCompositeBehaviorTrigger();
				nextCompositTrigger = followingBehavior;
				givenTrigger.Take(new BTriggerForBCheck { checkTrigger = orCheckTrigger, followingBehavior = followingBehavior });
			}
			else if (orCheckTrigger.triggers.Count == 1) {
				var followingBehavior = new StdCompositeBehaviorTrigger();
				nextCompositTrigger = followingBehavior;
				givenTrigger.Take(new BTriggerForBCheck { checkTrigger = orCheckTrigger, followingBehavior = followingBehavior });

			}
		}
		void GrammarBlockVisitor.IfGrammarUnit(GrammarUnit verbalUnit) { }
		void GrammarBlockVisitor.IfHasModifier(GrammarBlock modifier) { }
		void GrammarBlockVisitor.IfHasMetaInfo(GrammarBlock metaInfo) { }
		public class ConditionalVerbalUnitReader : GrammarBlockVisitor {
			public BehaviorSetCheck behaverSetCheck;
			public GrammarBlock subjectBlock;
			public BehaviorReadySupport support;
			public Taker<BehaviorCheckTrigger> bcTriggers;
			void GrammarBlockVisitor.IfHasMetaInfo(GrammarBlock meta) { }
			void GrammarBlockVisitor.IfGrammarUnit(GrammarUnit verbalUnit) {
				var bExpression = new StdBehaviorExpression(subjectBlock, verbalUnit);
				var trigger = behaverSetCheck.ReadyCheckBehavior(bExpression, support);
				if (GrammarBlockUtils.ShallowSeek(verbalUnit.metaInfo, StdMetaInfos.negated.word) != null) {
					bcTriggers.Take(new NegatingBCheckTrigger { clientTrigger = trigger });
				}
				else
					bcTriggers.Take(trigger);
			}

			void GrammarBlockVisitor.IfClusterGrammarBlock(ClusterGrammarBlock cluster) {
				foreach (var block in cluster.blocks) {
					GrammarBlockUtils.VisitGrammarBlock(block, this);
				}
			}

			void GrammarBlockVisitor.IfHasModifier(GrammarBlock mod) { }
			public class NegatingBCheckTrigger : BehaviorCheckTrigger {
				public BehaviorCheckTrigger clientTrigger;
				void BehaviorCheckTrigger.BeginBehavior(BehaviorCheckListener checkListener) {
					clientTrigger.BeginBehavior(new NegatingBehaviorCheckListener { clientListener = checkListener });
				}
				class NegatingBehaviorCheckListener : BehaviorCheckListener {
					public BehaviorCheckListener clientListener;
					void BehaviorCheckListener.OnResultInPositive() { clientListener.OnResultInNegative(); }
					void BehaviorCheckListener.OnResultInNegative() { clientListener.OnResultInPositive(); }
				}
			};
		}
	}
	class SentenceBlockRecursiveProcessor {
		public Dictionary<string, CompositeBehaviorTrigger> namedCBTriggers;
		public BehaviorSetCheck behaverSetCheck;
		public CompositeBehaviorTrigger subSentenceBehaviorTaker;
		public CompositeBehaviorTrigger followingSentenceBehaviorManagedTaker;
		public BehaviorReadySupport support;
		public bool doResetPreviousTrigger = false;
		public void GrammarBlockCommon(GrammarBlock block) {
			//Title
			if (GrammarBlockUtils.ShallowSeek(block.metaInfo, StdMetaInfos.title.word) != null) {
				if (block.unit != null) {
					followingSentenceBehaviorManagedTaker = subSentenceBehaviorTaker = new StdCompositeBehaviorTrigger();
					namedCBTriggers.Add(block.unit.word, followingSentenceBehaviorManagedTaker);
				}
				return;
			}
			//wait
			{
				if (GrammarBlockUtils.ShallowSeekModifier(block, "then") != null) {
					subSentenceBehaviorTaker = subSentenceBehaviorTaker.AddWaitTrigger();
				}
			}
			//read conditional SV
			if (block.modifier != null) {
				var conditionSVVisitor = new ConditionalSVVisitor { behaverSetCheck = behaverSetCheck, givenTrigger = subSentenceBehaviorTaker, support = support };
				var rootVisitor = new MetaInfoDependentGrammarBlockVisitor();
				rootVisitor.metaToVis.Add(StdMetaInfos.conditionSV.word, conditionSVVisitor);
				GrammarBlockUtils.VisitGrammarBlock(block.modifier, rootVisitor);
				if (conditionSVVisitor.nextCompositTrigger != null) {
					subSentenceBehaviorTaker = conditionSVVisitor.nextCompositTrigger;
				}
			}
			//read main SV
			{
				if (block.cluster != null) {
					if (GrammarBlockUtils.ShallowSeek(block.metaInfo, StdMetaInfos.sentenceCluster.word) != null) {
						foreach (var subBlock in block.cluster.blocks) {
							var subProcessor = new SentenceBlockRecursiveProcessor { behaverSetCheck = behaverSetCheck, subSentenceBehaviorTaker = subSentenceBehaviorTaker, namedCBTriggers = namedCBTriggers, support = support };
							subProcessor.GrammarBlockCommon(subBlock);
							if (subProcessor.followingSentenceBehaviorManagedTaker != null) {
								subSentenceBehaviorTaker = subProcessor.followingSentenceBehaviorManagedTaker;
							}
						}
					}
				}
				if (GrammarBlockUtils.ShallowSeek(block.metaInfo, StdMetaInfos.sv.word) != null) {
					var svVisitor = new SVSentenceVisitor { behaverSetCheck = behaverSetCheck, triggerColl = subSentenceBehaviorTaker, support = support };
					GrammarBlockUtils.VisitGrammarBlock(block, svVisitor);
				}
			}
		}
	}
	#endregion
	#region behavior
	public class StdBehaviorReadySupport : BehaviorReadySupport {
		public AssetMediator assetMediator;
		AssetMediator BehaviorReadySupport.assetMediator => assetMediator;
	}
	public class StubAssetMediator : AssetMediator {
		AssetType AssetMediator.GetImplementedAsset<AssetType>(GrammarBlock gBlock) {
			return default;
		}

		IEnumerable<AssetType> AssetMediator.GetImplementedAssets<AssetType>() {
			return default;
		}

		AssetType AssetMediator.GetImplementedModule<AssetType>() {
			return default;
		}

		void AssetMediator.SeekAsset<AssetType>(GrammarBlock gBlock, Taker<AssetType> taker) {
			taker.None();
		}

		void AssetMediator.SeekModule<AssetType>(Taker<AssetType> taker) {
			taker.None();
		}
	}
	public class StdBehaverGiver : ImmediateGiver<Behaver, GrammarBlock> {
		public ImmediateGiver<Behaver, GrammarBlock> clientBehaverGiver;
		public List<Behaver> behavers = new List<Behaver>();
		Behaver ImmediateGiver<Behaver, GrammarBlock>.PickBestElement(GrammarBlock key) {
			var foundBehaver = behavers.Find((behaver) => behaver.MatchAttribue(key) == AttributeMatchResult.POSITIVE);
			return foundBehaver != null ? foundBehaver : clientBehaverGiver.PickBestElement(key);
		}
	}
	public class StubBehaviorTrigger : BehaviorTrigger {
		void BehaviorTrigger.BeginBehavior(BehaviorListener behaviorListener) {
			behaviorListener.OnFinish();
		}
	}
	public class StdBSetCheck : BehaviorSetCheck {
		public ImmediateGiver<Behaver, GrammarBlock> behaverGiver;
		public List<BehaviorChecker> checkers= new List<BehaviorChecker>();
		public List<BehaviorSetter> setters = new List<BehaviorSetter>();
		BehaviorTrigger BehaviorSetter.ReadyBehavior(BehaviorExpression bExpr, BehaviorReadySupport support) {
			return BehaviorUtiles.BuildBTrigger(setters, bExpr, support);
		}

		BehaviorCheckTrigger BehaviorChecker.ReadyCheckBehavior(BehaviorExpression bExpr, BehaviorReadySupport support) {
			return BehaviorUtiles.BuildBCheckTrigger(checkers, bExpr, support);
		}
		public class ClusterBehaviorSetCheck : BehaviorSetCheck {
			List<BehaviorSetter> setters = new List<BehaviorSetter>();
			List<BehaviorChecker> checkers = new List<BehaviorChecker>();
			public void Add(BehaviorSetCheck sc) {
				setters.Add(sc);
				checkers.Add(sc);
			}
			BehaviorTrigger BehaviorSetter.ReadyBehavior(BehaviorExpression bExpr, BehaviorReadySupport support) {
				return BehaviorUtiles.BuildBTrigger(setters, bExpr, support);
			}

			BehaviorCheckTrigger BehaviorChecker.ReadyCheckBehavior(BehaviorExpression bExpr, BehaviorReadySupport support) {
				return BehaviorUtiles.BuildBCheckTrigger(checkers, bExpr, support);
			}
		}
	}
	public interface CompositeBehaviorTrigger :
		BehaviorTrigger,
		Taker<BehaviorTrigger>
	{
		CompositeBehaviorTrigger AddWaitTrigger();
	}
	public class TriggerOnPositiveBCheckListener : BehaviorCheckListener {
		public BehaviorListener clientListener;
		public BehaviorTrigger followingBehavior;
		public bool checkContinuously = false;
		void BehaviorCheckListener.OnResultInPositive() {
			if (checkContinuously) {
				//static ::AGDevStdUtil::EasyBehaviorListener stubListener;
				//followingBehavior.BeginBehavior(stubListener);
			}
			else {
				followingBehavior.BeginBehavior(clientListener);
			}
		}
		void BehaviorCheckListener.OnResultInNegative() { }
	};
	public class BTriggerForBCheck : BehaviorTrigger {
		public TriggerOnPositiveBCheckListener bridgeListener;
		public BehaviorCheckTrigger checkTrigger;
		public BehaviorTrigger followingBehavior;
		void BehaviorTrigger.BeginBehavior(BehaviorListener behaviorListener) {
			bridgeListener = new TriggerOnPositiveBCheckListener { clientListener = behaviorListener, followingBehavior = followingBehavior };
			checkTrigger.BeginBehavior(bridgeListener);
		}
	}
	public class BehaviorFinishCheckTrigger {
		public class PrvtLis : BehaviorListener, BehaviorController {
			public bool didFinish = false;
			public bool isPausing = false;
			public BehaviorListener bListener;
			public BehaviorTrigger followingBTrigger;
			public void OnFinish() {
				didFinish = true;
				if (!isPausing)
					followingBTrigger.BeginBehavior(bListener);
			}
			public void OnBegin() { }
			void BehaviorController.RequestStop() {
				isPausing = true;
			}
			void BehaviorController.RequestPlay() {
				isPausing = false;
				if (didFinish)
					bListener.OnFinish();
			}
		};
		public BehaviorTrigger followingBTrigger;
		public void BeginBehavior(System.Action<BehaviorListener> previousProcess, BehaviorListener bListener) {
			var lis = new PrvtLis { followingBTrigger = followingBTrigger, bListener = bListener };
			//bListener.OnBegin(*prvtListeners.back());
			previousProcess.Invoke(lis);
		}
	}
	public class VariousTrigger {
		public BehaviorTrigger trigger = null;
		public BehaviorFinishCheckTrigger waitTrigger = null;
	};
	public class VoterBehaviorListener : BehaviorListener {
		public VoteCounterBehaviorListener counter;
		public bool didFinish = false;
		void BehaviorListener.OnFinish() {
			didFinish = true;
			counter.ActualCheck();
		}
	};
	public class VoteCounterBehaviorListener {
		public bool isDoingActualCheck = false;
		public BehaviorListener clientListener = null;
		public List<VoterBehaviorListener> voterListeners = new List<VoterBehaviorListener>();
		public BehaviorListener NewListener() {
			voterListeners.Add(new VoterBehaviorListener { counter = this });
			return voterListeners.Last();
		}
		public void AllowDetermineResult() {
			isDoingActualCheck = true;
			ActualCheck();
		}
		public void ActualCheck() {
			if (isDoingActualCheck) {
				foreach (var listener in voterListeners) {
					if (!listener.didFinish)
						return;
				}
				clientListener.OnFinish();
			}
		}
	};
	public class InterceptBehaviorListener : BehaviorListener {
		public BehaviorListener mainListener = null;
		public BehaviorListener sideListener = null;
		public bool didFinish = false;

		void BehaviorListener.OnFinish() {
			didFinish = true;
			if (mainListener != null)
				mainListener.OnFinish();
			if (sideListener != null)
				sideListener.OnFinish();
		}
		public void Take(BehaviorListener _sideListener) {
			sideListener = _sideListener;
			if (didFinish)
				sideListener.OnFinish();
		}
	}
	public class StdCompositeBehaviorTrigger : CompositeBehaviorTrigger {
		public List<VariousTrigger> bTriggers = new List<VariousTrigger>();
		CompositeBehaviorTrigger CompositeBehaviorTrigger.AddWaitTrigger() {
			var followingBehavior = new StdCompositeBehaviorTrigger();
			bTriggers.Add(new VariousTrigger { waitTrigger = new BehaviorFinishCheckTrigger { followingBTrigger = followingBehavior } });
			return followingBehavior;
		}
		void BehaviorTrigger.BeginBehavior(BehaviorListener behaviorListener) {
			var voteListener = new VoteCounterBehaviorListener { clientListener = behaviorListener };
			var itr = bTriggers.GetEnumerator();
			var itrNext = bTriggers.GetEnumerator();
			itrNext.MoveNext();
			InterceptBehaviorListener interceptList = null;
			while (itr.MoveNext()) {
				var isNextExist = itrNext.MoveNext();
				if (itr.Current.trigger != null) {
					if (!isNextExist ? true : (itrNext.Current.waitTrigger == null ? true : false))
						itr.Current.trigger.BeginBehavior(voteListener.NewListener());
					else if (itrNext.Current.waitTrigger != null) {
						interceptList = new InterceptBehaviorListener { mainListener = voteListener.NewListener() };
						itr.Current.trigger.BeginBehavior(interceptList);
					}

				}
				else if (itr.Current.waitTrigger != null) {
					itr.Current.waitTrigger.BeginBehavior((trigger)=>interceptList.Take(trigger), voteListener.NewListener());
				}
			}
			voteListener.AllowDetermineResult();
		}

		void Taker<BehaviorTrigger>.Take(BehaviorTrigger newElement) {
			bTriggers.Add(new VariousTrigger { trigger = newElement });
		}
		void Taker<BehaviorTrigger>.None() {}
	}
	[System.Serializable]
	public class ProcessGroupSetting {
		public string groupName;
		public List<string> members;
		public string preProcess;
		public string postProcess;
		public string currentProcessName;
		public string waitingProcessName;
		public Dictionary<string, BehaviorTrigger> memberProcesses = new Dictionary<string, BehaviorTrigger>();
		public Dictionary<string, BehaviorListener> currentGivenProcessListeners = new Dictionary<string, BehaviorListener>();
		public BehaviorListener currentMainProcessListener;
		public BehaviorTrigger preProcessTrigger;
		public BehaviorTrigger postProcessTrigger;
		public void Do(string processName) {
			if (!string.IsNullOrEmpty(currentProcessName)) {
				waitingProcessName = processName;
				if (currentMainProcessListener != null)
					currentMainProcessListener.OnFinish();
				return;
			}
			currentProcessName = processName;
			if (preProcessTrigger != null)
				preProcessTrigger.BeginBehavior(new SelfNullifyListener_PreProcessListener { processName = processName, groupSetting = this });
			else {
				currentMainProcessListener = new SelfNullifyListener_MainProcessListener { processName = processName, groupSetting = this };
				memberProcesses[currentProcessName].BeginBehavior(currentMainProcessListener);
			}
		}
		public void OnGroupProcessEnd() {
			currentGivenProcessListeners[currentProcessName].OnFinish();
			currentProcessName = "";
			string nextProcess = waitingProcessName;
			waitingProcessName = "";
			if (!string.IsNullOrEmpty(nextProcess)) {
				Do(nextProcess);
			}
		}
		public class SelfNullifyListener_MainProcessListener : BehaviorListener {
			public bool didFinish = false;
			public string processName;
			public ProcessGroupSetting groupSetting;
			void BehaviorListener.OnFinish() {
				if (didFinish)
					return;
				didFinish = true;
				if (groupSetting.currentProcessName == processName) {
					groupSetting.currentMainProcessListener = null;
					if (groupSetting.postProcessTrigger != null) {
						groupSetting.postProcessTrigger.BeginBehavior(new SelfNullifyListener_PostProcessListener { processName = processName, groupSetting = groupSetting });
					}
					else {
						groupSetting.OnGroupProcessEnd();
					}
				}
			}
		}
		public class SelfNullifyListener_PreProcessListener : BehaviorListener {
			public bool didFinish = false;
			public string processName;
			public ProcessGroupSetting groupSetting;
			void BehaviorListener.OnFinish() {
				if (didFinish)
					return;
				didFinish = true;
				if (groupSetting.currentProcessName == processName) {
					groupSetting.currentMainProcessListener = new SelfNullifyListener_MainProcessListener { groupSetting = groupSetting };
					groupSetting.memberProcesses[groupSetting.currentProcessName].BeginBehavior(new SelfNullifyListener_MainProcessListener { processName = processName, groupSetting = groupSetting });
				}
			}
		}
		public class SelfNullifyListener_PostProcessListener : BehaviorListener {
			public bool didFinish = false;
			public string processName;
			public ProcessGroupSetting groupSetting;
			void BehaviorListener.OnFinish() {
				if (didFinish)
					return;
				didFinish = true;
				if (groupSetting.currentProcessName == processName) {
					groupSetting.OnGroupProcessEnd();
				}
			}
		}
	}
	#endregion

}