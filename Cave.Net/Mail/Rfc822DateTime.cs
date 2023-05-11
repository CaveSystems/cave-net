using System;
using System.Collections.Generic;
using System.IO;

namespace Cave.Mail;

/// <summary>Provides conversion routines for rfc822 datetime fields.</summary>
public static class Rfc822DateTime
{
    /// <summary>Decodes a rfc822 datetime field.</summary>
    /// <param name="rfc822DateTime"></param>
    /// <returns></returns>
    public static DateTime Decode(string rfc822DateTime)
    {
        if (rfc822DateTime == null)
        {
            throw new ArgumentNullException(nameof(rfc822DateTime));
        }
        //try default parser first
        {
            if (DateTime.TryParse(rfc822DateTime, out var result))
            {
                return result;
            }
        }
        //try to manually parse
        try
        {
            var result = new SimpleDate();
            double localDifference = 0;

            var date = rfc822DateTime.ToUpperInvariant();
            if (CheckString(ref date, "JAN"))
            {
                result.Month = 1;
            }
            else if (CheckString(ref date, "FEB"))
            {
                result.Month = 2;
            }
            else if (CheckString(ref date, "MAR"))
            {
                result.Month = 3;
            }
            else if (CheckString(ref date, "APR"))
            {
                result.Month = 4;
            }
            else if (CheckString(ref date, "MAY"))
            {
                result.Month = 5;
            }
            else if (CheckString(ref date, "JUN"))
            {
                result.Month = 6;
            }
            else if (CheckString(ref date, "JUL"))
            {
                result.Month = 7;
            }
            else if (CheckString(ref date, "AUG"))
            {
                result.Month = 8;
            }
            else if (CheckString(ref date, "SEP"))
            {
                result.Month = 9;
            }
            else if (CheckString(ref date, "OCT"))
            {
                result.Month = 10;
            }
            else if (CheckString(ref date, "NOV"))
            {
                result.Month = 11;
            }
            else if (CheckString(ref date, "DEC"))
            {
                result.Month = 12;
            }

            var timeZoneIndex = date.IndexOfAny(new[] { '+', '-' });
            if (timeZoneIndex > -1)
            {
                var timeZone = date.Substring(timeZoneIndex).Trim();
                date = date.Substring(0, timeZoneIndex);
                try
                {
                    if (timeZone.Length > 5)
                    {
                        timeZone = timeZone.Substring(0, 5);
                    }

                    localDifference = int.Parse(timeZone) / 100.0;
                }
                catch
                {
                    localDifference = 0;
                }
            }
            if (localDifference == 0)
            {
                if (CheckString(ref date, " mst"))
                {
                    localDifference = -7;
                }
                else if (CheckString(ref date, " mdt"))
                {
                    localDifference = -6;
                }
                else if (CheckString(ref date, " cst"))
                {
                    localDifference = -6;
                }
                else if (CheckString(ref date, " pst"))
                {
                    localDifference = -5;
                }
                else if (CheckString(ref date, " cdt"))
                {
                    localDifference = -5;
                }
                else if (CheckString(ref date, " est"))
                {
                    localDifference = -5;
                }
                else if (CheckString(ref date, " pdt"))
                {
                    localDifference = -4;
                }
                else if (CheckString(ref date, " edt"))
                {
                    localDifference = -4;
                }
                else if (CheckString(ref date, " a"))
                {
                    localDifference = +1;
                }
                else if (CheckString(ref date, " b"))
                {
                    localDifference = +2;
                }
                else if (CheckString(ref date, " c"))
                {
                    localDifference = +3;
                }
                else if (CheckString(ref date, " d"))
                {
                    localDifference = +4;
                }
                else if (CheckString(ref date, " e"))
                {
                    localDifference = +5;
                }
                else if (CheckString(ref date, " f"))
                {
                    localDifference = +6;
                }
                else if (CheckString(ref date, " g"))
                {
                    localDifference = +7;
                }
                else if (CheckString(ref date, " h"))
                {
                    localDifference = +8;
                }
                else if (CheckString(ref date, " i"))
                {
                    localDifference = +9;
                }
                else if (CheckString(ref date, " k"))
                {
                    localDifference = +10;
                }
                else if (CheckString(ref date, " l"))
                {
                    localDifference = +12;
                }
                else if (CheckString(ref date, " m"))
                {
                    localDifference = +12;
                }
                else if (CheckString(ref date, " n"))
                {
                    localDifference = -1;
                }
                else if (CheckString(ref date, " o"))
                {
                    localDifference = -2;
                }
                else if (CheckString(ref date, " p"))
                {
                    localDifference = -3;
                }
                else if (CheckString(ref date, " q"))
                {
                    localDifference = -4;
                }
                else if (CheckString(ref date, " r"))
                {
                    localDifference = -5;
                }
                else if (CheckString(ref date, " s"))
                {
                    localDifference = -6;
                }
                else if (CheckString(ref date, " t"))
                {
                    localDifference = -7;
                }
                else if (CheckString(ref date, " u"))
                {
                    localDifference = -8;
                }
                else if (CheckString(ref date, " v"))
                {
                    localDifference = -9;
                }
                else if (CheckString(ref date, " w"))
                {
                    localDifference = -10;
                }
                else if (CheckString(ref date, " x"))
                {
                    localDifference = -11;
                }
                else if (CheckString(ref date, " y"))
                {
                    localDifference = -12;
                }
            }

            var values = ValueExtractor(date);
            for (var i = 0; i < values.Count; i++)
            {
                if (values[i] > 1900)
                {
                    result.Year = values[i];
                    values.RemoveAt(i);
                    break;
                }
            }
            if (result.Year == 0)
            {
                if (values.Count >= 2)
                {
                    result.Year = values[1];
                    values.RemoveAt(1);
                }
            }

            if (values.Count >= 1)
            {
                result.Day = values[0];
            }

            if (values.Count >= 2)
            {
                result.Hour = values[1];
            }

            if (values.Count >= 3)
            {
                result.Min = values[2];
            }

            if (values.Count >= 4)
            {
                result.Sec = values[3];
            }

            return new DateTime(result.Year, result.Month, result.Day, result.Hour, result.Min, result.Sec, DateTimeKind.Utc).AddHours(-localDifference).ToLocalTime();
        }
        catch (Exception ex)
        {
            throw new InvalidDataException(string.Format("Invalid date format '{0}'!", rfc822DateTime), ex);
        }
    }

