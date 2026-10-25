using JetBrains.Annotations;
using System;
using System.Collections;
using System.Linq; // needed for Map.All
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking; // needed for WebGL

public enum LinkType
{
    NO_LINK_TYPE = 0,
    CONSTELLATION = 1,
    ASTERISM = 2,
    GEOMETRY = 3
}

public class StarData
{
    public uint hr;
    public uint id;
    public bool mainStar; //main Star in a binary system
    public string name;
    public float ra;
    public float dec;
    public float mag;
    public float dist;
    public float colorIndex;
    public uint binMain;
    public byte binInd;
    public uint[] idsBins = new uint[2] { 0, 0 };
    public byte binCount = 0;
    public Vector3 xyz;
}

public class ConstellationInfo //TBD csv ES
{
    public string id;
    public string itaName;
    public string engName;
    public string meaning;
    public string hemisphere;
    public LinkType type = LinkType.NO_LINK_TYPE;
    public string colorStr = "000000"; //no color
    public Color color = Color.clear; //no color
}

public class StarLinksItem
{
    public string identifier;
    public uint startId;
    public uint endId;
}

// --- DATABASE MANAGER ---

public class StarDatabase : MonoBehaviour
{
    private readonly byte xorKey = 12; //encoder key
    public readonly float maxNakedEyeMagnitude = 5.5f; //should be 6.5f
    const uint maxStars = 3000;

    public bool isLoaded = false;

    // public maps for StarGenerator
    public Dictionary<uint, StarData> starDict = new Dictionary<uint, StarData>();
    public Dictionary<string, ConstellationInfo> constellationDict = new Dictionary<string, ConstellationInfo>();
    public List<StarLinksItem> linkList = new List<StarLinksItem>(); //must be a list: couple of lines with the same constellation ID!

    private class StarCsvIndices
    {
        public readonly string fileName = "hyg_v42.csv"; //https://www.astronexus.com/projects/hyg
        public readonly char separator = ',';
        //https://www.astronexus.com/projects/hyg-details
        const string hrStr = "hr"; //hr -> Harvard Revised catalog, which is the same as its number in the Yale Bright Star Catalog
                                   //hip ->  hipparcos ID
        const string idStr = "id"; //primary id in this database
        const string nameStr = "proper"; //common name
        const string raStr = "RA"; //right ascension for epoch and equinox 2000.0
        const string decStr = "Dec"; //declination for epoch and equinox 2000.0
        const string magStr = "Mag"; //apparent magnitude
        const string distStr = "dist"; //distance ([pc] from catalogue)
        const string colStr = "CI"; //color index when known
        const string conStr = "con"; //std const abbreviation
        const string binIndStr = "comp"; //if the star is the 2nd/3rd star in a system, the number is > 1
        const string binMainStr = "comp_primary"; //if the star is the 2nd/3rd star in a system, != from id
        const string cartStr = "x"; //cartesian coordinates (y,z following)
        const string baseStr = "base"; //alternative/catalogue name

        // -1 as null idx
        public Dictionary<string, int> indices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { hrStr, -1 },
            { idStr, -1 },
            { nameStr, -1 },
            { raStr, -1 },
            { decStr, -1 },
            { magStr, -1 },
            { distStr, -1 },
            { colStr, -1 },
            { conStr, -1 },
            { binIndStr, -1 },
            { binMainStr, -1 },
            { cartStr, -1 },
            { baseStr, -1 }
        };

        // reading shortCut
        public int hr => indices[hrStr];
        public int id => indices[idStr];
        public int name => indices[nameStr];
        public int ra => indices[raStr];
        public int dec => indices[decStr];
        public int mag => indices[magStr];
        public int dist => indices[distStr];
        public int color => indices[colStr];
        public int con => indices[conStr];
        public int binInd => indices[binIndStr];
        public int binMain => indices[binMainStr];
        public int x => indices[cartStr];
        public int y => indices[cartStr]+1;
        public int z => indices[cartStr]+2;
        public int nbase => indices[baseStr];

