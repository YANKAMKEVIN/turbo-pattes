#!/usr/bin/env python3
"""Logo « Turbo Grid » en SVG (et PNG), reproductible depuis le dépôt.

Reprend exactement le logo dessiné en jeu par UiKit.Logo (écran titre et écran de marque) à l'échelle 720 p :
titre de Core/Brand.cs sur deux lignes penchées (pente LogoIntro.Slant), blanc piste puis rouge course, ombre carbone,
trois filets de vitesse, emblème damier + chevrons (Icons.Emblem) sur pastille carbone cerclée d'acier.
Les lettres sont converties en contours depuis Resources/Fonts/SairaCondensed-Bold.ttf : le SVG ne dépend d'aucune police.

Sorties (docs/design) :
  logo-turbo-grid.svg  logo seul, fond transparent ;
  ecran-de-marque.svg  écran de marque 1280 x 720 (état affiché, après l'apparition) ;
  .png des deux si Node et Playwright (Chromium) sont disponibles.

Usage : python3 tools/logo/turbo_grid_logo.py   (dépendance : fontTools)
"""
import os
import re
import shutil
import subprocess
import sys

from fontTools.pens.svgPathPen import SVGPathPen
from fontTools.pens.transformPen import TransformPen
from fontTools.ttLib import TTFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FONTS = os.path.join(ROOT, "Assets", "TurboPattes", "Resources", "Fonts")
OUT = os.path.join(ROOT, "docs", "design")


def read_core(name, pattern):
    with open(os.path.join(ROOT, "Assets", "TurboPattes", "Core", name), encoding="utf-8") as f:
        return re.search(pattern, f.read()).group(1)


TITLE = read_core("Brand.cs", r'Title = "([^"]+)"')
TAGLINE = read_core("Brand.cs", r'Tagline = "([^"]+)"')
SLANT = float(read_core("LogoIntro.cs", r"Slant = ([\d.]+)f"))
PALETTE = {k: read_core("Brand.cs", rf'{k} = Rgb\.Hex\("(#[0-9A-Fa-f]{{6}})"\)')
           for k in ("Carbon", "Chalk", "Red", "Steel")}
INK = "#1A1729"  # IconCanvas.Ink (0.1, 0.09, 0.16) : contour des icônes


