using CefSharp;
using CefSharp.OffScreen;
using System;
using System.Text;
using System.Threading;

namespace CefSharpTest {

    /// <summary>
    /// Console test.
    /// </summary>
    class Test {

        const bool IsFullTestEnabled = true;

        /// <summary>
        /// Entry point.
        /// </summary>
        /// <param name="args">Ignored arguments.</param>
        public static void Main(string[] args) {
            Browser = new ChromiumWebBrowser("https://www.google.com");
            Browser.JavascriptObjectRepository.ResolveObject += JavascriptObjectRepository_ResolveObject;
            Browser.FrameLoadEnd += Browser_FrameLoadEnd;
            TestTimer = new Timer(Reload, null, 2500, 1000);
            Console.WriteLine($"Observe {new CefSettings().BrowserSubprocessPath} memory usage (private working set).");
            Console.WriteLine("Testing, press Enter key to exit...");
            Console.ReadLine();
        }

        /// <summary>
        /// Executed on main frame loading end, binds C# class to V8 engine.
        /// </summary>
        /// <param name="sender">Browser.</param>
        /// <param name="e">Event arguments.</param>
        private static void Browser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e) {
            if (LeaveIt && !IsFullTestEnabled) return;
            var builder = new StringBuilder();
            builder.Append("(async()=>{");
            builder.Append($"if(typeof {JsBridgeName}==='undefined')await CefSharp.BindObjectAsync('{JsBridgeName}');");
            builder.Append("})();");
            e.Browser.MainFrame.ExecuteJavaScriptAsync(builder.ToString());
            LeaveIt = true;
        }

        /// <summary>
        /// Registers C# class in V8 repository, this is called once per session.
        /// </summary>
        /// <param name="sender">Browser.</param>
        /// <param name="e">Event arguments.</param>
        private static void JavascriptObjectRepository_ResolveObject(object sender, CefSharp.Event.JavascriptBindingEventArgs e) {
            if (e.ObjectName == JsBridgeName) e.ObjectRepository.Register(JsBridgeName, new TestJsRepo(), true);
        }

        /// <summary>
        /// Reloads page from timer to test memory leak during reloads.
        /// </summary>
        /// <param name="state">Unused, null.</param>
        private static void Reload(object state) => Browser.GetBrowser().MainFrame.EvaluateScriptAsync($"CefSharp.DeleteBoundObject('{JsBridgeName}');window.location.href='/';");

        /// <summary>
        /// Browser instance.
        /// </summary>
        static ChromiumWebBrowser Browser;

        /// <summary>
        /// <see cref="System.Threading.Timer"/>, made to automate page reloading.
        /// </summary>
        static Timer TestTimer;

        /// <summary>
        /// This is set for test purpose, when set the repository is no longer bound if full test is disabled.
        /// </summary>
        static bool LeaveIt;

        /// <summary>
        /// Test repository name.
        /// </summary>
        const string JsBridgeName = "TestJsRepo";

    }

    /// <summary>
    /// An empty class is enough to cause a huge memory leak.
    /// </summary>
    class TestJsRepo { }

}