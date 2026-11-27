using MissionPlanner.Controls;
using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MissionPlanner.GCSViews.ConfigurationView
{
    internal static class FirmwareListSettings
    {
        internal const string LoadInBackgroundKey = "firmware_list_background";
        internal static bool LoadInBackground => Settings.Instance.GetBoolean(LoadInBackgroundKey, true);

        internal static CheckBox CreateToggle()
        {
            var toggle = new CheckBox
            {
                AutoSize = true,
                Name = "CHK_firmware_list_background",
                Text = UiText.Translate("Load firmware lists in background"),
                Checked = LoadInBackground
            };
            toggle.CheckedChanged += (sender, args) =>
                Settings.Instance[LoadInBackgroundKey] = toggle.Checked.ToString();
            var help = new ToolTip();
            help.SetToolTip(toggle, UiText.Translate(
                "Reuse the firmware list when returning to the tab and keep the legacy page non-modal. Turning this off restores refresh-on-entry for the standard page and the waiting dialog for the legacy page."));
            toggle.Disposed += (sender, args) => help.Dispose();
            return toggle;
        }

        internal static string FailureText(Exception error) => UiText.Translate(error is OperationCanceledException
            ? "Firmware list load cancelled. Reopen this tab to retry."
            : "Could not load firmware list. Reopen this tab to retry.");

        // The opt-out retains the modal progress workflow used by the legacy page.
        internal static T WaitWithDialog<T>(Task<T> task)
        {
            using (var dialog = new ProgressReporterDialogue())
            {
                dialog.doWorkArgs.ForceExit = true;
                dialog.doWorkArgs.CancelRequestChanged += (sender, args) => { dialog.doWorkArgs.CancelAcknowledged = true; };
                dialog.DoWork += sender =>
                {
                    dialog.UpdateProgressAndStatus(-1, Strings.GettingFWList);
                    // Observe completion on the UI side below, including failures.
                    while (!task.IsCompleted && !sender.doWorkArgs.CancelRequested)
                        Thread.Sleep(50);
                };
                ThemeManager.ApplyThemeTo(dialog);
                dialog.RunBackgroundOperationAsync();
                if (dialog.doWorkArgs.CancelRequested || !task.IsCompleted)
                    throw new OperationCanceledException();
                return task.GetAwaiter().GetResult();
            }
        }
    }

    /// <summary>UI-thread-owned request state. Fetches never touch the page or any control.</summary>
    internal sealed class FirmwareListRequest<TKey, TResult>
    {
        private readonly Control owner;
        private readonly int uiThread;
        private readonly Dictionary<TKey, Task<TResult>> requests = new Dictionary<TKey, Task<TResult>>();
        private Task<TResult> tail;
        private int generation;

        public FirmwareListRequest(Control owner)
        {
            this.owner = owner;
            uiThread = Thread.CurrentThread.ManagedThreadId;
        }

        public void Invalidate()
        {
            generation++;
        }

        public void Start(TKey requestedKey, Func<TKey, TResult> fetch, bool background,
            Action<TResult> apply, Action<Exception> failed, bool reuseCompleted = true)
        {
            int token = ++generation;
            foreach (var request in requests.Where(item => item.Value.IsCompleted &&
                !EqualityComparer<TKey>.Default.Equals(item.Key, requestedKey)).ToArray())
                requests.Remove(request.Key);

            if (!requests.TryGetValue(requestedKey, out Task<TResult> task) ||
                task.IsFaulted || task.IsCanceled || (!reuseCompleted && task.IsCompleted))
            {
                var previous = tail;
                // Different channels/versions can share underlying loader state. Serialize per page.
                task = Task.Run(async () =>
                {
                    if (previous != null)
                    {
                        try { await previous.ConfigureAwait(false); }
                        catch (Exception) { /* observed by the prior UI request */ }
                    }
                    return fetch(requestedKey);
                });
                requests[requestedKey] = tail = task;
            }

            if (background || task.IsCompleted)
                ObserveOnUi(task, requestedKey, token, apply, failed);
            else
                Complete(task, requestedKey, token, () => FirmwareListSettings.WaitWithDialog(task), apply, failed);
        }

        private async void ObserveOnUi(Task<TResult> task, TKey requestedKey, int token, Action<TResult> apply, Action<Exception> failed)
        {
            try { await task.ConfigureAwait(false); }
            catch (Exception) { /* report only if this view still owns the request */ }
            // WinForms on Mono does not always install a usable synchronization context. Target the
            // actual Backstage child handle, and drop completion if disposed/recreated first.
            if (!owner.IsHandleCreated || owner.IsDisposed || owner.Disposing)
                return;
            Action complete = () => Complete(task, requestedKey, token, () => task.GetAwaiter().GetResult(), apply, failed);
            if (Thread.CurrentThread.ManagedThreadId == uiThread)
            {
                complete();
                return;
            }
            try { owner.BeginInvoke(complete); }
            catch (InvalidOperationException) { /* the page handle was removed before dispatch */ }
        }

        private void Complete(Task<TResult> task, TKey requestedKey, int token, Func<TResult> result, Action<TResult> apply, Action<Exception> failed)
        {
            if (token != generation || owner.IsDisposed || owner.Disposing)
                return;
            try
            {
                var value = result();
                if (token == generation && !owner.IsDisposed && !owner.Disposing)
                    apply(value);
            }
            catch (Exception error)
            {
                if (token != generation || owner.IsDisposed || owner.Disposing)
                    return;
                // Empty or invalid successful returns must be eligible for retry on next activation.
                if (task.IsCompleted && requests.TryGetValue(requestedKey, out var latest) && ReferenceEquals(latest, task))
                    requests.Remove(requestedKey);
                failed(error);
            }
        }
    }
}
