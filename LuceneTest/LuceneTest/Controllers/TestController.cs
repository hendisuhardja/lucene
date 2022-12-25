using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Microsoft.AspNetCore.Mvc;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using static Lucene.Net.Analysis.Synonym.SynonymMap;
using static Lucene.Net.Util.Packed.PackedInt32s;
using Lucene.Net.Util.Automaton;
using Lucene.Net.Search.Spans;

namespace LuceneTest.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
                public static List<Person> Data { get; set; }
        private static RAMDirectory _directory;
        private static string _personGuidToBeUpdated;

        public TestController()
        {
            _directory = new RAMDirectory();

            var guidField = new StringField("GUID", "", Field.Store.YES);
            var fNameField = new TextField("FirstName", "", Field.Store.YES) { Boost = 4.0f };
            var mNameField = new TextField("MiddleName", "", Field.Store.YES);
            var lNameField = new TextField("LastName", "", Field.Store.YES);
            var descriptionField = new TextField("Description", "", Field.Store.YES);

            var d = new Document()
            {
                guidField,
                fNameField,
                mNameField,
                lNameField,
                descriptionField
            };

            GetData();
            using (var analyer = new StandardAnalyzer(LuceneVersion.LUCENE_48))
            {
                using (var write = new IndexWriter(_directory, new IndexWriterConfig(LuceneVersion.LUCENE_48, analyer)))
                {
                    foreach (Person person in Data)
                    {
                        guidField.SetStringValue(person.GUID);
                        fNameField.SetStringValue(person.FirstName);
                        mNameField.SetStringValue(person.MiddleName);
                        lNameField.SetStringValue(person.LastName);
                        descriptionField.SetStringValue(person.Description);

                        write.AddDocument(d);
                    }

                    write.Commit();
                }
            }
        }

        public static void GetData()
        {
            Data = new List<Person>()
            {
                new Person(Guid.NewGuid().ToString(),"Fred","Michle","Herb","A tall thin man."),
                new Person(Guid.NewGuid().ToString(),"Frank","Ed","Stevens","A short fat man."),
                new Person(Guid.NewGuid().ToString(),"Alfred","Edward","Stewart","A medium average man."),
                new Person(Guid.NewGuid().ToString(),"Joe","Rand","Smith","A very tall large man."),
                new Person(Guid.NewGuid().ToString(),"Abigal","Elizabeth","Spear","A tall thin woman."),
                new Person(Guid.NewGuid().ToString(),"Michael","Rose","Garcia","A small average woman."),
                new Person(Guid.NewGuid().ToString(),"Mike","Jordan","Davis","A tall large woman."),
                new Person(Guid.NewGuid().ToString(),"Michella","Madison","Jones","A short fat woman."),
                new Person(Guid.NewGuid().ToString(),"Clint","Johnny","Williams","A very tiny boy."),
                new Person(Guid.NewGuid().ToString(),"Susan","Michele","Brown","A very tiny girl.")
            };
        }


        [HttpGet(Name = "Search")]
        public IEnumerable<string> Get(string search)
        {

            var results = new List<string>();
            const int hitsLImit = 100;
            using (var analyer = new StandardAnalyzer(LuceneVersion.LUCENE_48))
            {
                using (var reader = DirectoryReader.Open(_directory))
                {
                    var searcher = new IndexSearcher(reader);
                    string[] fnames = { "GUID", "FirstName", "MiddleName", "LastName", "Age", "Description" };
                    var parser = new MultiFieldQueryParser(LuceneVersion.LUCENE_48, fnames, analyer);

                    Term term = new Term("FirstName", search.Trim());
                    var fuzzyAutomation = new LevenshteinAutomata(search.Trim(), false);

                    var contentQuery = new FuzzyQuery(new Term("FirstName", search.Trim()), 2);
                    var lastNameQuery = new FuzzyQuery(new Term("MiddleName", search.Trim()), 2);

                    SpanQuery[] clauses = new SpanQuery[] {
                      new SpanMultiTermQueryWrapper<MultiTermQuery>(contentQuery),
                      new SpanMultiTermQueryWrapper<MultiTermQuery>(lastNameQuery)
                    };
                    BooleanQuery composedQuery = new BooleanQuery();
                    composedQuery.Add(contentQuery, Occur.SHOULD);
                    composedQuery.Add(lastNameQuery, Occur.SHOULD);

                    //SpanNearQuery spanNearQuery = new SpanNearQuery(clauses, 3, false);

                    MultiPhraseQuery multiQuery = new MultiPhraseQuery();

                    var query = new AutomatonQuery(term, fuzzyAutomation.ToAutomaton(2));

                    ScoreDoc[] docs = searcher.Search(composedQuery, null, hitsLImit, Sort.RELEVANCE).ScoreDocs;



                    for (int i = 0; i < docs.Length; i++)
                    {
                        Document d = searcher.Doc(docs[i].Doc);
                        string guid = d.Get("GUID");
                        string firstname = d.Get("FirstName");
                        string middlename = d.Get("MiddleName");
                        string lastname = d.Get("LastName");
                        string description = d.Get("Description");

                        results.Add($"{guid} {firstname} {middlename} {lastname} {description}");
                    }
                }
            }


            return results;
        }
    }
}