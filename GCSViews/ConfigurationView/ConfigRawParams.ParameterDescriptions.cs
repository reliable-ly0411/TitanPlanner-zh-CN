using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using MissionPlanner.Utilities;

namespace MissionPlanner.GCSViews.ConfigurationView
{
    public partial class ConfigRawParams
    {
        private sealed class DescriptionSession
        {
            internal object Vehicle;
            internal string Identity;
            internal bool Chinese;
            internal ParameterDescriptionContext Context;
            internal ParameterDescriptionSnapshot Snapshot;
            internal readonly ConcurrentDictionary<string, ParameterDescriptionText> Text = new ConcurrentDictionary<string, ParameterDescriptionText>(StringComparer.Ordinal);
        }

        private volatile DescriptionSession _descriptionSession;
        private static bool UseEnhancedParameterDescriptions =>
            string.Equals(CultureInfo.CurrentUICulture.Name, "zh-CN", StringComparison.Ordinal);
        private static readonly Lazy<ParameterDescriptionCatalog> DescriptionCatalog = new Lazy<ParameterDescriptionCatalog>(() =>
            new ParameterDescriptionCatalog(ReadDescriptionResource("ParameterDescriptions.zh-CN.xml")));
        private static readonly Lazy<ParameterDescriptionStore> DescriptionStore = new Lazy<ParameterDescriptionStore>(() =>
            new ParameterDescriptionStore(Path.Combine(Settings.GetUserDataDirectory(), "ParameterDescriptions"), ReadDescriptionResource("ParameterDescriptions.official.xml")));

        private static byte[] ReadDescriptionResource(string name)
        {
            using (var input = typeof(ParameterDescriptionCatalog).Assembly.GetManifestResourceStream("MissionPlanner.Utilities." + name))
            using (var output = new MemoryStream())
            {
                if (input == null) throw new InvalidDataException("Missing parameter description resource: " + name);
                input.CopyTo(output);
                return output.ToArray();
            }
        }

        private void InitializeParameterDescriptions()
        {
            // Displayed rows resize for the complete bilingual text, including after column resizing.
#if !LIB
            if (!UseEnhancedParameterDescriptions) return;
            Params.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
            Desc.HeaderText += "（双击展开）";
            Params.CellDoubleClick += (sender, e) =>
            {
                if (!UseEnhancedParameterDescriptions || e.RowIndex < 0 || (e.ColumnIndex != Desc.Index && e.ColumnIndex != Options.Index)) return;
                using (var details = CreateParameterDescriptionDetails(Params.Rows[e.RowIndex])) details.ShowDialog(FindForm());
            };
#endif
        }

#if !LIB
        private Form CreateParameterDescriptionDetails(DataGridViewRow row)
        {
            // A long bilingual paragraph may exceed the viewport even with its full preferred row height.
            var details = new Form { Text = row.Cells[Command.Index].Value?.ToString(), Width = 860, Height = 640, StartPosition = FormStartPosition.CenterParent };
            var text = new RichTextBox
            {
                Dock = DockStyle.Fill, ReadOnly = true, WordWrap = true, Font = Params.Font,
                Text = row.Cells[Command.Index].ToolTipText + "\n\n" + row.Cells[Units.Index].Value + "\n" + row.Cells[Options.Index].Value
            };
            details.Controls.Add(text);
            return details;
        }
#endif

        private void PrepareParameterDescriptions()
        {
            if (!UseEnhancedParameterDescriptions)
            {
                if (_descriptionSession != null)
                {
                    _descriptionSession = null;
                    ResetParameterDescriptionRows();
                }
                return;
            }
            var vehicle = MainV2.comPort.MAV;
            var culture = CultureInfo.CurrentUICulture.Name;
            var identity = vehicle.cs.firmware + "|" + vehicle.VersionString + "|" + vehicle.cs.version + "|" + culture;
            if (_descriptionSession != null && ReferenceEquals(_descriptionSession.Vehicle, vehicle) && _descriptionSession.Identity == identity) return;
            var session = new DescriptionSession
            {
                Vehicle = vehicle, Identity = identity, Chinese = true,
                Context = ParameterDescriptionContext.TryCreate(vehicle.cs.firmware.ToString(), vehicle.VersionString, vehicle.cs.version)
            };
            try { session.Snapshot = DescriptionStore.Value.Bundled(session.Context); }
            catch (Exception ex) { log.Error("Bundled parameter descriptions", ex); }
            _descriptionSession = session;
            ResetParameterDescriptionRows();
            if (session.Context == null) return;
            Task.Run(async () =>
            {
                try
                {
                    var snapshot = await DescriptionStore.Value.LoadAsync(session.Context).ConfigureAwait(false);
                    if (IsDisposed || !IsHandleCreated) return;
                    BeginInvoke((Action)(() =>
                    {
                        if (!IsCurrentDescriptionSession(session) || IsDisposed) return;
                        _descriptionSession = new DescriptionSession
                        {
                            Vehicle = session.Vehicle, Identity = session.Identity, Chinese = session.Chinese,
                            Context = session.Context, Snapshot = snapshot
                        };
                        _metadataLoaded.Clear();
                        filterList(txt_search.Text);
                        ScheduleVisibleMetadataLoad();
                    }));
                }
                catch (Exception ex) { log.Error("Versioned parameter descriptions", ex); }
            });
        }