def logo_lines(title):
    """Même coupe que Brand.LogoLines : à l'espace le plus proche du milieu."""
    t = title.strip().upper()
    spaces = [i for i, c in enumerate(t) if c == " "]
    if not spaces:
        return [t, ""]
    best = min(spaces, key=lambda i: abs(i - len(t) // 2))
    return [t[:best].strip(), t[best + 1:].strip()]


class Font:
    def __init__(self, file):
        self.tt = TTFont(os.path.join(FONTS, file))
        self.upm = self.tt["head"].unitsPerEm
        hhea = self.tt["hhea"]
        self.ascent, self.descent, self.gap = hhea.ascent, hhea.descent, hhea.lineGap
        self.cmap = self.tt.getBestCmap()
        self.glyphs = self.tt.getGlyphSet()

    def width(self, text, size):
        return sum(self.tt["hmtx"][self.cmap[ord(c)]][0] for c in text) * size / self.upm

    def path(self, text, size, x, baseline):
        """Contours du texte, origine du premier caractère en (x, ligne de base), axe y vers le bas."""
        pen = SVGPathPen(self.glyphs)
        k = size / self.upm
        for c in text:
            name = self.cmap[ord(c)]
            self.glyphs[name].draw(TransformPen(pen, (k, 0, 0, -k, x, baseline)))
            x += self.tt["hmtx"][name][0] * k
        return pen.getCommands()

    def centered(self, text, size, rect):
        """Texte centré dans un rectangle comme TextAnchor.MiddleCenter d'IMGUI (boîte de ligne centrée)."""
        rx, ry, rw, rh = rect
        k = size / self.upm
        line = (self.ascent - self.descent + self.gap) * k
        baseline = ry + (rh - line) / 2 + self.ascent * k
        return self.path(text, size, rx + (rw - self.width(text, size)) / 2, baseline)


def poly(points, scale, ox, oy):
    return " ".join(f"{ox + x * scale:.2f},{oy + y * scale:.2f}" for x, y in points)


def emblem(cx, cy, size=104):
    """Icons.Emblem : drapeau à damier penché, hampe acier, deux chevrons rouges (toile 0..1, y vers le bas)."""
    ox, oy, k = cx - size / 2, cy - size / 2, size
    flag = [(0.16, 0.26), (0.62, 0.26), (0.52, 0.66), (0.06, 0.66)]
    out = [f'<clipPath id="flag"><polygon points="{poly(flag, k, ox, oy)}"/></clipPath>',
           f'<polygon points="{poly(flag, k, ox, oy)}" fill="{INK}" stroke="{INK}" stroke-width="{0.078 * k:.2f}" stroke-linejoin="round"/>',
           f'<polygon points="{poly(flag, k, ox, oy)}" fill="{PALETTE["Chalk"]}" stroke="{PALETTE["Chalk"]}" stroke-width="{0.03 * k:.2f}" stroke-linejoin="round"/>']
    cells = []
    for row in range(3):
        for col in range(4):
            if (row + col) % 2 == 0:
                continue
            y0 = 0.26 + row * 0.1333
            y1 = y0 + 0.1333
            x0 = 0.16 + col * 0.115 - (y0 - 0.26) * 0.25
            x1 = x0 + 0.115
            cells.append(f'<polygon points="{poly([(x0, y0), (x1, y0), (x1 - 0.0333, y1), (x0 - 0.0333, y1)], k, ox, oy)}"/>')
    out.append(f'<g fill="{PALETTE["Carbon"]}" clip-path="url(#flag)">{"".join(cells)}</g>')
    pole = f'x1="{ox + 0.15 * k:.2f}" y1="{oy + 0.22 * k:.2f}" x2="{ox + 0.04 * k:.2f}" y2="{oy + 0.84 * k:.2f}"'
    out.append(f'<line {pole} stroke="{INK}" stroke-width="{2 * (0.022 + 0.0144) * k:.2f}" stroke-linecap="round"/>')
    out.append(f'<line {pole} stroke="{PALETTE["Steel"]}" stroke-width="{0.044 * k:.2f}" stroke-linecap="round"/>')
    for x in (0.62, 0.76):
        pts = poly([(x, 0.3), (x + 0.08, 0.3), (x + 0.2, 0.46), (x + 0.08, 0.62), (x, 0.62), (x + 0.12, 0.46)], k, ox, oy)
        out.append(f'<polygon points="{pts}" fill="{PALETTE["Red"]}" stroke="{INK}" stroke-width="{2 * 0.0168 * k:.2f}" stroke-linejoin="round" paint-order="stroke"/>')
        out.append(f'<polygon points="{pts}" fill="{PALETTE["Red"]}" stroke="{PALETTE["Red"]}" stroke-width="{0.02 * k:.2f}" stroke-linejoin="round"/>')
    return "\n".join(out)


def logo(px, py, bold):
    """UiKit.Logo à l'échelle 1 (720 p), pivot (px, py)."""
    lines = logo_lines(TITLE)
    shear = f"matrix(1 0 {-SLANT} 1 {SLANT * py:.3f} 0)"
    r0 = (px - 400, py - 136, 860, 140)
    r1 = (px - 400, py - 14, 860, 140)
    text0, text1 = bold.centered(lines[0], 130, r0), bold.centered(lines[1], 130, r1)
    return f"""<g transform="{shear}">
  <rect x="{px - 420}" y="{py - 34}" width="300" height="10" fill="{PALETTE['Steel']}" fill-opacity="0.9"/>
  <rect x="{px - 470}" y="{py - 8}" width="380" height="14" fill="{PALETTE['Red']}" fill-opacity="0.9"/>
  <rect x="{px - 430}" y="{py + 24}" width="260" height="6" fill="{PALETTE['Steel']}" fill-opacity="0.9"/>
  <g transform="translate(6 6)" fill="{PALETTE['Carbon']}" fill-opacity="0.75"><path d="{text0}"/><path d="{text1}"/></g>
  <path d="{text0}" fill="{PALETTE['Chalk']}"/>
  <path d="{text1}" fill="{PALETTE['Red']}"/>
</g>
<circle cx="{px - 290}" cy="{py - 62}" r="70" fill="{PALETTE['Steel']}"/>
<circle cx="{px - 290}" cy="{py - 62}" r="64" fill="{PALETTE['Carbon']}"/>
{emblem(px - 290, py - 62)}"""


def svg(w, h, body, background=None):
    bg = f'<rect width="{w}" height="{h}" fill="{background}"/>\n' if background else ""
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}">\n'
            f"<!-- Généré par tools/logo/turbo_grid_logo.py : ne pas modifier à la main. -->\n{bg}{body}\n</svg>\n")


