#if !LIB
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Windows.Forms;
using MissionPlanner.Controls.Scene3D;
using MissionPlanner.Utilities;

namespace MissionPlanner.GCSViews
{
    public partial class FlightPlanner
    {
        private const string PreviewChanged = "The plan changed. Press Ctrl+M to refresh.";
        private LocalPlanPreviewWindow _localPlanPreview;
        private LocalPlanElevationSource _previewElevation;
        private PlanPreviewSnapshot _previewPlan;
        private (object Port, object Stream, object PlaybackFile, uint SystemId, int ComponentId, bool Connected, bool Playback) _previewScope;
        private System.Windows.Forms.Timer _previewScopeMonitor;
        private MAVLinkInterface _previewScopePort;
        private long _previewVehicleRevision;
        private long _previewPlanVehicleRevision;

        private void InitializeLocalPlanPreview()
        {
            Commands.RowsAdded += (sender, args) => InvalidateLocalPlanPreview();
            Commands.RowsRemoved += (sender, args) => InvalidateLocalPlanPreview();
            Commands.Sorted += (sender, args) => InvalidateLocalPlanPreview();
            Commands.CellBeginEdit += (sender, args) => InvalidateLocalPlanPreview();
            Commands.CellValueChanged += (sender, args) =>
            {
                if (args.RowIndex >= 0 && args.ColumnIndex >= 0)
                {
                    var column = Commands.Columns[args.ColumnIndex];
                    if (column == Command || column == Frame || column == Param1 || column == Param2 ||
                        column == Param3 || column == Param4 || column == Lat || column == Lon || column == Alt)
                        InvalidateLocalPlanPreview();
                }
            };
            TXT_homelat.TextChanged += (sender, args) => InvalidateLocalPlanPreview();
            TXT_homelng.TextChanged += (sender, args) => InvalidateLocalPlanPreview();
            TXT_homealt.TextChanged += (sender, args) => InvalidateLocalPlanPreview();
            cmb_missiontype.SelectedIndexChanged += (sender, args) => InvalidateLocalPlanPreview();
            HandleDestroyed += (sender, args) => _localPlanPreview?.Close();
            Disposed += (sender, args) => _localPlanPreview?.Close();
        }

        [KeyboardShortcut(Keys.Control | Keys.M, "Preview the local plan in 2D and 3D")]
        private void ShortcutLocalPlanPreview()
        {
            if (IsPlanEditInProgress())
            {
                ClearLocalPlanPreview(UiText.Translate("Finish the current plan edit before opening a preview."));
                return;
            }

            if (_localPlanPreview == null)
            {
                var window = new LocalPlanPreviewWindow { CanPublishSource = IsLocalPlanPreviewCurrent };
                _localPlanPreview = window;
                window.ChooseElevationSource += ChoosePlanPreviewElevation;
                window.FormClosed += ReleaseLocalPlanPreview;
                window.Disposed += ReleaseLocalPlanPreview;
                ObservePreviewPort(MainV2.comPort);
                _previewScopeMonitor = new System.Windows.Forms.Timer { Interval = 200 };
                _previewScopeMonitor.Tick += (sender, args) =>
                {
                    if (_previewPlan != null && (!PreviewScopeMatches() ||
                        CurrentState.multiplieralt != _previewPlan.HomeDisplayUnitsPerMetre))
                        InvalidateLocalPlanPreview();
                };
                _previewScopeMonitor.Start();
                window.Show(FindForm());
            }
            RefreshLocalPlanPreview();
            _localPlanPreview?.Activate();
        }

        private void ReleaseLocalPlanPreview(object sender, EventArgs args)
        {
            if (!ReferenceEquals(sender, _localPlanPreview))
                return;
            _previewScopeMonitor?.Dispose();
            _previewScopeMonitor = null;
            ObservePreviewPort(null);
            _localPlanPreview = null;
            _previewPlan = null;
            _previewScope = default;
            _previewElevation = null;
        }

