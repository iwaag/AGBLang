using System.Collections.Generic;
using System.Runtime.Serialization;
using AGDev;
namespace AGBLang {
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
	#endregion

}