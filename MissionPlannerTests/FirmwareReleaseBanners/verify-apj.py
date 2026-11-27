"""Read only: verify already-local official firmware files without executing them."""
import argparse
import base64
import hashlib
import json
from pathlib import Path
import zlib

RELEASES = {
    "4.4.4": ("16b783822905668b160b0eb1ef5599ce0d3a0231", "01dc7507dd96424438ee803f75a3e61a7c469f68d8efb2892a40989132cf011c"),
    "4.5.7": ("0358a9c210bc6c965006f5d6029239b7033616df", "3c146fb298bda312f8f7c6842e8e6cac2b38717db42c1c237c89a76c7b1fc65a"),
    "4.6.3": ("3fc7011a7d3dc047cbb17d8bd98ee94577d144c6", "73155fef81a1f741b36b8b3a5df7a542f8dde9bd2fb17381d9b01cd9a71aa7ee"),
    "4.7.3": ("d9c5097c3d8333435712ffed86bca8ce00c4bb46", "46cf5b83e9dc92a42a46fe030781c67f171edd1b84c4c68cfcb30ef5ddaf296b"),
}
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("root", type=Path, help="directory with local fw-4.x.y.apj files")
args = parser.parse_args()
for version, (commit, expected_digest) in RELEASES.items():
    path = args.root / f"fw-{version}.apj"
    if not path.is_file() or path.stat().st_size > 4_000_000:
        raise SystemExit(f"Refusing unknown firmware file: {path.name}")
    data = path.read_bytes()
    if hashlib.sha256(data).hexdigest() != expected_digest:
        raise SystemExit(f"Pinned digest mismatch: {path.name}")
    outer = json.loads(data)
    short = commit[:8]
    if outer["git_identity"] != short:
        raise SystemExit(f"Embedded revision mismatch: {path.name}")
    image = zlib.decompress(base64.b64decode(outer["image"], validate=True))
    literal = f"ArduPlane V{version} ({short})"
    if image.count(literal.encode("ascii") + b"\0") != 1:
        raise SystemExit(f"Firmware literal mismatch: {path.name}")
    print(f"PASS {path.name}: {literal}")