        private void ChoosePlanPreviewElevation(object sender, EventArgs args)
        {
            using (var picker = new OpenFileDialog
            {
                Title = UiText.Translate("Choose elevation source"),
                Filter = UiText.Translate("Offline elevation manifest") + " (*.json)|*.json",
                CheckFileExists = true,
                RestoreDirectory = true
            })
            {
                if (picker.ShowDialog(_localPlanPreview) != DialogResult.OK || _localPlanPreview == null)
                    return;
                _previewElevation = null;
                InvalidateLocalPlanPreview();
                try
                {
                    _previewElevation = LocalPlanElevationSource.Open(picker.FileName);
                }
                catch (PlanPreviewDataException ex)
                {
                    ClearLocalPlanPreview(UiText.Translate(ex.Message));
                    return;
                }
                catch (Exception ex) when (IsPreviewFileException(ex))
                {
                    log.Warn("Cannot read local preview elevation source.", ex);
                    ClearLocalPlanPreview(UiText.Translate("Could not read the offline elevation files."));
                    return;
                }
            }
            RefreshLocalPlanPreview();
        }

        private void RefreshLocalPlanPreview()
        {
            try
            {
                var plan = CaptureLocalPlanPreview();
                if (plan == null)
                {
                    ClearLocalPlanPreview(UiText.Translate("The local plan is empty."));
                    return;
                }
                if (_previewElevation == null)
                {
                    ClearLocalPlanPreview(UiText.Translate("Choose an offline elevation manifest to preview this plan."));
                    return;
                }

                ObservePreviewPort(MainV2.comPort);
                var scope = ReadPreviewScope();
                var vehicleRevision = Interlocked.Read(ref _previewVehicleRevision);
                var scene = _previewElevation.BuildScene(plan);
                _previewPlan = plan;
                _previewScope = scope;
                _previewPlanVehicleRevision = vehicleRevision;
                _localPlanPreview.Replace(scene);
            }
            catch (PlanPreviewDataException ex)
            {
                string error = UiText.Translate(ex.Message);
                if (ex.Row > 0)
                    error = string.Format(UiText.Translate("Waypoint {0} · {1}"), ex.Row, error);
                ClearLocalPlanPreview(error);
            }
            catch (Exception ex) when (IsPreviewFileException(ex))
            {
                log.Warn("Cannot read local preview elevation files.", ex);
                ClearLocalPlanPreview(UiText.Translate("Could not read the offline elevation files."));
            }
        }

        private bool IsLocalPlanPreviewCurrent()
        {
            if (_previewPlan == null || !PreviewScopeMatches())
                return false;

            try
            {
                return _previewPlan.Matches(CaptureLocalPlanPreview());
            }
            catch (PlanPreviewDataException)
            {
                return false;
            }
        }

        private bool PreviewScopeMatches() =>
            Interlocked.Read(ref _previewVehicleRevision) == _previewPlanVehicleRevision && ReadPreviewScope().Equals(_previewScope);

        private void ObservePreviewPort(MAVLinkInterface port)
        {
            if (ReferenceEquals(_previewScopePort, port))
                return;
            var previous = Interlocked.Exchange(ref _previewScopePort, port);
            if (previous != null)
            {
                previous.MavChanged -= PreviewVehicleChanged;
                previous.CommsClose -= PreviewVehicleChanged;
            }
            if (port != null)
            {
                port.MavChanged += PreviewVehicleChanged;
                port.CommsClose += PreviewVehicleChanged;
            }
        }

        private void PreviewVehicleChanged(object sender, EventArgs args)
        {
            // The link raises this from either thread. Record every selection, even
            // if two changes restore the same device before the next UI dispatch.
            if (ReferenceEquals(sender, Volatile.Read(ref _previewScopePort)))
                Interlocked.Increment(ref _previewVehicleRevision);
        }

        private static (object, object, object, uint, int, bool, bool) ReadPreviewScope()
        {
            var port = MainV2.comPort;
            var stream = port?.BaseStream;
            return (port, stream, port?.logplaybackfile, port?.sysidcurrent ?? 0,
                port?.compidcurrent ?? 0, stream?.IsOpen == true, port?.logreadmode == true);
        }

        private static bool IsPreviewFileException(Exception ex) =>
            ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException ||
            ex is ArgumentException || ex is NotSupportedException;

