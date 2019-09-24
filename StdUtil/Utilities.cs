using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using AGDev;
namespace AGBLang.StdUtil {
	public class ClusterBehaviorSetter : BehaviorSetter {
		public IEnumerable<BehaviorSetter> bSetters;
		void BehaviorSetter.ReadyBehavior(BehaviorExpression gBLock, BehaviorRequestListener reqListener) {
			var outerListener = new ClusterizingBReqListener { clientListener = reqListener };
			foreach (var bSetter in bSetters) {
				bSetter.ReadyBehavior(gBLock, outerListener);
			}
			if (outerListener.triggers.Count > 0) {
				reqListener.OnSucceed(outerListener.clusterTrigger);
			}
		}
		public class ClusterizingBReqListener : BehaviorRequestListener {
			public BehaviorRequestListener clientListener;
			public List<BehaviorTrigger> triggers = new List<BehaviorTrigger>();
			public ClusterBehaviorTrigger clusterTrigger;
			public ClusterizingBReqListener() {
				clusterTrigger = new ClusterBehaviorTrigger { triggers = triggers };
			}
			void BehaviorRequestListener.OnSucceed(BehaviorTrigger controller) {
				triggers.Add(controller);
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
		}
	}
	public class ClusterBehaviorChecker : BehaviorChecker {
		public IEnumerable<BehaviorChecker> bCheckers;
		void BehaviorChecker.ReadyCheckBehavior(BehaviorExpression gBLock, BehaviorCheckRequestListener reqListener) {
			var outerListener = new ClusterizingBReqListener { clientListener = reqListener };
			foreach (var bChecker in bCheckers) {
				bChecker.ReadyCheckBehavior(gBLock, outerListener);
			}
			if (outerListener.triggers.Count > 0)
				reqListener.OnSucceed(outerListener.clusterTrigger);
		}
		public class ClusterizingBReqListener : BehaviorCheckRequestListener {
			public BehaviorCheckRequestListener clientListener;
			public List<BehaviorCheckTrigger> triggers = new List<BehaviorCheckTrigger>();
			public ClusterBehaviorCheckTrigger clusterTrigger;
			public ClusterizingBReqListener() {
				clusterTrigger = new ClusterBehaviorCheckTrigger { triggers = triggers };
			}
			void BehaviorCheckRequestListener.OnSucceed(BehaviorCheckTrigger controller) {
				triggers.Add(controller);
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
		}
	}
	#region grammar block
	[System.Serializable]
	[DataContract]
	public class GBlockUtilities {
 
	}
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
		public Collector<BehaviorTrigger> triggerColl;
		void GrammarBlockVisitor.IfHasMetaInfo(GrammarBlock meta) { }

		void GrammarBlockVisitor.IfGrammarUnit(GrammarUnit unit) { }

		void GrammarBlockVisitor.IfClusterGrammarBlock(ClusterGrammarBlock cluster) {
			var vReader = new VerbalUnitReader { behaverSetCheck = behaverSetCheck, subjectBlock = cluster.blocks[0], bhvrTrgColl = triggerColl };
			GrammarBlockUtils.VisitGrammarBlock(cluster.blocks[1], vReader);
		}

		void GrammarBlockVisitor.IfHasModifier(GrammarBlock mod) {
		}
		public class VerbalUnitReader : GrammarBlockVisitor {
			public BehaviorSetCheck behaverSetCheck;
			public GrammarBlock subjectBlock;
			public Collector<BehaviorTrigger> bhvrTrgColl;
			void GrammarBlockVisitor.IfHasMetaInfo(GrammarBlock meta) { }
			void GrammarBlockVisitor.IfGrammarUnit(GrammarUnit unit) {
				var behaviorExpression = new StdBehaviorExpression(subjectBlock, unit);
				var easyListener = new PrvtRequestListener { parent = this };
				behaverSetCheck.ReadyBehavior(behaviorExpression, easyListener);
			}

			void GrammarBlockVisitor.IfClusterGrammarBlock(ClusterGrammarBlock cluster) {
				foreach (var block in cluster.blocks) {
					GrammarBlockUtils.VisitGrammarBlock(block, this);
				}
			}

