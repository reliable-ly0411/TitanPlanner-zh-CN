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
        for release in self.bundle:
            expected = 2 if release.get('family') == 'Tracker' else 37 if release.get('family') == 'Copter' else 53 if release.get('version') == '4.7.1' else 51
            self.assertEqual(expected, len(list(release.iter('param'))))
            self.assertEqual(release.get('family') + '-' + release.get('version'), release.find('paramfile/firmware').get('git_tag'))

    def test_scope_counts_and_held_versions(self):
        notes = self.catalog['translations']
        descriptions = {(n['family'], v, n['parameter']) for n in notes if n['field'] == 'Description' for v in n['versions']}
        self.assertEqual(268, len(descriptions))
        self.assertEqual(121, len({(f, p) for f, v, p in descriptions}))
        self.assertFalse(any(v == '4.7.2' for f, v, p in descriptions))
        excluded_copter = {'RTL_CONE_SLOPE', 'LAND_SPEED', 'LAND_SPD_MS', 'LOG_FILE_BUFSIZE', 'FS_GCS_ENABLE'}
        excluded_plane = {'TECS_CLMB_MAX', 'TECS_SINK_MIN', 'ROLL_LIMIT_DEG'}
        self.assertFalse(any(p in (excluded_copter if f == 'Copter' else excluded_plane) for f, v, p in descriptions))

    def test_tracker_slice_is_pinned_and_contains_only_two_read_only_statistics(self):
        release = self.bundle.find("release[@family='Tracker']")
        self.assertEqual('4.7.1', release.get('version'))
        self.assertEqual('143350e5806885fd34cd9c710cfbe6f9b758bee0260c5e081bfef6eab05e91fc', release.get('sourceSha256'))
        self.assertEqual('dbe792162d06cab66c3475fd5556bf7a120f119e', release.get('gitSha'))
        params = {p.get('name'): p for p in release.iter('param')}
        self.assertEqual({'STAT_BOOTCNT', 'STAT_RUNTIME'}, set(params))
        for name, units in [('STAT_BOOTCNT', None), ('STAT_RUNTIME', 's')]:
            self.assertEqual('True', params[name].findtext("field[@name='ReadOnly']"))
            self.assertEqual(units, params[name].findtext("field[@name='Units']"))
            self.assertIsNone(params[name].find('values'))
        notes = [t for t in self.catalog['translations'] if t['family'] == 'Tracker']
        self.assertEqual(2, len(notes))
        self.assertEqual(set(params), {t['parameter'] for t in notes})
        self.assertTrue(all(t['versions'] == ['4.7.1'] and t['field'] == 'Description' for t in notes))
        altered = copy.deepcopy(self.bundle)
        altered.find("release[@family='Tracker']//param/field[@name='ReadOnly']").text = 'False'
        with self.assertRaises(ValueError):
            catalog_module.resource(self.catalog, altered)

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
