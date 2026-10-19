"""Gera os ícones do PWA a partir da logo original (círculo dourado/grafite numa página A4 branca).

Uso (na raiz do projeto, requer Pillow: pip install pillow):
    python docs/marca/gerar_icones.py docs/marca/logo-original.jpg frontend/public/icons
Também gera docs/marca/logo-pdf.png (a logo do PDF do orçamento).
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw

origem = Path(sys.argv[1])
destino = Path(sys.argv[2])
destino.mkdir(parents=True, exist_ok=True)

GRAFITE = (0x37, 0x33, 0x34, 255)

pagina = Image.open(origem).convert("RGB")
# Recorta o quadrado do círculo (área não branca), com 1px de folga para não cortar a borda.
mascara_cinza = pagina.convert("L").point(lambda v: 255 if v < 235 else 0)
x0, y0, x1, y1 = mascara_cinza.getbbox()
lado = max(x1 - x0, y1 - y0)
logo = pagina.crop((x0 - 1, y0 - 1, x0 + lado + 1, y0 + lado + 1)).convert("RGBA")

# Cantos transparentes: aplica uma máscara circular (desenhada em 4x e reduzida = borda suave).
escala = 4
circulo = Image.new("L", (logo.width * escala, logo.height * escala), 0)
ImageDraw.Draw(circulo).ellipse((0, 0, circulo.width - 1, circulo.height - 1), fill=255)
logo.putalpha(circulo.resize(logo.size, Image.LANCZOS))


def redondo(tamanho: int) -> Image.Image:
    """Logo circular com fundo transparente (ícone 'any', favicon, imagem da tela de login)."""
    return logo.resize((tamanho, tamanho), Image.LANCZOS)


def em_fundo_grafite(tamanho: int, proporcao_logo: float) -> Image.Image:
    """Logo centralizada num quadrado grafite (ícone 'maskable' e ícone do iPhone)."""
    fundo = Image.new("RGBA", (tamanho, tamanho), GRAFITE)
    lado_logo = round(tamanho * proporcao_logo)
    margem = (tamanho - lado_logo) // 2
    fundo.alpha_composite(redondo(lado_logo), (margem, margem))
    return fundo


redondo(192).save(destino / "icon-192.png", optimize=True)
redondo(512).save(destino / "icon-512.png", optimize=True)
# Maskable: Android recorta o ícone em círculo/gota/quadrado; só os 80% centrais são garantidos.
em_fundo_grafite(512, 0.78).save(destino / "icon-maskable-512.png", optimize=True)
# iPhone não aceita transparência (vira preto) e arredonda os cantos sozinho.
em_fundo_grafite(180, 0.88).convert("RGB").save(destino / "apple-touch-icon.png", optimize=True)
redondo(64).save(destino / "favicon-64.png", optimize=True)
redondo(256).save(destino / "logo-256.png", optimize=True)

# Logo do PDF do orçamento (vai para o banco com o comando definir-logo). 480 px impressos em ~2 cm
# dão ~600 dpi: nítida no papel e leve (o limite da oficina é 1 MB). Fica ao lado do original, não no app.
logo_pdf = origem.parent / "logo-pdf.png"
redondo(480).save(logo_pdf, optimize=True)
print(f"{logo_pdf.name:24} 480x480  RGBA  {logo_pdf.stat().st_size / 1024:6.1f} KB  (em {logo_pdf.parent})")

for arquivo in sorted(destino.glob("*.png")):
    with Image.open(arquivo) as im:
        print(f"{arquivo.name:24} {im.size[0]}x{im.size[1]}  {im.mode:5} {arquivo.stat().st_size / 1024:6.1f} KB")
