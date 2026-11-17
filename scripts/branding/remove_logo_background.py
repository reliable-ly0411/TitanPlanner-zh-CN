#!/usr/bin/env python3
"""Remove the white matte from the supplied JPEG; never redraw or resize its artwork.

Requires Pillow. The original JPEG stays in Resources as the provenance reference.
Opaque interior pixels are copied exactly. Only the matte/antialias boundary is
unmixed against white, using the nearest interior pixel as the foreground estimate.
"""
from collections import deque
import hashlib
from pathlib import Path

from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Resources/LocalizedBrandLogo.jpg"
OUTPUT = ROOT / "Resources/LocalizedBrandLogo.png"
SOURCE_SHA256 = "db432ec916099133ef4cda90466df219cdc6230291c7d45f73073c74db521209"


def remove_white_matte(source):
    source = source.convert("RGB")
    width, height = source.size
    pixels = list(source.getdata())
    core = Image.new("L", source.size)
    core.putdata([255 if min(pixel) < 110 else 0 for pixel in pixels])
    core_pixels = list(core.filter(ImageFilter.MinFilter(3)).getdata())
    nearest = [-1] * len(pixels)
    queue = deque()
    for index, value in enumerate(core_pixels):
        if value:
            nearest[index] = index
            queue.append(index)
    if not queue:
        raise ValueError("The supplied logo has no opaque interior")
    # Deterministic four-neighbour propagation, with no shape generation or blur.
    while queue:
        index = queue.popleft()
        x, y = index % width, index // width
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            neighbour = ny * width + nx
            if 0 <= nx < width and 0 <= ny < height and nearest[neighbour] < 0:
                nearest[neighbour] = nearest[index]
                queue.append(neighbour)
    result = []
    for index, pixel in enumerate(pixels):
        # Discard white and neutral near-white JPEG ringing, including letter counters.
        if min(pixel) >= 250 or (min(pixel) >= 235 and max(pixel) - min(pixel) <= 12):
            result.append((0, 0, 0, 0))
        elif core_pixels[index]:
            result.append((*pixel, 255))
        else:
            foreground = pixels[nearest[index]]
            distance = [255 - channel for channel in foreground]
            alpha = max(1, min(255, round(255 * sum(
                (255 - pixel[channel]) * distance[channel] for channel in range(3)
            ) / sum(channel * channel for channel in distance))))
            # Undo C = alpha * F + (1 - alpha) * white; prevent a white edge on dark UI.
            color = tuple(max(0, min(255, round(
                (channel * 255 - 255 * (255 - alpha)) / alpha
            ))) for channel in pixel)
            result.append((*color, alpha))
    output = Image.new("RGBA", source.size)
    output.putdata(result)
    return output


def main():
    if hashlib.sha256(SOURCE.read_bytes()).hexdigest() != SOURCE_SHA256:
        raise ValueError("Source JPEG differs from the supplied original; inspect it before processing")
    with Image.open(SOURCE) as source:
        output = remove_white_matte(source)
    output.save(OUTPUT)
    print(f"{OUTPUT.relative_to(ROOT)}: {output.size[0]}x{output.size[1]} RGBA")
    print(f"SHA256 {hashlib.sha256(OUTPUT.read_bytes()).hexdigest()}")


if __name__ == "__main__":
    main()