    /// <summary>Encodes a rfc822 datetime field.</summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string Encode(DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Utc)
        {
            return dateTime.ToString("ddd, dd MMM yyyy HH':'mm':'ss GMT");
        }
        var localDifference = (int)(100.0 * DateTimeOffset.Now.Offset.TotalHours);
        if (localDifference > 0)
        {
            return dateTime.ToString("ddd, dd MMM yyyy HH':'mm':'ss +" + localDifference);
        }
        return dateTime.ToString("ddd, dd MMM yyyy HH':'mm':'ss " + localDifference);
    }

    static bool CheckString(ref string date, string pattern)
    {
        var index = date.IndexOf(pattern);
        if (index < 0)
        {
            return false;
        }

        date = date.Remove(index, pattern.Length);
        return true;
    }

    static List<int> ValueExtractor(string date)
    {
        var result = new List<int>();
        var gotOne = false;
        var current = 0;
        foreach (var c in date)
        {
            switch (c)
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    gotOne = true;
                    current = (current * 10) + (c - '0');
                    break;
                default:
                    if (gotOne)
                    {
                        result.Add(current);
                        current = 0;
                        gotOne = false;
                    }
                    break;
            }
        }
        if (gotOne)
        {
            result.Add(current);
        }

        return result;
    }

    struct SimpleDate
    {
        public int Day;
        public int Month;
        public int Year;
        public int Hour;
        public int Min;
        public int Sec;
    }
}
