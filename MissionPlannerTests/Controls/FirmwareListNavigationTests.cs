using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner;
using MissionPlanner.ArduPilot;
using MissionPlanner.Controls;
using MissionPlanner.GCSViews.ConfigurationView;
using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using Backstage = MissionPlanner.Controls.BackstageView.BackstageView;

namespace MissionPlannerTests.Controls
{
    [TestClass]
    [DoNotParallelize]
    public class FirmwareListNavigationTests
    {
        private const string Setting = "firmware_list_background";

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void InitialSetupNavigationRemainsAvailableDuringDelayedFirmwareList(bool legacy)
        {
            InSta(() =>
            {
                using (var release = new ManualResetEventSlim())
                using (var started = new ManualResetEventSlim())
                using (var finished = new ManualResetEventSlim())
                {
                    int workerThread = 0, calls = 0;
                    int uiThread = Thread.CurrentThread.ManagedThreadId;
                    Action wait = () =>
                    {
                        workerThread = Thread.CurrentThread.ManagedThreadId;
                        Interlocked.Increment(ref calls);
                        started.Set();
                        if (!release.Wait(TimeSpan.FromSeconds(6)))
                            throw new TimeoutException("Navigation failed to release the offline fixture.");
                        finished.Set();
                    };
                    using (var page = legacy
                        ? CreatePage<ConfigFirmware>(new Func<string, List<Firmware.software>>(_ =>
                        {
                            wait();
                            return new List<Firmware.software> { new Firmware.software { name = "Fixture Rover", url2560 = "apm1/APMRover" } };
                        }))
                        : (UserControl)CreatePage<ConfigFirmwareManifest>(new Func<APFirmware.RELEASE_TYPES, List<APFirmware.FirmwareInfo>>(_ =>
                        {
                            wait();
                            return RoverManifest();
                        })))
                    using (var window = new Form { Width = 1050, Height = 550 })
                    using (var backstage = new Backstage { Dock = DockStyle.Fill })
                    {
                        window.Controls.Add(backstage);
                        var alternate = backstage.AddPage(typeof(UserControl), "Another setup tab", null, false);
                        var firmware = backstage.AddPage(page.GetType(), "Install Firmware", null, false);
                        firmware.Page = page;
                        window.Show();
                        backstage.ActivatePage(firmware);
                        WaitUi(() => started.IsSet, "Fixture list never started");
                        Assert.AreNotEqual(uiThread, workerThread);
                        var rover = Find(page, "pictureBoxRover");
                        var initialText = rover.Text;
                        Assert.IsFalse(rover.Enabled);
                        if (!legacy)
                        {
                            Assert.IsFalse(Find(page, "lbl_devfw").Enabled, "Beta link should retain its prior loading state");
                            Assert.IsTrue(Find(page, "lbl_Custom_firmware_label").Enabled, "Independent local-firmware action should stay available");
                        }
                        backstage.ActivatePage(alternate);
                        Assert.AreSame(alternate, backstage.SelectedPage);
                        Assert.IsFalse(finished.IsSet, "Navigation waited for the network fixture");

                        // While still pending, returning attaches to the existing task rather than starting another.
                        backstage.ActivatePage(firmware);
                        backstage.ActivatePage(alternate);
                        release.Set();
                        WaitUi(() => finished.IsSet, "Fixture list never completed");
                        PumpUi();
                        Assert.AreEqual(initialText, rover.Text, "Inactive page received a stale completion");
                        backstage.ActivatePage(firmware);
                        WaitUi(() => rover.Enabled, "Returning did not attach or re-use the list");
                        Assert.IsTrue(rover.Text.Contains("Fixture Rover"));
                        if (!legacy)
                            Assert.IsTrue(Find(page, "lbl_devfw").Enabled, "Beta link did not become available");
                        Assert.AreEqual(1, Volatile.Read(ref calls));
                    }
                }
            });
        }