        private void InvalidateLocalPlanPreview()
        {
            if (_previewPlan != null)
                ClearLocalPlanPreview(UiText.Translate(PreviewChanged));
        }

        private void ClearLocalPlanPreview(string reason)
        {
            _previewPlan = null;
            _localPlanPreview?.Clear(reason);
        }

        private bool IsPlanEditInProgress() =>
            quickadd || Commands.IsCurrentCellInEditMode || Commands.IsCurrentCellDirty;

        private PlanPreviewSnapshot CaptureLocalPlanPreview()
        {
            if (!(cmb_missiontype.SelectedValue is MAVLink.MAV_MISSION_TYPE type) ||
                type != MAVLink.MAV_MISSION_TYPE.MISSION)
                throw new PlanPreviewDataException("Select the mission plan to open a local preview.");
            if (IsPlanEditInProgress())
                throw new PlanPreviewDataException("Finish the current plan edit before opening a preview.");
            if (!Commands.Rows.Cast<DataGridViewRow>().Any(row => !row.IsNewRow))
                return null;

            double homeUnits = CurrentState.multiplieralt;
            if (!PlanPreviewSnapshot.IsFinite(homeUnits) || homeUnits <= 0 ||
                !TryPreviewNumber(TXT_homelat.Text, out var homeLat) ||
                !TryPreviewNumber(TXT_homelng.Text, out var homeLon) ||
                !PlanPreviewSnapshot.IsGeographic(homeLat, homeLon) ||
                !TryPreviewNumber(TXT_homealt.Text, out var homeAlt) ||
                !PlanPreviewSnapshot.IsFinite(homeAlt / homeUnits))
                throw new PlanPreviewDataException("The planned Home is incomplete or invalid.");

            var columns = new[] { Param1.Index, Param2.Index, Param3.Index, Param4.Index, Lat.Index, Lon.Index, Alt.Index };
            var route = new List<PlanPreviewSnapshot.Waypoint>();
            foreach (DataGridViewRow row in Commands.Rows)
            {
                if (row.IsNewRow) continue;
                int sequence = route.Count + 1;
                var command = row.Cells[Command.Index].Value as string;
                if (command != nameof(MAVLink.MAV_CMD.WAYPOINT))
                    throw new PlanPreviewDataException("This command is unsupported by the local preview.", sequence);
                var frameValue = row.Cells[Frame.Index].Value;
                if (!TryPreviewNumber(frameValue, out var frame) || frame < 0 || frame > byte.MaxValue || frame % 1 != 0)
                    throw new PlanPreviewDataException("The waypoint has an invalid coordinate frame.", sequence);

                cmdParamMultipliers.TryGetValue(command, out var multipliers);
                var fields = new double[columns.Length];
                for (int i = 0; i < columns.Length; i++)
                {
                    double scale = multipliers == null ? 1 : multipliers[i];
                    if (!TryPreviewNumber(row.Cells[columns[i]].Value, out double displayed) || scale <= 0 ||
                        !PlanPreviewSnapshot.IsFinite(scale) || !PlanPreviewSnapshot.IsFinite(fields[i] = displayed / scale))
                        throw new PlanPreviewDataException("The waypoint has an invalid numeric field.", sequence);
                }
                if (!PlanPreviewSnapshot.IsGeographic(fields[4], fields[5]))
                    throw new PlanPreviewDataException("The waypoint has invalid geographic coordinates.", sequence);
                route.Add(new PlanPreviewSnapshot.Waypoint((ushort)MAVLink.MAV_CMD.WAYPOINT, (byte)frame, fields));
            }

            return new PlanPreviewSnapshot(homeLat, homeLon, homeAlt / homeUnits, homeUnits, route.ToArray());
        }

        private static bool TryPreviewNumber(object value, out double number)
        {
            var culture = CultureInfo.CurrentCulture;
            if (value is string text)
                return double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, culture, out number) &&
                    PlanPreviewSnapshot.IsFinite(number);
            if (value is int || value is byte || value is ushort || value is float || value is double || value is decimal)
            {
                number = Convert.ToDouble(value, culture);
                return PlanPreviewSnapshot.IsFinite(number);
            }
            number = 0;
            return false;
        }
    }
}
#endif
