from PIL import Image, ImageDraw, ImageFont, ImageFilter

W, H = 1280, 720
S = 2  # supersampling
FD = "/usr/share/fonts/opentype/noto/"


def font(weight, size):
    return ImageFont.truetype(FD + "NotoSansCJK-%s.ttc" % weight, int(size * S), index=0)


def rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


ACCENT = rgb("#2ec4b6")
ACCENT2 = rgb("#ffd166")
WHITE = (245, 248, 252)
DIM = rgb("#8fa3b8")

# ---- background gradient ----
base = Image.new("RGB", (W * S, H * S))
bd = ImageDraw.Draw(base)
top, bot = rgb("#0d1520"), rgb("#1c2c40")
for y in range(H * S):
    t = y / (H * S)
    bd.line([(0, y), (W * S, y)], fill=tuple(int(top[i] + (bot[i] - top[i]) * t) for i in range(3)))
img = base.convert("RGBA")

# dot grid
dots = Image.new("RGBA", img.size, (0, 0, 0, 0))
dd = ImageDraw.Draw(dots)
for gx in range(0, W, 36):
    for gy in range(0, H, 36):
        dd.ellipse([(gx - 1.5) * S, (gy - 1.5) * S, (gx + 1.5) * S, (gy + 1.5) * S], fill=(255, 255, 255, 14))
img = Image.alpha_composite(img, dots)

# glow behind card
glow = Image.new("RGBA", img.size, (0, 0, 0, 0))
ImageDraw.Draw(glow).ellipse([600 * S, 40 * S, 1280 * S, 700 * S], fill=(46, 196, 182, 22))
glow = glow.filter(ImageFilter.GaussianBlur(40 * S))
img = Image.alpha_composite(img, glow)

# card geometry
CX0, CY0, CX1, CY1 = 690, 70, 1226, 650

# soft shadow
sh = Image.new("RGBA", img.size, (0, 0, 0, 0))
ImageDraw.Draw(sh).rounded_rectangle([CX0 * S, (CY0 + 16) * S, CX1 * S, (CY1 + 16) * S], 22 * S, fill=(0, 0, 0, 150))
sh = sh.filter(ImageFilter.GaussianBlur(18 * S))
img = Image.alpha_composite(img, sh)

d = ImageDraw.Draw(img, "RGBA")


def text(xy, s, f, fill, anchor="la"):
    d.text((xy[0] * S, xy[1] * S), s, font=f, fill=fill, anchor=anchor)


def rrect(box, r, fill=None, outline=None, width=1):
    d.rounded_rectangle([box[0] * S, box[1] * S, box[2] * S, box[3] * S], r * S, fill=fill, outline=outline, width=int(width * S))


# ---- left: title block ----
d.rounded_rectangle([70 * S, 112 * S, 78 * S, 326 * S], 4 * S, fill=ACCENT)
text((100, 94), "Options", font("Black", 118), WHITE)
text((100, 216), "Search", font("Black", 118), ACCENT)
text((104, 366), "オプション検索", font("Bold", 52), WHITE)
text((104, 446), "Find any setting in any mod.", font("Medium", 32), DIM)
text((104, 492), "全MODの設定を、ひとつの検索ボックスから。", font("Medium", 25), DIM)

cx = 104
for label in ("EN", "JA"):
    rrect((cx, 590, cx + 78, 636), 23, fill=rgb("#14303a"), outline=ACCENT, width=2)
    text((cx + 39, 613), label, font("Bold", 26), ACCENT, anchor="mm")
    cx += 96
text((cx + 6, 613), "Cities: Skylines", font("Medium", 26), DIM, anchor="lm")

# ---- right: mock UI card ----
rrect((CX0, CY0, CX1, CY1), 22, fill=rgb("#16212e"), outline=rgb("#2c3e52"), width=2)

for i, c in enumerate(("#ff6b6b", "#ffd166", "#06d6a0")):
    d.ellipse([(CX0 + 26 + i * 26) * S, (CY0 + 22) * S, (CX0 + 42 + i * 26) * S, (CY0 + 38) * S], fill=rgb(c))

SB = (CX0 + 24, CY0 + 62, CX1 - 24, CY0 + 122)
rrect(SB, 14, fill=rgb("#0e1722"), outline=ACCENT, width=3)
mx, my = SB[0] + 34, SB[1] + 29
d.ellipse([(mx - 12) * S, (my - 12) * S, (mx + 12) * S, (my + 12) * S], outline=ACCENT, width=4 * S)
d.line([(mx + 9) * S, (my + 9) * S, (mx + 22) * S, (my + 22) * S], fill=ACCENT, width=5 * S)
f32 = font("Medium", 32)
text((SB[0] + 70, SB[1] + 30), "electricity", f32, WHITE, anchor="lm")
cursor_x = SB[0] + 70 + d.textlength("electricity", font=f32) / S + 6
d.rectangle([cursor_x * S, (SB[1] + 14) * S, (cursor_x + 2.5) * S, (SB[1] + 46) * S], fill=ACCENT)

text((CX0 + 30, CY0 + 140), "12 results", font("Medium", 20), DIM)

rows = [
    ("Electricity consumption multiplier", "Mods > Example Utility Tweaks", True),
    ("Show electricity overlay on start", "Mods > Demo Info Views", False),
    ("Power plant output", "Mods > Sample Balance Pack", False),
    ("Electricity", "Gameplay", False),
    ("Electric vehicle ratio", "Mods > Placeholder Traffic Mod", False),
]
ry = CY0 + 176
for title, crumb, hot in rows:
    box = (CX0 + 20, ry, CX1 - 20, ry + 68)
    if hot:
        rrect(box, 12, fill=rgb("#1e575b"), outline=ACCENT, width=2)
    else:
        rrect(box, 12, fill=rgb("#1e2c3c"))
    text((box[0] + 20, box[1] + 11), title, font("Bold", 24), WHITE)
    text((box[0] + 20, box[1] + 42), crumb, font("Medium", 18), ACCENT2 if hot else DIM)
    ry += 78

out = img.convert("RGB").resize((W, H), Image.LANCZOS)
out.save("Workshop_Thumbnail.png", optimize=True)
print("saved", out.size)
