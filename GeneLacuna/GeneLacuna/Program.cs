using System.Collections;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

// Create user

var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://gene.lacuna.cc/api/users/create");
httpWebRequest.ContentType = "application/json";
httpWebRequest.Method = "POST";

using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
{
    string json = "{\"username\":\"lvgvspe\"," +
                  "\"email\":\"lucas-camargo@outlook.com\"," +
                  "\"password\":\"lksjk1996\"}";

    streamWriter.Write(json);
}

var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
var streamReader = new StreamReader(httpResponse.GetResponseStream());
var result = streamReader.ReadToEnd();

// Request access token

var tokenRequest = (HttpWebRequest)WebRequest.Create("http://gene.lacuna.cc/api/users/login");
tokenRequest.ContentType = "application/json";
tokenRequest.Method = "POST";

using (var streamWriter = new StreamWriter(tokenRequest.GetRequestStream()))
{
    string json = "{\"username\":\"lvgvspe\"," +
                  "\"password\":\"lksjk1996\"}";

    streamWriter.Write(json);
}

var tokenResponse = (HttpWebResponse)tokenRequest.GetResponse();
var tokenStreamReader = new StreamReader(tokenResponse.GetResponseStream());
var tokenResult = tokenStreamReader.ReadToEnd();
var tokenDict = JsonSerializer.Deserialize<Dictionary<string, string>>(tokenResult);

// Request job

var jobRequest = (HttpWebRequest)WebRequest.Create("https://gene.lacuna.cc/api/dna/jobs");
jobRequest.Headers.Add("Authorization", tokenDict["accessToken"]);

HttpWebResponse response = (HttpWebResponse)jobRequest.GetResponse();
Stream stream = response.GetResponseStream();
StreamReader reader = new StreamReader(stream);
var jobResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(reader.ReadToEnd());
var jobDict = JsonSerializer.Deserialize<Dictionary<string, string>>(jobResponse["job"].ToString());
Console.WriteLine(jobDict["type"]);

if (jobDict["type"] == "DecodeStrand")
{
    var strandDecoded = DecodeStrand(jobDict["strandEncoded"]);
    var id = jobDict["id"];
    string url = $"https://gene.lacuna.cc/api/dna/jobs/{id}/decode";

    HttpWebRequest? decodeRequest = (HttpWebRequest)WebRequest.Create(url);
    decodeRequest.Headers.Add("Authorization", tokenDict["accessToken"]);
    decodeRequest.ContentType = "application/json";
    decodeRequest.Method = "POST";

    using (var streamWriter = new StreamWriter(decodeRequest.GetRequestStream()))
    {
        string json = $"{{\"strand\":\"{strandDecoded}\"}}";

        streamWriter.Write(json);
    }

    var decodeResponse = (HttpWebResponse)decodeRequest.GetResponse();
    var decodeStreamReader = new StreamReader(decodeResponse.GetResponseStream());
    var decodeResult = decodeStreamReader.ReadToEnd();
    Console.WriteLine(decodeResult);
}

if (jobDict["type"] == "EncodeStrand")
{

    var strandEncoded = EncodeStrand(jobDict["strand"]);
    var id = jobDict["id"];
    string url = $"https://gene.lacuna.cc/api/dna/jobs/{id}/encode";

    HttpWebRequest? encodeRequest = (HttpWebRequest)WebRequest.Create(url);
    encodeRequest.Headers.Add("Authorization", tokenDict["accessToken"]);
    encodeRequest.ContentType = "application/json";
    encodeRequest.Method = "POST";

    using (var streamWriter = new StreamWriter(encodeRequest.GetRequestStream()))
    {
        string json = $"{{\"strandEncoded\":\"{strandEncoded}\"}}";

        streamWriter.Write(json);
    }

    var encodeResponse = (HttpWebResponse)encodeRequest.GetResponse();
    var encodeStreamReader = new StreamReader(encodeResponse.GetResponseStream());
    var encodeResult = encodeStreamReader.ReadToEnd();
    Console.WriteLine(encodeResult);
}

if (jobDict["type"] == "CheckGene")
{
    var strandDecoded = DecodeStrand(jobDict["strandEncoded"]);
    var geneDecoded = DecodeGene(jobDict["geneEncoded"]);
    var checkGene = CheckGene(geneDecoded, strandDecoded);
    var id = jobDict["id"];
    string url = $"https://gene.lacuna.cc/api/dna/jobs/{id}/gene";

    HttpWebRequest? geneRequest = (HttpWebRequest)WebRequest.Create(url);
    geneRequest.Headers.Add("Authorization", tokenDict["accessToken"]);
    geneRequest.ContentType = "application/json";
    geneRequest.Method = "POST";

    using (var streamWriter = new StreamWriter(geneRequest.GetRequestStream()))
    {
        string json = $"{{\"isActivated\":\"{checkGene}\"}}";

        streamWriter.Write(json);
    }

    var geneResponse = (HttpWebResponse)geneRequest.GetResponse();
    var geneStreamReader = new StreamReader(geneResponse.GetResponseStream());
    var geneResult = geneStreamReader.ReadToEnd();
    Console.WriteLine(geneResult);

}