        public bool IsValid()
        {
            return indices.Values.All(val => val > -1);
        }

        public string keyNotValid()
        {
            //return any key not valid
            return indices.FirstOrDefault(item => item.Value == -1).Key ?? "";
        }
    }

    private class ConstCsvIndices
    {
        public readonly string fileName = "constellationsES.csv";
        public readonly char separator = ',';
        const string idStr = "Sigla";//"ID";
        const string itaNameStr = "Costellazione";//"ItaName";
        const string engNameStr = "Nome Inglese";
        const string meanStr = "Origine";//"Meaning";
        const string hemisphStr = "Visibilit"; //"Hemisphere";

        // -1 as null idx
        public Dictionary<string, int> indices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { idStr, -1 },
            { itaNameStr, -1 },
            { engNameStr, -1 },
            { meanStr, -1 },
            { hemisphStr, -1 }
        };

        // reading shortCut
        public int id => indices[idStr];
        public int itaName => indices[itaNameStr];
        public int engName => indices[engNameStr];
        public int mean => indices[meanStr];
        public int hemisph => indices[hemisphStr];

        public bool IsValid()
        {
            return indices.Values.All(val => val > -1);
        }

        public string keyNotValid()
        {
            //return any key not valid
            return indices.FirstOrDefault(item => item.Value == -1).Key ?? "";
        }
    }
    
    private class LineCsvIndices
    {
        public readonly string fileName;
        public readonly char separator;

        const string idStr = "abr";
        const string numStr = "nr";
        const string colStr = "hexCol";

        // Customize instance
        public LineCsvIndices(string fileName, char separator = ',')
        {
            this.fileName = fileName;
            this.separator = separator;
        }

        // -1 as null idx
        public Dictionary<string, int> indices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { idStr, -1 },
            { numStr, -1 },
            { colStr, -1 }
    };

        // reading shortCut
        public int id => indices[idStr];
        public int num => indices[numStr];
        public int col => indices[colStr];

    public bool IsValid(LinkType type)
        {
            if (type != LinkType.CONSTELLATION)
                return indices.Values.All(val => val > -1);
            else //original constellations does not have col
                return indices.All(item => item.Value > -1 || item.Key == colStr);
        }

        public string keyNotValid()
        {
            //return any key not valid
            return indices.FirstOrDefault(item => item.Value == -1).Key ?? "";
        }
    }

    public bool IsReady { get; private set; } = false;
    public float downloadProgress { get; private set; } = 0f; //0f->1f = 100%
    private string datFilePath;
    private string datFileName = "db.dat";

    void Awake()
    {
        InitializeDatabase();
    }

    public void InitializeDatabase()
    {
        bool loaded = true;
        datFilePath = Path.Combine(Application.streamingAssetsPath, datFileName);
        datFilePath = datFilePath.Replace("\\", "/");
//avoid csv reading when deploy as WEBGL + always load DB from csv when in editor to avoid mismatches
#if (!UNITY_WEBGL || UNITY_EDITOR)
        {
            if (!File.Exists(datFilePath))
            {
                CustomLogger.Log("File " + datFileName + " non trovato. Generazione dai CSV...");
            }
            else
            {
                CustomLogger.Log("Sovrascrivo DB. Generazione dai CSV...");
            }

                loaded = ReadFromCsvAndCreateDat();
            if (loaded)
                StartCoroutine(LoadDatabaseRoutine());
        }
#else
            //Assuming .dat present
            StartCoroutine(LoadDatabaseRoutine());
#endif

        if (!loaded)
        {
            CustomLogger.LogWarning("Impossibile avviare la simulazione: il database non è pronto.");
        }
    }

    // --- READING FROM .DAT FILE ---
    private bool LoadFromDat(byte[] fileData)
    {
        //clear list to force re-reading from bin
        //so names are in the right version
        starDict.Clear();
        constellationDict.Clear();
        linkList.Clear();

        try
        {
            //using (BinaryReader reader = new BinaryReader(File.Open(datFilePath, FileMode.Open)))
            using (MemoryStream ms = new MemoryStream(fileData))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                // 1. Read star DB
                uint starCount = reader.ReadUInt32();
                CustomLogger.Log($"[DB-R] n° stelle: {starCount}");
                for (uint i = 0; i < starCount; i++)
                {
                    StarData star = new StarData();
                    star.id = reader.ReadUInt32();
                    star.hr = reader.ReadUInt32();
                    star.name = ObfuscateString(reader.ReadString());
                    star.ra = reader.ReadSingle();
                    star.dec = reader.ReadSingle();
                    star.mag = reader.ReadSingle();
                    star.dist = reader.ReadSingle();
                    star.colorIndex = reader.ReadSingle();
                    star.binMain = reader.ReadUInt32();
                    star.binInd = reader.ReadByte();
                    star.mainStar = reader.ReadBoolean();
                    float x = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    float z = reader.ReadSingle();
                    star.xyz = new Vector3(x, y, z);
                    star.idsBins[0] = reader.ReadUInt32();
                    star.idsBins[1] = reader.ReadUInt32();
                    star.binCount = reader.ReadByte();

                    starDict.Add(star.id, star);
                }
                CustomLogger.Log($"[DB-R] lettura stelle: {starDict.Count}");

                // 2. Read constellation DB
                uint constCount = reader.ReadUInt32();
                CustomLogger.Log($"[DB-R] n° costellazioni: {constCount}");
                for (uint i = 0; i < constCount; i++)
                {
                    ConstellationInfo info = new ConstellationInfo();
                    info.id = ObfuscateString(reader.ReadString());
                    info.itaName = ObfuscateString(reader.ReadString());
                    info.engName = ObfuscateString(reader.ReadString());
                    info.meaning = ObfuscateString(reader.ReadString(), xorKey);
                    info.hemisphere = ObfuscateString(reader.ReadString(), xorKey);
                    info.type       = (LinkType) reader.ReadByte();
                    info.colorStr = ObfuscateString(reader.ReadString(), xorKey);
                    if (info.type is LinkType.ASTERISM or LinkType.GEOMETRY)
                    {
                        //Trim() removes invisible \n o \r
                        if (!ColorUtility.TryParseHtmlString(info.colorStr.Trim(), out Color tempColor))
                        {
                            info.color = Color.clear; //protection
                        }
                        else
                            info.color = tempColor;
                    }


                    constellationDict.Add(info.id, info);
                }
                CustomLogger.Log($"[DB-R] lettura costellazioni: {constellationDict.Count}");

                // 3. Read lines DB
                uint linkCount = reader.ReadUInt32();
                CustomLogger.Log($"[DB-R] n° linee: {linkCount}");
                for (uint i = 0; i < linkCount; i++)
                {
                    StarLinksItem link = new StarLinksItem();
                    link.identifier = ObfuscateString(reader.ReadString());
                    link.startId = reader.ReadUInt32();
                    link.endId = reader.ReadUInt32();
                    //link.type = (LinkType)reader.ReadByte();

                    linkList.Add(link);
                }
                CustomLogger.Log($"[DB-R] lettura linee: {linkList.Count}");

            }

            isLoaded = true;
            IsReady = true;
            CustomLogger.Log($"Caricamento .DAT completato: {starDict.Count} stelle, {constellationDict.Count} costellazioni, {linkList.Count} linee.");
            return true;
        }
        catch (System.Exception e)
        {
            CustomLogger.LogError("Errore durante il parsing dei dati: " + e.Message);
            return false;
        }
    }

    // --- READ FROM .CSV and CREATING .DAT ---
    private bool ReadFromCsvAndCreateDat()
    {
        StarCsvIndices starIdx = new StarCsvIndices();
        ConstCsvIndices constIdx = new ConstCsvIndices();
        //LinkCsvIndices linkIdx = new LinkCsvIndices();

        //Lines CSVs:

        //1 - proper constellation lines
        //https://github.com/MarcvdSluys/ConstellationLines?tab=readme-ov-file
        //uses ID as YALE BRIGHT STAR CATALOGUE //"constellations.csv";
        LineCsvIndices linkIdx = new LineCsvIndices("ConstellationLines.csv");

        //2 - asterisms
        LineCsvIndices astIdx = new LineCsvIndices("AsterismLines.csv");

        //3 - additional geometries (how to navigate using the stars)
        LineCsvIndices geomIdx = new LineCsvIndices("GeometryLines.csv");

        string starsCsvPath = Path.Combine(Application.streamingAssetsPath, "csv", starIdx.fileName);
        string constDataCsvPath = Path.Combine(Application.streamingAssetsPath, "csv", constIdx.fileName);
        string constLinksCsvPath = Path.Combine(Application.streamingAssetsPath, "csv", linkIdx.fileName);

        // Check CSV exist
        if (!File.Exists(starsCsvPath) || !File.Exists(constDataCsvPath) || !File.Exists(constLinksCsvPath))
        {
            CustomLogger.LogError("Impossibile trovare i file CSV necessari in StreamingAssets!");
            return false;
        }

        // 1. Parse Constellation Info CSV
        string[] constLines = File.ReadAllLines(constDataCsvPath);
        string[] constHeader = constLines[0].Split(constIdx.separator);

        MapHeaderIndices(constHeader, constIdx.indices);

        if (!constIdx.IsValid())
        {
            CustomLogger.LogError("Formato " + constIdx.fileName + " non valido: manca " + constIdx.keyNotValid());
            return false;
        }

        for (int i = 1; i < constLines.Length; i++)
        {
            string[] data = constLines[i].Split(constIdx.separator);
            if (data.Length >= constIdx.indices.Count)
            {
                ConstellationInfo info = new ConstellationInfo();
                info.id = ObfuscateString(data[constIdx.id].ToUpper());
                if (constellationDict.ContainsKey(info.id))
                {
                    CustomLogger.Log("Costellazione con id " + data[constIdx.id].ToUpper() + " duplicata");
                    continue;
                }

                info.itaName = ObfuscateString(data[constIdx.itaName]);
                info.engName = ObfuscateString(data[constIdx.engName]);
                info.meaning = ObfuscateString(data[constIdx.mean], xorKey);
                info.hemisphere = ObfuscateString(data[constIdx.hemisph], xorKey);
                //type and color will be retrieved after
                constellationDict.Add(info.id, info);
            }
        }

        CustomLogger.Log($"caricamento costellazioni: {constellationDict.Count}");

        // 2. Parse Stars CSV
        string[] starLines = File.ReadAllLines(starsCsvPath);
        string[] starHeader = starLines[0].Split(starIdx.separator);

        MapHeaderIndices(starHeader, starIdx.indices);

        if (!starIdx.IsValid())
        {
            CustomLogger.LogError("Formato " + starIdx.fileName + " non valido: manca " + starIdx.keyNotValid());
            return false;
        }

        uint missedStars = 0;
        for (uint i = 1; i < starLines.Length; i++) //  jump header
        {
            string[] data = starLines[i].Split(starIdx.separator);
            if (data.Length >= starIdx.indices.Count)
            {
                if (FilterStar(data, starIdx, constellationDict))
                    continue;

                if (starDict.Count >= maxStars)
                {
                    missedStars += 1;
                    continue; //safe exit after maxStars stars, TBD filtering
                }

                StarData star = new StarData();
                star.id = uint.Parse(data[starIdx.id]);
                star.hr = uint.Parse(data[starIdx.hr]);
                star.mainStar = true;
                star.name = ObfuscateString(data[starIdx.name]);
                star.ra = float.Parse(data[starIdx.ra], CultureInfo.InvariantCulture); //InvariantCulture set -> . as decimal for all users
                star.dec = float.Parse(data[starIdx.dec], CultureInfo.InvariantCulture);
                star.mag = float.Parse(data[starIdx.mag], CultureInfo.InvariantCulture);
                star.dist = float.Parse(data[starIdx.dist], CultureInfo.InvariantCulture);
                star.colorIndex = float.Parse(data[starIdx.color], CultureInfo.InvariantCulture);
                star.binMain = uint.Parse(data[starIdx.binMain]);
                star.binInd = byte.Parse(data[starIdx.binInd]);
                star.xyz = new Vector3(float.Parse(data[starIdx.x], CultureInfo.InvariantCulture),
                                       float.Parse(data[starIdx.y], CultureInfo.InvariantCulture),
                                       float.Parse(data[starIdx.z], CultureInfo.InvariantCulture));
                string baseName = ObfuscateString(data[starIdx.nbase]);
                if (string.IsNullOrEmpty(star.name) || star.name == "\"\"")
                    star.name = baseName;
                starDict.Add(star.id, star);
            }                
        }

        //check for binaries TBD refinition
        foreach (var star in starDict)
        {
            if (star.Value.binMain != 0 && star.Value.binMain != star.Value.id && star.Value.binInd > 1)
            {
                //binary found
                star.Value.mainStar = false;
                //insert id in the binary array of the main star, if exist (TBD filter)
                if (starDict.ContainsKey(star.Value.binMain))
                {
                    //main stars have binInd == 1
                    //secondary stars have 2 or 3
                    starDict[star.Value.binMain].idsBins[star.Value.binInd - 2] = star.Value.id;
                    ++starDict[star.Value.binMain].binCount; //update counter
                }
            }
        }

        CustomLogger.Log($"caricamento stelle: {starDict.Count}");

        if (missedStars > 0)
            CustomLogger.LogWarning("Massimo di stelle raggiunto a " + maxStars.ToString() + " mancherebbero " + missedStars.ToString());
        // 3. Parse Links local function

        uint ParseLinks(string path, LineCsvIndices indices, LinkType type)
        {
            string[] linkLines = File.ReadAllLines(path);
            string[] linkHeader = linkLines[0].Split(indices.separator);

            MapHeaderIndices(linkHeader, indices.indices);

            if (!indices.IsValid(type))
            {
                CustomLogger.LogError("Formato " + indices.fileName + " non valido: manca " + indices.keyNotValid());
                return 0;
            }

            uint lineCount = 0;
            uint counter = 0;
            string color = "000000";
            uint startStarHr = 0; uint endStarHr = 0;

            for (uint i = 1; i < linkLines.Length; i++)
            {
                string[] data = linkLines[i].Split(indices.separator);
                if (data.Length >= indices.indices.Count)
                {
                    lineCount = uint.Parse(data[indices.num]);
                    string cName = ObfuscateString(data[indices.id].ToUpper());
#if UNITY_EDITOR
                    string altName = ObfuscateString(cName); //debug only
                                                             //cName = altName;
#endif
                    if (indices.col > 0)
                    {
                        color = ObfuscateString(data[indices.col].ToUpper());
                    }
                        
                    if (!constellationDict.ContainsKey(cName)) //filter by constellation
                        continue; //next line
                    else
                    {
                        //update properties
                        constellationDict[cName].type = type;
                        constellationDict[cName].colorStr = ObfuscateString(color, xorKey); ;
                    }

                    StarData startStar;
                    StarData endStar;
                    for (uint startId = 1; startId < lineCount; startId++)
                    {
                        startStarHr = uint.Parse(data[indices.num + startId]);
                        //if (!starDict.ContainsKey(startStar)) now hr is not key
                        startStar = starDict.Values.FirstOrDefault(s => s.hr == startStarHr);
                        if (startStar == null)
                            continue; //advance index

                        //check for a real endline to not miss lines for missing stars
                        uint endId = startId; //because +1 will be added next line
                        do
                        {
                            endId++;
                            endStarHr = uint.Parse(data[indices.num + endId]);
                            endStar = starDict.Values.FirstOrDefault(s => s.hr == endStarHr);
                            if (endStar != null) //valid line found
                                break;
                        }
                        while (endId < lineCount);

                        if (startStar == null || endStar == null)
                            continue; //collapsed line, unneded safety check

                        if (startStar.hr == endStar.hr)
                            continue; //collapsed line, unneded safety check

                        if (startStarHr == 0 || endStarHr == 0)
                            continue; //invalid id

                        //add only referenced stars
                        if (starDict.ContainsKey(startStar.id) && starDict.ContainsKey(endStar.id)) //redundant check
                        {
                            StarLinksItem link = new StarLinksItem();
                            link.identifier = cName;
                            link.startId = startStar.id;
                            link.endId = endStar.id;
                            linkList.Add(link);
                        }
                    }
                    ++counter;
                }
            }

            return counter;
        }

        uint lines;
        // 3A. Parse Constellations
        lines = ParseLinks(constLinksCsvPath, linkIdx, LinkType.CONSTELLATION);
        if (lines > 0)
            CustomLogger.Log($"[DB-W] elaborazione costellazioni: {lines}");

        // 3B. Parse Asterism and Geometries
        string asterismCsvPath = Path.Combine(Application.streamingAssetsPath, "csv", astIdx.fileName);
        string geometriesCsvPath = Path.Combine(Application.streamingAssetsPath, "csv", geomIdx.fileName);

        if (!File.Exists(asterismCsvPath) || !File.Exists(geometriesCsvPath))
        {
            CustomLogger.LogWarning("Impossibile trovare i file CSV di asterismi o geometrie");
        }

        lines = ParseLinks(asterismCsvPath, linkIdx, LinkType.ASTERISM);
        if (lines > 0)
            CustomLogger.Log($"[DB-W] elaborazione asterismi: {lines}");

        lines = ParseLinks(geometriesCsvPath, linkIdx, LinkType.GEOMETRY);
        if (lines > 0)
            CustomLogger.Log($"[DB-W] elaborazione geometrie: {lines}");

        CustomLogger.Log($"[DB-W] totale linee elaborate: {linkList.Count}");

        // --- SAVING .DAT binary file ---
        using (BinaryWriter writer = new BinaryWriter(File.Open(datFilePath, FileMode.Create)))
        {
            // Stars
            writer.Write(starDict.Count);
            CustomLogger.Log($"[DB-W] scrittura stelle: {starDict.Count}");
            foreach (var kvp in starDict)
            {
                writer.Write(kvp.Value.id);
                writer.Write(kvp.Value.hr);
                writer.Write(kvp.Value.name);
                writer.Write(kvp.Value.ra);
                writer.Write(kvp.Value.dec);
                writer.Write(kvp.Value.mag);
                writer.Write(kvp.Value.dist);
                writer.Write(kvp.Value.colorIndex);
                writer.Write(kvp.Value.binMain);
                writer.Write(kvp.Value.binInd);
                writer.Write(kvp.Value.mainStar);
                writer.Write(kvp.Value.xyz.x);
                writer.Write(kvp.Value.xyz.y);
                writer.Write(kvp.Value.xyz.z);
                writer.Write(kvp.Value.idsBins[0]);
                writer.Write(kvp.Value.idsBins[1]);
                writer.Write(kvp.Value.binCount);
            }

            // Contsellation info
            writer.Write(constellationDict.Count);
            CustomLogger.Log($"[DB-W] scrittura costellazioni: {constellationDict.Count}");
            foreach (var kvp in constellationDict)
            {
                writer.Write(kvp.Value.id);
                writer.Write(kvp.Value.itaName);
                writer.Write(kvp.Value.engName);
                writer.Write(kvp.Value.meaning);
                writer.Write(kvp.Value.hemisphere);
                writer.Write((byte)kvp.Value.type);
                writer.Write(kvp.Value.colorStr);
            }

            // Constellation lines
            writer.Write(linkList.Count);
            CustomLogger.Log($"[DB-W] scrittura linee: {linkList.Count}");
            foreach (var link in linkList)
            {
                writer.Write(link.identifier);
                writer.Write(link.startId);
                writer.Write(link.endId);
            }
        }

        isLoaded = true;
        CustomLogger.Log($"File " + datFileName + " generato con successo dai CSV! ({starDict.Count} stelle)");

        return isLoaded;
    }

    private void MapHeaderIndices(string[] headerColumns, Dictionary<string, int> targetMap)
    {
        for (uint i = 0; i < headerColumns.Length; i++)
        {
            string columnName = headerColumns[i].Trim().Trim('"'); //delete spaces and " "

            //do not need to perform ToLower, StringComparer.OrdinalIgnoreCase in dictionary handle this
            if (targetMap.ContainsKey(columnName))
            {
                targetMap[columnName] = System.Convert.ToInt32(i);
            }
        }
    }

    // Symmetric XOR function to encode/decode string in bin file
    private string ObfuscateString(string input, int key = 0)
    {
        //int a;
        if (string.IsNullOrEmpty(input))
            return input;
        if (input == "\"\"")
            return "";
        if (key == 0)
            return input;

        //if (input == "\"\"")
        //    a = 1;

        char[] chars = input.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            // Apply to all chars XOR with key
            chars[i] = (char)(chars[i] ^ key);
        }
        return new string(chars);
    }

    //filter string from extensive catalogue
    private bool FilterStar(string[] data, StarCsvIndices starIdx, Dictionary<string, ConstellationInfo> constellationDict)
    {
        //return true if a star is to be excluted
        if (data.Length == 0) return true;

        foreach (var ind in starIdx.indices)
        {
            //star can't have missing values, TBD default
            if (data[ind.Value].Trim() == "") return true;
        }

        //filter by magnitude
        if (float.Parse(data[starIdx.mag], CultureInfo.InvariantCulture) > maxNakedEyeMagnitude) return true;

        //filter by constellation
        //constellationDict is already obfuscated ... 
        if (!constellationDict.ContainsKey(ObfuscateString(data[starIdx.con].ToUpper()))) return true;

        return false;
    }

    private IEnumerator LoadDatabaseRoutine()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // =========================================================
        // BUILD WEBGL
        // =========================================================

        // UnityWebRequest call for download
        using (UnityWebRequest uwr = UnityWebRequest.Get(datFilePath))
        {
            // wait until ends while getting pointer
            var operation = uwr.SendWebRequest();
            // Finché l'operazione non ha finito, aggiorniamo il progresso frame per frame!
            while (!operation.isDone)
            {
                downloadProgress = uwr.downloadProgress;
                yield return null; // Aspetta il prossimo frame
            }

            downloadProgress = 1f; //safe set

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                CustomLogger.LogError("Errore critico: Impossibile caricare stars.dat - " + uwr.error);
                IsReady = false;
            }
            else
            {
                byte[] rawData = uwr.downloadHandler.data;
                IsReady = LoadFromDat(rawData);
                rawData = null; //clean byte array
            }
        }

        // Unload RAM from not needed assets (.dat)
        yield return Resources.UnloadUnusedAssets();
#else
        // =========================================================
        // BUILD DESKTOP
        // =========================================================
#if UNITY_EDITOR
        // Test loading bar
        float simDelay = 2.0f;
        float myClock = 0f;

        while (myClock < simDelay)
        {
            myClock += Time.deltaTime;
            downloadProgress = myClock / simDelay;

            yield return null; // update a frame
        }
#endif
        downloadProgress = 1f;
        try
        {
            if (!File.Exists(datFilePath))
                yield break;

            byte[] rawData = System.IO.File.ReadAllBytes(datFilePath);
            IsReady = LoadFromDat(rawData);
            if (IsReady)
                CustomLogger.Log("DB Read: OK");
            else
                CustomLogger.Log("DB Read: FAIL");

            rawData = null;
        }
        catch (System.Exception e)
        {
            CustomLogger.LogError("Errore lettura locale: " + e.Message);
            IsReady = false;
        }

        yield return null;
#endif
        System.GC.Collect();
    }
}