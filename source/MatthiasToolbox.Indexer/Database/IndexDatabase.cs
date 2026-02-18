using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;

using MatthiasToolbox.Basics.Algorithms;
using MatthiasToolbox.Indexer.Interfaces;
using MatthiasToolbox.Indexer.Service;
using MatthiasToolbox.Logging;
using MatthiasToolbox.Basics.Utilities;
using MatthiasToolbox.Indexer.Properties;

namespace MatthiasToolbox.Indexer.Database
{
    /// <summary>
    /// Database index designed for a single language. Multi-language queries should be merged using separate index-db's.
    /// TODO: cache results, nearest token neighbours (edit distance), frequent token neighbours (position in document)
    /// </summary>
    public class IndexDatabase : DataContext
    {
        #region cvar

        private bool isActiveInstance;

        #region static

        private static IndexDatabase activeInstance;

        private static List<IndexDatabase> openInstances;

        public static readonly string[] StopWordsEnglish1 
            = { "i", "a", "about", "an", "are", "as", "at", "be", "by", "com", "for", "from", "how", "in", "is", "it", "of", 
                "on", "or", "that", "the", "this", "to", "was", "what", "when", "where", "who", "will", "with", "the", "www" };
        
        public static readonly string[] StopWordsEnglish2 
            = { "a", "able", "about", "across", "after", "all", "almost", "also", "am", "among", "an", "and", "any", "are", 
                  "as", "at", "be", "because", "been", "but", "by", "can", "cannot", "could", "dear", "did", "do", "does", 
                  "either", "else", "ever", "every", "for", "from", "get", "got", "had", "has", "have", "he", "her", "hers", 
                  "him", "his", "how", "however", "i", "if", "in", "into", "is", "it", "its", "just", "least", "let", "like", 
                  "likely", "may", "me", "might", "most", "must", "my", "neither", "no", "nor", "not", "of", "off", "often", 
                  "on", "only", "or", "other", "our", "own", "rather", "said", "say", "says", "she", "should", "since", "so", 
                  "some", "than", "that", "the", "their", "them", "then", "there", "these", "they", "this", "tis", "to", "too", 
                  "twas", "us", "wants", "was", "we", "were", "what", "when", "where", "which", "while", "who", "whom", "why",
                  "will", "with", "would", "yet", "you", "your" };

        public static readonly string[] StopWordsEnglish3
           = { "a", "about", "above", "after", "again", "against", "all", "am", "an", "and", "any", "are", "aren't", "as", "at", 
                 "be", "because", "been", "before", "being", "below", "between", "both", "but", "by", "can't", "cannot", "could", 
                 "couldn't", "did", "didn't", "do", "does", "doesn't", "doing", "don't", "down", "during", "each", "few", "for", 
                 "from", "further", "had", "hadn't", "has", "hasn't", "have", "haven't", "having", "he", "he'd", "he'll", "he's", 
                 "her", "here", "here's", "hers", "herself", "him", "himself", "his", "how", "how's", "i", "i'd", "i'll", "i'm", 
                 "i've", "if", "in", "into", "is", "isn't", "it", "it's", "its", "itself", "let's", "me", "more", "most", "mustn't", 
                 "my", "myself", "no", "nor", "not", "of", "off", "on", "once", "only", "or", "other", "ought", "our", "ours", 
                 "ourselves", "out", "over", "own", "same", "shan't", "she", "she'd", "she'll", "she's", "should", "shouldn't", 
                 "so", "some", "such", "than", "that", "that's", "the", "their", "theirs", "them", "themselves", "then", "there", 
                 "there's", "these", "they", "they'd", "they'll", "they're", "they've", "this", "those", "through", "to", "too", 
                 "under", "until", "up", "very", "was", "wasn't", "we", "we'd", "we'll", "we're", "we've", "were", "weren't", 
                 "what", "what's", "when", "when's", "where", "where's", "which", "while", "who", "who's", "whom", "why", "why's", 
                 "with", "won't", "would", "wouldn't", "you", "you'd", "you'll", "you're", "you've", "your", "yours", "yourself", 
                 "yourselves" };

        public static readonly string[] StopWordsEnglish4
           = { "a", "about", "above", "across", "after", "afterwards", "again", "against", "all", "almost", "alone", "along", "already", 
                 "also", "although", "always", "am", "among", "amongst", "amoungst", "amount", "an", "and", "another", "any", "anyhow", 
                 "anyone", "anything", "anyway", "anywhere", "are", "around", "as", "at", "back", "be", "became", "because", "become", 
                 "becomes", "becoming", "been", "before", "beforehand", "behind", "being", "below", "beside", "besides", "between", "beyond", 
                 "bill", "both", "bottom", "but", "by", "call", "can", "cannot", "cant", "co", "computer", "con", "could", "couldnt", "cry", 
                 "de", "describe", "detail", "do", "done", "down", "due", "during", "each", "eg", "eight", "either", "eleven", "else", 
                 "elsewhere", "empty", "enough", "etc", "even", "ever", "every", "everyone", "everything", "everywhere", "except", "few", 
                 "fifteen", "fify", "fill", "find", "fire", "first", "five", "for", "former", "formerly", "forty", "found", "four", "from", 
                 "front", "full", "further", "get", "give", "go", "had", "has", "hasnt", "have", "he", "hence", "her", "here", "hereafter", 
                 "hereby", "herein", "hereupon", "hers", "herse”", "him", "himse”", "his", "how", "however", "hundred", "i", "ie", "if", 
                 "in", "inc", "indeed", "interest", "into", "is", "it", "its", "itse”", "keep", "last", "latter", "latterly", "least", "less", 
                 "ltd", "made", "many", "may", "me", "meanwhile", "might", "mill", "mine", "more", "moreover", "most", "mostly", "move", 
                 "much", "must", "my", "myse”", "name", "namely", "neither", "never", "nevertheless", "next", "nine", "no", "nobody", "none", 
                 "noone", "nor", "not", "nothing", "now", "nowhere", "of", "off", "often", "on", "once", "one", "only", "onto", "or", "other", 
                 "others", "otherwise", "our", "ours", "ourselves", "out", "over", "own", "part", "per", "perhaps", "please", "put", "rather", 
                 "re", "same", "see", "seem", "seemed", "seeming", "seems", "serious", "several", "she", "should", "show", "side", "since", 
                 "sincere", "six", "sixty", "so", "some", "somehow", "someone", "something", "sometime", "sometimes", "somewhere", "still", 
                 "such", "system", "take", "ten", "than", "that", "the", "their", "them", "themselves", "then", "thence", "there", "thereafter", 
                 "thereby", "therefore", "therein", "thereupon", "these", "they", "thick", "thin", "third", "this", "those", "though", "three", 
                 "through", "throughout", "thru", "thus", "to", "together", "too", "top", "toward", "towards", "twelve", "twenty", "two", "un", 
                 "under", "until", "up", "upon", "us", "very", "via", "was", "we", "well", "were", "what", "whatever", "when", "whence", "whenever", 
                 "where", "whereafter", "whereas", "whereby", "wherein", "whereupon", "wherever", "whether", "which", "while", "whither", "who", 
                 "whoever", "whole", "whom", "whose", "why", "will", "with", "within", "without", "would", "yet", "you", "your", "yours", "yourself", 
                 "yourselves" };