static string EncodeStrand(string strand)
{
    string strandEncoded = "strandEncoded";
    if (strand.StartsWith('C'))
    {
        var strand1 = strand.Replace("A", "00");
        var strand2 = strand1.Replace("C", "01");
        var strand3 = strand2.Replace("G", "10");
        var strandfinal = strand3.Replace("T", "11");
        strandEncoded = GetEncoding(strandfinal);
    }
    else
    {
        var strand1 = strand.Replace("T", "00");
        var strand2 = strand1.Replace("G", "01");
        var strand3 = strand2.Replace("C", "10");
        var strandfinal = strand3.Replace("A", "11");
        strandEncoded = GetEncoding(strandfinal);
    }
    static string GetEncoding(string strandfinal)
    {
        static byte[] GetBytes(string bitString)
        {
            return Enumerable.Range(0, bitString.Length / 8).
                Select(pos => Convert.ToByte(
                    bitString.Substring(pos * 8, 8),
                    2)
                ).ToArray();
        }

        var array = GetBytes(strandfinal);

        var encode = Convert.ToBase64String(array, 0, array.Length);

        return encode;
    }

    return strandEncoded;

}

static string DecodeGene(string base64)
{
    var decode = Convert.FromBase64String(base64);

    BitArray bits = new BitArray(decode);

    static string ToBitString(BitArray ba)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < ba.Count; i++)
        {
            char c = ba[i] ? '1' : '0';
            sb.Append(c);
        }

        return sb.ToString();
    }

    var bits2 = Convert.ToHexString(decode);

    static BitArray ConvertHexToBitArray(string hexData)
    {
        if (hexData == null)
            return null;

        BitArray ba = new BitArray(4 * hexData.Length);
        for (int i = 0; i < hexData.Length; i++)
        {
            byte b = byte.Parse(hexData[i].ToString(), NumberStyles.HexNumber);
            for (int j = 0; j < 4; j++)
            {
                ba.Set(i * 4 + j, (b & (1 << (3 - j))) != 0);
            }
        }
        return ba;
    }

    var bitArray = ConvertHexToBitArray(bits2);

    var bitString = ToBitString(bitArray);

    static IEnumerable<string> Split(string str, int chunkSize)
    {
        return Enumerable.Range(0, str.Length / chunkSize)
            .Select(i => str.Substring(i * chunkSize, chunkSize));
    }

    var stringArray = Split(bitString, 2);

    List<string> strandList = new();

    foreach (var chunk in stringArray)
    {
        if (chunk == "00")
        {
            strandList.Add("A");
        }
        if (chunk == "01")
        {
            strandList.Add("C");
        }
        if (chunk == "10")
        {
            strandList.Add("G");
        }
        if (chunk == "11")
        {
            strandList.Add("T");
        }
    }

    string geneDecoded = string.Join("", strandList);
    return geneDecoded;
}

static string DecodeStrand(string base64)
{
    var decode = Convert.FromBase64String(base64);

    BitArray bits = new BitArray(decode);

    static string ToBitString(BitArray ba)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < ba.Count; i++)
        {
            char c = ba[i] ? '1' : '0';
            sb.Append(c);
        }

        return sb.ToString();
    }

    var bits2 = Convert.ToHexString(decode);

    static BitArray ConvertHexToBitArray(string hexData)
    {
        if (hexData == null)
            return null;

        BitArray ba = new BitArray(4 * hexData.Length);
        for (int i = 0; i < hexData.Length; i++)
        {
            byte b = byte.Parse(hexData[i].ToString(), NumberStyles.HexNumber);
            for (int j = 0; j < 4; j++)
            {
                ba.Set(i * 4 + j, (b & (1 << (3 - j))) != 0);
            }
        }
        return ba;
    }

    var bitArray = ConvertHexToBitArray(bits2);

    var bitString = ToBitString(bitArray);

    static IEnumerable<string> Split(string str, int chunkSize)
    {
        return Enumerable.Range(0, str.Length / chunkSize)
            .Select(i => str.Substring(i * chunkSize, chunkSize));
    }

    var stringArray = Split(bitString, 2);

    List<string> strandList = new();

    if (stringArray.First() == "01")
    {
        foreach (var chunk in stringArray)
        {
            if (chunk == "00")
            {
                strandList.Add("A");
            }
            if (chunk == "01")
            {
                strandList.Add("C");
            }
            if (chunk == "10")
            {
                strandList.Add("G");
            }
            if (chunk == "11")
            {
                strandList.Add("T");
            }
        }
    }
    if (stringArray.First() == "10")
    {
        foreach (var chunk in stringArray)
        {
            if (chunk == "00")
            {
                strandList.Add("T");
            }
            if (chunk == "01")
            {
                strandList.Add("G");
            }
            if (chunk == "10")
            {
                strandList.Add("C");
            }
            if (chunk == "11")
            {
                strandList.Add("A");
            }
        }
    }

    string strandDecoded = string.Join("", strandList);
    return strandDecoded;
}

static bool CheckGene(string gene, string strand)
{
    string longestSubstring = LongestSubstring(gene, strand);
    Console.WriteLine("Substring: " + longestSubstring);
    double lSLength = longestSubstring.Length;
    double gLength = gene.Length;
    double length = lSLength / gLength;
    Console.WriteLine("Length :" + length);
    if (length > 0.5)
    {
        return true;
    }
    else
    {
        return false;
    }

    static string LongestSubstring(string a, string b)
    {
        var substringsOfA = FindAllSubstrings(a);
        var substringsOfB = FindAllSubstrings(b);
        var commonSubstrings = substringsOfA.Intersect(substringsOfB);
        string longestSubstring = commonSubstrings.OrderByDescending(x => x.Length).FirstOrDefault();
        return longestSubstring;

        static IEnumerable<string> FindAllSubstrings(string s)
        {
            List<string> list = new List<string>();
            for (int i = 0; i < s.Length; i++)
            {
                for (int j = i; j < s.Length; j++)
                {
                    string ss = s.Substring(i, j - i + 1);
                    list.Add(ss);
                }
            }
            return list;
        }
    }

}