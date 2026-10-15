import json
import zipfile
from pathlib import Path
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from package_msi import author, msi_version, NS
from release_artifacts import artifact_names, write_manifest, verify_manifest


class MsiTests(unittest.TestCase):
    def test_payload_and_chinese_languages_have_stable_individual_components(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            names = ['MissionPlanner.exe', 'zh-Hans/MissionPlanner.resources.dll', 'zh-CN/MissionPlanner.resources.dll', 'x64/libSkiaSharp.dll']
            for name in names:
                p = root/name; p.parent.mkdir(parents=True, exist_ok=True); p.write_bytes(b'payload')
            tree = author(root, 'a'*40, '1.0.2')
            ns = {'w': NS}
            files = tree.findall('.//w:File', ns)
            self.assertEqual(set(names), {str(Path(f.attrib['Source']).relative_to(root)).replace('\\', '/') for f in files})
            self.assertEqual(len(files), len({f.attrib['Id'] for f in files}))
            self.assertEqual('perUser', tree.find('.//w:Package', ns).attrib['InstallScope'])
            self.assertFalse(tree.findall('.//w:RemoveFile', ns))
            again = author(root, 'b'*40, '1.0.3')
            self.assertEqual([c.attrib['Guid'] for c in tree.findall('.//w:Component', ns)], [c.attrib['Guid'] for c in again.findall('.//w:Component', ns)])
            self.assertNotEqual(tree.find('.//w:Product', ns).attrib['Id'], again.find('.//w:Product', ns).attrib['Id'])

    def test_version_monotonic_and_bounded(self):
        self.assertEqual('1.0.1', msi_version(1))
        self.assertEqual('1.1.0', msi_version(65536))
        for invalid in (0, -1, 256*65536):
            with self.assertRaises(ValueError): msi_version(invalid)

    def test_both_assets_required_and_hashes_checked(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp); sha = 'a'*40
            names = artifact_names(sha, '1.3.83')
            with zipfile.ZipFile(root/names[0], 'w') as archive:
                archive.writestr('BUILD-INFO.json', json.dumps({'commit': sha, 'version': '1.3.83'}))
            with self.assertRaises(FileNotFoundError): write_manifest(root, sha, '1.3.83')
            (root/names[1]).write_bytes(b'msi')
            write_manifest(root, sha, '1.3.83')
            self.assertEqual([root/name for name in names], verify_manifest(root, sha))
            (root/names[1]).write_bytes(b'bad')
            with self.assertRaises(ValueError): verify_manifest(root, sha)
            with self.assertRaises(ValueError): verify_manifest(root, 'b'*40)

    def test_partial_release_and_path_injection_are_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp); sha = 'a'*40
            for name in artifact_names(sha, '1.3.83'): (root/name).write_bytes(b'payload')
            write_manifest(root, sha, '1.3.83')
            manifest = json.loads((root/'build-info.json').read_text())
            manifest['artifacts'][1]['name'] = '../outside.msi'
            (root/'build-info.json').write_text(json.dumps(manifest))
            with self.assertRaises(ValueError): verify_manifest(root, sha)
            with self.assertRaises(ValueError): artifact_names('../bad', '1.3.83')
