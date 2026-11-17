using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner.Utilities;

namespace MissionPlannerTests.Utilities
{
    [TestClass]
    public class WindowIconRefreshTests
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr parameter, IntPtr data);

        private static void Apply(Icon icon, Form mainWindow)
        {
            // Exercise the production assignment routine without constructing MainV2's vehicle/map subsystem.
            var method = typeof(DesktopAppearance).GetMethod("ApplyWindowIcon", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            method.Invoke(null, new object[] { icon, mainWindow });
        }

        private static void OnSta(Action test)
        {
            Exception failure = null;
            var thread = new Thread(() => { try { test(); } catch (Exception ex) { failure = ex; } });
            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(20)), "Window icon test timed out.");
            if (failure != null) Assert.Fail(failure.ToString());
        }

        private static void CheckIcon(Form form, Icon expected)
        {
            Assert.AreSame(expected, form.Icon);
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                Assert.AreEqual(expected.Handle, SendMessage(form.Handle, 0x007f, new IntPtr(1), IntPtr.Zero),
                    "WM_GETICON / ICON_BIG must follow the managed Icon, including default restoration.");
        }

        [TestMethod]
        public void MainWindowOutsideOpenFormsReceivesCustomAndDefaultIcons()
        {
            OnSta(() =>
            {
                using (var mainWindow = new Form())
                using (var otherWindow = new Form())
                using (var original = (Icon)SystemIcons.Application.Clone())
                using (var custom = (Icon)SystemIcons.Warning.Clone())
                {
                    mainWindow.Icon = original;
                    var handle = mainWindow.Handle; // A real HWND, without Show/OnLoad registering the form.
                    otherWindow.Icon = original;
                    otherWindow.Show();
                    Assert.IsFalse(Application.OpenForms.Cast<Form>().Contains(mainWindow));
                    Assert.IsTrue(Application.OpenForms.Cast<Form>().Contains(otherWindow));
                    Apply(custom, mainWindow);
                    CheckIcon(mainWindow, custom); CheckIcon(otherWindow, custom);
                    Apply(original, mainWindow);
                    CheckIcon(mainWindow, original); CheckIcon(otherWindow, original);
                    // The main window may also be registered normally.
                    mainWindow.Show();
                    Apply(custom, mainWindow);
                    CheckIcon(mainWindow, custom);
                }
            });
        }

        [TestMethod]
        public void NullOrDisposedMainDoesNotBlockOtherWindows()
        {
            OnSta(() =>
            {
                using (var closed = new Form())
                using (var otherWindow = new Form())
                using (var first = (Icon)SystemIcons.Application.Clone())
                using (var second = (Icon)SystemIcons.Warning.Clone())
                {
                    otherWindow.Show(); closed.Dispose();
                    Apply(first, null); CheckIcon(otherWindow, first);
                    Apply(second, closed); CheckIcon(otherWindow, second);
                }
            });
        }
    }
}
