using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using MatthiasToolbox.Logging;
using MatthiasToolbox.Logging.Loggers;
using System.IO;
using MatthiasToolbox.Semantics.Metamodel;
using MatthiasToolbox.Semantics.Utilities;

namespace MatthiasToolbox.ImageLibrary
{
    public static class Program
    {
        public static Concept ImageConcept { get; private set; }
        public static Concept TopicConcept { get; private set; }

        public static Property NameProperty { get; private set; }

        public static Property FileProperty { get; private set; }
        public static Property ChecksumProperty { get; private set; }
        public static Property FileSizeProperty { get; private set; }
        public static Property WidthProperty { get; private set; }
        public static Property HeightProperty { get; private set; }

        public static Relation HasTopicRelation { get; private set; }
        public static Relation HasParentRelation { get; private set; }

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Logger.Add(new PlainTextFileLogger(new FileInfo("MatthiasToolbox.ImageLibrary.log")));

            Ontology onto = new Ontology("ImageOntology", "Image Ontology");

            onto.Initialize(false, false, CreateDatabase);
            
            Application.Run(new Form1());
        }

        private static void CreateDatabase(Ontology o)
        {
            ImageConcept = o.CreateConcept("Image");
            TopicConcept = o.CreateConcept("Topic");

            NameProperty = o.CreateProperty<string>("Name", o.RootConcept, false);

            FileProperty = o.CreateProperty<string>("File", ImageConcept, false);
            ChecksumProperty = o.CreateProperty<string>("Checksum", ImageConcept, false);
            FileSizeProperty = o.CreateProperty<long>("FileSize", ImageConcept, false);
            WidthProperty = o.CreateProperty<long>("Width", ImageConcept, false);
            HeightProperty = o.CreateProperty<long>("Height", ImageConcept, false);
            
            HasTopicRelation = o.CreateRelation("has topic", ImageConcept, Cardinality.AnyToAny, TopicConcept);
            HasParentRelation = o.CreateRelation("has parent", TopicConcept, Cardinality.ManyToOne, TopicConcept);

        }
    }
}
