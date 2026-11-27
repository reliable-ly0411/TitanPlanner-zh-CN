import copy
import importlib.util
import json
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
spec = importlib.util.spec_from_file_location('parameter_catalog', ROOT / 'scripts/localization/build_parameter_catalog.py')
OSD_MESSAGE_TIME_HELP_KEYS = {'OSD_MSG_TIME'}
LOG_STORAGE_HELP_KEYS = {'LOG_MAX_FILES', 'LOG_FILE_MB_FREE'}
ONBOARD_DISPLAY_HELP_KEYS = {'NTF_DISPLAY_TYPE'}
SIDEBAR_GEOMETRY_KEYS = {'OSD_SB_H_OFS', 'OSD_SB_V_EXT'}
IMU_DIAGNOSTIC_HELP_KEYS = {'INS_LOG_BAT_CNT', 'INS_LOG_BAT_LGIN', 'INS_LOG_BAT_LGCT'}
OSD_SCREEN_FONT_KEYS = [f'OSD{screen}_FONT' for screen in range(1, 5)]
OSD_FONT_KEYS = {'OSD_FONT', *OSD_SCREEN_FONT_KEYS}
LOGGER_BACKEND_HELP_KEYS = {
    'LOG_BACKEND_TYPE', 'LOG_FILE_DSRMROT', 'LOG_MAV_BUFSIZE', 'LOG_FILE_TIMEOUT',
    'LOG_FILE_RATEMAX', 'LOG_MAV_RATEMAX', 'LOG_BLK_RATEMAX', 'LOG_DARM_RATEMAX',
}
DISPLAY_AND_LOG_HELP_KEYS = (
    SIDEBAR_GEOMETRY_KEYS | IMU_DIAGNOSTIC_HELP_KEYS | OSD_MESSAGE_TIME_HELP_KEYS
    | LOG_STORAGE_HELP_KEYS | OSD_FONT_KEYS | ONBOARD_DISPLAY_HELP_KEYS | LOGGER_BACKEND_HELP_KEYS
    | {'OSD_OPTIONS', 'OSD_UNITS'}
    | {'LOG_FILE_BUFSIZE', 'OSD_H_OFFSET', 'OSD_V_OFFSET'}
)
catalog_module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(catalog_module)


