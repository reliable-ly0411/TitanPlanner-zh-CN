import hashlib
import json
import zipfile
from pathlib import Path
import sys
import tempfile
import unittest
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from release_artifacts import application_version, artifact_names, legacy_artifact_names, release_tag, validate_version, write_manifest, verify_manifest


class ReleaseVersionTests(unittest.TestCase):
    def test_application_version_ignores_dependency_versions(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root/'Properties').mkdir()
            (root/'MissionPlanner.csproj').write_text('<Project><PropertyGroup><Version>1.3.83</Version></PropertyGroup><ItemGroup><PackageReference><Version>9.9.9</Version></PackageReference></ItemGroup></Project>')
            source = root/'Properties'/'AssemblyInfo.cs'
            source.write_text('[assembly: AssemblyFileVersion("1.3.83.0")]')
            self.assertEqual('1.3.83', application_version(root))
            source.write_text('[assembly: AssemblyFileVersion("1.3.84")]')
            with self.assertRaises(ValueError): application_version(root)

    def test_names_and_tags_use_validated_version_and_commit(self):
        sha = 'a'*40
        self.assertEqual(['TitanPlanner-zh-CN.zip', 'TitanPlanner-zh-CN.msi'], artifact_names(sha, '1.3.83'))
        self.assertEqual('v1.3.83-zh2-aaaaaaaaaaaa', release_tag(sha, '1.3.83'))
        for value in (None, '', '../1.3.83', '1.3', '1.3.*', '01.3.83', '1.3.83;cmd', '1.3.83\n'):
            with self.assertRaises(ValueError): validate_version(value)

    def test_manifest_version_change_is_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp); sha = 'a'*40
            names = artifact_names(sha, '1.3.83')
            with zipfile.ZipFile(root/names[0], 'w') as archive:
                archive.writestr('BUILD-INFO.json', json.dumps({'commit': sha, 'version': '1.3.83'}))
            (root/names[1]).write_bytes(b'test')
            write_manifest(root, sha, '1.3.83')
            manifest = json.loads((root/'build-info.json').read_text())
            self.assertEqual('1.3.83', manifest['version'])
            manifest['version'] = '1.3.84'
            (root/'build-info.json').write_text(json.dumps(manifest))
            with self.assertRaises(ValueError): verify_manifest(root, sha)


    def test_fixed_names_still_validate_sha_and_version(self):
        self.assertEqual(artifact_names('a'*40, '1.3.83'), artifact_names('b'*40, '2.0.0'))
        for sha in ('../bad', 'a'*39, 'A'*40, 'a'*40+'\n'):
            with self.assertRaises(ValueError): artifact_names(sha, '1.3.83')
        with self.assertRaises(ValueError): artifact_names('a'*40, 'invalid')

    def test_legacy_pair_remains_readable_but_mixed_names_are_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp); sha = 'a'*40; version = '1.3.83'
            names = legacy_artifact_names(sha, version)
            self.assertEqual('TitanPlanner-zh-CN-v1.3.83-aaaaaaaaaaaa-windows.zip', names[0])
            entries = []
            for name in names:
                (root/name).write_bytes(b'legacy')
                entries.append({'name': name, 'size': 6, 'sha256': hashlib.sha256(b'legacy').hexdigest()})
            data = {'commit': sha, 'version': version, 'artifacts': entries}
            (root/'build-info.json').write_text(json.dumps(data))
            (root/'SHA256SUMS').write_text(''.join(f"{e['sha256']}  {e['name']}\n" for e in entries))
            self.assertEqual([root/name for name in names], verify_manifest(root, sha))
            entries[1]['name'] = artifact_names(sha, version)[1]
            (root/'build-info.json').write_text(json.dumps(data))
            with self.assertRaises(ValueError): verify_manifest(root, sha)
