import json
import sys
from pathlib import Path

root = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(root / '.agents/skills/scenario-unity-expert/scripts'))
import ut_review

folder = root / (sys.argv[1] if len(sys.argv) > 1 else 'Artifacts/ArenaValidation/2026-10-12')
if (folder / 'center-look-left.png').exists():
    paths = [folder / 'before-ground.png', folder / 'center-look-left.png', folder / 'center-look-right.png']
elif (folder / 'center-reticle-forward.png').exists():
    paths = [folder / 'center-reticle-forward.png', folder / 'center-reticle-turned.png', folder / 'arena-hands-final.png']
else:
    paths = [folder / 'editor-hud.png', folder / 'left-flank-final.png', folder / 'weapon-grip-final.png']
paths += sorted((folder / 'player').glob('*.png'))
checks = []
for path in paths:
    check = ut_review.image_checks(str(path))
    checks.append({'file': str(path.relative_to(root)), 'checks': check, 'verdict': ut_review.image_verdict(check)})
ut_review.contact_sheet([str(p) for p in paths], str(folder / 'contact-sheet.png'), labels=[p.stem for p in paths])
(folder / 'image-review.json').write_text(json.dumps(checks, indent=2), encoding='utf-8')
print(json.dumps([{'file': c['file'], 'verdict': c['verdict']} for c in checks], indent=2))
try:
    from PIL import Image
    im = Image.open(paths[0]).convert('RGB')
    center = (640, 360) if paths[0].name.startswith('center-reticle') else (640, 283)
    print('Reticle center:', im.getpixel(center))
except ImportError:
    pass