        public static readonly string[] StopWordsEnglish5
           = { "a", "able", "about", "above", "abst", "accordance", "according", "accordingly", "across", "act", "actually", "added", "adj", 
                 "adopted", "affected", "affecting", "affects", "after", "afterwards", "again", "against", "ah", "all", "almost", "alone", 
                 "along", "already", "also", "although", "always", "am", "among", "amongst", "an", "and", "announce", "another", "any", "anybody", 
                 "anyhow", "anymore", "anyone", "anything", "anyway", "anyways", "anywhere", "apparently", "approximately", "are", "aren", "arent", 
                 "arise", "around", "as", "aside", "ask", "asking", "at", "auth", "available", "away", "awfully", "b", "back", "be", "became", 
                 "because", "become", "becomes", "becoming", "been", "before", "beforehand", "begin", "beginning", "beginnings", "begins", 
                 "behind", "being", "believe", "below", "beside", "besides", "between", "beyond", "biol", "both", "brief", "briefly", "but", "by", 
                 "c", "ca", "came", "can", "cannot", "can't", "cause", "causes", "certain", "certainly", "co", "com", "come", "comes", "contain", 
                 "containing", "contains", "could", "couldnt", "d", "date", "did", "didn't", "different", "do", "does", "doesn't", "doing", "done",
                 "don't", "down", "downwards", "due", "during", "e", "each", "ed", "edu", "effect", "eg", "eight", "eighty", "either", "else", 
                 "elsewhere", "end", "ending", "enough", "especially", "et", "et-al", "etc", "even", "ever", "every", "everybody", "everyone", 
                 "everything", "everywhere", "ex", "except", "f", "far", "few", "ff", "fifth", "first", "five", "fix", "followed", "following", 
                 "follows", "for", "former", "formerly", "forth", "found", "four", "from", "further", "furthermore", "g", "gave", "get", "gets", 
                 "getting", "give", "given", "gives", "giving", "go", "goes", "gone", "got", "gotten", "h", "had", "happens", "hardly", "has", 
                 "hasn't", "have", "haven't", "having", "he", "hed", "hence", "her", "here", "hereafter", "hereby", "herein", "heres", "hereupon", 
                 "hers", "herself", "hes", "hi", "hid", "him", "himself", "his", "hither", "home", "how", "howbeit", "however", "hundred", "i", 
                 "id", "ie", "if", "i'll", "im", "immediate", "immediately", "importance", "important", "in", "inc", "indeed", "index", "information", 
                 "instead", "into", "invention", "inward", "is", "isn't", "it", "itd", "it'll", "its", "itself", "i've", "j", "just", "k", "keep", 
                 "keeps", "kept", "keys", "kg", "km", "know", "known", "knows", "l", "largely", "last", "lately", "later", "latter", "latterly", 
                 "least", "less", "lest", "let", "lets", "like", "liked", "likely", "line", "little", "'ll", "look", "looking", "looks", "ltd", "m", 
                 "made", "mainly", "make", "makes", "many", "may", "maybe", "me", "mean", "means", "meantime", "meanwhile", "merely", "mg", "might", 
                 "million", "miss", "ml", "more", "moreover", "most", "mostly", "mr", "mrs", "much", "mug", "must", "my", "myself", "n", "na", "name", 
                 "namely", "nay", "nd", "near", "nearly", "necessarily", "necessary", "need", "needs", "neither", "never", "nevertheless", "new", 
                 "next", "nine", "ninety", "no", "nobody", "non", "none", "nonetheless", "noone", "nor", "normally", "nos", "not", "noted", "nothing", 
                 "now", "nowhere", "o", "obtain", "obtained", "obviously", "of", "off", "often", "oh", "ok", "okay", "old", "omitted", "on", "once", 
                 "one", "ones", "only", "onto", "or", "ord", "other", "others", "otherwise", "ought", "our", "ours", "ourselves", "out", "outside", 
                 "over", "overall", "owing", "own", "p", "page", "pages", "part", "particular", "particularly", "past", "per", "perhaps", "placed", 
                 "please", "plus", "poorly", "possible", "possibly", "potentially", "pp", "predominantly", "present", "previously", "primarily", 
                 "probably", "promptly", "proud", "provides", "put", "q", "que", "quickly", "quite", "qv", "r", "ran", "rather", "rd", "re", "readily", 
                 "really", "recent", "recently", "ref", "refs", "regarding", "regardless", "regards", "related", "relatively", "research", "respectively", 
                 "resulted", "resulting", "results", "right", "run", "s", "said", "same", "saw", "say", "saying", "says", "sec", "section", "see", 
                 "seeing", "seem", "seemed", "seeming", "seems", "seen", "self", "selves", "sent", "seven", "several", "shall", "she", "shed", "she'll", 
                 "shes", "should", "shouldn't", "show", "showed", "shown", "showns", "shows", "significant", "significantly", "similar", "similarly",
                 "since", "six", "slightly", "so", "some", "somebody", "somehow", "someone", "somethan", "something", "sometime", "sometimes", "somewhat",
                 "somewhere", "soon", "sorry", "specifically", "specified", "specify", "specifying", "state", "states", "still", "stop", "strongly", 
                 "sub", "substantially", "successfully", "such", "sufficiently", "suggest", "sup", "sure", "t", "take", "taken", "taking", "tell", 
                 "tends", "th", "than", "thank", "thanks", "thanx", "that", "that'll", "thats", "that've", "the", "their", "theirs", "them", "themselves", 
                 "then", "thence", "there", "thereafter", "thereby", "thered", "therefore", "therein", "there'll", "thereof", "therere", "theres",
                 "thereto", "thereupon", "there've", "these", "they", "theyd", "they'll", "theyre", "they've", "think", "this", "those", "thou", "though",
                 "thoughh", "thousand", "throug", "through", "throughout", "thru", "thus", "til", "tip", "to", "together", "too", "took", "toward", 
                 "towards", "tried", "tries", "truly", "try", "trying", "ts", "twice", "two", "u", "un", "under", "unfortunately", "unless", "unlike", 
                 "unlikely", "until", "unto", "up", "upon", "ups", "us", "use", "used", "useful", "usefully", "usefulness", "uses", "using", "usually", 
                 "v", "value", "various", "'ve", "very", "via", "viz", "vol", "vols", "vs", "w", "want", "wants", "was", "wasn't", "way", "we", "wed", 
                 "welcome", "we'll", "went", "were", "weren't", "we've", "what", "whatever", "what'll", "whats", "when", "whence", "whenever", "where",
                 "whereafter", "whereas", "whereby", "wherein", "wheres", "whereupon", "wherever", "whether", "which", "while", "whim", "whither", "who", 
                 "whod", "whoever", "whole", "who'll", "whom", "whomever", "whos", "whose", "why", "widely", "willing", "wish", "with", "within", "without",
                 "won't", "words", "world", "would", "wouldn't", "www", "x", "y", "yes", "yet", "you", "youd", "you'll", "your", "youre", "yours", "yourself", 
                 "yourselves", "you've", "z", "zero" };

