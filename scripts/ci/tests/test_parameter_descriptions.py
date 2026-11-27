import copy
import importlib.util
import json
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
spec = importlib.util.spec_from_file_location('parameter_catalog', ROOT / 'scripts/localization/build_parameter_catalog.py')
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
        self.assertEqual(533, len(descriptions))
        self.assertEqual(248, len({(f, p) for f, v, p in descriptions}))
        self.assertFalse(any(v == '4.7.2' for f, v, p in descriptions))
        excluded_copter = {'RTL_CONE_SLOPE', 'LAND_SPEED', 'LAND_SPD_MS', 'LOG_FILE_BUFSIZE', 'FS_GCS_ENABLE'}
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
            records = {r.get('name').split(':')[-1]: r for r in release.iter('param') if not r.get('name').startswith('OSD_SB_')}
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
        records = {p.get('name'): p for p in scope.iter('param') if not p.get('name').startswith(('STAT_', 'OSD_SB_'))}
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
