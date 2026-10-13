from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[3]
image = Image.open(ROOT / "Assets/Sprites/pause-button.png").convert("RGBA")
pixels = image.load()

# Keep the pause button's 16-pixel circle and replace only its black glyph.
for y in range(16):
    for x in range(16):
        if pixels[x, y] == (0, 0, 0, 255):
            pixels[x, y] = (179, 207, 42, 255) if x >= 9 else (195, 230, 25, 255)

roof = {(7, 4), (8, 4), (6, 5), (9, 5), (5, 6), (10, 6), (4, 7), (11, 7)}
walls = {(5, y) for y in range(8, 12)} | {(10, y) for y in range(8, 12)}
base = {(x, 12) for x in range(4, 12)}
door = {(x, y) for x in (7, 8) for y in range(9, 12)}
for x, y in roof | walls | base | door:
    pixels[x, y] = (0, 0, 0, 255)

image.save(ROOT / "Assets/Sprites/home-button.png")