        [TestMethod]
        public void ManifestOptOutStillNavigatesAndRetriesAfterOfflineFailure()
        {
            InSta(() =>
            {
                Settings.Instance[Setting] = "False";
                int calls = 0;
                var loader = new Func<APFirmware.RELEASE_TYPES, List<APFirmware.FirmwareInfo>>(_ =>
                {
                    if (Interlocked.Increment(ref calls) == 1)
                        throw new IOException("Fixture offline");
                    return RoverManifest();
                });
                using (var page = CreatePage<ConfigFirmwareManifest>(loader))
                using (var window = new Form())
                {
                    window.Controls.Add(page);
                    window.Show();
                    page.Activate();
                    WaitUi(() => Find(page, "lbl_status").Text != Strings.GettingFWList, "No offline error message");
                    Assert.IsFalse(Find(page, "pictureBoxRover").Enabled);
                    page.Deactivate();
                    page.Activate();
                    WaitUi(() => Find(page, "pictureBoxRover").Enabled, "Returning did not retry");
                    Assert.IsFalse(Find(page, "pictureBoxPlane").Enabled, "Partial list activated absent firmware");
                    page.Deactivate();
                    page.Activate();
                    WaitUi(() => Volatile.Read(ref calls) == 3 && Find(page, "pictureBoxRover").Enabled,
                        "Opt-out did not refresh the finished list on the next entry");
                }
            });
        }

        [TestMethod]
        public void LegacyOptOutUsesWaitingDialog()
        {
            InSta(() =>
            {
                Settings.Instance[Setting] = "False";
                bool showedDialog = false;
                using (var release = new ManualResetEventSlim())
                using (var timer = new System.Windows.Forms.Timer { Interval = 20 })
                using (var page = CreatePage<ConfigFirmware>(new Func<string, List<Firmware.software>>(_ =>
                {
                    if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException();
                    return new List<Firmware.software> { new Firmware.software { name = "Fixture Rover", url2560 = "apm1/APMRover" } };
                })))
                using (var window = new Form())
                {
                    timer.Tick += (sender, args) =>
                    {
                        if (Application.OpenForms.OfType<ProgressReporterDialogue>().Any(dialog => dialog.Visible))
                        {
                            showedDialog = true;
                            release.Set();
                        }
                    };
                    window.Controls.Add(page);
                    window.Show();
                    timer.Start();
                    page.Activate();
                    timer.Stop();
                    release.Set();
                    Assert.IsTrue(showedDialog, "Legacy opt-out no longer opens its waiting dialog");
                    Assert.IsTrue(Find(page, "pictureBoxRover").Enabled);
                }
            });
        }

        [TestMethod]
        public void DisposedPageIgnoresCompletedOfflineFixture()
        {
            InSta(() =>
            {
                using (var release = new ManualResetEventSlim())
                using (var finished = new ManualResetEventSlim())
                using (var page = CreatePage<ConfigFirmwareManifest>(new Func<APFirmware.RELEASE_TYPES, List<APFirmware.FirmwareInfo>>(_ =>
                {
                    if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException();
                    finished.Set();
                    return RoverManifest();
                })))
                using (var window = new Form())
                {
                    window.Controls.Add(page);
                    window.Show();
                    var rover = Find(page, "pictureBoxRover");
                    int changes = 0;
                    rover.TextChanged += (sender, args) => changes++;
                    page.Activate();
                    page.Dispose();
                    int changesAtDisposal = changes;
                    release.Set();
                    WaitUi(() => finished.IsSet, "Disposed offline fixture did not finish");
                    PumpUi();
                    Assert.AreEqual(changesAtDisposal, changes, "A disposed page received refreshed firmware text");
                }
            });
        }

