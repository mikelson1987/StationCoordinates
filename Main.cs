using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Drawing.Imaging;
using System.Collections;
using System.Web;
using System.Globalization;



class Program
{
    static string dirOut = @".\out\"; // куда сохранять результат (если директории нет - создаётся автоматически)
    static string fileOutNodeCsvStation = "tochka-na-karte-Estonia.csv";//"jd-station.csv";//"railwayzinfo-station.csv"; ///"gdevagon-station.csv";

    private static string urlCodePattern = @"Код ЕСР:</td><td>*?";
    private static string urlCoordinatesPattern = @"http(s)?://maps.google.com/maps*?";
    private static string urlGoogleCoordinatesPattern = "google.maps.LatLng(*?";
    
    private static string tagPattern = @"<a\b[^>]*(.*?)";

    public static string DecodeFromUtf8(string code)
    {
        string utf8_String = string.Empty;
        byte[] bytes = Encoding.Default.GetBytes(code);
        utf8_String = Encoding.UTF8.GetString(bytes);
        return utf8_String;
    }
    public static string GetHtmlPage(string url)
    {
        string HtmlText = string.Empty;
        HttpWebRequest myHttwebrequest = (HttpWebRequest)HttpWebRequest.Create(url);
        HttpWebResponse myHttpWebresponse = (HttpWebResponse)myHttwebrequest.GetResponse();
        StreamReader strm = new StreamReader(myHttpWebresponse.GetResponseStream(), Encoding.UTF8);
        HtmlText = strm.ReadToEnd();
        HtmlText = DecodeFromUtf8(HtmlText);
        return HtmlText;
    }
    
    public static string GetEncodingPage(string sURL)
    {
        string strWebPage = "";
        System.Net.WebRequest objRequest = System.Net.HttpWebRequest.Create(sURL);
        System.Net.HttpWebResponse objResponse;
        objResponse = (System.Net.HttpWebResponse)objRequest.GetResponse();
        
        // get correct charset and encoding from the server's header
        string Charset = "WINDOWS-1251";
        Encoding encoding = Encoding.GetEncoding(Charset);
        // read response
        using (StreamReader sr = new StreamReader(objResponse.GetResponseStream(), encoding))
        {
            strWebPage = sr.ReadToEnd();
            // Close and clean up the StreamReader
            sr.Close();
        }

        return strWebPage;
    }

