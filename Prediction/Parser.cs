using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace Prediction
{
    public class Parser : IDisposable
    {
        private HttpClient _httpClient;
        private HtmlParser _htmlParser;
        private const string _baseAdress = "https://int.soccerway.com";

        public Parser()
        {
            _httpClient = new HttpClient();
            _htmlParser = new HtmlParser();
        }

        public void GetMatchesForTheWeek ()
        {
            DateTime date = DateTime.Today;
            for (int i = 0; i < 8; i++)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(date.ToString());
                try
                {
                    GetMatchesForTheDay(date);
                }
                catch (Exception)
                {
                    continue;
                }
                date = date.AddDays(1.0);
            }
        }
        public void GetMatchesForTheDay (DateTime date)
        {
            string page = _httpClient.GetStringAsync(GetDayUrl(date)).Result;
            var parsed = _htmlParser.ParseDocument(page);
            var table = parsed.Body.GetElementsByClassName("matches date_matches grouped ")[0].GetElementsByTagName("tbody")[0];
            var links = table.GetElementsByClassName("competition-link");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Uncancelled matches for the day");
            foreach (var link in links)
            {
                string url = link.GetElementsByTagName("a")[0].GetAttribute("href");
                try
                {
                    GetUncancelledMatchesFromDate(url, date);
                }
                catch
                {
                    continue;
                }
            }

        }

        private void ParseMatchTable (string url, out List<IElement> elements)
        {
            if (_httpClient.GetAsync(_baseAdress + url).Result.IsSuccessStatusCode)
            {
                string page = _httpClient.GetStringAsync(_baseAdress + url).Result;
                var parsed = _htmlParser.ParseDocument(page);
                var table = parsed.Body.GetElementsByClassName("matches   ")[0].GetElementsByTagName("tbody")[0];
                var elementsHtml = table.GetElementsByTagName("tr");
                elements = elementsHtml.ToList();
                elements.RemoveAll(el => el.ClassName == "even aggr-even aggr" || el.ClassName == "odd aggr-odd aggr");
            }
            else
            {
                elements = null;
            }
        }

        private void GetUncancelledMatchesFromDate(string url, DateTime date)
        {
            ParseMatchTable(url, out List<IElement> elements);
            List<string> links = GetUncancelledMatchesFromDate(elements, date);
            foreach (var link in links)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(link);
                try
                {
                    CheckHeadToHead(link + "head2head/", date);
                }
                catch
                {
                    continue;
                }
            }
        }
        private List<string> GetUncancelledMatchesFromDate(List<IElement> elements, DateTime date)
        {
            List<string> links = new List<string>();
            if (elements != null)
            {
                foreach (var el in elements)
                {
                    string matchDate;
                    var dates = el.GetElementsByClassName("date no-repetition");
                    if (dates == null || dates.Count() == 0) continue;
                    else if (dates[0].GetElementsByClassName("timestamp").Count() == 0) matchDate = dates[0].InnerHtml;
                    else matchDate = dates[0].GetElementsByClassName("timestamp")[0].InnerHtml;
                    DateTime dateTime = DateTime.Parse(matchDate);
                    if (dateTime.CompareTo(date) == 0)
                    {
                        string scoreTimeStatus = el.GetElementsByTagName("td")[3].GetElementsByTagName("a")[0].GetAttribute("title");
                        if (scoreTimeStatus != "Postponed" && scoreTimeStatus != "Cancelled")
                        {
                            links.Add(el.GetElementsByClassName("info-button button")?[0].GetElementsByTagName("a")?[0].GetAttribute("href"));
                        }
                    }
                }
            }
            return links;
        }

        private void CheckHeadToHead(string url, DateTime date)
        {
            ParseMatchTable(url, out List<IElement> elements);
            List<(string, string)> links = CheckHeadToHead(elements, date);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Match that happened in the same week day earlier between teams: ");
            foreach (var link in links)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{link.Item1} - {link.Item2}");
                try
                {
                    CheckScore(link.Item1, DateTime.Parse(link.Item2));
                }
                catch
                {
                    continue;
                }
            }

        }
        private List<(string, string)> CheckHeadToHead(List<IElement> elements, DateTime date)
        {
            List<(string, string)> links = new List<(string, string)>();
            if (elements != null)
            {
                bool stop = false;
                foreach (var el in elements)
                {
                    var day = date.DayOfWeek;
                    var dayFromElement = el.GetElementsByClassName("day no-repetition")[0].InnerHtml;
                    if (!(new List<string>() { "Mon", "Tue", "Wed", "Thu", "Fri", "Sun", "Sat" }.Contains(dayFromElement)))
                    {
                        dayFromElement = el.GetElementsByClassName("day no-repetition")[0].GetElementsByClassName("timestamp")[0].InnerHtml;
                    };
                    var datesFromElement = el.GetElementsByClassName("full-date")[0].GetElementsByClassName("timestamp").ToList();
                    string dateFromElement;
                    if (datesFromElement.Count == 0) dateFromElement = el.GetElementsByClassName("full-date")[0].InnerHtml;
                    else dateFromElement = datesFromElement[0].InnerHtml;
                    if (DateTime.Compare(DateTime.Parse(dateFromElement), date) < 0)
                    {
                        switch (day)
                        {
                            case DayOfWeek.Monday:
                                if (dayFromElement == "Mon")
                                {
                                    links.Add(new ValueTuple<string, string>(el.GetElementsByClassName("team team-a ")[0].GetElementsByTagName("a")[0].GetAttribute("href"),
                                        dateFromElement));
                                    links.Add(new ValueTuple<string, string>(el.GetElementsByClassName("team team-b ")[0].GetElementsByTagName("a")[0].GetAttribute("href"),
                                        dateFromElement));
                                    stop = true;
                                }
                                break;
                            case DayOfWeek.Tuesday:
                                if (dayFromElement == "Tue")
                                {
                                    links.Add(new ValueTuple<string, string>(el.GetElementsByClassName("team team-a ")[0].GetElementsByTagName("a")[0].GetAttribute("href"),
                                        dateFromElement));
                                    links.Add(new ValueTuple<string, string>(el.GetElementsByClassName("team team-b ")[0].GetElementsByTagName("a")[0].GetAttribute("href"),
                                        dateFromElement));
                                    stop = true;
                                }
                                break;
                            case DayOfWeek.Wednesday:
                                if (dayFromElement == "Wed")
                                    {
                                        links.Add(new ValueTuple<string, string>(el.GetElementsByClassName("team team-a ")[0].GetElementsByTagName("a")[0].GetAttribute("href"),
                                            dateFromElement));
                                        links.Add(new ValueTuple<string, string>(el.GetElementsByClassName("team team-b ")[0].GetElementsByTagName("a")[0].GetAttribute("href"),
                                            dateFromElement));
                                        stop = true;
                                    }
                                break;
                            case DayOfWeek.Thursday:
                                if (dayFromElement == "Thu")
                                {
                                    links.Add(new ValueTuple<string, string>(el.GetElementsByClassName("team team-a ")[0].GetElementsByTagName("a")[0].GetAttribute("href"),
                                        dateFromElement));
                                    links.Add(new ValueTuple<string, string>(el.GetElementsByClassName("team team-b ")[0].GetElementsByTagName("a")[0].GetAttribute("href"),
                                        dateFromElement));
                                    stop = true;
                                }
                                break;
                            case DayOfWeek.Friday:
                                if (dayFromElement == "Fri")
                                    {
                                        links.Add(new ValueTuple<string, string>(el.GetElementsByClassName("team team-a ")[0].GetElementsByTagName("a")[0].GetAttribute("href"),
                                            dateFromElement));
                                        links.Add(new ValueTuple<string, string>(el.GetElementsByClassName("team team-b ")[0].GetElementsByTagName("a")[0].GetAttribute("href"),
                                            dateFromElement));
                                        stop = true;
                                    }
                                break;
                            case DayOfWeek.Saturday:
                                if (dayFromElement == "Sat")
                                {
                                    links.Add(new ValueTuple<string, string>(el.GetElementsByClassName("team team-a ")[0].GetElementsByTagName("a")[0].GetAttribute("href"),
                                        dateFromElement));
                                    links.Add(new ValueTuple<string, string>(el.GetElementsByClassName("team team-b ")[0].GetElementsByTagName("a")[0].GetAttribute("href"),
                                        dateFromElement));
                                    stop = true;
                                }
                                break;
                            case DayOfWeek.Sunday:
                                if (dayFromElement == "Sun")
                                {
                                    links.Add(new ValueTuple<string, string>(el.GetElementsByClassName("team team-a ")[0].GetElementsByTagName("a")[0].GetAttribute("href"),
                                        dateFromElement));
                                    links.Add(new ValueTuple<string, string>(el.GetElementsByClassName("team team-b ")[0].GetElementsByTagName("a")[0].GetAttribute("href"),
                                        dateFromElement));
                                    stop = true;
                                }
                                break;
                        }
                    }
                    if (stop) break;

                }
            }
            return links;
        }

        private void CheckScore (string url, DateTime date)
        {
            ParseMatchTable(url + "matches/", out List<IElement> elements);
            string score = CheckScore(elements, date);
            if (score != null)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(score);
            }

        }

        private string CheckScore (List<IElement> elements, DateTime date)
        {
            string score = "";
            if (elements != null)
            {
                bool next = false;
                foreach (var el in elements)
                {
                    if (next == true)
                    {
                        var teamInfo = el.GetElementsByTagName("td")[3].GetElementsByTagName("a")[0].InnerHtml.Trim();
                        teamInfo += " - ";
                        teamInfo += el.GetElementsByTagName("td")[5].GetElementsByTagName("a")[0].InnerHtml.Trim();
                        teamInfo += "   ";
                        var scoreElement = el.GetElementsByTagName("td")[4].GetElementsByTagName("a")[0];
                        score = scoreElement.TextContent.Trim();
                        while (score.Contains("P")) { score = score.Remove(score.IndexOf("P"), 1); }
                        if ((score.Contains("1 - 0") || score.Contains("0 - 1") || score.Contains("0 - 0")) && !(score.Contains("10 - 0") || score.Contains("0 - 10")))
                            return teamInfo+=score;
                        else break;
                    }
                    else
                    {
                        var datesFromElement = el.GetElementsByClassName("full-date")[0].GetElementsByClassName("timestamp").ToList();
                        string dateFromElement;
                        if (datesFromElement.Count == 0) dateFromElement = el.GetElementsByClassName("full-date")[0].InnerHtml;
                        else dateFromElement = datesFromElement[0].InnerHtml;
                        if (DateTime.Compare(DateTime.Parse(dateFromElement), date) == 0) next = true;
                    }
                }
            }
            return null;
        }
        
        private string GetDayUrl (DateTime date)
        {
            string url = _baseAdress + "/matches/" + date.Year;
            if (date.Month < 10) url += "/0" + date.Month;
            else url += "/" + date.Month;

            if (date.Day < 10) url += "/0" + date.Day;
            else url += "/" + date.Day;
            return url;
        }

        private bool _disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    _httpClient.Dispose();
                }
            }
            this._disposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
