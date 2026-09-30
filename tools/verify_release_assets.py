"""Verify the intended commercial WAD set and its accompanying notices."""
from pathlib import Path
import hashlib
import json

root = Path(__file__).resolve().parents[1]
assets = root / 'Assets'
expected = {
    'fsfc1.wad': '02f5029452e74338cb2a6548cd0b99bed94edd371f084b77e99f2a9532cd8702',
    'fssc1.wad': '962b408df639fd0cbd29db34b3e38bf55754265ee4a7197ed5dceb001a5f0cc6',
    'freedoom1.wad': '7323bcc168c5a45ff10749b339960e98314740a734c30d4b9f3337001f9e703d',
    'freedoom2.wad': 'a8772e088847032510d97ba2312406a6998f21cbab44d4ff10696faa9c0ecd4b',
    'freedm.wad': 'd9adc4d792627e7fc47b09067b15486da724010c71dd12831e1cf8e0755b68ad',
}
actual = {p.relative_to(assets).as_posix(): p for p in assets.rglob('*')
          if p.is_file() and p.suffix.lower() == '.wad'}
assert set(actual) == {'doom/' + name for name in expected}, 'Unexpected/missing WADs: ' + str(list(actual))
for name, digest in expected.items():
    assert hashlib.sha256(actual['doom/' + name].read_bytes()).hexdigest() == digest, name + ' differs from licensed release'
for group in ('freedoom', 'freedm', 'freedomscoops'):
    for notice in (('COPYING', 'CREDITS', 'CREDITS-MUSIC', 'FDCREDITS') if group == 'freedomscoops' else ('COPYING', 'CREDITS', 'CREDITS-MUSIC')):
        packaged = assets / 'doom' / f'{group}-{notice}.txt'
        expected_notice = json.loads((root / 'tools/notice-hashes.json').read_text())[packaged.name]
        assert hashlib.sha256(packaged.read_bytes().replace(b'\r\n', b'\n')).hexdigest() == expected_notice, str(packaged) + ' differs from upstream notice'
assert (assets / 'doom/GPL-2.0.txt').read_text() == (root / 'LICENSE').read_text()
assert (assets / 'doom/THIRD_PARTY_NOTICES.txt').read_text() == (root / 'THIRD_PARTY_NOTICES.md').read_text()
assert (assets / 'doom/NOTICES.txt').stat().st_size > 0
print('PASS: exactly 5 approved WADs; release hashes, licenses and credits verified.')