def main():
    bold = Font("SairaCondensed-Bold.ttf")
    semi = Font("SairaCondensed-SemiBold.ttf")
    os.makedirs(OUT, exist_ok=True)

    # Logo seul : cadre ajusté (filets de vitesse à gauche, haut penché du titre à droite).
    lines = logo_lines(TITLE)
    right = max(30 + bold.width(t, 130) / 2 + SLANT * (136 if i == 0 else 14) for i, t in enumerate(lines))
    px, py = 490, 152
    w, h = int(px + right + 24), 300
    with open(os.path.join(OUT, "logo-turbo-grid.svg"), "w", encoding="utf-8") as f:
        f.write(svg(w, h, logo(px, py, bold)))

    # Écran de marque (BrandScreen.OnGUI) en 1280 x 720 : logo, accroche, trait rouge entièrement tiré.
    W, H = 1280, 720
    px, py = W * 0.5, H * 0.44
    tag = (W * 0.5 - 290, py + 122, 580, 44)
    body = (logo(px, py, bold)
            + f'\n<path d="{semi.centered(TAGLINE.upper(), 29, tag)}" fill="{PALETTE["Chalk"]}"/>'
            + f'\n<rect x="{tag[0]}" y="{tag[1] + tag[3] + 6}" width="{tag[2]}" height="5" fill="{PALETTE["Red"]}"/>')
    with open(os.path.join(OUT, "ecran-de-marque.svg"), "w", encoding="utf-8") as f:
        f.write(svg(W, H, body, PALETTE["Carbon"]))

    render_png(["logo-turbo-grid", "ecran-de-marque"])


def render_png(names):
    """PNG via Chromium (Playwright pour Node), s'il est installé ; sinon les SVG suffisent."""
    if not shutil.which("node"):
        print("Node absent : PNG non générés.")
        return
    js = r"""
const { chromium } = require('playwright');
const fs = require('fs');
(async () => {
  const browser = await chromium.launch();
  for (const base of process.argv.slice(1)) {
    const svg = fs.readFileSync(base + '.svg', 'utf8');
    const [, w, h] = svg.match(/width="(\d+)" height="(\d+)"/);
    const page = await browser.newPage({ viewport: { width: +w, height: +h } });
    await page.setContent('<html><body style="margin:0;background:transparent">' + svg + '</body></html>');
    await page.screenshot({ path: base + '.png', omitBackground: true });
    await page.close();
  }
  await browser.close();
})().catch(e => { console.error(e.message); process.exit(1); });
"""
    try:
        subprocess.run(["node", "-e", js] + [os.path.join(OUT, n) for n in names], check=True)
    except subprocess.CalledProcessError:
        print("Playwright indisponible : PNG non générés.", file=sys.stderr)


if __name__ == "__main__":
    main()
