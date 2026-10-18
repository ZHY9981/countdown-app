using System;
using System.Collections.Generic;
using System.IO;
using CountdownApp;

internal static class CountdownLogicTests
{
    private static int _failures;

    private static void Main()
    {
        TestDateParsing();
        TestTodaySnapshot();
        TestUpcomingSnapshot();
        TestEndedSnapshot();
        TestSorting();
        TestValidation();
        TestStoreRoundTrip();

        if (_failures > 0)
        {
            Console.Error.WriteLine(_failures.ToString() + " test(s) failed.");
            Environment.Exit(1);
        }

        Console.WriteLine("All Countdown App tests passed.");
    }

    private static void TestDateParsing()
    {
        DateTime value;
        Assert(CountdownEngine.TryParseDate("2027-06-01", out value), "valid ISO date should parse");
        Assert(value.Year == 2027 && value.Month == 6 && value.Day == 1, "parsed date fields");
        Assert(!CountdownEngine.TryParseDate("2027/06/01", out value), "slash date should be rejected");
        Assert(!CountdownEngine.TryParseDate("", out value), "empty date should be rejected");
    }

    private static void TestTodaySnapshot()
    {
        CountdownSnapshot snapshot = CountdownEngine.Calculate("2026-10-18", new DateTime(2026, 10, 18, 9, 30, 0));
        Assert(snapshot.Status == CountdownStatus.Today, "today status");
        Assert(snapshot.CalendarDays == 0, "today calendar days");
        Assert(CountdownEngine.FormatRemaining(snapshot, true) == "今天", "today display");
    }

    private static void TestUpcomingSnapshot()
    {
        CountdownSnapshot snapshot = CountdownEngine.Calculate("2026-10-20", new DateTime(2026, 10, 18, 12, 0, 0));
        Assert(snapshot.Status == CountdownStatus.Upcoming, "upcoming status");
        Assert(snapshot.CalendarDays == 2, "upcoming calendar days");
        Assert(snapshot.Remaining.TotalHours == 36d, "upcoming remaining time");
        Assert(CountdownEngine.FormatRemaining(snapshot, true) == "1 天 12:00:00", "upcoming display");
    }

    private static void TestEndedSnapshot()
    {
        CountdownSnapshot snapshot = CountdownEngine.Calculate("2026-10-17", new DateTime(2026, 10, 18, 12, 0, 0));
        Assert(snapshot.Status == CountdownStatus.Ended, "ended status");
        Assert(snapshot.Remaining == TimeSpan.Zero, "ended remaining time");
        Assert(CountdownEngine.FormatRemaining(snapshot, true) == "已到期", "ended display");
    }

    private static void TestSorting()
    {
        List<CountdownEvent> events = new List<CountdownEvent>();
        events.Add(CreateEvent("到期", "2026-10-17"));
        events.Add(CreateEvent("较远", "2026-10-30"));
        events.Add(CreateEvent("最近", "2026-10-19"));

        List<CountdownEvent> sorted = CountdownEngine.SortForDisplay(events, new DateTime(2026, 10, 18, 12, 0, 0));
        Assert(sorted[0].Name == "最近", "nearest event first");
        Assert(sorted[1].Name == "较远", "future event order");
        Assert(sorted[2].Name == "到期", "ended event last");
    }

    private static void TestValidation()
    {
        string error;
        CountdownEvent item = new CountdownEvent();
        item.Name = "考试";
        item.Date = "2026-12-20";
        Assert(CountdownEngine.ValidateEvent(item, out error), "valid event");

        item.Name = " ";
        Assert(!CountdownEngine.ValidateEvent(item, out error), "blank name rejected");
        Assert(error == "请填写事件名称。", "blank name error");
    }

    private static void TestStoreRoundTrip()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "countdown-app-tests-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "events.json");

        try
        {
            AppState state = new AppState();
            state.Initialized = true;
            CountdownEvent item = CreateEvent("存档测试", "2027-01-15");
            state.Events.Add(item);

            EventStore store = new EventStore(path);
            store.Save(state);
            AppState loaded = store.Load();

            Assert(loaded.Initialized, "store initialized flag");
            Assert(loaded.Events.Count == 1, "store event count");
            Assert(loaded.Events[0].Name == "存档测试", "store event name");
            Assert(loaded.Events[0].Date == "2027-01-15", "store event date");
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private static CountdownEvent CreateEvent(string name, string date)
    {
        CountdownEvent item = new CountdownEvent();
        item.Name = name;
        item.Date = date;
        return item;
    }

    private static void Assert(bool condition, string name)
    {
        if (condition) return;
        _failures++;
        Console.Error.WriteLine("FAIL: " + name);
    }
}