class ParameterCatalogTests(unittest.TestCase):
    def setUp(self):
        self.catalog = json.loads((ROOT / 'localization/zh-Hans/parameter-translations.json').read_text(encoding='utf-8'))
        self.bundle = ET.parse(ROOT / 'localization/parameters/official-first-batch.xml').getroot()

    def test_committed_resource_is_current_and_scoped(self):
        self.assertTrue(catalog_module.generated_matches((ROOT / 'localization/parameters/zh-CN.xml').read_bytes(), catalog_module.resource(self.catalog, self.bundle)))
        sources = json.loads((ROOT / 'localization/parameters/sources.json').read_text(encoding='utf-8'))['releases']
        by_scope = {(source['family'], source['version']): source for source in sources}
        self.assertEqual(len(sources), len(by_scope))
        self.assertEqual(set(by_scope), {(release.get('family'), release.get('version')) for release in self.bundle})
        for release in self.bundle:
            source = by_scope[release.get('family'), release.get('version')]
            self.assertEqual(source['parameters'], len(list(release.iter('param'))))
            self.assertEqual(source['sourceSha256'], release.get('sourceSha256'))
            self.assertEqual(source['sourceUrl'], release.get('sourceUrl'))
            firmware = release.find('paramfile/firmware')
            self.assertEqual(source['gitSha'], release.get('gitSha'))
            self.assertEqual(source['gitSha'], firmware.get('git_sha'))
            self.assertEqual(source['gitTag'], firmware.get('git_tag'))

    def test_scope_counts_and_held_versions(self):
        notes = self.catalog['translations']
        descriptions = {(n['family'], v, n['parameter']) for n in notes if n['field'] == 'Description' for v in n['versions']}
        self.assertEqual(710, len(descriptions))
        self.assertEqual(325, len({(f, p) for f, v, p in descriptions}))
        self.assertFalse(any(v == '4.7.2' for f, v, p in descriptions))
        excluded_copter = {'RTL_CONE_SLOPE', 'LAND_SPEED', 'LAND_SPD_MS', 'FS_GCS_ENABLE'}
        excluded_plane = {'TECS_CLMB_MAX', 'TECS_SINK_MIN', 'ROLL_LIMIT_DEG'}
        self.assertFalse(any(p in (excluded_copter if f == 'Copter' else excluded_plane) for f, v, p in descriptions))

    def test_copter_catalog_additions_are_version_exact_read_only_descriptions(self):
        # Snapshot of the original accepted coverage per version. This assertion
        # guards the narrowly reviewed expansion, not actual vehicle inventory.
        legacy_counts = {'4.5.7': 37, '4.6.3': 37, '4.7.1': 37}
        reviewed_counts = {'4.5.7': 36, '4.6.3': 36, '4.7.1': 42}
        pairs = (('GPS_CAN_NODEID1', 'GPS_CAN_NODEID2'), ('GPS1_CAN_NODEID', 'GPS2_CAN_NODEID'))
        for release in self.bundle.findall("release[@family='Copter']"):
            v = release.get('version')
            records = {r.get('name').split(':')[-1]: r for r in release.iter('param')
                       if r.get('name') not in DISPLAY_AND_LOG_HELP_KEYS}
            self.assertEqual(legacy_counts[v] + reviewed_counts[v], len(records))
            readonly = {k: r for k, r in records.items() if r.findtext("field[@name='ReadOnly']") == 'True'}
            self.assertEqual(reviewed_counts[v], len(readonly))
            self.assertNotIn('STAT_RESET', readonly)
            selected, held = pairs if v == '4.5.7' else pairs[::-1]
            self.assertTrue(set(selected) <= readonly.keys())
            self.assertFalse(set(held) & readonly.keys())
            for key, record in readonly.items():
                notes = [n for n in self.catalog['translations'] if n['family'] == 'Copter'
                         and n['parameter'] == key and v in n['versions']]
                self.assertEqual(1, len(notes))
                self.assertEqual(('Description', catalog_module.record_fingerprint(record)),
                                 (notes[0]['field'], notes[0]['recordSha256']))
                self.assertEqual(record.get('documentation'), notes[0]['english'])

    def test_plane_read_only_additions_preserve_version_and_field_identity(self):
        expected = {'4.5.7': 33, '4.6.3': 33, '4.7.1': 39}  # includes the earlier 4.6.3 BARO1
        for release in self.bundle.findall("release[@family='Plane']"):
            v = release.get('version')
            selected = {r.get('name'): r for r in release.iter('param') if r.findtext("field[@name='ReadOnly']") == 'True'}
            self.assertEqual(expected[v], len(selected))
            self.assertIn('BARO1_DEVID', selected)
            self.assertEqual(v == '4.7.1', 'ARSPD3_DEVID' in selected)
            self.assertEqual(v == '4.5.7', 'GPS_CAN_NODEID1' in selected)
            self.assertNotIn('STAT_RESET', selected)
            for key, record in selected.items():
                notes = [n for n in self.catalog['translations'] if n['family'] == 'Plane'
                         and n['parameter'] == key and v in n['versions']]
                self.assertEqual(1, len(notes))
                self.assertEqual(('Description', record.get('documentation'), catalog_module.record_fingerprint(record)),
                                 (notes[0]['field'], notes[0]['english'], notes[0]['recordSha256']))

    def test_sub_slice_contains_only_exact_release_read_only_statistics(self):
        reviewed = {'STAT_BOOTCNT', 'STAT_FLTTIME', 'STAT_RUNTIME', 'STAT_FLTCNT'}
        release = self.bundle.find("release[@family='Sub']")
        self.assertEqual('4.7.1', release.get('version'))
        official = {param.get('name'): param for param in release.iter('param') if param.get('name').startswith('STAT_')}
        self.assertEqual(reviewed, set(official))
        for name, param in official.items():
            self.assertEqual('True', param.find("field[@name='ReadOnly']").text)
            self.assertEqual('s' if name.endswith('TIME') else None, param.findtext("field[@name='Units']"))
            self.assertIsNone(param.find("field[@name='Range']"))
            self.assertIsNone(param.find('values'))
        notes = [note for note in self.catalog['translations'] if note['family'] == 'Sub' and note['parameter'].startswith('STAT_')]
        self.assertEqual(4, len(notes))
        self.assertEqual(reviewed, {note['parameter'] for note in notes})
        self.assertTrue(all(note['versions'] == ['4.7.1'] and note['field'] == 'Description' for note in notes))

        altered = copy.deepcopy(self.bundle)
        altered.find("release[@family='Sub']//param/field[@name='ReadOnly']").text = 'False'
        with self.assertRaises(ValueError):
            catalog_module.resource(self.catalog, altered)

    def test_sub_narrow_expansion_adds_only_unique_read_only_records(self):
        scope = self.bundle.find("release[@family='Sub'][@version='4.7.1']")
        help_keys = DISPLAY_AND_LOG_HELP_KEYS | {'LOG_DISARMED', 'LOG_REPLAY'}
        records = {p.get('name'): p for p in scope.iter('param')
                   if not p.get('name').startswith('STAT_') and p.get('name') not in help_keys}
        self.assertEqual(34, len(records))
        self.assertEqual({'GPS1_CAN_NODEID','GPS2_CAN_NODEID'}, {key for key in records if key.startswith('GPS')})
        self.assertTrue({'MIS_TOTAL','VTX_FREQ'} <= records.keys())
        self.assertIsNone(self.bundle.find("release[@family='Sub'][@version='4.6.3']"))
        for key, record in records.items():
            notes = [n for n in self.catalog['translations'] if n['family'] == 'Sub' and n['parameter'] == key]
            self.assertEqual(1, len(notes))
            self.assertEqual('True', record.findtext("field[@name='ReadOnly']"))
            self.assertEqual((['4.7.1'], 'Description', catalog_module.record_fingerprint(record)),
                             (notes[0]['versions'], notes[0]['field'], notes[0]['recordSha256']))

    def test_osd_sidebar_geometry_positions_preserve_spacing_direction(self):
        selected = {'OSD_SB_H_OFS':('0 20','列','右侧'), 'OSD_SB_V_EXT':('0 10','行','等量')}
        for release in self.bundle:
            f,v = release.get('family'),release.get('version')
            records = {r.get('name'):r for r in release.iter('param') if r.get('name') in selected}
            self.assertEqual(set() if f=='Tracker' or v=='4.5.7' else set(selected),set(records))
            for key,r in records.items():
                expected,unit,detail = selected[key]
                self.assertEqual(expected,r.findtext("field[@name='Range']"))
                self.assertIsNone(r.find("field[@name='Units']"))
                notes = [n for n in self.catalog['translations'] if n['family']==f and n['parameter']==key and v in n['versions']]
                self.assertEqual(1,len(notes))
                self.assertEqual(('Description',r.get('documentation'),catalog_module.record_fingerprint(r)),
                                 (notes[0]['field'],notes[0]['english'],notes[0]['recordSha256']))
                self.assertIn(unit,notes[0]['chinese'])
                self.assertIn(detail,notes[0]['chinese'])

    def test_imu_logging_help_keeps_diagnostic_sampling_semantics(self):
        keys = {'INS_LOG_BAT_CNT', 'INS_LOG_BAT_LGIN', 'INS_LOG_BAT_LGCT'}
        for release in self.bundle:
            f, v = release.get('family'), release.get('version')
            records = {r.get('name'): r for r in release.iter('param') if r.get('name') in keys}
            self.assertEqual(keys, set(records))
            self.assertEqual('True', records['INS_LOG_BAT_CNT'].findtext("field[@name='RebootRequired']"))
            self.assertEqual('ms', records['INS_LOG_BAT_LGIN'].findtext("field[@name='Units']"))
            self.assertEqual('32', records['INS_LOG_BAT_CNT'].findtext("field[@name='Increment']"))
            for key, r in records.items():
                hints = [n for n in self.catalog['translations'] if n['parameter'] == key and n['family'] == f and v in n['versions']]
                self.assertEqual(1, len(hints))
                self.assertEqual(('Description',r.get('documentation'),catalog_module.record_fingerprint(r)),
                                 (hints[0]['field'],hints[0]['english'],hints[0]['recordSha256']))
                self.assertIsNone(r.find("field[@name='ReadOnly']"))
                if key.endswith('LGCT'):
                    self.assertIn('INS_LOG_BAT_LGIN', hints[0]['chinese'])

    def test_osd_message_time_help_preserves_range_and_source_metadata(self):
        for release in self.bundle:
            family, version = release.get('family'), release.get('version')
            rows = [r for r in release.iter('param') if r.get('name') in OSD_MESSAGE_TIME_HELP_KEYS]
            self.assertEqual(1, len(rows))
            row = rows[0]
            self.assertEqual('1 20', row.findtext("field[@name='Range']"))
            self.assertIsNone(row.findtext("field[@name='Units']"))
            self.assertEqual([], row.findall('values/value'))
            notes = [n for n in self.catalog['translations'] if n['family'] == family
                     and version in n['versions'] and n['parameter'] in OSD_MESSAGE_TIME_HELP_KEYS]
            self.assertEqual(1, len(notes))
            english = row.get('documentation')
            self.assertEqual('Sets message duration seconds', english)
            self.assertEqual(('Description', english, catalog_module.record_fingerprint(row)),
                             (notes[0]['field'], notes[0]['english'], notes[0]['recordSha256']))
            self.assertEqual('设置 OSD 消息在屏幕上的显示时长，单位为秒。', notes[0]['chinese'])

    def test_storage_help_does_not_collapse_identical_text_with_different_range(self):
        names = {'LOG_MAX_FILES','LOG_FILE_MB_FREE'}
        seen = set()
        for release in self.bundle:
            f, v = release.get('family'), release.get('version')
            rows = {r.get('name'): r for r in release.iter('param') if r.get('name') in names}
            self.assertEqual(names, set(rows))
            maximum, free = rows['LOG_MAX_FILES'], rows['LOG_FILE_MB_FREE']
            self.assertEqual(('2 500', 'True'), (maximum.findtext("field[@name='Range']"), maximum.findtext("field[@name='RebootRequired']")))
            self.assertEqual(('10 1000' if v in ('4.5.7','4.6.3') else '2 1000','MB'),
                             (free.findtext("field[@name='Range']"), free.findtext("field[@name='Units']")))
            seen.add(catalog_module.record_fingerprint(free))
            for key, r in rows.items():
                entries = [n for n in self.catalog['translations'] if n['family'] == f and v in n['versions'] and n['parameter'] == key]
                self.assertEqual(1, len(entries))
                self.assertEqual(('Description',r.get('documentation'),catalog_module.record_fingerprint(r)),
                                 (entries[0]['field'],entries[0]['english'],entries[0]['recordSha256']))
        self.assertEqual(2, len(seen))

    def test_logger_descriptions_preserve_bitmask_formats_and_zero_rate_meanings(self):
        bits = [('0', 'File'), ('1', 'MAVLink'), ('2', 'Block')]
        for release in self.bundle:
            family, version = release.get('family'), release.get('version')
            with self.subTest(family=family, version=version):
                notes = [n for n in self.catalog['translations'] if n['family'] == family
                         and version in n['versions'] and n['parameter'] in LOGGER_BACKEND_HELP_KEYS]
                self.assertCountEqual(LOGGER_BACKEND_HELP_KEYS, [n['parameter'] for n in notes])
                self.assertTrue(all(n['field'] == 'Description' for n in notes))
                chinese = {n['parameter']: n['chinese'] for n in notes}

                group = release.find("paramfile/libraries/parameters[@name='LOG']")
                rows = {p.get('name'): p for p in group.findall('param')}
                backend = rows['LOG_BACKEND_TYPE']
                self.assertEqual('0:File,1:MAVLink,2:Block', backend.findtext("field[@name='Bitmask']"))
                self.assertEqual([] if version == '4.5.7' else bits,
                                 [(bit.get('code'), bit.text) for bit in backend.findall('bitmask/bit')])
                self.assertEqual([('0', 'Disabled'), ('1', 'Enabled')],
                                 [(v.get('code'), v.text) for v in rows['LOG_FILE_DSRMROT'].findall('values/value')])
                self.assertIn('LOG_DISARMED', chinese['LOG_FILE_DSRMROT'])
                self.assertEqual('kB', rows['LOG_MAV_BUFSIZE'].findtext("field[@name='Units']"))
                self.assertEqual('s', rows['LOG_FILE_TIMEOUT'].findtext("field[@name='Units']"))
                for key in sorted(k for k in LOGGER_BACKEND_HELP_KEYS if k.endswith('_RATEMAX')):
                    self.assertEqual(('Hz', '0 1000', '0.1'), tuple(
                        rows[key].findtext(f"field[@name='{name}']") for name in ('Units', 'Range', 'Increment')))
                    self.assertIn('沿用' if key == 'LOG_DARM_RATEMAX' else '关闭此项限速', chinese[key])

    def test_file_log_buffer_keeps_versioned_units_and_limits(self):
        expected = {
            '4.5.7': (None, None, None, '本版说明列出的上限为 64 千字节'),
            '4.6.3': ('kB', 'kilobytes', '4 200', '以 kB 计'),
            '4.7.1': ('KiB', 'kibibytes', '4 200', '以 KiB 计'),
        }
        signatures = {}
        for release in self.bundle:
            family, version = release.get('family'), release.get('version')
            with self.subTest(family=family, version=version):
                row = release.find("paramfile/libraries/parameters[@name='LOG']/param[@name='LOG_FILE_BUFSIZE']")
                note, = [n for n in self.catalog['translations'] if n['family'] == family
                         and n['parameter'] == 'LOG_FILE_BUFSIZE' and version in n['versions']]
                fields = tuple(row.findtext(f"field[@name='{key}']") for key in ('Units', 'UnitText', 'Range'))
                self.assertEqual(expected[version][:3], fields)
                self.assertIn(expected[version][3], note['chinese'])
                if version == '4.5.7':
                    self.assertIn('PixHawk 至少需要 4 千字节', note['chinese'])
                self.assertEqual(('Description', row.get('documentation'), catalog_module.record_fingerprint(row)),
                                 (note['field'], note['english'], note['recordSha256']))
                signatures[family, version] = note['englishSha256'], note['recordSha256']
        for family in ('Copter', 'Plane'):
            older, newer = signatures[family, '4.6.3'], signatures[family, '4.7.1']
            self.assertEqual(older[0], newer[0])
            self.assertNotEqual(older[1], newer[1])

    def test_sub_log_descriptions_keep_options_and_replay_conditions(self):
        group = self.bundle.find("release[@family='Sub'][@version='4.7.1']/paramfile/libraries/parameters[@name='LOG']")
        basic = [('0', 'Disabled'), ('1', 'Enabled')]
        expected = {
            'LOG_DISARMED': (basic + [('2', 'Disabled on USB connection'), ('3', 'Discard log on reboot if never armed')],
                             ('检测到 USB 连接时除外', '始终未解锁', '文件系统后端', '下次启动')),
            'LOG_REPLAY': (basic, ('EKF2 和 EKF3', '飞行前数据', 'LOG_DISARMED 必须设为 1 或 2')),
        }
        for key, (options, conditions) in expected.items():
            with self.subTest(key=key):
                row = group.find(f"param[@name='{key}']")
                note, = [n for n in self.catalog['translations'] if n['family'] == 'Sub' and n['parameter'] == key]
                self.assertEqual((['4.7.1'], 'Description', row.get('documentation'), catalog_module.record_fingerprint(row)),
                                 (note['versions'], note['field'], note['english'], note['recordSha256']))
                self.assertEqual(options, [(v.get('code'), v.text) for v in row.findall('values/value')])
                for condition in conditions:
                    self.assertIn(condition, note['chinese'])

    def test_onboard_display_help_preserves_four_choices_and_documented_default(self):
        for release in self.bundle:
            family, version = release.get('family'), release.get('version')
            rows = [r for r in release.iter('param') if r.get('name') in ONBOARD_DISPLAY_HELP_KEYS]
            self.assertEqual(1, len(rows))
            row = rows[0]
            self.assertEqual([('0','Disable'),('1','ssd1306'),('2','sh1106'),('10','SITL')],
                             [(value.get('code'), value.text) for value in row.findall('values/value')])
            notes = [n for n in self.catalog['translations'] if n['family'] == family
                     and version in n['versions'] and n['parameter'] in ONBOARD_DISPLAY_HELP_KEYS]
            self.assertEqual(1, len(notes))
            english = row.get('documentation')
            self.assertIn('Disabled by default.', english)
            self.assertEqual(('Description', english, catalog_module.record_fingerprint(row)),
                             (notes[0]['field'], notes[0]['english'], notes[0]['recordSha256']))
            self.assertEqual('选择板载 I²C 显示屏的类型。默认禁用。', notes[0]['chinese'])

    def test_osd_fonts_keep_each_screen_and_its_backend(self):
        for release in self.bundle:
            family, version = release.get('family'), release.get('version')
            notes = [n for n in self.catalog['translations'] if n['family'] == family
                     and version in n['versions'] and n['parameter'] in OSD_FONT_KEYS]
            self.assertCountEqual(OSD_FONT_KEYS, [n['parameter'] for n in notes])
            self.assertTrue(all(n['field'] == 'Description' for n in notes))
            chinese = {n['parameter']: n['chinese'] for n in notes}
            groups = release.find('paramfile/libraries')
            general = groups.find("parameters[@name='OSD']/param[@name='OSD_FONT']")
            self.assertEqual([('field', 'RebootRequired', 'True')],
                             [(c.tag, c.get('name'), c.text) for c in general])
            self.assertIn('可用字体', chinese['OSD_FONT'])
            self.assertIn('重启', chinese['OSD_FONT'])
            self.assertNotIn('21', chinese['OSD_FONT'])
            for screen, key in enumerate(OSD_SCREEN_FONT_KEYS, 1):
                row = groups.find(f"parameters[@name='OSD{screen}_']/param[@name='{key}']")
                self.assertEqual([('field', 'Range', '0 21')], [(c.tag, c.get('name'), c.text) for c in row])
                self.assertIn(f'第 {screen} 屏', chinese[key])
                self.assertIn('仅适用于 MSP DisplayPort', chinese[key])

    def test_source_changes_cannot_automatically_reapprove_notes(self):
        for change in ('English', 'Range', 'Unit'):
            source = copy.deepcopy(self.bundle)
            param = next(source.iter('param'))
            if change == 'English':
                param.set('documentation', param.get('documentation') + ' changed')
            else:
                field = next(f for f in param.findall('field') if f.get('name') == ('Range' if change == 'Range' else 'Units'))
                field.text += ' changed'
            with self.assertRaises(ValueError):
                catalog_module.resource(self.catalog, source)

    def test_duplicate_scope_requires_review(self):
        self.catalog['translations'].append(self.catalog['translations'][0])
        with self.assertRaises(ValueError):
            catalog_module.resource(self.catalog, self.bundle)

    def test_stock_ui_dispatch_and_overlay_are_strictly_scoped(self):
        view = (ROOT / 'GCSViews/ConfigurationView/ConfigRawParams.cs').read_text(encoding='utf-8')
        overlay = (ROOT / 'GCSViews/ConfigurationView/ConfigRawParams.ParameterDescriptions.cs').read_text(encoding='utf-8')
        self.assertIn('string.Equals(CultureInfo.CurrentUICulture.Name, "zh-CN", StringComparison.Ordinal)', overlay)
        prepare = overlay.split('private void PrepareParameterDescriptions()', 1)[1].split('private void ResetParameterDescriptionRows()', 1)[0]
        self.assertLess(prepare.index('if (!UseEnhancedParameterDescriptions)'), prepare.index('var vehicle'))
        self.assertLess(prepare.index('return;'), prepare.index('DescriptionStore.Value.Bundled'))
        self.assertLess(prepare.index('return;'), prepare.index('DescriptionStore.Value.LoadAsync'))
        exact = overlay.split('private ParameterDescriptionText ExactDescription(', 1)[1].split('private ParameterDescriptionText ReadParameterDescription(', 1)[0]
        self.assertLess(exact.index('if (!UseEnhancedParameterDescriptions'), exact.index('DescriptionCatalog.Value.Present'))
        choice = overlay.split('private List<KeyValuePair<int, string>> BilingualParameterOptions(', 1)[1]
        self.assertLess(choice.index('if (!UseEnhancedParameterDescriptions) return original'), choice.index('DescriptionCatalog.Value'))
        self.assertIn('new KeyValuePair<int, string>(option.Key,', choice)
        self.assertIn('Task.Run(() => LoadMetadataAndUpdateCells(rowsToLoad, firmware));', view)
        stock = view.split('private void LoadMetadataAndUpdateCells(', 1)[1].split('private void LoadEnhancedMetadataAndUpdateCells(', 1)[0]
        self.assertNotIn('DescriptionCatalog', stock)
        self.assertIn('ParameterMetaDataRepository.GetParameterMetaData', stock)
        self.assertIn('capturedRow.Cells[Units.Index].Value = capturedUnits;', stock)
        self.assertIn('capturedRow.Cells[Desc.Index].Value = capturedDesc;', stock)
        self.assertIn('cmb.DataSource = UseEnhancedParameterDescriptions ? BilingualParameterOptions(param_name, options) : options;', view)

    def test_generated_eol_check_accepts_only_crlf_or_lf(self):
        expected = catalog_module.resource(self.catalog, self.bundle)
        self.assertTrue(catalog_module.generated_matches(expected.replace(b'\n', b'\r\n'), expected))
        for changed in (expected.replace(b'\n', b'\r'), expected.replace(b'parameterTranslations', b'parameterTranslationx'),
                        expected.replace(b'  <translation', b' <translation')):
            self.assertFalse(catalog_module.generated_matches(changed, expected))


if __name__ == '__main__':
    unittest.main()