        [TestMethod]
        public void LayoutCheckboxUsesTheFirmwareSettingAndNormalProfileSave()
        {
            InSta(() =>
            {
                var helper = typeof(ConfigPlanner).Assembly.GetType("MissionPlanner.GCSViews.ConfigurationView.FirmwareListSettings", true);
                var create = helper.GetMethod("CreateToggle", BindingFlags.Static | BindingFlags.NonPublic);
                var directory = Path.Combine(Path.GetTempPath(), "firmware-list-setting-" + Guid.NewGuid().ToString("N"));
                var currentProfile = typeof(Settings).GetField("_currentConfigName", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.IsNotNull(currentProfile);
                var oldProfile = currentProfile.GetValue(null);
                var oldDirectory = Settings.CustomUserDataDirectory;
                try
                {
                    Settings.CustomUserDataDirectory = directory;
                    currentProfile.SetValue(null, "FirmwareFixture");
                    Directory.CreateDirectory(Settings.GetUserDataDirectory());
                    Settings.Instance.Remove(Setting);
                    using (var first = (CheckBox)create.Invoke(null, null))
                    {
                        Assert.IsTrue(first.Checked);
                        first.Checked = false;
                        Assert.AreEqual("False", Settings.Instance[Setting]);
                    }
                    using (var second = (CheckBox)create.Invoke(null, null))
                        Assert.IsFalse(second.Checked);

                    // This is the normal application save path (MainV2 calls Settings.Save on close).
                    Settings.Instance.Save();
                    var profileFile = Path.Combine(Settings.GetUserDataDirectory(), "config_FirmwareFixture.xml");
                    var settings = new XmlDocument();
                    settings.Load(profileFile);
                    var entry = settings.DocumentElement[Setting];
                    Assert.IsNotNull(entry);
                    Assert.AreEqual("False", entry.InnerText);
                }
                finally
                {
                    Settings.CustomUserDataDirectory = oldDirectory;
                    currentProfile.SetValue(null, oldProfile);
                    if (Directory.Exists(directory))
                        Directory.Delete(directory, true);
                }
            });
        }

        private static List<APFirmware.FirmwareInfo> RoverManifest() => new List<APFirmware.FirmwareInfo>
        {
            new APFirmware.FirmwareInfo
            {
                MavType = APFirmware.MAV_TYPE.GROUND_ROVER.ToString(), MavFirmwareVersion = new Version(1, 2),
                MavFirmwareVersionStr = "1.2", MavFirmwareVersionType = "OFFICIAL", VehicleType = "Fixture Rover"
            }
        };

        private static Control Find(Control page, string name) => page.Controls.Find(name, true).Single();

        private static T CreatePage<T>(Delegate fetch) where T : UserControl =>
            (T)Activator.CreateInstance(typeof(T), BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { fetch }, null);

        private static void WaitUi(Func<bool> predicate, string failure)
        {
            var deadline = Stopwatch.StartNew();
            while (!predicate() && deadline.Elapsed < TimeSpan.FromSeconds(5))
            {
                Application.DoEvents();
                Thread.Sleep(10);
            }
            Assert.IsTrue(predicate(), failure);
        }

        private static void PumpUi()
        {
            for (int i = 0; i < 12; i++)
            {
                Application.DoEvents();
                Thread.Sleep(10);
            }
        }

        private static void InSta(Action action)
        {
            Exception error = null;
            var thread = new Thread(() =>
            {
                var previous = Settings.Instance[Setting];
                bool previousCheck = Control.CheckForIllegalCrossThreadCalls;
                try
                {
                    Settings.Instance[Setting] = "True";
                    Control.CheckForIllegalCrossThreadCalls = true;
                    action();
                }
                catch (Exception ex) { error = ex; }
                finally
                {
                    if (previous == null) Settings.Instance.Remove(Setting);
                    else Settings.Instance[Setting] = previous;
                    Control.CheckForIllegalCrossThreadCalls = previousCheck;
                }
            }) { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(20)), "Firmware UI test timed out");
            if (error != null) throw new Exception("Firmware UI test failed", error);
        }
    }
}
