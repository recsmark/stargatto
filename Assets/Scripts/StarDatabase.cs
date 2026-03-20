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

public class StarData
{
    public int id;
    public string name;
    public float ra;
    public float dec;
    public float mag;
    public float colorIndex;
}

public class ConstellationInfo //TBD csv ES
{
    public string id;
    public string itaName;
    public string engName;
    public string meaning;
    public string hemisphere;
}

public class ConstellationLink
{
    public string constellationId;
    public int startId;
    public int endId;
}

// --- DATABASE MANAGER ---

public class StarDatabase : MonoBehaviour
{
    private readonly byte xorKey = 12; //encoder key
    public readonly float maxNakedEyeMagnitude = 5.5f; //should be 6.5f
    const int maxStars = 3000;

    public bool isLoaded = false;

    // public maps for StarGenerator
    public Dictionary<int, StarData> starDict = new Dictionary<int, StarData>();
    public Dictionary<string, ConstellationInfo> constellationDict = new Dictionary<string, ConstellationInfo>();
    public List<ConstellationLink> linkList = new List<ConstellationLink>();

    private class StarCsvIndices
    {
        public readonly string fileName = "hyg_v42.csv"; //https://www.astronexus.com/projects/hyg
        public readonly char separator = ',';
        //https://www.astronexus.com/projects/hyg-details
        const string idStr = "hr"; //hr -> Harvard Revised catalog, which is the same as its number in the Yale Bright Star Catalog
                                   //hip ->  hipparcos ID
        const string nameStr = "proper"; //common name
        const string raStr = "RA"; //right ascension for epoch and equinox 2000.0
        const string decStr = "Dec"; //declination for epoch and equinox 2000.0
        const string magStr = "Mag"; //apparent magnitude
        const string colStr = "CI"; //color index when known
        const string conStr = "con"; //std const abbreviation

        // -1 as null idx
        public Dictionary<string, int> indices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { idStr, -1 },
            { nameStr, -1 },
            { raStr, -1 },
            { decStr, -1 },
            { magStr, -1 },
            { colStr, -1 },
            { conStr, -1 }
        };

        // reading shortCut
        public int id => indices[idStr];
        public int name => indices[nameStr];
        public int ra => indices[raStr];
        public int dec => indices[decStr];
        public int mag => indices[magStr];
        public int color => indices[colStr];
        public int con => indices[conStr];

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

    private class LinkCsvIndices
    {
        //https://github.com/MarcvdSluys/ConstellationLines?tab=readme-ov-file
        public readonly string fileName = "ConstellationLines.csv"; //uses ID as YALE BRIGHT STAR CATALOGUE //"constellations.csv";
        public readonly char separator = ',';
        const string idStr = "abr";
        const string numStr = "nr";

        // -1 as null idx
        public Dictionary<string, int> indices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { idStr, -1 },
            { numStr, -1 }
        };

        // reading shortCut
        public int id => indices[idStr];
        public int num => indices[numStr];


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
#if (!UNITY_WEBGL || UNITY_EDITOR) //avoid csv reading when deploy as WEBGL
        {
            Debug.Log("File "+ datFileName + " non trovato. Generazione dai CSV...");
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
            Debug.LogWarning("Impossibile avviare la simulazione: il database non è pronto.");
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
                int starCount = reader.ReadInt32();
                for (int i = 0; i < starCount; i++)
                {
                    StarData star = new StarData();
                    star.id = reader.ReadInt32();
                    star.name = ObfuscateString(reader.ReadString());
                    star.ra = reader.ReadSingle();
                    star.dec = reader.ReadSingle();
                    star.mag = reader.ReadSingle();
                    star.colorIndex = reader.ReadSingle();

                    starDict.Add(star.id, star);
                }

                // 2. Read constellation DB
                int constCount = reader.ReadInt32();
                for (int i = 0; i < constCount; i++)
                {
                    ConstellationInfo info = new ConstellationInfo();
                    info.id = ObfuscateString(reader.ReadString());
                    info.itaName = ObfuscateString(reader.ReadString());
                    info.engName = ObfuscateString(reader.ReadString());
                    info.meaning = ObfuscateString(reader.ReadString());
                    info.hemisphere = ObfuscateString(reader.ReadString());

                    constellationDict.Add(info.id, info);
                }

                // 3. Read lines DB
                int linkCount = reader.ReadInt32();
                for (int i = 0; i < linkCount; i++)
                {
                    ConstellationLink link = new ConstellationLink();
                    link.constellationId = ObfuscateString(reader.ReadString());
                    link.startId = reader.ReadInt32();
                    link.endId = reader.ReadInt32();

                    linkList.Add(link);
                }
            }

