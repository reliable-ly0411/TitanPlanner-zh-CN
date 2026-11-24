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


def publish(svg):
    current = git("ls-remote", "--heads", "origin", STATUS_REF).split()
    parent = current[0].decode() if current else None
    if parent:
        git("fetch", "--no-tags", "--depth=1", "origin", STATUS_REF)
        if git("rev-parse", "FETCH_HEAD").decode().strip() != parent:
            raise RuntimeError("status branch advanced during fetch; keeping the remote version")
        if git("ls-tree", "--name-only", parent).decode().splitlines() != [FILENAME]:
            raise RuntimeError("status branch contains unexpected files")
        old = subprocess.run(["git", "show", f"{parent}:{FILENAME}"], capture_output=True)
        if old.returncode:
            raise RuntimeError("status branch does not contain the expected file")
        if old.stdout == svg:
            return
    blob = git("hash-object", "-w", "--stdin", data=svg).decode().strip()
    tree = git("mktree", data=f"100644 blob {blob}\t{FILENAME}\n".encode()).decode().strip()
    commit = git("-c", "user.name=github-actions[bot]", "-c",
                 "user.email=41898282+github-actions[bot]@users.noreply.github.com",
                 "commit-tree", tree, *(["-p", parent] if parent else []),
                 "-m", "Refresh README upstream comparison").decode().strip()
    git("push", "origin", f"{commit}:{STATUS_REF}")


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("repository")
    parser.add_argument("--output", type=Path, help="write a local preview instead of pushing")
    args = parser.parse_args()
    svg = render(collect(args.repository))
    if args.output:
        args.output.write_bytes(svg)
    else:
        publish(svg)
