# Склеивает ракурсы в одну картинку: python3 grid.py <dir> <prefix> <out.png> [cols]
import sys, glob, os
from PIL import Image
d, prefix, out = sys.argv[1], sys.argv[2], sys.argv[3]
cols = int(sys.argv[4]) if len(sys.argv) > 4 else 2
order = ['fl', 'rl', 'side', 'front', 'rear', 'top']
files = [os.path.join(d, f'{prefix}_{n}.png') for n in order if os.path.exists(os.path.join(d, f'{prefix}_{n}.png'))]
files += sorted(f for f in glob.glob(os.path.join(d, prefix + '_*.png')) if f not in files and not f.endswith('_grid.png'))
ims = [Image.open(f).convert('RGB') for f in files]
w, h = ims[0].size
s = 0.5 if len(ims) > 2 else 1
w2, h2 = int(w * s), int(h * s)
rows = (len(ims) + cols - 1) // cols
g = Image.new('RGB', (w2 * cols, h2 * rows), 'white')
for i, im in enumerate(ims):
    g.paste(im.resize((w2, h2), Image.LANCZOS), ((i % cols) * w2, (i // cols) * h2))
g.save(out)
