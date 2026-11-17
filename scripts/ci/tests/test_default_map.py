"""Source-level guardrails; Windows UI/network verification is separate."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[3]

class DefaultMapTests(unittest.TestCase):
    def setUp(self):
        self.source = (ROOT / 'GCSViews/FlightPlanner.cs').read_text(encoding='utf-8-sig')
        self.method = self.source.split('private void updateMapType(', 1)[1].split('private void updateRowNumbers', 1)[0]

    def test_saved_selection_has_priority(self):
        saved, defaults = self.method.split('            else\n', 1)
        self.assertIn('if (!string.IsNullOrEmpty(mapType))', saved)
        self.assertIn('x.Name == mapType', saved)
        self.assertNotIn('BingSatelliteMap', saved)

    def test_only_simplified_chinese_default_changes(self):
        self.assertIn('L10N.ConfigLang.IsChildOf(CultureInfo.GetCultureInfo("zh-Hans"))', self.method)
        self.assertIn('mapType = "GoogleSatelliteMap";', self.method)

    def test_bing_selected_by_identity_not_translated_name(self):
        self.assertIn('x => x == GMapProviders.BingSatelliteMap', self.method)
        self.assertNotIn('谷歌中国卫星地图', self.method)
        self.assertNotIn('火星坐标', self.method)

    def test_selection_still_syncs_maps_and_persists(self):
        handler = self.source.split('private void comboBoxMapType_SelectedValueChanged(', 1)[1].split('\n        private ', 1)[0]
        self.assertIn('MainMap.MapProvider = (GMapProvider) comboBoxMapType.SelectedItem;', handler)
        self.assertIn('FlightData.mymap.MapProvider = (GMapProvider) comboBoxMapType.SelectedItem;', handler)
        self.assertIn('Settings.Instance["MapType"] = comboBoxMapType.Text;', handler)

    def test_provider_projections_unchanged(self):
        providers = ROOT / 'ExtLibs/GMap.NET.Core/GMap.NET.MapProviders'
        self.assertIn('return MercatorProjection.Instance;', (providers / 'Bing/BingMapProvider.cs').read_text())
        self.assertIn('return MercatorProjectionGCJ.Instance;', (providers / 'Google/China/GoogleChinaSatelliteMapProvider.cs').read_text())

if __name__ == '__main__':
    unittest.main()