    static void Main(string[] args)
    {
        Coordinates_RecreateFile(fileOutNodeCsvStation); // пересоздать итоговый файл

        Console.WriteLine("Start reading pages:\n");

        const string start_page = "http://tochka-na-karte.ru";
        const string page = "http://tochka-na-karte.ru/modules/travel/all_stations.php?page=";
        const string page_addict = "&terr_id=130";//Estonia //69";//Ukraine //308";//Uzbekistan //309";//Turkmenistan //411";//Tadjikistan //67";//Slovakia //64";//Roumania //63"; //Russia //61";//Poland //57";//Moldavia //52";//Lithuania //51";//Latvia //408";//Kirgizia //185";//Georgia //35";//Byelorussia 426"; //Armenia //202"; //Азербайджан //8"; //Казахстан

        var stations_page = page;
        for (int pageNumber = 1; pageNumber < 3; pageNumber++)
        {
            stations_page = page;
            stations_page += pageNumber;
            stations_page += page_addict;

            var log_page = "Просмотр страницы: ";
            log_page += pageNumber;
            Console.WriteLine(log_page);
            Console.WriteLine(stations_page);

            string HtmlText = string.Empty, Coordinates = string.Empty;

            try
            {
                HtmlText = GetEncodingPage(stations_page);
            }
            catch (WebException ex)
            {
                using (var stream = ex.Response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    Console.WriteLine(reader.ReadToEnd());
                }
                continue;
            }

            string info = string.Empty;

            string HRefPattern = "href\\s*=\\s*(?:[\"'](?<1>[^\"']*)[\"']|(?<1>\\S+)) class='lnk05'";
            var matches = Regex.Matches(HtmlText, HRefPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var links = new List<string>();

            foreach (Match item in matches)
            {
                string link = item.Groups[1].Value;
                links.Add(link);
            }

            Console.WriteLine("Найдено станций на странице: ");
            Console.WriteLine(links.Count());

            Console.WriteLine("Поиск в ссылках:");
            var link_page = start_page;
            var link_log = string.Empty;
            int link_number = 0;

            foreach (string link in links)
            {
                link_page = start_page;
                link_page += link;

                link_number++;
                link_log = link_number.ToString();
                link_log += ". ";
                link_log += link_page;

                Console.WriteLine(link_log);

                try
                {
                    HtmlText = GetEncodingPage(link_page);
                }
                catch (WebException ex)
                {
                    continue;
                }

                info = string.Empty;
                
                if (Regex.IsMatch(HtmlText, urlCodePattern))
                {
                    var patternCode = new Regex(@"Код ЕСР:</td><td>+(?<val>.*?)</td>", RegexOptions.Compiled | RegexOptions.Singleline);
                    foreach (Match m in patternCode.Matches(HtmlText))
                        if (m.Success)
                        {
                            //меж скобок ( )  
                            info += m.Groups["val"].Value;
                            info += ',';
                            info += ' ';
                            break;
                        }


                    var parternCoordinates = @"[0-9]+\.[0-9]+\,[0-9]+\.[0-9]+";
                    //check if the links is referred to the same site
                    if (Regex.IsMatch(HtmlText, parternCoordinates))
                    {
                        var patternCoordinates = new Regex(parternCoordinates);
                        var matchCodeCoordinates = patternCoordinates.Match(HtmlText);
                        var CoordValue = matchCodeCoordinates.Value;

                        info += CoordValue;
                        Console.WriteLine(info);
                        info += '\n';
                        Coordinates_WriteFile(fileOutNodeCsvStation, info.ToString());
                    }
                }
            }
        }
            //  for (int orderNumber = 1; orderNumber < 23760; orderNumber++)
            // {
            //-------------------------------------------------------
            //запросы к сайту http://www.gdevagon.ru
            //-------------------------------------------------------
            //string Coordinates = code;
            //string page = "http://www.gdevagon.ru/scripts/info/station_detail.php?stid=";
            //page += code;

            //-------------------------------------------------------
            //запросы к сайту http://railwayz.info
            //-------------------------------------------------------
            //string page = "http://railwayz.info/photolines/station/";
            //page += orderNumber;

            //-------------------------------------------------------
            //запросы к сайту http://www.vagonnik.net.ru
            //-------------------------------------------------------
          
            //foreach(string code in StationCode)
            //{
            //    string page = "http://www.vagonnik.net.ru/rasp/station/";
            //    page += code;

            //    Console.WriteLine(page);

            //    string HtmlText = string.Empty, Coordinates = string.Empty;

            //    try
            //    {
            //        HtmlText = GetEncodingPage(page);
            //    }
            //    catch (WebException ex)
            //    {
            //        using (var stream = ex.Response.GetResponseStream())
            //        using (var reader = new StreamReader(stream))
            //        {
            //            Console.WriteLine(reader.ReadToEnd());
            //        }
            //        continue;
            //    }

            //    string info = string.Empty;

            //    info += code;

            //    var patternCoordinates = new Regex(@"[0-9]+\.[0-9]+""\,""[0-9]+\.[0-9]+\""");
            //    Match m = patternCoordinates.Match(HtmlText);
            //    if (m.Success)
            //    {
            //        info += ',';
            //        info += m.Value;

            //        info += '\n';
            //        Console.WriteLine(info);
            //        Coordinates_WriteFile(fileOutNodeCsvStation, info.ToString());
            //    }
            //}

            //-------------------------------------------------------
            //запросы к сайту http://www.gdevagon.ru
            //-------------------------------------------------------
            //List<string> rez = new List<string>();
            //Regex pattern =
            // new Regex(@"<a.*?href=[""'](?<url>.*?)[""'].*?>(?<name>.*?)</a>",
            //     RegexOptions.Compiled |
            //     RegexOptions.Singleline);
      
            //foreach (Match m in pattern.Matches(HtmlText))
            //    if (m.Success)
            //        Coordinates = m.Groups["name"].Value;

            //-------------------------------------------------------
            //запросы к сайту http://www.vagonnik.net.ru
            //-------------------------------------------------------
            //List<string> links = getMatches(HtmlText);
            //foreach (string link in links)
            //{
            //    //check if the links is referred to the same site
            //    if (Regex.IsMatch(link, urlCodePattern))
            //    {
            //        //var pattern = new Regex(@"\d{6}");
            //        //var matchCode = pattern.Match(link);
            //        //var code = matchCode.Value;

            //        //info += code;
            //        //info += ',';

            //        foreach (string linkCoord in links)
            //        {
            //            //check if the links is referred to the same site
            //            if (Regex.IsMatch(linkCoord, urlCoordinatesPattern))
            //            {
            //                var patternCoordinates = new Regex(@"[0-9]+\.[0-9]+\,[0-9]+\.[0-9]+");
            //                var matchCodeCoordinates = patternCoordinates.Match(linkCoord);
            //                var CoordValue = matchCodeCoordinates.Value;

            //                info += CoordValue;
            //                info += '\n';
            //                Console.WriteLine(info);
            //                Coordinates_WriteFile(fileOutNodeCsvStation, info.ToString());
            //                break;
            //            }
            //        }

            //        break; 
            //    }
            //}

            //-------------------------------------------------------
            //запросы к сайту http://railwayz.info
            //-------------------------------------------------------
            //List<string> rez = new List<string>();
            //Regex pattern =
            // new Regex(@"GeoPoint:+\((?<val>.*?)\)",
            //     RegexOptions.Compiled |
            //     RegexOptions.Singleline);
      
            //foreach (Match m in pattern.Matches(HtmlText))
            //    if (m.Success)
            //    {
            //        //меж скобок ( )  
            //        Coordinates += ',';
            //        Coordinates += ' ';
            //        Coordinates += m.Groups["val"].Value;
            //        Coordinates += '\n';
            //        Coordinates_WriteFile(fileOutNodeCsvStation, Coordinates.ToString());
            //        break;
            //    }
            //}
    }

    // get all links that the page contains
    private static List<string> getMatches(string source)
    {
        var matchesList = new List<string>();
        //get the collection that match the tag pattern
        MatchCollection matches = Regex.Matches(source, tagPattern);
        //add the text under the href attribute
        //to the list
        foreach (Match match in matches)
        {
            string val = match.Value.Trim();
            if (val.Contains("href=\""))
            {
                matchesList.Add(val);
            }
        }
        return matchesList;
    }


    //========
    // Пересоздать файл
    //========
    private static void Coordinates_RecreateFile(string fileName)
    {
        string fileFullName = dirOut + fileName;

        if (File.Exists(fileFullName))
            File.Delete(fileFullName);

        using (File.Create(fileFullName));
    }
    //========
    // Записать в файл
    //========
    private static void Coordinates_WriteFile(string fileName, string strX)
    {
        string fileFullName = dirOut + fileName;

        if (File.Exists(fileFullName))
            using (StreamWriter sw = File.AppendText(fileFullName))
                sw.Write(strX);
        else
            using (StreamWriter sw = File.CreateText(fileFullName))
                sw.Write(strX);
    }
}
