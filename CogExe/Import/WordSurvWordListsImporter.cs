using System.Collections.Generic;
using System.Xml.Linq;
using SIL.Machine;

namespace SIL.Cog.Import
{
	public class WordSurvWordListsImporter : IWordListsImporter
	{
		public void Import(string path, CogProject project)
		{
			XElement root = XElement.Load(path);
			var varieties = new Dictionary<string, Variety>();
			using (project.Varieties.BulkUpdate())
			{
				foreach (XElement wordListElem in root.Elements("word_lists").Elements("word_list"))
				{
					var id = (string) wordListElem.Attribute("id");
					var name = (string) wordListElem.Element("name");
					var variety = new Variety(name);
					project.Varieties.Add(variety);
					varieties[id] = variety;
				}
			}

			using (project.Senses.BulkUpdate())
			{
				foreach (XElement glossElem in root.Elements("glosses").Elements("gloss"))
				{
					var gloss = (string) glossElem.Element("name");
					var pos = (string) glossElem.Element("part_of_speech");
					var sense = new Sense(gloss.Trim(), pos);
					project.Senses.Add(sense);
					foreach (XElement transElem in glossElem.Elements("transcriptions").Elements("transcription"))
					{
						var varietyID = (string) transElem.Element("word_list_id");
						var wordform = (string) transElem.Element("name");
						if (wordform != null)
						{
							Variety variety;
							if (varieties.TryGetValue(varietyID, out variety))
							{
								foreach (string w in wordform.Split(','))
								{
									string str = w.Trim();
									Shape shape;
									if (!project.Segmenter.ToShape(null, str, null, out shape))
										shape = project.Segmenter.EmptyShape;
									variety.Words.Add(new Word(str, shape, sense));
								}
							}
						}
					}
				}
			}
		}
	}
}