        public static readonly string[] StopWordsGerman1
            = { "aber", "als", "am", "an", "auch", "auf", "aus", "bei", "bin", "bis", "bist", "da", "dadurch", "daher", "darum", "das", "daß", "dass", 
                  "dein", "deine", "dem", "den", "der", "des", "dessen", "deshalb", "die", "dies", "dieser", "dieses", "doch", "dort", "du", "durch", 
                  "ein", "eine", "einem", "einen", "einer", "eines", "er", "es", "euer", "eure", "für", "hatte", "hatten", "hattest", "hattet", "hier", 
                  "hinter", "ich", "ihr", "ihre", "im", "in", "ist", "ja", "jede", "jedem", "jeden", "jeder", "jedes", "jener", "jenes", "jetzt", "kann", 
                  "kannst", "können", "könnt", "machen", "mein", "meine", "mit", "muß", "mußt", "musst", "müssen", "müßt", "nach", "nachdem", "nein", 
                  "nicht", "nun", "oder", "seid", "sein", "seine", "sich", "sie", "sind", "soll", "sollen", "sollst", "sollt", "sonst", "soweit", "sowie", 
                  "und", "unser", "unsere", "unter", "vom", "von", "vor", "wann", "warum", "was", "weiter", "weitere", "wenn", "wer", "werde", "werden", 
                  "werdet", "weshalb", "wie", "wieder", "wieso", "wir", "wird", "wirst", "wo", "woher", "wohin", "zu", "zum", "zur", "über" };