			void GrammarBlockVisitor.IfHasModifier(GrammarBlock mod) { }
			public class PrvtRequestListener : BehaviorRequestListener {
				public VerbalUnitReader parent;
				void BehaviorRequestListener.OnSucceed(BehaviorTrigger trigger) {
					parent.bhvrTrgColl.Collect(trigger);
				}
			}
		}
	}
	class OR_BehaviorCheckTrigger : BehaviorCheckTrigger, BehaviorCheckRequestListener {
		public List<BehaviorCheckTrigger> triggers = new List<BehaviorCheckTrigger>();
		void BehaviorCheckTrigger.BeginBehavior(BehaviorCheckListener behaviorListener) {
			foreach (var trigger in triggers) {
				trigger.BeginBehavior(behaviorListener);
			}
		}
		void BehaviorCheckRequestListener.OnSucceed(BehaviorCheckTrigger trigger) {
			triggers.Add(trigger);
		}
	}
	public class ConditionalSVVisitor : GrammarBlockVisitor {
		public BehaviorSetCheck behaverSetCheck;
		public CompositeBehaviorTrigger givenTrigger;
		public CompositeBehaviorTrigger nextCompositTrigger;
		void GrammarBlockVisitor.IfClusterGrammarBlock(ClusterGrammarBlock cluster) {
			var orCheckTrigger = new OR_BehaviorCheckTrigger { };
			var vReader = new ConditionalVerbalUnitReader { behaverSetCheck = behaverSetCheck, subjectBlock = cluster.blocks[0], chkListener = orCheckTrigger, bhvrTrgColl = givenTrigger };
			GrammarBlockUtils.VisitGrammarBlock(cluster.blocks[1], vReader);
			if (orCheckTrigger.triggers.Count > 1) {
				var followingBehavior = new StdCompositeBehaviorTrigger();
				nextCompositTrigger = followingBehavior;
				givenTrigger.Collect(new BTriggerForBCheck { checkTrigger = orCheckTrigger, followingBehavior = followingBehavior });
			}
			else if (orCheckTrigger.triggers.Count == 1) {
				var followingBehavior = new StdCompositeBehaviorTrigger();
				nextCompositTrigger = followingBehavior;
				givenTrigger.Collect(new BTriggerForBCheck { checkTrigger = orCheckTrigger, followingBehavior = followingBehavior });

			}
		}
		void GrammarBlockVisitor.IfGrammarUnit(GrammarUnit verbalUnit) { }
		void GrammarBlockVisitor.IfHasModifier(GrammarBlock modifier) { }
		void GrammarBlockVisitor.IfHasMetaInfo(GrammarBlock metaInfo) { }
		public class ConditionalVerbalUnitReader : GrammarBlockVisitor {
			public BehaviorSetCheck behaverSetCheck;
			public GrammarBlock subjectBlock;
			public Collector<BehaviorTrigger> bhvrTrgColl;
			public BehaviorCheckRequestListener chkListener;
			void GrammarBlockVisitor.IfHasMetaInfo(GrammarBlock meta) { }
			void GrammarBlockVisitor.IfGrammarUnit(GrammarUnit verbalUnit) {
				var bExpression = new StdBehaviorExpression(subjectBlock, verbalUnit);
				if (GrammarBlockUtils.ShallowSeek(verbalUnit.metaInfo, StdMetaInfos.negated.word) != null) {
					var outerListner = new NegateBCheckReqListener { listener = chkListener };
					behaverSetCheck.ReadyCheckBehavior(bExpression, outerListner);
				}
				else
					behaverSetCheck.ReadyCheckBehavior(bExpression, chkListener);
			}

			void GrammarBlockVisitor.IfClusterGrammarBlock(ClusterGrammarBlock cluster) {
				foreach (var block in cluster.blocks) {
					GrammarBlockUtils.VisitGrammarBlock(block, this);
				}
			}

