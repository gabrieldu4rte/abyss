"""Convert the original illustrations into plain ASCII; Pillow is needed only to rebuild art."""
from pathlib import Path
from PIL import Image, ImageOps, ImageFilter
import json
ROOT = Path(__file__).resolve().parent.parent
RAMP = " .,:;irsXA253hMHGS#9B&@"
PORTRAITS = {'warrior','mage','archer','rogue','warrior_critical','mage_critical','archer_critical','rogue_critical','rat','skeleton','goblin','warden','unknown','merchant'}
def convert(source, destination, columns):
    rgba=Image.open(source).convert('RGBA')
    image=Image.new('RGBA',rgba.size,(0,0,0,255))
    image.alpha_composite(rgba)
    image=ImageOps.autocontrast(image.convert('L'),cutoff=.5)
    rows=round(columns*.6*image.height/image.width)
    image=image.resize((columns,rows),Image.Resampling.LANCZOS)
    image=image.filter(ImageFilter.UnsharpMask(radius=.7,percent=140,threshold=2))
    pixels=list(image.get_flattened_data()) if hasattr(image,"get_flattened_data") else list(image.getdata())
    chars=[RAMP[min(len(RAMP)-1,round((v/255)**.70*(len(RAMP)-1)))] if v>5 else ' ' for v in pixels]
    tones=[str(min(7,round((v/255)**.7*7))) for v in pixels]
    destination.with_suffix('.tone').write_text('\n'.join(''.join(tones[y*columns:(y+1)*columns]) for y in range(rows)),encoding='ascii')
    destination.write_text('\n'.join(''.join(chars[y*columns:(y+1)*columns]) for y in range(rows)),encoding='ascii')
    return {'columns':columns,'rows':rows,'source':source.name,'file':destination.name}
if __name__=='__main__':
    index={}
    for source in sorted((ROOT/'ArtSources').glob('*.png')):
        index[source.stem]=convert(source,ROOT/'Art'/(source.stem+'.txt'),100 if source.stem in PORTRAITS else 160)
    (ROOT/'Art'/'index.json').write_text(json.dumps(index,indent=2))
    print(f'Converted {len(index)} original illustrations into printable ASCII.')
