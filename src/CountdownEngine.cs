using System;
using System.Collections.Generic;
using System.Globalization;

namespace CountdownApp
{
    public enum CountdownStatus
    {
        Upcoming,
        Today,
        Ended,
        Invalid
    }

    public class CountdownSnapshot
    {
        public DateTime TargetDate { get; set; }
        public TimeSpan Remaining { get; set; }
        public int CalendarDays { get; set; }
        public CountdownStatus Status { get; set; }

        public CountdownSnapshot()
        {
            TargetDate = DateTime.MinValue;
            Remaining = TimeSpan.Zero;
            CalendarDays = 0;
            Status = CountdownStatus.Invalid;
        }
    }

    public static class CountdownEngine
    {
        public const string DateFormat = "yyyy-MM-dd";

        public static bool TryParseDate(string value, out DateTime date)
        {
            return DateTime.TryParseExact(
                value == null ? string.Empty : value.Trim(),
                DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date);
        }

        public static DateTime TargetFor(DateTime date)
        {
            return date.Date;
        }

        public static CountdownSnapshot Calculate(string dateText, DateTime now)
        {
            CountdownSnapshot snapshot = new CountdownSnapshot();
            DateTime date;
            if (!TryParseDate(dateText, out date))
            {
                return snapshot;
            }

            DateTime target = TargetFor(date);
            snapshot.TargetDate = target;

            if (now.Date > target.Date)
            {
                snapshot.Status = CountdownStatus.Ended;
                snapshot.Remaining = TimeSpan.Zero;
                snapshot.CalendarDays = 0;
                return snapshot;
            }

            snapshot.Status = now.Date == target.Date
                ? CountdownStatus.Today
                : CountdownStatus.Upcoming;
            snapshot.Remaining = target > now ? target - now : TimeSpan.Zero;
            snapshot.CalendarDays = Math.Max(0, (target.Date - now.Date).Days);
            return snapshot;
        }

        public static CountdownSnapshot Calculate(CountdownEvent item, DateTime now)
        {
            return Calculate(item == null ? string.Empty : item.Date, now);
        }

        public static bool ValidateEvent(CountdownEvent item, out string error)
        {
            error = string.Empty;
            if (item == null)
            {
                error = "事件不能为空。";
                return false;
            }
            if (string.IsNullOrEmpty(item.Name) || item.Name.Trim().Length == 0)
            {
                error = "请填写事件名称。";
                return false;
            }

            DateTime date;
            if (!TryParseDate(item.Date, out date))
            {
                error = "请选择有效日期。";
                return false;
            }

            return true;
        }

        public static List<CountdownEvent> SortForDisplay(IEnumerable<CountdownEvent> source)
        {
            List<CountdownEvent> result = new List<CountdownEvent>();
            if (source != null)
            {
                foreach (CountdownEvent item in source)
                {
                    if (item != null) result.Add(item);
                }
            }

            result.Sort(delegate(CountdownEvent left, CountdownEvent right)
            {
                DateTime leftDate;
                DateTime rightDate;
                bool leftValid = TryParseDate(left.Date, out leftDate);
                bool rightValid = TryParseDate(right.Date, out rightDate);
                bool leftEnded = leftValid && leftDate.Date < DateTime.Today;
                bool rightEnded = rightValid && rightDate.Date < DateTime.Today;

                if (!leftValid && !rightValid) return CompareNames(left, right);
                if (!leftValid) return 1;
                if (!rightValid) return -1;
                if (leftEnded != rightEnded) return leftEnded ? 1 : -1;

                int dateCompare = leftDate.Date.CompareTo(rightDate.Date);
                if (leftEnded) dateCompare = -dateCompare;
                if (dateCompare != 0) return dateCompare;
                return CompareNames(left, right);
            });

            return result;
        }

        public static List<CountdownEvent> SortForDisplay(IEnumerable<CountdownEvent> source, DateTime now)
        {
            List<CountdownEvent> result = new List<CountdownEvent>();
            if (source != null)
            {
                foreach (CountdownEvent item in source)
                {
                    if (item != null) result.Add(item);
                }
            }

            result.Sort(delegate(CountdownEvent left, CountdownEvent right)
            {
                CountdownSnapshot leftSnapshot = Calculate(left, now);
                CountdownSnapshot rightSnapshot = Calculate(right, now);
                bool leftEnded = leftSnapshot.Status == CountdownStatus.Ended || leftSnapshot.Status == CountdownStatus.Invalid;
                bool rightEnded = rightSnapshot.Status == CountdownStatus.Ended || rightSnapshot.Status == CountdownStatus.Invalid;

                if (leftEnded != rightEnded) return leftEnded ? 1 : -1;
                if (leftEnded)
                {
                    int reverseCompare = rightSnapshot.TargetDate.CompareTo(leftSnapshot.TargetDate);
                    if (reverseCompare != 0) return reverseCompare;
                }
                else
                {
                    int targetCompare = leftSnapshot.TargetDate.CompareTo(rightSnapshot.TargetDate);
                    if (targetCompare != 0) return targetCompare;
                }

                return CompareNames(left, right);
            });

            return result;
        }

        private static int CompareNames(CountdownEvent left, CountdownEvent right)
        {
            return string.Compare(
                left.Name == null ? string.Empty : left.Name,
                right.Name == null ? string.Empty : right.Name,
                StringComparison.CurrentCulture);
        }

        public static string StatusText(CountdownStatus status)
        {
            if (status == CountdownStatus.Today) return "今天";
            if (status == CountdownStatus.Ended) return "已到期";
            if (status == CountdownStatus.Invalid) return "日期异常";
            return "倒计时中";
        }

        public static string FormatRemaining(CountdownSnapshot snapshot, bool includeSeconds)
        {
            if (snapshot == null || snapshot.Status == CountdownStatus.Invalid) return "--";
            if (snapshot.Status == CountdownStatus.Ended) return "已到期";
            if (snapshot.Status == CountdownStatus.Today) return "今天";

            int remainingDays = (int)snapshot.Remaining.TotalDays;
            string value = remainingDays.ToString(CultureInfo.InvariantCulture) + " 天 "
                + snapshot.Remaining.Hours.ToString("00", CultureInfo.InvariantCulture) + ":"
                + snapshot.Remaining.Minutes.ToString("00", CultureInfo.InvariantCulture);
            if (includeSeconds)
            {
                value += ":" + snapshot.Remaining.Seconds.ToString("00", CultureInfo.InvariantCulture);
            }
            return value;
        }

        public static string FormatTargetDate(DateTime value)
        {
            return value == DateTime.MinValue ? "--" : value.ToString("yyyy年M月d日", CultureInfo.CurrentCulture);
        }
    }
}
