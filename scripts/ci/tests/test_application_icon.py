"""Keep the shared application icon readable by Windows and Mono's Icon parser."""
from pathlib import Path
import struct
import unittest
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parents[3]


class ApplicationIconTests(unittest.TestCase):
    def test_native_icon_cleanup_is_windows_only(self):
        source = (ROOT / 'Utilities/ThemeManager.cs').read_text(encoding='utf-8-sig')
        self.assertRegex(source, r'if\s*\(Environment\.OSVersion\.Platform\s*==\s*'
                         r'PlatformID\.Win32NT\)\s*DestroyIcon\(handle\);')
        self.assertEqual(1, source.count('DestroyIcon(handle);'))

    def test_splash_reuses_the_mono_compatible_application_icon(self):
        root = ET.parse(ROOT / 'Splash.resx').getroot()
        icon = next(item for item in root.findall('data')
                    if item.attrib.get('name') == '$this.Icon')
        self.assertTrue(icon.attrib.get('type', '').startswith('System.Resources.ResXFileRef,'))
        self.assertEqual('mpdesktop.ico', icon.findtext('value').split(';')[0])
        self.assertNotIn('mimetype', icon.attrib)

    def test_all_icon_sizes_use_complete_uncompressed_dib_frames(self):
        data = (ROOT / 'mpdesktop.ico').read_bytes()
        reserved, kind, count = struct.unpack_from('<HHH', data)
        self.assertEqual((0, 1, 7), (reserved, kind, count))
        sizes = set()
        next_offset = 6 + count * 16
        for index in range(count):
            width, height, _, _, _, bits, length, offset = struct.unpack_from(
                '<BBBBHHII', data, 6 + index * 16)
            width, height = width or 256, height or 256
            sizes.add((width, height))
            with self.subTest(size=(width, height)):
                self.assertEqual(next_offset, offset)
                self.assertLessEqual(offset + length, len(data))
                # Mono interprets small PNG-compressed frames as a DIB header.
                # It also requires the AND mask even for 32-bit alpha icons.
                self.assertEqual((40, width, height * 2, 1, 32, 0),
                                 struct.unpack_from('<IiiHHI', data, offset))
                self.assertEqual(32, bits)
                xor_bytes = width * height * 4
                and_stride = ((width + 31) // 32) * 4
                self.assertEqual(40 + xor_bytes + and_stride * height, length)
            next_offset = offset + length
        self.assertEqual(len(data), next_offset)
        self.assertEqual({(n, n) for n in (16, 24, 32, 48, 64, 128, 256)}, sizes)


if __name__ == '__main__':
    unittest.main()
