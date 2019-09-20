using System.Collections.Generic;
using AGBLang;
namespace AGDev.StdUtil {
	public interface GBlockConvertListener {
		void AdditionalEdit(MutableGrammarBlock mgBlock);
		GBlockConverter subBlockConverter { get; }
		GBlockConverter metaConverter { get; }
		GBlockConverter modConverter { get; }
	};
	public interface GBlockConverter {
		GBlockConvertResult ConvertGBlock(GrammarBlock sourceGBlock, GBlockConvertListener listener);
	};
	public class PassThroughGBlockConverter : GBlockConverter {
		static public GBlockConverter instance => _instance;
		static public GBlockConverter _instance = new PassThroughGBlockConverter();
		GBlockConvertResult GBlockConverter.ConvertGBlock(GrammarBlock sourceGBlock, GBlockConvertListener listener) {
			return new GBlockConvertResult();
		}
	}
	public class PassThroughGBlockConvertListener : GBlockConvertListener {
		static public GBlockConvertListener instance => _instance;
		static public GBlockConvertListener _instance = new PassThroughGBlockConvertListener();
		GBlockConverter GBlockConvertListener.subBlockConverter => PassThroughGBlockConverter.instance;
		GBlockConverter GBlockConvertListener.metaConverter => PassThroughGBlockConverter.instance;
		GBlockConverter GBlockConvertListener.modConverter => PassThroughGBlockConverter.instance;
		void GBlockConvertListener.AdditionalEdit(MutableGrammarBlock mgBlock) {}
	}
	public class StdGBlockConvertListener : GBlockConvertListener {
		GBlockConverter GBlockConvertListener.subBlockConverter => _subBlockConverter;

		GBlockConverter GBlockConvertListener.metaConverter => _metaConverter;

		GBlockConverter GBlockConvertListener.modConverter => _modConverter;
		public GBlockConverter _subBlockConverter;
		public GBlockConverter _metaConverter;
		public GBlockConverter _modConverter;
		void GBlockConvertListener.AdditionalEdit(MutableGrammarBlock mgBlock) {}
	}
	public class RootGBlockConverter : GBlockConverter {
		public GBlockConverter_Replace replacer = new GBlockConverter_Replace();
		public GBlockConverter_PronounSpecifier pronouc = new GBlockConverter_PronounSpecifier();
		public GBlockConverter_Default defaultConv = new GBlockConverter_Default();
		public GBlockConverter_Activizer activizer = new GBlockConverter_Activizer();
		public GBlockConverter_EachBlock eachConv = new GBlockConverter_EachBlock();
		public ClusterGBlockConverter mainConverter = new ClusterGBlockConverter();
		public RootGBlockConverter() {
			var convList = new List<GBlockConverter>();
			mainConverter.converters = convList;
			replacer.number = new Dictionary<string, GrammarBlock>(); 
			replacer.number.Add("one", new MinimumGBUnit { word = "1"});
			replacer.number.Add("two", new MinimumGBUnit { word = "2" });
			replacer.number.Add("three", new MinimumGBUnit { word = "3" });
			pronouc.dict = new Dictionary<string, string>();
			convList.Add(replacer);
			convList.Add(pronouc);
			convList.Add(activizer);
			convList.Add(eachConv);
			convList.Add(defaultConv);
			activizer.defaultSubject = new StdMutableGUnit { word = "player"};
		}
		GBlockConvertResult GBlockConverter.ConvertGBlock(GrammarBlock sourceGBlock, GBlockConvertListener listener) {
			return (mainConverter as GBlockConverter).ConvertGBlock(sourceGBlock, listener);
		}
	}
	public class StdGBlockFilter : ImmediatePicker<GrammarBlock, GrammarBlock> {
		public GBlockConverter converter;
		GrammarBlock ImmediatePicker<GrammarBlock, GrammarBlock>.PickBestElement(GrammarBlock key) {
			var listener = new StdGBlockConvertListener();
			var rootConv = new RootGBlockConverter();
			listener._metaConverter = GBlockConverter_Default.instance;
			listener._modConverter = rootConv;
			listener._subBlockConverter = rootConv;
			return (rootConv as GBlockConverter).ConvertGBlock(key, listener).result;
		}
	}
	/*public class LoggingGBConvListener : GBlockConvertListener {
		public GBlockConvertListener clientListener;
		GBlockConverter GBlockConvertListener.subBlockConverter => clientListener.subBlockConverter;

		GBlockConverter GBlockConvertListener.metaConverter => clientListener.metaConverter;

		GBlockConverter GBlockConvertListener.modConverter => clientListener.modConverter;

		void GBlockConvertListener.AcceptAdditional(Collector<GrammarBlock> metaCollector, Collector<GrammarBlock> modCollector) {
			clientListener.AcceptAdditional(metaCollector, modCollector);
		}
	}*/

} 