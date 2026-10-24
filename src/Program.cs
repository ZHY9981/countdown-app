using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace CountdownApp
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool demoMode = false;
            bool startInWidgetMode = false;
            bool minimumWindow = false;
            string screenshotPath = string.Empty;
            int screenshotDelayMs = 350;
            int animationDelayMs = 80;
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "--demo", StringComparison.OrdinalIgnoreCase))
                {
                    demoMode = true;
                }
                else if (string.Equals(args[i], "--widget", StringComparison.OrdinalIgnoreCase))
                {
                    startInWidgetMode = true;
                }
                else if (args[i] != null && args[i].StartsWith("--screenshot=", StringComparison.OrdinalIgnoreCase))
                {
                    screenshotPath = args[i].Substring("--screenshot=".Length);
                }
                else if (string.Equals(args[i], "--window-size=min", StringComparison.OrdinalIgnoreCase))
                {
                    minimumWindow = true;
                }
                else if (args[i] != null && args[i].StartsWith("--screenshot-delay=", StringComparison.OrdinalIgnoreCase))
                {
                    int parsedDelay;
                    if (int.TryParse(
                        args[i].Substring("--screenshot-delay=".Length),
                        out parsedDelay)
                        && parsedDelay >= 0)
                    {
                        screenshotDelayMs = parsedDelay;
                    }
                }
                else if (args[i] != null && args[i].StartsWith("--animation-delay=", StringComparison.OrdinalIgnoreCase))
                {
                    int parsedDelay;
                    if (int.TryParse(
                        args[i].Substring("--animation-delay=".Length),
                        out parsedDelay)
                        && parsedDelay >= 0)
                    {
                        animationDelayMs = parsedDelay;
                    }
                }
            }

            bool createdNew;
            using (Mutex mutex = new Mutex(true, "CountdownApp.SingleInstance", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show(
                        "倒计时已经在运行，请在任务栏托盘查看。",
                        "倒计时",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                try
                {
                    EventStore store = new EventStore();
                    AppState state = demoMode ? CreateDemoState() : LoadState(store);
                    MainForm form = new MainForm(
                        state,
                        store,
                        demoMode,
                        startInWidgetMode);
                    if (minimumWindow) form.Size = form.MinimumSize;
                    if (string.IsNullOrEmpty(screenshotPath))
                    {
                        Application.Run(form);
                    }
                    else
                    {
                        CaptureScreenshot(form, screenshotPath, screenshotDelayMs, animationDelayMs);
                    }
                }
                catch (Exception ex)
                {
                    if (!string.IsNullOrEmpty(screenshotPath))
                    {
                        try
                        {
                            File.WriteAllText(screenshotPath + ".error.txt", ex.ToString());
                        }
                        catch
                        {
                            // The original exception remains the primary failure.
                        }
                        return;
                    }
                    MessageBox.Show(
                        "程序启动失败：" + ex.Message,
                        "倒计时",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private static void CaptureScreenshot(MainForm form, string path, int delayMs, int animationDelayMs)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            form.Show();
            form.Refresh();
            Application.DoEvents();
            DateTime warmupUntil = DateTime.UtcNow.AddMilliseconds(delayMs);
            while (DateTime.UtcNow < warmupUntil)
            {
                Thread.Sleep(10);
                Application.DoEvents();
            }
            DateTime animationUntil = DateTime.UtcNow.AddMilliseconds(animationDelayMs);
            while (DateTime.UtcNow < animationUntil)
            {
                Thread.Sleep(10);
                Application.DoEvents();
            }

            if (form.HasWebView)
            {
                System.Threading.Tasks.Task captureTask =
                    form.CaptureWebViewToFileAsync(path + ".web.png");
                while (!captureTask.IsCompleted)
                {
                    Thread.Sleep(10);
                    Application.DoEvents();
                }
                captureTask.GetAwaiter().GetResult();

                System.Threading.Tasks.Task<string> diagnosticsTask = form.GetWebDiagnosticsAsync();
                while (!diagnosticsTask.IsCompleted)
                {
                    Thread.Sleep(10);
                    Application.DoEvents();
                }
                File.WriteAllText(path + ".web.txt", diagnosticsTask.GetAwaiter().GetResult());
            }
            form.CaptureToFile(path);
            form.CloseForAutomation();
        }

        private static AppState LoadState(EventStore store)
        {
            AppState state = store.Load();
            bool changed = false;
            if (!state.Initialized)
            {
                if (state.Events.Count == 0) SeedDefaults(state);
                state.Initialized = true;
                changed = true;
            }
            if (changed) store.Save(state);
            return state;
        }

        internal static AppState CreateDemoState()
        {
            AppState state = new AppState();
            state.Initialized = true;
            SeedDefaults(state);
            state.Events[0].Name = "产品发布";
            state.Events[0].Date = DateTime.Today.AddDays(3).ToString("yyyy-MM-dd");
            state.Events[0].CreatedAt = DateTime.Now.AddDays(-1);
            state.Events[1].Name = "年度复盘";
            state.Events[1].Date = DateTime.Today.AddDays(38).ToString("yyyy-MM-dd");
            state.Events[1].CreatedAt = DateTime.Now.AddDays(-10);

            CountdownEvent third = new CountdownEvent();
            third.Name = "2027 黑客松";
            third.Date = DateTime.Today.AddDays(226).ToString("yyyy-MM-dd");
            third.CreatedAt = DateTime.Now.AddDays(-100);
            state.Events.Add(third);
            return state;
        }

        private static void SeedDefaults(AppState state)
        {
            state.Events = new List<CountdownEvent>();

            CountdownEvent first = new CountdownEvent();
            first.Name = "雅思考试";
            first.Date = "2026-10-25";
            state.Events.Add(first);

            CountdownEvent second = new CountdownEvent();
            second.Name = "2027 黑客松";
            second.Date = "2027-06-01";
            state.Events.Add(second);
        }
    }
}