            isLoaded = true;
            IsReady = true;
            Debug.Log($"Caricamento .DAT completato: {starDict.Count} stelle, {constellationDict.Count} costellazioni, {linkList.Count} linee.");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Errore durante il parsing dei dati: " + e.Message);
            return false;
        }
    }

    // --- READ FROM .CSV and CREATING .DAT ---
    private bool ReadFromCsvAndCreateDat()
    {
        StarCsvIndices starIdx = new StarCsvIndices();
        ConstCsvIndices constIdx = new ConstCsvIndices();
        LinkCsvIndices linkIdx = new LinkCsvIndices();

        string starsCsvPath = Path.Combine(Application.streamingAssetsPath, "csv", starIdx.fileName);
        string constDataCsvPath = Path.Combine(Application.streamingAssetsPath, "csv", constIdx.fileName);
        string constLinksCsvPath = Path.Combine(Application.streamingAssetsPath, "csv", linkIdx.fileName);

        // Check CSV exist
        if (!File.Exists(starsCsvPath) || !File.Exists(constDataCsvPath) || !File.Exists(constLinksCsvPath))
        {
            Debug.LogError("Impossibile trovare i file CSV necessari in StreamingAssets!");
            return false;
        }

        // 1. Parse Constellation Info CSV
        string[] constLines = File.ReadAllLines(constDataCsvPath);
        string[] constHeader = constLines[0].Split(constIdx.separator);

        MapHeaderIndices(constHeader, constIdx.indices);

        if (!constIdx.IsValid())
        {
            Debug.LogError("Formato " + constIdx.fileName + " non valido: manca " + constIdx.keyNotValid());
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
                    Debug.Log("Costellazione con id " + data[constIdx.id].ToUpper() + " duplicata");
                    continue;
                }

                info.itaName = ObfuscateString(data[constIdx.itaName]);
                info.engName = ObfuscateString(data[constIdx.engName]);
                info.meaning = ObfuscateString(data[constIdx.mean]);
                info.hemisphere = ObfuscateString(data[constIdx.hemisph]);
                constellationDict.Add(info.id, info);
            }
        }

        // 2. Parse Stars CSV
        string[] starLines = File.ReadAllLines(starsCsvPath);
        string[] starHeader = starLines[0].Split(starIdx.separator);

        MapHeaderIndices(starHeader, starIdx.indices);

        if (!starIdx.IsValid())
        {
            Debug.LogError("Formato " + starIdx.fileName + " non valido: manca " + starIdx.keyNotValid());
            return false;
        }

        int missedStars = 0;
        for (int i = 1; i < starLines.Length; i++) //  jump header
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
                star.id = int.Parse(data[starIdx.id]);
                star.name = ObfuscateString(data[starIdx.name]);
                star.ra = float.Parse(data[starIdx.ra], CultureInfo.InvariantCulture); //InvariantCulture set -> . as decimal for all users
                star.dec = float.Parse(data[starIdx.dec], CultureInfo.InvariantCulture);
                star.mag = float.Parse(data[starIdx.mag], CultureInfo.InvariantCulture);
                star.colorIndex = float.Parse(data[starIdx.color], CultureInfo.InvariantCulture);
                starDict.Add(star.id, star);
            }                
        }

        if (missedStars > 0)
            Debug.LogWarning("Massimo di stelle raggiunto a " + maxStars.ToString() + " mancherebbero " + missedStars.ToString());

        // 3. Parse Constellation Links CSV
        string[] linkLines = File.ReadAllLines(constLinksCsvPath);
        string[] linkHeader = linkLines[0].Split(linkIdx.separator);

        MapHeaderIndices(linkHeader, linkIdx.indices);

        if (!linkIdx.IsValid())
        {
            Debug.LogError("Formato " + linkIdx.fileName + " non valido: manca " + linkIdx.keyNotValid());
            return false;
        }

        int lineCount = 0;
        int startStar = 0; int endStar = 0;

        for (int i = 1; i < linkLines.Length; i++)
        {
            string[] data = linkLines[i].Split(linkIdx.separator);
            if (data.Length >= linkIdx.indices.Count)
            {   
                lineCount = int.Parse(data[linkIdx.num]);
                string cName = ObfuscateString(data[linkIdx.id].ToUpper());
#if UNITY_EDITOR
                string altName = ObfuscateString(cName); //debug only
#endif
                if (!constellationDict.ContainsKey(cName)) //filter by constellation
                    continue; //next line

                for (int startId = 1; startId < lineCount; startId++)
                {
                    startStar = int.Parse(data[linkIdx.num + startId]);
                    if (!starDict.ContainsKey(startStar))
                        continue; //advance index

                    //check for a real endline to not miss lines for missing stars
                    int endId = startId; //because +1 will be added next line
                    do
                    {
                        endId++;
                        endStar = int.Parse(data[linkIdx.num + endId]);
                        if (starDict.ContainsKey(endStar)) //valid line found
                            break;
                    } 
                    while (endId < lineCount);

                    if (startStar == endStar)
                        continue; //collapsed line, unneded safety check

                    if (startStar == 0 || endStar == 0)
                        continue; //invalid id

                    //add only referenced stars
                    if (starDict.ContainsKey(startStar) && starDict.ContainsKey(endStar)) //redundant check
                    {
                        ConstellationLink link = new ConstellationLink();
                        link.constellationId = cName;
                        link.startId = startStar;
                        link.endId = endStar;
                        linkList.Add(link);
                    }
                }
            }
        }

        // --- SAVING .DAT binary file ---
        using (BinaryWriter writer = new BinaryWriter(File.Open(datFilePath, FileMode.Create)))
        {
            // Stars
            writer.Write(starDict.Count);
            foreach (var kvp in starDict)
            {
                writer.Write(kvp.Value.id);
                writer.Write(kvp.Value.name);
                writer.Write(kvp.Value.ra);
                writer.Write(kvp.Value.dec);
                writer.Write(kvp.Value.mag);
                writer.Write(kvp.Value.colorIndex);
            }

            // Contsellation info
            writer.Write(constellationDict.Count);
            foreach (var kvp in constellationDict)
            {
                writer.Write(kvp.Value.id);
                writer.Write(kvp.Value.itaName);
                writer.Write(kvp.Value.engName);
                writer.Write(kvp.Value.meaning);
                writer.Write(kvp.Value.hemisphere);
            }

            // Constellation lines
            writer.Write(linkList.Count);
            foreach (var link in linkList)
            {
                writer.Write(link.constellationId);
                writer.Write(link.startId);
                writer.Write(link.endId);
            }
        }

        isLoaded = true;
        Debug.Log($"File " + datFileName + " generato con successo dai CSV! ({starDict.Count} stelle)");

        return isLoaded;
    }

    private void MapHeaderIndices(string[] headerColumns, Dictionary<string, int> targetMap)
    {
        for (int i = 0; i < headerColumns.Length; i++)
        {
            string columnName = headerColumns[i].Trim().Trim('"'); //delete spaces and " "

            //do not need to perform ToLower, StringComparer.OrdinalIgnoreCase in dictionary handle this
            if (targetMap.ContainsKey(columnName))
            {
                targetMap[columnName] = i;
            }
        }
    }

    // Symmetric XOR function to encode/decode string in bin file
    private string ObfuscateString(string input, string alternative = "")
    {
        //int a;
        if (string.IsNullOrEmpty(input))
            return input;

        //if (input == "\"\"")
        //    a = 1;

        char[] chars = input.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            // Apply to all chars XOR with key
            chars[i] = (char)(chars[i] ^ xorKey);
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
                Debug.LogError("Errore critico: Impossibile caricare stars.dat - " + uwr.error);
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

            rawData = null;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Errore lettura locale: " + e.Message);
            IsReady = false;
        }

        yield return null;
#endif
        System.GC.Collect();
    }
}