        private void ResetParameterDescriptionRows()
        {
            // A same-size list from a different vehicle, version or language must not reuse old translated rows.
            startup = true;
            _metadataLoaded.Clear();
            foreach (DataGridViewRow row in Params.Rows)
            {
                row.Cells[Desc.Index].Value = null;
                row.Cells[Options.Index].Value = null;
                row.Cells[Units.Index].Value = null;
                row.Cells[Desc.Index].ToolTipText = "";
                row.Cells[Command.Index].ToolTipText = "";
                row.Cells[Value.Index].ToolTipText = "";
                row.Cells[Options.Index].ToolTipText = "";
            }
        }

        private bool IsCurrentDescriptionSession(DescriptionSession session)
        {
            if (!UseEnhancedParameterDescriptions || session == null || !ReferenceEquals(_descriptionSession, session) || !ReferenceEquals(MainV2.comPort.MAV, session.Vehicle)) return false;
            var vehicle = MainV2.comPort.MAV;
            return session.Identity == vehicle.cs.firmware + "|" + vehicle.VersionString + "|" + vehicle.cs.version + "|" + CultureInfo.CurrentUICulture.Name;
        }

        private ParameterDescriptionText ExactDescription(string name, DescriptionSession session)
        {
            if (!UseEnhancedParameterDescriptions || session == null || !session.Chinese || session.Snapshot == null) return null;
            ParameterDescriptionText value;
            if (session.Text.TryGetValue(name, out value)) return value;
            value = DescriptionCatalog.Value.Present(session.Snapshot, name, session.Chinese);
            if (value != null) session.Text.TryAdd(name, value);
            return value;
        }

        private ParameterDescriptionText ReadParameterDescription(string name, string firmware, DescriptionSession session)
        {
            var exact = ExactDescription(name, session);
            if (exact != null) return exact;
            // Retain the existing English fallback; its version is not evidence for applying Chinese notes.
            var desc = ParameterMetaDataRepository.GetParameterMetaData(name, ParameterMetaDataConstants.Description, firmware);
            var range = ParameterMetaDataRepository.GetParameterMetaData(name, ParameterMetaDataConstants.Range, firmware);
            var options = ParameterMetaDataRepository.GetParameterMetaData(name, ParameterMetaDataConstants.Values, firmware);
            return new ParameterDescriptionText(desc,
                ParameterMetaDataRepository.GetParameterMetaData(name, ParameterMetaDataConstants.Units, firmware),
                (range + "\n" + options.Replace(",", "\n")).Trim(),
                session != null && session.Chinese ? "已有英文元数据；未核实此条目与固件版本的对应关系。" : "Existing English metadata; release match not verified for this entry.");
        }

        private void ApplyParameterDescriptionToRow(DataGridViewRow row, ParameterDescriptionText text)
        {
            row.Cells[Command.Index].ToolTipText = text.Description + "\n\n" + text.Source;
            row.Cells[Value.Index].ToolTipText = row.Cells[Command.Index].ToolTipText;
            row.Cells[Units.Index].Value = text.Units;
            row.Cells[Options.Index].Value = text.Options;
            row.Cells[Options.Index].ToolTipText = text.Options;
            row.Cells[Desc.Index].Value = text.Description;
            row.Cells[Desc.Index].ToolTipText = row.Cells[Command.Index].ToolTipText;
        }

        private bool MatchesParameterDescription(string name, Regex filter)
        {
            var text = ExactDescription(name, _descriptionSession);
            return text != null && filter.IsMatch(text.SearchText);
        }

        private List<KeyValuePair<int, string>> BilingualParameterOptions(string name, List<KeyValuePair<int, string>> original)
        {
            if (!UseEnhancedParameterDescriptions) return original;
            var session = _descriptionSession;
            var record = session?.Snapshot?.Find(name);
            if (session == null || !session.Chinese || record == null || record.Ambiguous || !session.Snapshot.MatchesReportedBuild) return original;
            return original.Select(option =>
            {
                var code = option.Key.ToString(CultureInfo.InvariantCulture);
                // The original numeric keys and selection/write path stay intact. Do not translate a stale editor label.
                var source = record.Values.FirstOrDefault(v => v.Code == code && v.English == option.Value);
                return new KeyValuePair<int, string>(option.Key, source == null ? option.Value
                    : DescriptionCatalog.Value.ChoiceLabel(session.Context, name, "Value", code, source.English, true, record.Fingerprint));
            }).ToList();
        }
    }
}