        public static readonly string[] StopWordsGerman2
            = { "ab", "bei", "da", "deshalb", "ein", "für", "haben", "hier", "ich", "ja", "kann", "machen", "muesste", "nach", "oder", "seid", "sonst", 
                  "und", "vom", "wann", "wenn", "wie", "zu", "bin", "eines", "hat", "manche", "solches", "an", "anderm", "bis", "das", "deinem", "demselben",
                  "dir", "doch", "einig", "er", "eurer", "hatte", "ihnen", "ihre", "ins", "jenen", "keinen", "manchem", "meinen", "nichts", "seine", "soll", 
                  "unserm", "welche", "werden", "wollte", "während", "alle", "allem", "allen", "aller", "alles", "als", "also", "am", "ander", "andere", 
                  "anderem", "anderen", "anderer", "anderes", "andern", "anderr", "anders", "auch", "auf", "aus", "bist", "bsp.", "daher", "damit", "dann", 
                  "dasselbe", "dazu", "daß", "dein", "deine", "deinen", "deiner", "deines", "dem", "den", "denn", "denselben", "der", "derer", "derselbe", 
                  "derselben", "des", "desselben", "dessen", "dich", "die", "dies", "diese", "dieselbe", "dieselben", "diesem", "diesen", "dieser", "dieses", 
                  "dort", "du", "durch", "eine", "einem", "einen", "einer", "einige", "einigem", "einigen", "einiger", "einiges", "einmal", "es", "etwas", 
                  "euch", "euer", "eure", "eurem", "euren", "eures", "ganz", "ganze", "ganzen", "ganzer", "ganzes", "gegen", "gemacht", "gesagt", "gesehen", 
                  "gewesen", "gewollt", "hab", "habe", "hatten", "hin", "hinter", "ihm", "ihn", "ihr", "ihrem", "ihren", "ihrer", "ihres", "im", "in", 
                  "indem", "ist", "jede", "jedem", "jeden", "jeder", "jedes", "jene", "jenem", "jener", "jenes", "jetzt", "kein", "keine", "keinem", "keiner", 
                  "keines", "konnte", "können", "könnte", "mache", "machst", "macht", "machte", "machten", "man", "manchen", "mancher", "manches", "mein", 
                  "meine", "meinem", "meiner", "meines", "mich", "mir", "mit", "muss", "musste", "müßt", "nicht", "noch", "nun", "nur", "ob", "ohne", "sage", 
                  "sagen", "sagt", "sagte", "sagten", "sagtest", "sehe", "sehen", "sehr", "seht", "sein", "seinem", "seinen", "seiner", "seines", "selbst", 
                  "sich", "sicher", "sie", "sind", "so", "solche", "solchem", "solchen", "solcher", "sollte", "sondern", "um", "uns", "unse", "unsen", 
                  "unser", "unses", "unter", "viel", "von", "vor", "war", "waren", "warst", "was", "weg", "weil", "weiter", "welchem", "welchen", "welcher", 
                  "welches", "werde", "wieder", "will", "wir", "wird", "wirst", "wo", "wolle", "wollen", "wollt", "wollten", "wolltest", "wolltet", "würde", 
                  "würden", "z.B.", "zum", "zur", "zwar", "zwischen", "über", "aber", "abgerufen", "abgerufene", "abgerufener", "abgerufenes", "acht", "acute", 
                  "allein", "allerdings", "allerlei", "allg", "allgemein", "allmählich", "allzu", "alsbald", "amp", "and", "andererseits", "andernfalls", 
                  "anerkannt", "anerkannte", "anerkannter", "anerkanntes", "anfangen", "anfing", "angefangen", "angesetze", "angesetzt", "angesetzten", 
                  "angesetzter", "ansetzen", "anstatt", "arbeiten", "aufgehört", "aufgrund", "aufhören", "aufhörte", "aufzusuchen", "ausdrücken", "ausdrückt", 
                  "ausdrückte", "ausgenommen", "ausser", "ausserdem", "author", "autor", "außen", "außer", "außerdem", "außerhalb", "background", "bald", 
                  "bearbeite", "bearbeiten", "bearbeitete", "bearbeiteten", "bedarf", "bedurfte", "bedürfen", "been", "befragen", "befragte", "befragten", 
                  "befragter", "begann", "beginnen", "begonnen", "behalten", "behielt", "beide", "beiden", "beiderlei", "beides", "beim", "beinahe", 
                  "beitragen", "beitrugen", "bekannt", "bekannte", "bekannter", "bekennen", "benutzt", "bereits", "berichten", "berichtet", "berichtete", 
                  "berichteten", "besonders", "besser", "bestehen", "besteht", "beträchtlich", "bevor", "bezüglich", "bietet", "bisher", "bislang", "biz", 
                  "bleiben", "blieb", "bloss", "bloß", "border", "brachte", "brachten", "brauchen", "braucht", "bringen", "bräuchte", "bzw", "böden", "ca", 
                  "ca.", "collapsed", "com", "comment", "content", "da?", "dabei", "dadurch", "dafür", "dagegen", "dahin", "damals", "danach", "daneben", 
                  "dank", "danke", "danken", "dannen", "daran", "darauf", "daraus", "darf", "darfst", "darin", "darum", "darunter", "darüber", "darüberhinaus", 
                  "dass", "davon", "davor", "demnach", "denen", "dennoch", "derart", "derartig", "derem", "deren", "derjenige", "derjenigen", "derzeit", 
                  "desto", "deswegen", "diejenige", "diesseits", "dinge", "direkt", "direkte", "direkten", "direkter", "doc", "doppelt", "dorther", "dorthin", 
                  "drauf", "drei", "dreißig", "drin", "dritte", "drunter", "drüber", "dunklen", "durchaus", "durfte", "durften", "dürfen", "dürfte", "eben", 
                  "ebenfalls", "ebenso", "ehe", "eher", "eigenen", "eigenes", "eigentlich", "einbaün", "einerseits", "einfach", "einführen", "einführte", 
                  "einführten", "eingesetzt", "einigermaßen", "eins", "einseitig", "einseitige", "einseitigen", "einseitiger", "einst", "einstmals", "einzig", 
                  "elf", "ende", "entsprechend", "entweder", "ergänze", "ergänzen", "ergänzte", "ergänzten", "erhalten", "erhielt", "erhielten", "erhält", 
                  "erneut", "erst", "erste", "ersten", "erster", "eröffne", "eröffnen", "eröffnet", "eröffnete", "eröffnetes", "etc", "etliche", "etwa", 
                  "fall", "falls", "fand", "fast", "ferner", "finden", "findest", "findet", "folgende", "folgenden", "folgender", "folgendes", "folglich", 
                  "for", "fordern", "fordert", "forderte", "forderten", "fortsetzen", "fortsetzt", "fortsetzte", "fortsetzten", "fragte", "frau", "frei", 
                  "freie", "freier", "freies", "fuer", "fünf", "gab", "ganzem", "gar", "gbr", "geb", "geben", "geblieben", "gebracht", "gedurft", "geehrt", 
                  "geehrte", "geehrten", "geehrter", "gefallen", "gefiel", "gefälligst", "gefällt", "gegeben", "gehabt", "gehen", "geht", "gekommen", "gekonnt", 
                  "gemocht", "gemäss", "genommen", "genug", "gern", "gestern", "gestrige", "getan", "geteilt", "geteilte", "getragen", "gewissermaßen", 
                  "geworden", "ggf", "gib", "gibt", "gleich", "gleichwohl", "gleichzeitig", "glücklicherweise", "gmbh", "gratulieren", "gratuliert", 
                  "gratulierte", "gute", "guten", "gängig", "gängige", "gängigen", "gängiger", "gängiges", "gänzlich", "haette", "halb", "hallo", "hast", 
                  "hattest", "hattet", "heraus", "herein", "heute", "heutige", "hiermit", "hiesige", "hinein", "hinten", "hinterher", "hoch", "html", "http", 
                  "hundert", "hätt", "hätte", "hätten", "höchstens", "igitt", "image", "immer", "immerhin", "important", "indessen", "info", "infolge", "innen", 
                  "innerhalb", "insofern", "inzwischen", "irgend", "irgendeine", "irgendwas", "irgendwen", "irgendwer", "irgendwie", "irgendwo", "je", "jed", 
                  "jedenfalls", "jederlei", "jedoch", "jemand", "jenseits", "jährig", "jährige", "jährigen", "jähriges", "kam", "kannst", "kaum", "kei nes", 
                  "keinerlei", "keineswegs", "klar", "klare", "klaren", "klares", "klein", "kleinen", "kleiner", "kleines", "koennen", "koennt", "koennte", 
                  "koennten", "komme", "kommen", "kommt", "konkret", "konkrete", "konkreten", "konkreter", "konkretes", "konnten", "könn", "könnt", "könnten", 
                  "künftig", "lag", "lagen", "langsam", "lassen", "laut", "lediglich", "leer", "legen", "legte", "legten", "leicht", "leider", "lesen", "letze",
                  "letzten", "letztendlich", "letztens", "letztes", "letztlich", "lichten", "liegt", "liest", "links", "längst", "längstens", "mag", "magst", 
                  "mal", "mancherorts", "manchmal", "mann", "margin", "med", "mehr", "mehrere", "meist", "meiste", "meisten", "meta", "mindestens", "mithin", 
                  "mochte", "morgen", "morgige", "muessen", "muesst", "musst", "mussten", "muß", "mußt", "möchte", "möchten", "möchtest", "mögen", "möglich", 
                  "mögliche", "möglichen", "möglicher", "möglicherweise", "müssen", "müsste", "müssten", "müßte", "nachdem", "nacher", "nachhinein", "nahm", 
                  "natürlich", "ncht", "neben", "nebenan", "nehmen", "nein", "neu", "neue", "neuem", "neuen", "neuer", "neues", "neun", "nie", "niemals", 
                  "niemand", "nimm", "nimmer", "nimmt", "nirgends", "nirgendwo", "nter", "nutzen", "nutzt", "nutzung", "nächste", "nämlich", "nötigenfalls", 
                  "nützt", "oben", "oberhalb", "obgleich", "obschon", "obwohl", "oft", "online", "org", "padding", "per", "pfui", "plötzlich", "pro", "reagiere", 
                  "reagieren", "reagiert", "reagierte", "rechts", "regelmäßig", "rief", "rund", "sang", "sangen", "schlechter", "schließlich", "schnell", 
                  "schon", "schreibe", "schreiben", "schreibens", "schreiber", "schwierig", "schätzen", "schätzt", "schätzte", "schätzten", "sechs", "sect", 
                  "sehrwohl", "sei", "seit", "seitdem", "seite", "seiten", "seither", "selber", "senke", "senken", "senkt", "senkte", "senkten", "setzen", 
                  "setzt", "setzte", "setzten", "sicherlich", "sieben", "siebte", "siehe", "sieht", "singen", "singt", "sobald", "sodaß", "soeben", "sofern", 
                  "sofort", "sog", "sogar", "solange", "solchen", "solch", "sollen", "sollst", "sollt", "sollten", "solltest", "somit", "sonstwo", "sooft", 
                  "soviel", "soweit", "sowie", "sowohl", "spielen", "später", "startet", "startete", "starteten", "statt", "stattdessen", "steht", "steige", 
                  "steigen", "steigt", "stets", "stieg", "stiegen", "such", "suchen", "sämtliche", "tages", "tat", "tatsächlich", "tatsächlichen", 
                  "tatsächlicher", "tatsächliches", "tausend", "teile", "teilen", "teilte", "teilten", "titel", "total", "trage", "tragen", "trotzdem", "trug", 
                  "trägt", "tun", "tust", "tut", "txt", "tät", "ueber", "umso", "unbedingt", "ungefähr", "unmöglich", "unmögliche", "unmöglichen", "unmöglicher", 
                  "unnötig", "unsem", "unser", "unsere", "unserem", "unseren", "unserer", "unseres", "unten", "unterbrach", "unterbrechen", "unterhalb", 
                  "unwichtig", "usw", "var", "vergangen", "vergangene", "vergangener", "vergangenes", "vermag", "vermutlich", "vermögen", "verrate", "verraten", 
                  "verriet", "verrieten", "version", "versorge", "versorgen", "versorgt", "versorgte", "versorgten", "versorgtes", "veröffentlichen", 
                  "veröffentlicher", "veröffentlicht", "veröffentlichte", "veröffentlichten", "veröffentlichtes", "viele", "vielen", "vieler", "vieles", 
                  "vielleicht", "vielmals", "vier", "vollständig", "voran", "vorbei", "vorgestern", "vorher", "vorne", "vorüber", "völlig", "während", "wachen", 
                  "waere", "warum", "weder", "wegen", "weitere", "weiterem", "weiteren", "weiterer", "weiteres", "weiterhin", "weiß", "wem", "wen", "wenig", 
                  "wenige", "weniger", "wenigstens", "wenngleich", "wer", "werdet", "weshalb", "wessen", "wichtig", "wieso", "wieviel", "wiewohl", "willst", 
                  "wirklich", "wodurch", "wogegen", "woher", "wohin", "wohingegen", "wohl", "wohlweislich", "womit", "woraufhin", "woraus", "worin", "wurde",
                  "wurden", "währenddessen", "wär", "wäre", "wären", "zahlreich", "zehn", "zeitweise", "ziehen", "zieht", "zog", "zogen", "zudem", "zuerst", 
                  "zufolge", "zugleich", "zuletzt", "zumal", "zurück", "zusammen", "zuviel", "zwanzig", "zwei", "zwölf", "ähnlich", "übel", "überall", 
                  "überallhin", "überdies", "übermorgen", "übrig", "übrigens" };

