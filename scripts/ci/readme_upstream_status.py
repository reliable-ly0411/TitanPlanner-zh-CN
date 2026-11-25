import json
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET

BRANCHES = ("zh-CN", "zh-CN-dev", "Titanmain", "missionmain")
STATUS_REF = "refs/heads/readme-upstream-status"
FILENAME = "dual-upstream.svg"


def git(*args, data=None):
    return subprocess.check_output(["git", *args], input=data)


def read_heads():
    refs = ["refs/heads/" + branch for branch in BRANCHES]
    found = {ref: sha for sha, ref in
             (line.split() for line in git("ls-remote", "--heads", "origin", *refs).decode().splitlines())}
    return {branch: found["refs/heads/" + branch] for branch in BRANCHES}


def compare(repo, base, head):
    result = json.loads(subprocess.check_output(
        ["gh", "api", f"repos/{repo}/compare/{base}...{head}?per_page=1"]))
    counts = result["ahead_by"], result["behind_by"]
    if any(type(count) is not int or count < 0 for count in counts):
        raise ValueError("invalid GitHub comparison counts")
    return counts


def collect(repo, heads=read_heads, lookup=compare):
    initial = heads()
    counts = [[lookup(repo, initial[base], initial[line])
               for base in BRANCHES[2:]] for line in BRANCHES[:2]]
    if heads() != initial:
        raise RuntimeError("compared branches changed during refresh; retaining the previous snapshot")
    return counts


def render(counts):
    title = "领先 / 落后提交数"
    description = "；".join(f"{line} 相对 {base}：领先 {ahead}，落后 {behind}"
                           for line, row in zip(BRANCHES, counts)
                           for base, (ahead, behind) in zip(BRANCHES[2:], row))
    svg = ET.Element("svg", xmlns="http://www.w3.org/2000/svg", width="540", height="126",
                     viewBox="0 0 540 126", role="img")
    svg.set("aria-labelledby", "title description")
    ET.SubElement(svg, "title", id="title").text = title
    ET.SubElement(svg, "desc", id="description").text = description
    ET.SubElement(svg, "rect", width="540", height="126", rx="8", fill="#f6f8fa")
    group = ET.SubElement(svg, "g", fill="#1f2328")
    group.set("font-family", "system-ui,sans-serif")
    group.set("font-size", "16")
    rows = [("ahead / behind", "Titanmain", "missionmain")]
    rows.extend((line, *(f"{ahead} / {behind}" for ahead, behind in row))
                for line, row in zip(BRANCHES[:2], counts))
    for y, row in zip((29, 66, 103), rows):
        for x, value in zip((16, 200, 370), row):
            ET.SubElement(group, "text", x=str(x), y=str(y)).text = value
    return ET.tostring(svg, encoding="utf-8", xml_declaration=True) + b"\n"


DIRECTIONS = (("ahead", "领先", "#0969da"), ("behind", "落后", "#a34000"))
BADGE_NAMES = tuple(f"{line}-{base}-{direction}.svg"
                    for line in BRANCHES[:2] for base in BRANCHES[2:]
                    for direction, _, _ in DIRECTIONS)


def render_badge(line, base, direction, label, color, count):
    label_width = 14 + 7 * len(direction)
    width = label_width + 14 + 8 * len(str(count))
    svg = ET.Element("svg", xmlns="http://www.w3.org/2000/svg",
                     width=str(width), height="20", viewBox=f"0 0 {width} 20",
                     role="img")
    svg.set("aria-labelledby", "title")
    ET.SubElement(svg, "title", id="title").text = f"{line} 相对 {base}：{label} {count}"
    defs = ET.SubElement(svg, "defs")
    clip = ET.SubElement(defs, "clipPath", id="badge")
    ET.SubElement(clip, "rect", width=str(width), height="20", rx="4")
    bg = ET.SubElement(svg, "g", {"clip-path": "url(#badge)"})
    ET.SubElement(bg, "rect", width=str(width), height="20", fill=color)
    ET.SubElement(bg, "rect", width=str(label_width), height="20", fill="#424a53")
    g = ET.SubElement(svg, "g", {"fill": "#fff", "font-family": "Verdana, sans-serif",
                                  "font-size": "11", "text-anchor": "middle"})
    ET.SubElement(g, "text", x=str(label_width / 2), y="14").text = direction
    ET.SubElement(g, "text", x=str((label_width + width) / 2), y="14").text = str(count)
    return ET.tostring(svg, encoding="utf-8", xml_declaration=True) + b"\n"


def render_snapshot(counts):
    assets = {FILENAME: render(counts)}
    names = iter(BADGE_NAMES)
    for line, row in zip(BRANCHES[:2], counts):
        for base, pair in zip(BRANCHES[2:], row):
            for (direction, label, color), count in zip(DIRECTIONS, pair):
                assets[next(names)] = render_badge(line, base, direction, label, color, count)
    return assets


def publish(assets):
    expected = {FILENAME, *BADGE_NAMES}
    if assets.keys() != expected:
        raise ValueError("incomplete README status snapshot")

    current = git("ls-remote", "--heads", "origin", STATUS_REF).split()
    parent = current[0].decode() if current else None
    if parent:
        git("fetch", "--no-tags", "--depth=1", "origin", STATUS_REF)
        if git("rev-parse", "FETCH_HEAD").decode().strip() != parent:
            raise RuntimeError("status branch advanced during fetch; keeping the remote version")
        old_files = set(git("ls-tree", "--name-only", parent).decode().splitlines())
        if old_files not in ({FILENAME}, expected):
            raise RuntimeError("status branch contains unexpected or missing files")
        if old_files == expected and all(git("show", f"{parent}:{name}") == data
                                         for name, data in assets.items()):
            return
    entries = []
    for name, data in sorted(assets.items()):
        blob = git("hash-object", "-w", "--stdin", data=data).decode().strip()
        entries.append(f"100644 blob {blob}\t{name}\n")
    tree = git("mktree", data="".join(entries).encode()).decode().strip()
    commit = git("-c", "user.name=github-actions[bot]", "-c",
                 "user.email=41898282+github-actions[bot]@users.noreply.github.com",
                 "commit-tree", tree, *(["-p", parent] if parent else []),
                 "-m", "Refresh README upstream comparison").decode().strip()
    git("push", "origin", f"{commit}:{STATUS_REF}")


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("repository")
    preview = parser.add_mutually_exclusive_group()
    preview.add_argument("--output", type=Path, help="write the legacy local preview")
    preview.add_argument("--output-dir", type=Path, help="write all status SVGs as local previews")
    args = parser.parse_args()
    assets = render_snapshot(collect(args.repository))
    if args.output:
        args.output.write_bytes(assets[FILENAME])
    elif args.output_dir:
        args.output_dir.mkdir(parents=True, exist_ok=True)
        for name, data in assets.items():
            (args.output_dir / name).write_bytes(data)
    else:
        publish(assets)