			void GrammarBlockVisitor.IfHasModifier(GrammarBlock mod) { }
			public class NegateBCheckReqListener : BehaviorCheckRequestListener {
				public BehaviorCheckRequestListener listener;
				void BehaviorCheckRequestListener.OnSucceed(BehaviorCheckTrigger trigger) {
					listener.OnSucceed(new NegatingBCheckTrigger { clientTrigger = trigger });
				}
			}
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
		public CompositeBehaviorTrigger subSentenceBehaviorCollector;
		public CompositeBehaviorTrigger followingSentenceBehaviorManagedCollector;
		public bool doResetPreviousTrigger = false;
		public void GrammarBlockCommon(GrammarBlock block) {
			//Title
			if (GrammarBlockUtils.ShallowSeek(block.metaInfo, StdMetaInfos.title.word) != null) {
				if (block.unit != null) {
					followingSentenceBehaviorManagedCollector = subSentenceBehaviorCollector = new StdCompositeBehaviorTrigger();
					namedCBTriggers.Add(block.unit.word, followingSentenceBehaviorManagedCollector);
				}
				return;
			}
			//wait
			{
				if (GrammarBlockUtils.ShallowSeekModifier(block, "then") != null) {
					subSentenceBehaviorCollector = subSentenceBehaviorCollector.AddWaitTrigger();
				}
			}
			//read conditional SV
			if (block.modifier != null) {
				var conditionSVVisitor = new ConditionalSVVisitor { behaverSetCheck = behaverSetCheck, givenTrigger = subSentenceBehaviorCollector };
				var rootVisitor = new MetaInfoDependentGrammarBlockVisitor();
				rootVisitor.metaToVis.Add(StdMetaInfos.conditionSV.word, conditionSVVisitor);
				GrammarBlockUtils.VisitGrammarBlock(block.modifier, rootVisitor);
				if (conditionSVVisitor.nextCompositTrigger != null) {
					subSentenceBehaviorCollector = conditionSVVisitor.nextCompositTrigger;
				}
			}
			//read main SV
			{
				if (block.cluster != null) {
					if (GrammarBlockUtils.ShallowSeek(block.metaInfo, StdMetaInfos.sentenceCluster.word) != null) {
						foreach (var subBlock in block.cluster.blocks) {
							var subProcessor = new SentenceBlockRecursiveProcessor { behaverSetCheck = behaverSetCheck, subSentenceBehaviorCollector = subSentenceBehaviorCollector, namedCBTriggers = namedCBTriggers };
							subProcessor.GrammarBlockCommon(subBlock);
							if (subProcessor.followingSentenceBehaviorManagedCollector != null) {
								subSentenceBehaviorCollector = subProcessor.followingSentenceBehaviorManagedCollector;
							}
						}
					}
				}
				if (GrammarBlockUtils.ShallowSeek(block.metaInfo, StdMetaInfos.sv.word) != null) {
					var svVisitor = new SVSentenceVisitor { behaverSetCheck = behaverSetCheck, triggerColl = subSentenceBehaviorCollector };
					GrammarBlockUtils.VisitGrammarBlock(block, svVisitor);
				}
			}
		}
	}
	#endregion
	#region behavior
	public class StdBehaverPicker : ImmediatePicker<Behaver, GrammarBlock> {
		public ImmediatePicker<Behaver, GrammarBlock> clientBehaverPicker;
		public List<Behaver> behavers = new List<Behaver>();
		Behaver ImmediatePicker<Behaver, GrammarBlock>.PickBestElement(GrammarBlock key) {
			var foundBehaver = behavers.Find((behaver) => behaver.MatchAttribue(key) == AttributeMatchResult.POSITIVE);
			return foundBehaver != null ? foundBehaver : clientBehaverPicker.PickBestElement(key);
		}
	}
	public class StdBSetCheck : BehaviorSetCheck {
		public ImmediatePicker<Behaver, GrammarBlock> behaverPicker;
		public ClusterBehaviorSetCheck builtinSetCheck = new ClusterBehaviorSetCheck();
		void BehaviorSetter.ReadyBehavior(BehaviorExpression bExpr, BehaviorRequestListener reqListener) {
			(builtinSetCheck as BehaviorSetCheck).ReadyBehavior(bExpr, reqListener);
			var behaver = behaverPicker.PickBestElement(bExpr.subject);
			if (behaver != null) {
				behaver.ReadyBehavior(bExpr, reqListener);
			}
		}

		void BehaviorChecker.ReadyCheckBehavior(BehaviorExpression bExpr, BehaviorCheckRequestListener chkReqListener) {
			(builtinSetCheck as BehaviorSetCheck).ReadyCheckBehavior(bExpr, chkReqListener);
			var behaver = behaverPicker.PickBestElement(bExpr.subject);
			if (behaver != null) {
				behaver.ReadyCheckBehavior(bExpr, chkReqListener);
			}
		}
		public class ClusterBehaviorSetCheck : BehaviorSetCheck {
			public List<BehaviorSetCheck> cluster = new List<BehaviorSetCheck>();
			void BehaviorSetter.ReadyBehavior(BehaviorExpression bExpr, BehaviorRequestListener reqListener) {
				foreach (var item in cluster) {
					item.ReadyBehavior(bExpr, reqListener);
				}
			}

			void BehaviorChecker.ReadyCheckBehavior(BehaviorExpression bExpr, BehaviorCheckRequestListener chkReqListener) {
				foreach (var item in cluster) {
					item.ReadyCheckBehavior(bExpr, chkReqListener);
				}
			}
		}
	}
	public interface CompositeBehaviorTrigger :
		BehaviorTrigger,
		Collector<BehaviorTrigger> {
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
		public void BeginBehavior(Collector<BehaviorListener> previousProcess, BehaviorListener bListener) {
			var lis = new PrvtLis { followingBTrigger = followingBTrigger, bListener = bListener };
			//bListener.OnBegin(*prvtListeners.back());
			previousProcess.Collect(lis);
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
	public class InterceptBehaviorListener : BehaviorListener, Collector<BehaviorListener> {
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
		void Collector<BehaviorListener>.Collect(BehaviorListener _sideListener) {
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
					itr.Current.waitTrigger.BeginBehavior(interceptList, voteListener.NewListener());
				}
			}
			voteListener.AllowDetermineResult();
		}

		void Collector<BehaviorTrigger>.Collect(BehaviorTrigger newElement) {
			bTriggers.Add(new VariousTrigger { trigger = newElement });
		}
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