        #endregion
        #region weights

        private double wExact = 0.21;
        private double wApprox = 0.19;
        private double wEval = 0.19;
        private double wOpen = 0.18;
        private double wAge = 0.23;

        #endregion
        #region connection

        private string connectionString;
        private bool isConnected;

        #endregion
        #region tables

        /// <summary>
        /// The tokens.
        /// </summary>
        public readonly Table<Token> TokenTable;
        
        /// <summary>
        /// The stop-words.
        /// </summary>
        public readonly Table<StopWord> StopWordTable;
        
        /// <summary>
        /// The indexed documents.
        /// </summary>
        public readonly Table<Document> DocumentTable;
        
        /// <summary>
        /// A white-list
        /// </summary>
        public readonly Table<WhiteListWord> WhiteListTable;

        /// <summary>
        /// Table of frequent queries.
        /// </summary>
        public readonly Table<FrequentQuery> FrequentQueryTable;

        /// <summary>
        /// Table of user-rated search result.
        /// </summary>
        public readonly Table<ResultEvaluation> ResultEvaluationTable;

        /// <summary>
        /// Token occurrences.
        /// </summary>
        public readonly Table<TokenOccurrence> TokenOccurrenceTable;

        /// <summary>
        /// Document metadata.
        /// </summary>
        public readonly Table<MetaData> MetaDataTable;

        #endregion

        #endregion
        #region prop

        #region Main

        /// <summary>
        /// A document processor.
        /// </summary>
        public IDocumentProcessor DocumentProcessor { get; set; }

        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Get or set the status. If this is set to true, the DataSource of all mapping classes will be set to this IndexDatabase instance.
        /// </summary>
        public bool IsActiveInstance
        {
            get
            {
                return isActiveInstance;
            }
            set
            {
                if (value == true)
                {
                    foreach (IndexDatabase db in openInstances) db.IsActiveInstance = false;
                    Token.DataSource = this;
                    Document.DataSource = this;
                    TokenOccurrence.DataSource = this;
                    ResultEvaluation.DataSource = this;
                    // not needed yet: FrequentQuery, WhiteList, StopWord
                    activeInstance = this;
                }
                isActiveInstance = value;
            }
        }

        /// <summary>
        /// Get or set the active instance reference.
        /// </summary>
        public static IndexDatabase ActiveInstance
        {
            get
            {
                return activeInstance;
            }
            set
            {
                if (value != null)
                {
                    value.IsActiveInstance = true;
                }
                else
                {
                    activeInstance = value;
                }
            }
        }

        #endregion
        #region Connection

        public string ConnectionString { get { return connectionString; } }
        public bool IsConnected { get { return isConnected; } }
        
        #endregion
        #region Indexing

        //public int MaxNumberOfTokenPositions { get; set; }
        public bool CalculateTokenPositionVariance { get; set; }
        public bool CalculateTokenPositionSteepness { get; set; }
        public bool CalculateTokenPositionAverage { get; set; }
        public bool CalculateTokenPositionMedian { get; set; }
        public bool CountTokenOccurence { get; set; }

        #endregion
        #region Searching

        public bool UseRankingFunction { get; set; }

        public Func<Document, SearchQuery, double> RankingFunction { get; set; }

        public double RankingWeightExact
        {
            get { return wExact; }
            set { wExact = value; }
        }

        public double RankingWeightApprox
        {
            get { return wApprox; }
            set { wApprox = value; }
        }

        public double RankingWeightEval
        {
            get { return wEval; }
            set { wEval = value; }
        }

        public double RankingWeightOpen
        {
            get { return wOpen; }
            set { wOpen = value; }
        }

        public double RankingWeightAge
        {
            get { return wAge; }
            set { wAge = value; }
        }

        #endregion
        #region Statistics

        /// <summary>
        /// defaults to 1000.
        /// </summary>
        public int NumberOfFrequentSearchesToStore { get; set; }

        #endregion
        #region Data

        /// <summary>
        /// Number of documents.
        /// </summary>
        public int DocumentCount
        {
            get 
            {
                return DocumentTable.Count();
            }
        }

        /// <summary>
        /// The number of duplicates found in this session.
        /// </summary>
        public int Duplicates { get; set; }

        public double MaxOpenCount { get; set; }

        public double MaxAge { get; set; }

        #endregion

        #endregion
        #region ctor

        static IndexDatabase()
        {
            openInstances = new List<IndexDatabase>();
        }

