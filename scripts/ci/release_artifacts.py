"""Hash and require the complete ZIP/MSI pair before publishing."""
import hashlib
import json
from pathlib import Path
import re
import xml.etree.ElementTree as ET
import zipfile


def validate_version(version):
    if not isinstance(version, str) or not re.fullmatch(r'(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)(?:\.(?:0|[1-9][0-9]*))?', version):
        raise ValueError('Invalid application version')
    return version


def application_version(root=None):
    root = Path(root) if root is not None else Path(__file__).resolve().parents[2]
    values = ET.parse(root/'MissionPlanner.csproj').getroot().findall('./PropertyGroup/Version')
    if len(values) != 1:
        raise ValueError('Expected one application version in MissionPlanner.csproj')
    version = validate_version(values[0].text)
    assembly = (root/'Properties'/'AssemblyInfo.cs').read_text(encoding='utf-8-sig')
    match = re.search(r'AssemblyFileVersion\("([0-9.]+)"\)', assembly)
    def parts(value):
        return tuple(int(n) for n in value.split('.')) + (0,) * (4-len(value.split('.')))
    if not match or parts(validate_version(match[1])) != parts(version):
        raise ValueError('Project version differs from AssemblyFileVersion; refusing misleading release names')
    return version


def artifact_names(sha, version):
    if not re.fullmatch(r'[0-9a-f]{40}', sha):
        raise ValueError('Invalid commit SHA')
    validate_version(version)
    return [f'TitanPlanner-zh-CN.{ext}' for ext in ('zip', 'msi')]


def legacy_artifact_names(sha, version):
    artifact_names(sha, version)  # Keep historical validation as strict as new output.
    return [f'TitanPlanner-zh-CN-v{version}-{sha[:12]}-windows.{ext}' for ext in ('zip', 'msi')]


def release_tag(sha, version):
    artifact_names(sha, version)  # Validate both fields before using them in a tag.
    return f'v{version}-zh2-{sha[:12]}'


def write_manifest(root, sha, version, **extra):
    root = Path(root)
    entries = [{'name': name, 'sha256': hashlib.sha256((root/name).read_bytes()).hexdigest(),
                'size': (root/name).stat().st_size} for name in artifact_names(sha, version)]
    (root/'build-info.json').write_text(json.dumps({'commit': sha, 'version': version, 'artifacts': entries, **extra}, indent=2)+'\n', encoding='utf-8')
    (root/'SHA256SUMS').write_text(''.join(f"{e['sha256']}  {e['name']}\n" for e in entries), encoding='ascii')


def verify_manifest(root, sha):
    root = Path(root)
    data = json.loads((root/'build-info.json').read_text(encoding='utf-8'))
    version = validate_version(data.get('version'))
    entries = data.get('artifacts', [])
    names = [e['name'] for e in entries]
    if data['commit'] != sha or names not in (artifact_names(sha, version), legacy_artifact_names(sha, version)):
        raise ValueError('Release must contain exactly the validated ZIP and MSI')
    for e in entries:
        path = root/e['name']
        if path.stat().st_size != e['size'] or hashlib.sha256(path.read_bytes()).hexdigest() != e['sha256']:
            raise ValueError('Artifact hash/size mismatch: ' + e['name'])
    if (root/'SHA256SUMS').read_text(encoding='ascii') != ''.join(f"{e['sha256']}  {e['name']}\n" for e in entries):
        raise ValueError('Checksum manifest mismatch')
    # Stable filenames no longer bind the version; check the immutable ZIP metadata.
    # Old versioned manifests remain readable for existing release artifacts.
    if names == artifact_names(sha, version):
        with zipfile.ZipFile(root/names[0]) as archive:
            info = json.loads(archive.read('BUILD-INFO.json'))
        if info.get('commit') != sha or info.get('version') != version:
            raise ValueError('ZIP commit/version mismatch')
    return [root/e['name'] for e in entries]
