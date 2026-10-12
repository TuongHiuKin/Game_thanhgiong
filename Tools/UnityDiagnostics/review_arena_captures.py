import json
import sys
from pathlib import Path

root = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(root / '.agents/skills/scenario-unity-expert/scripts'))
import ut_review

folder = root / 'Artifacts/ArenaValidation/2026-10-12'
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
    im = Image.open(folder / 'editor-hud.png').convert('RGB')
    print('Reticle center:', im.getpixel((640, 283)))
    print('Reticle ticks:', [im.getpixel(p) for p in [(640,271),(640,296),(627,283),(653,283)]])
except ImportError:
    pass