        /// <summary>
        /// Connect DataContext using given connection string and
        /// create an instance of IndexDatabase
        /// CAUTION: Further initialization required before full use. See <code>Initialize</code>.
        /// </summary>
        /// <param name="connectionString">a connection string or db filename</param>
        /// <param name="processor">If no document processor is provided the default processor will be used.</param>
        public IndexDatabase(string connectionString, IDocumentProcessor processor = null)
            : base(connectionString)
        {
            openInstances.Add(this);

            this.connectionString = connectionString;
            this.isConnected = true;

            this.Log<INFO>("Connected to " + base.Connection.DataSource);
            
            //MaxHits = 1000;
            //MaxHitsPerTerm = 100;
            //MaxNumberOfTokenPositions = 10;
            NumberOfFrequentSearchesToStore = 1000;

            if (processor == null)
            {
                DefaultProcessor proc = new DefaultProcessor(this, false);
                proc.SetDefaultResolvers();
                this.DocumentProcessor = proc;
            }
            else this.DocumentProcessor = processor;

            this.RankingFunction = DefaultRankingFunction;


            this.Log<INFO>("IndexDatabase ready.");
        }

        #endregion
        #region init

        /// <summary>
        /// Initialize this instance.
        /// </summary>
        /// <returns>success flag</returns>
        public bool Initialize(bool resetDB = false)
        {
            #region checks

            if (IsInitialized)
            {
#if DEBUG
                this.Log<WARN>("The IndexDatabase was already initialized.");
#endif
                return true;
            }

            try
            {
                bool b = DatabaseExists();
            }
            catch (Exception e)
            {
                this.Log<FATAL>("Error opening DB connection: ", e);
                return false;
            }

            try
            {
                if (resetDB && DatabaseExists()) DeleteDatabase();
            }
            catch (Exception e)
            {
                this.Log<ERROR>("Error resetting the database: ", e);
                return false;
            }

            try
            {
                if (!DatabaseExists()) CreateDatabase();
            }
            catch (Exception e)
            {
                this.Log<FATAL>("Error creating a new database: ", e);
                return false;
            }

            try
            {
                SubmitChanges();
            }
            catch (Exception e)
            {
                this.Log<FATAL>("Error while trying to submit data: ", e);
                return false;
            }

            #endregion

            if(Settings.Default.ReadUncommitted) 
                ExecuteCommand("SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;");

            if (FrequentQueryTable.Any()) 
                CleanupFrequentQueryTable();
            
            if (DocumentTable.Any())
            {
                MaxOpenCount = (from row in DocumentTable
                                select row.OpenCount).Max();

                MaxAge = 0; // TODO: ██████ PERFORMANCE
                foreach (Document doc in DocumentTable)
                    if (doc.AgeDays > MaxAge) MaxAge = doc.AgeDays;
            }

            IsInitialized = true;
            return true;
        }

        public void SetInitialized() { IsInitialized = true; }

        #endregion
        #region impl

        #region ctrl

        public void Close()
        {
            Connection.Close();
        }

        #endregion
        #region documents

        /// <summary>
        /// Get the token count for a document by hash value.
        /// </summary>
        /// <param name="checksum"></param>
        /// <returns>Will return -1 if the document is not in the database.</returns>
        public int TokenCount(int checksum)
        {
            var q = (from row in DocumentTable where row.Checksum == checksum select row.TokenCount);
            if (q.Any()) return q.First();
            return -1;
        }

        #region exists

        /// <summary>
        /// Find out if a document already exists in the database. This
        /// will only compare for the hash, not for the path. If the
        /// </summary>
        /// <param name="checksum">The hash value of the document.</param>
        /// <returns>True only if a document with the given hash exists in the database.</returns>
        public bool DocumentExists(int checksum)
        {
            var q = from row in DocumentTable 
                    where row.Checksum == checksum 
                    select row;

            Exception ex;
            bool result;

            if (!ExceptionHelper.RetryOperationDelayed<bool>(() => q.Any(), 10, TimeSpan.FromSeconds(3), out result, out ex))
                throw ex;

            return result;
        }

        /// <summary>
        /// Find out if a document already exists in the database. This
        /// only returns true if the document exists with the given path.
        /// If the same document is already in the database but with a 
        /// different path, this will return false.
        /// </summary>
        /// <param name="checksum">The hash value of the document.</param>
        /// <param name="path">The path of the document.</param>
        /// <returns>True only if a document with the given hash AND the given path exists in the database.</returns>
        public bool DocumentExists(int checksum, string path)
        {
            var q = from row in DocumentTable
                    where row.Checksum == checksum && row.Path == path
                    select row;

            Exception ex;
            bool result;

            if (!ExceptionHelper.RetryOperationDelayed<bool>(() => q.Any(), 10, TimeSpan.FromSeconds(3), out result, out ex))
                throw ex;

            return result;
        }

        /// <summary>
        /// Find out if a document with the given path and filename exists in the database.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool DocumentExists(string path)
        {
            var q = from row in DocumentTable
                    where row.Path == path
                    select row;

            Exception ex;
            bool result;

            if (!ExceptionHelper.RetryOperationDelayed<bool>(() => q.Any(), 10, TimeSpan.FromSeconds(3), out result, out ex))
                throw ex;

            return result;
        }

        #endregion
        #region retrieve

        /// <summary>
        /// Retrieve a document with the given path from the document table. If no match is found this will return null.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Document GetDocument(string path)
        {
            var q = from row in DocumentTable
                    where row.Path == path
                    select row;

            Exception ex;
            Document firstOrDefault;

            if (!ExceptionHelper.RetryOperationDelayed<Document>(() => q.FirstOrDefault(), 10, TimeSpan.FromSeconds(2), out firstOrDefault, out ex))
                throw ex;

            return firstOrDefault;
        }

        /// <summary>
        /// Retrieve a document with the given checksum and path from the document table. If no match is found this will return null.
        /// </summary>
        /// <param name="checksum"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public Document GetDocument(int checksum, string path)
        {
            var q = from row in DocumentTable
                    where row.Checksum == checksum && row.Path == path
                    select row;

            Exception ex;
            Document firstOrDefault;

            if (!ExceptionHelper.RetryOperationDelayed<Document>(() => q.FirstOrDefault(), 10, TimeSpan.FromSeconds(2), out firstOrDefault, out ex))
                throw ex;

            return firstOrDefault;
        }

        /// <summary>
        /// Retrieve a document with the given checksum from the document table. If no match is found this will return null.
        /// </summary>
        /// <param name="checksum"></param>
        /// <returns></returns>
        public Document GetDocument(int checksum)
        {
            var q = from row in DocumentTable 
                    where row.Checksum == checksum 
                    select row;

            Exception ex;
            Document firstOrDefault;

            if (!ExceptionHelper.RetryOperationDelayed<Document>(() => q.FirstOrDefault(), 10, TimeSpan.FromSeconds(2), out firstOrDefault, out ex))
                throw ex;

            return firstOrDefault;
        }

