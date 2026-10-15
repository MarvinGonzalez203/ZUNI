"""Comprueba claves explícitas y parámetros del catálogo, sin consultar datos de usuarios."""
import json,re
from pathlib import Path
root=Path(__file__).resolve().parents[1]; app=root/'Zuni'
catalog=json.loads((app/'Localizacion/en.json').read_text(encoding='utf8'))
errors=[];keys=set()
for p in app.rglob('*'):
    if p.suffix not in ('.cs','.cshtml') or any(part in ('obj','bin','Migrations') for part in p.parts):continue
    for match in re.finditer(r'Idioma.Texto\(("(?:\\.|[^"\\])*")',p.read_text(encoding='utf-8-sig')):
        key=json.loads(match[1]);keys.add(key)
        if key not in catalog:errors.append(str(p.relative_to(root))+': '+key)
for key,value in catalog.items():
    if sorted(re.findall(r'\{\d+\}',key))!=sorted(re.findall(r'\{\d+\}',value)):errors.append('Parámetros diferentes: '+key)
if errors:raise SystemExit('\n'.join(errors))
print(f'OK: {len(keys)} claves explícitas; {len(catalog)} traducciones con parámetros consistentes.')