        #endregion
        #region add or update

        /// <summary>
        /// Create a new Document in the database.
        /// 
        /// If doSubmitChanges is set to false (default) this 
        /// does not submit all changes. (A new document will be 
        /// submitted, but the token count will be missing if it 
        /// is not provided in tokenCount or by the processor.)
        /// 
        /// CAUTION: this will create a new document entry if an existing 
        /// document with changed checksum is given (the caller may want
        /// to have a history for the document). If the document is 
        /// unchanged, nothing will happen though.
        /// </summary>
        /// <param name="checksum"></param>
        /// <param name="path"></param>
        /// <param name="tokenCount">
        /// If known, provide the number of tokens in the document. 
        /// Otherwise use -1 to indicate that you do not know the value.
        /// In this case the value will either be retrieved from the db
        /// if the document already exists or by invoking the processor.
        /// </param>
        /// <param name="data">
        /// If you do not provide data directly the processor will try
        /// to retrieve it using it's resolvers. In case of an existing
        /// document the data will be ignored.
        /// </param>
        public bool AddDocument(int checksum, string path, int tokenCount = -1, IEnumerable<string> data = null, bool doSubmitChanges = false)
        {
            try
            {
                if (DocumentExists(checksum, path)) return true;
                else if (DocumentExists(checksum))
                {
                    Duplicates++;
#if DEBUG
                this.Log<WARN>("Found a duplicate document.");
#endif
                }

                int c = tokenCount;
                Document doc = new Document(checksum, path, c);
                DocumentTable.InsertOnSubmit(doc);
                SubmitChanges();

                if (data != null) // data was already provided as parameter
                {
                    doc.HasData = true;
                    doc.Data = data;
                }

                c = DocumentProcessor.ProcessDocument(doc);
                if (doc.TokenCount == -1) doc.TokenCount = c;

                if (doSubmitChanges) SubmitChanges();

                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Create a new Document in the database.
        /// 
        /// If doSubmitChanges is set to false (default) this 
        /// does not submit all changes. (A new document will be 
        /// submitted, but the token count will be missing if it 
        /// is not provided in tokenCount or by the processor.)
        /// 
        /// CAUTION: this will create a new document entry if an existing 
        /// document with changed checksum is given (the caller may want
        /// to have a history for the document). If the document is 
        /// unchanged, nothing will happen though.
        /// 
        /// CAUTION: A checksum and a path for the document must be provided!
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="doSubmitChanges"></param>
        public bool AddDocument(Document doc, bool doSubmitChanges = false)
        {
            try
            {
                if (DocumentExists(doc.Checksum, doc.Path)) return true;
                else if (DocumentExists(doc.Checksum))
                {
                    Duplicates++;
#if DEBUG
                this.Log<WARN>("Found a duplicate document.");
#endif
                }

                DocumentTable.InsertOnSubmit(doc);
                SubmitChanges();

                int c = DocumentProcessor.ProcessDocument(doc);
                if (doc.TokenCount == -1) doc.TokenCount = c;

                if (doSubmitChanges) SubmitChanges();

                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Re-process the given document, assuming the contents have changed.
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="doSubmitChanges"></param>
        public bool UpdateDocument(Document doc, bool doSubmitChanges = false)
        {
            try
            {
                doc.Reset(this);
                doc.TokenCount = DocumentProcessor.ProcessDocument(doc);
                if (doSubmitChanges) SubmitChanges();
                return true;
            }
            catch { return false; }
        }

        #endregion

        #endregion
        #region tokens

        /// <summary>
        /// Get the total count for a given token
        /// </summary>
        /// <param name="token"></param>
        /// <returns>The total count for a given token.</returns>
        public int TotalCount(Token token)
        {
            var q = (from row in TokenOccurrenceTable where row.TokenID == token.ID select row.Count);
            if (q.Any()) return q.Sum();
            return 0;
        }

        /// <summary>
        /// Retrieves the token for the given string or creates and submits a new token if it doesn't exist.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Token GetToken(string data)
        {
            var q = from row in TokenTable where row.TokenData == data select row;
            if (q.Any()) return q.First();
            Token t = new Token(data);
            TokenTable.InsertOnSubmit(t);
            SubmitChanges();
            return t;
        }

        /// <summary>
        /// Only exact matches
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public IQueryable<Token> FindFullTokenCaseSensitive(string word)
        {
            return from row in TokenTable where row.TokenData == word select row;
        }

        /// <summary>
        /// Ignores casing.
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public IQueryable<Token> FindFullTokenIgnoreCase(string word)
        {
            return from row in TokenTable where row.TokenData.ToUpper() == word.ToUpper() select row;
        }

        /// <summary>
        /// This uses contains which will be translated to a LIKE operator.
        /// It depends on the database installation if this really ignores casing!
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public IQueryable<Token> FindPartTokenIgnoreCase(string word)
        {
            return from row in TokenTable where row.TokenData.Contains(word) select row;
        }

        /// <summary>
        /// Return all matches with a levenshtein distance &lt;= maxLevenshtein
        /// </summary>
        /// <param name="word"></param>
        /// <param name="ignoreCase"></param>
        /// <param name="maxLevenshtein"></param>
        /// <returns></returns>
        public IEnumerable<Token> FindToken(string word, bool ignoreCase = true, int maxLevenshtein = 3)
        {
            string data;
            string search = ignoreCase ? word.ToLower() : word;
            foreach (Token t in TokenTable)
            {
                data = ignoreCase ? t.TokenData.ToLower() : t.TokenData;
                int distance = LevenshteinDistance.Get(data, search);
                if (distance <= maxLevenshtein) yield return t;
            }
        }

        #endregion
        #region occurrence

        /// <summary>
        /// Retrieves the token occurrence for the given token or creates and submits a new token occurrence if it doesn't exist.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="document"></param>
        /// <returns></returns>
        public TokenOccurrence GetOccurrence(Token token, Document document)
        {
            var q = 
                from row in TokenOccurrenceTable 
                where row.TokenID == token.ID && row.DocumentID == document.ID 
                select row;

            Exception ex;
            bool tokenExists;

            if (!ExceptionHelper.RetryOperationDelayed<bool>(() => q.Any(), 10, TimeSpan.FromSeconds(3), out tokenExists, out ex))
                throw ex;

            if (tokenExists) return q.First();
            
            TokenOccurrence o = new TokenOccurrence(token, document);
            TokenOccurrenceTable.InsertOnSubmit(o);

            SubmitChanges();

            return o;
        }

        /// <summary>
        /// Count the number of documents in which the given token occurs.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public int CountDocumentsContaining(Token token)
        {
            return TokenOccurrenceTable.Count(o => o.TokenID == token.ID);
        }

        #endregion
        #region search

        #region OR

        /// <summary>
        /// Simple OR search.
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public IQueryable<Document> FindAny(params Token[] tokens)
        {
            return FindAny(tokens, DocumentTable);
        }

        /// <summary>
        /// Simple OR search.
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public IQueryable<Document> FindAny(IEnumerable<Token> tokens)
        {
            return FindAny(tokens, DocumentTable);
        }

        /// <summary>
        /// Simple OR search.
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public IQueryable<Document> FindAny(IEnumerable<Token> tokens, IQueryable<Document> documents)
        {
            var tt = from t in tokens select t.ID;

            var ids = from to in TokenOccurrenceTable
                      join doc in documents on to.DocumentID equals doc.ID
                      where tt.Contains(to.TokenID)
                      group to by to.DocumentID into Group
                      select new { id = Group.Key, count = Group.Count() };

            var res = from id in ids
                      from doc in documents
                      where id.id == doc.ID
                      orderby id.count descending, 
                        doc.ModificationDate descending, 
                        doc.RatingSum / doc.RatingCount descending, 
                        doc.OpenCount descending
                      select doc;

            return res;
        }

        #endregion
        #region AND

        /// <summary>
        /// AND search
        /// Find all documents containing all of the given tokens.
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public IQueryable<Document> FindAllExact(params Token[] tokens)
        {
            return FindAllExact(tokens, DocumentTable);
        }

        /// <summary>
        /// AND search
        /// Find all documents containing all of the given tokens.
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public IQueryable<Document> FindAllExact(IEnumerable<Token> tokens)
        {
            return FindAllExact(tokens, DocumentTable);
        }

        /// <summary>
        /// AND search
        /// Find all documents in the given list which contain all of the given tokens.
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="documents"></param>
        /// <returns></returns>
        public IQueryable<Document> FindAllExact(IEnumerable<Token> tokens, IQueryable<Document> documents)
        {
            var tt = from t in tokens select t.ID;

            var ids = from to in TokenOccurrenceTable
                      join doc in documents on to.DocumentID equals doc.ID
                      where tt.Contains(to.TokenID)
                      group to by to.DocumentID into Group
                      where Group.Count() == tt.Count()
                      select Group.Key;

            var res = from id in ids
                      from doc in documents
                      where id == doc.ID
                      orderby doc.ModificationDate descending, 
                        doc.RatingSum / doc.RatingCount descending, 
                        doc.OpenCount descending
                      select doc;
            
            return res;
        }

        /// <summary>
        /// AND search
        /// Find all documents containing at least one of the tokens from each group.
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public IQueryable<Document> FindOneOfEach(params List<Token>[] tokens)
        {
            return FindOneOfEach(tokens, DocumentTable);
        }

        /// <summary>
        /// AND search
        /// Find all documents containing at least one of the tokens from each group.
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public IQueryable<Document> FindOneOfEach(IEnumerable<IEnumerable<Token>> tokens)
        {
            return FindOneOfEach(tokens, DocumentTable);
        }

        /// <summary>
        /// AND search
        /// Find all documents containing at least one of the tokens from each group.
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="documents"></param>
        /// <returns></returns>
        public IQueryable<Document> FindOneOfEach(IEnumerable<IEnumerable<Token>> tokens, IQueryable<Document> documents)
        {
            var result = documents;

            foreach (IEnumerable<Token> tokenGroup in tokens)
            {
                var ids = from item in tokenGroup select item.ID;
                result = FindAny(tokenGroup, result);
            }

            return result;
        }

        #endregion
        #region NOT

        /// <summary>
        /// NOT search
        /// Find all documents not containing the given tokens.
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public IQueryable<Document> FindNot(params Token[] tokens)
        {
            return FindNot(tokens, DocumentTable);
        }

        /// <summary>
        /// NOT search
        /// Find all documents not containing the given tokens.
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public IQueryable<Document> FindNot(IEnumerable<Token> tokens)
        {
            return FindNot(tokens, DocumentTable);
        }

        /// <summary>
        /// NOT search
        /// Find all documents in the collection which do not contain any of the given tokens.
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="documents"></param>
        /// <returns></returns>
        public IQueryable<Document> FindNot(IEnumerable<Token> tokens, IQueryable<Document> documents)
        {
            return documents.Except(FindAny(tokens, documents));
        }

        #endregion
        #region Rank

        private double DefaultRankingFunction(Document doc, SearchQuery query)
        {
            double exactHitsFraction =
                (double)(doc.CountExactHits(query.RequiredTerms) + doc.CountExactHits(query.OptionalTerms)) /
                (double)(query.RequiredTerms.Count + query.OptionalTerms.Count);

            double approximateHitsFraction =
                (double)doc.CountApproximateHits(query.OptionalTerms) /
                (double)query.OptionalTerms.Count;

            return wExact * exactHitsFraction + 
                wApprox * approximateHitsFraction + 
                wEval * ((double)doc.AverageRating / 100d) + 
                wOpen * ((double)doc.OpenCount / MaxOpenCount) +
                wAge * (1d - (doc.Age.TotalDays / MaxAge));
        }

        #endregion

        #endregion
        #region statistics

        public void UpdateFrequentQueries(SearchQuery query, bool doSubmit = false)
        {
            var q = from row in FrequentQueryTable
                    where row.SearchString == query.ToString()
                    select row;

            if (q.Any()) q.First().SearchCount++;
            else FrequentQueryTable.InsertOnSubmit(new FrequentQuery(query));

            if (doSubmit) SubmitChanges();
        }

        public void UpdateStatistics(SearchQuery query, bool doSubmit)
        {
            if (query.HasExactTokens) IncrementSearchCount(query.ExactTokens, false);
            if (query.PrepareRequiredTokens(this, false, true)) IncrementSearchCount(query.RequiredTokens, false);
            if (query.PrepareOptionalTokens(this, true)) IncrementSearchCount(query.OptionalTokens, false);
            UpdateFrequentQueries(query, doSubmit);
        }

        public void IncrementSearchCount(List<Token> tokens, bool doCommit = true)
        {
            foreach (Token t in tokens) t.SearchCount++;
            if (doCommit) SubmitChanges();
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (base.Connection != null)
            {
                if (base.Connection.State != System.Data.ConnectionState.Closed)
                {
                    base.Connection.Close();
                    base.Connection.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #endregion
        #region util

        private void CleanupFrequentQueryTable()
        {
            var f = from row in FrequentQueryTable
                    orderby row.SearchCount
                    select row;

            if (f.Count() > NumberOfFrequentSearchesToStore)
            {
                int c = 0;
                foreach (FrequentQuery q in f)
                {
                    c++;
                    if (c > NumberOfFrequentSearchesToStore) FrequentQueryTable.DeleteOnSubmit(q);
                }
                SubmitChanges();
            }
        }

        #endregion
    }
}