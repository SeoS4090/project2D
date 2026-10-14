"""Read-only authoring checks. Usage: python validate_myth_workbook.py workbook.xlsx."""
import argparse
import collections
import json
import math
from pathlib import Path
import posixpath
import re
import sys
import xml.etree.ElementTree as ET
import zipfile

NS = {'s': 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}
REL = '{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id'

def read_xlsx(filename):
    sheets, formula_errors = {}, []
    with zipfile.ZipFile(filename) as z:
        shared = []
        if 'xl/sharedStrings.xml' in z.namelist():
            shared = [''.join(n.itertext()) for n in ET.fromstring(z.read('xl/sharedStrings.xml')).findall('s:si', NS)]
        rels = {r.attrib['Id']: r.attrib['Target'] for r in ET.fromstring(z.read('xl/_rels/workbook.xml.rels'))}
        for s in ET.fromstring(z.read('xl/workbook.xml')).findall('s:sheets/s:sheet', NS):
            target = rels[s.attrib[REL]]
            target = target.lstrip('/') if target.startswith('/') else posixpath.normpath('xl/' + target)
            rows = []
            for row in ET.fromstring(z.read(target)).findall('s:sheetData/s:row', NS):
                vals = {}
                for c in row.findall('s:c', NS):
                    addr = c.attrib['r']; letters = re.match('[A-Z]+', addr)[0]
                    col = 0
                    for ch in letters: col = col * 26 + ord(ch) - 64
                    value = c.find('s:v', NS); typ = c.attrib.get('t')
                    value = value.text if value is not None else None
                    if typ == 's': value = shared[int(value)] if value is not None else None
                    elif typ == 'inlineStr': value = ''.join(c.find('s:is', NS).itertext())
                    elif typ == 'b': value = value == '1'
                    elif typ == 'e': formula_errors.append([s.attrib['name'], addr, value])
                    elif value is not None and typ not in ('str', 'e'):
                        value = float(value); value = int(value) if value.is_integer() else value
                    vals[col - 1] = value
                rows.append((int(row.attrib['r']), vals))
            sheets[s.attrib['name']] = dict(rows)
    return sheets, formula_errors

def load(filename, contract):
    raw, errors = read_xlsx(filename); tables = {}
    for name, cols in contract['tables'].items():
        if name not in raw: raise ValueError('Missing sheet ' + name)
        actual = [raw[name].get(1, {}).get(i) for i in range(len(cols))]
        if actual != cols: raise ValueError('Header mismatch: ' + name)
        tables[name] = [dict(zip(cols, [vals.get(i) for i in range(len(cols))]))
                        for idx, vals in sorted(raw[name].items()) if idx > 1 and any(v not in (None, '') for v in vals.values())]
    return tables, errors

def validate(t, fields, source=None):
    errors, pending = [], []
    def check(ok, message):
        if not ok: errors.append(message)
    def blank(x): return x is None or x == ''
    def num(x): return isinstance(x, (int, float)) and not isinstance(x, bool) and math.isfinite(x)
    bytable = collections.defaultdict(list)
    for d in fields: bytable[d[0]].append(d)
    for name, ds in bytable.items():
        keys = [d[1] for d in sorted(ds, key=lambda d:d[4]) if d[4]]
        seen = set()
        for i, row in enumerate(t[name], 2):
            tag = f'{name}:{i}'
            if keys:
                key = tuple(row[k] for k in keys)
                check(key not in seen, tag + ' duplicate PK ' + str(key)); seen.add(key)
            for _, col, typ, required, _, ref, refcol, _ in ds:
                v=row[col]
                if blank(v):
                    check(not required, tag + ' required ' + col); continue
                check((typ=='text' and isinstance(v,str)) or (typ=='boolean' and isinstance(v,bool)) or
                      (typ=='number' and num(v)) or (typ=='integer' and num(v) and int(v)==v), tag+' type '+col)
                if ref:
                    candidates=[r for r in t[ref] if r[refcol]==v and (ref!='Text' or r['locale']=='ko')]
                    check(len(candidates)==1, tag+' missing/ambiguous FK '+col+'='+str(v))
    index={name:{r[cols[0]]:r for r in rows} for name,rows in t.items() if rows for cols in [[next(iter(rows[0]))]]}
    graph=collections.defaultdict(list);incoming=collections.Counter()
    for row in t['ProgressionEdge']:
        kind=row['from_type']; a=row['from_id'];b=row['to_upgrade_id']
        check(kind in ('Weapon','Upgrade'),'edge invalid source type')
        src=index.get(kind,{}).get(a);dst=index['Upgrade'].get(b)
        check(src is not None and dst is not None,'edge missing endpoint '+row['edge_id'])
        if src and dst:
            fam=src.get('family_id',src.get('weapon_family_id'))
            stage=1 if kind=='Weapon' else src['stage_index']
            check(fam==dst['weapon_family_id'],'edge changes weapon family '+row['edge_id'])
            check(dst['stage_index']==stage+1,'edge skips/reverses stage '+row['edge_id'])
        graph[a].append(b);incoming[b]+=1
    visiting,done=set(),set()
    def walk(n):
        if n in visiting: errors.append('cycle '+n);return
        if n in done:return
        visiting.add(n)
        for k in graph[n]:walk(k)
        visiting.remove(n);done.add(n)
    for n in list(graph):walk(n)
    reachable=set()
    def reach(n):
        if n in reachable:return
        reachable.add(n)
        for k in graph[n]:reach(k)
    for n in index['Weapon']:reach(n)
    for uid,row in index['Upgrade'].items():
        check(incoming[uid]>0 and uid in reachable,'unreachable upgrade '+uid)
        check(row['stage_index'] in (2,3),'unsupported stage '+uid)
        bindings=[r for r in t['UpgradeEffect'] if r['upgrade_id']==uid]
        check(sum(r['layer']=='base' for r in bindings)==1,'base profile count '+uid)
        check(sum(r['layer']=='resonance' for r in bindings)==(1 if row['stage_index']==3 else 0),'resonance count '+uid)
        check(len({r['order_index'] for r in bindings})==len(bindings),'duplicate order '+uid)
    used=set()
    for r in t['UpgradeEffect']:
        used.add(r['effect_id']);eff=index['Effect'].get(r['effect_id'],{})
        check(r['layer']==eff.get('layer'),'effect layer mismatch '+r['effect_id'])
        if r['layer']=='base': check(blank(r['divinity_id']) and blank(r['min_divinity_stage']),'base requires divinity')
        else: check(not blank(r['divinity_id']) and r['min_divinity_stage'] in (1,2,3),'resonance gate missing')
    check(used==set(index['Effect']),'unbound effect profile')
    for r in t['NumericValue']:
        tag=f"{r['owner_id']}/{r['parameter_id']}";v=r['number_value'];status=r['value_status']
        check(r['owner_id'] in index.get(r['owner_table'],{}),'numeric owner missing '+tag)
        p=index['ParameterDefinition'].get(r['parameter_id'])
        check(status in ('pending','provisional','fixed'),'invalid value status '+tag)
        if status=='pending':check(blank(v),'pending value is not blank '+tag);pending.append(tag);continue
        check(num(v),'non-numeric decided value '+tag)
        if p and num(v):
            check(p['value_type']!='integer' or int(v)==v,'fractional count '+tag)
            check(p['min_value'] is None or v>=p['min_value'],'below minimum '+tag)
            check(p['max_value'] is None or v<=p['max_value'],'above maximum '+tag)
            check(p['allow_zero'] or v!=0,'zero disallowed '+tag)
    for n in ('Weapon','Upgrade','Divinity','CardCandidate'):
        for r in t[n]:check(r['enabled'] is False,n+' prematurely runtime-enabled')
    for r in t['CardCandidate']:
        check((r['category']=='general' and r['access_policy']=='none') or
              (r['category']=='divinity' and r['access_policy']=='selected_divinity'),'card gate mismatch '+r['card_id'])
    if source:
        expected=set(re.findall(r'^\| (M[123]-[A-Z-]+) \*\*',Path(source).read_text(encoding='utf-8'),re.M))
        actual={r['design_key'] for n in ('Weapon','Upgrade') for r in t[n]}
        check(expected==actual,'design coverage differs: '+str(expected^actual))
    return {'errors':errors,'error_count':len(errors),'pending_numeric_count':len(pending),
            'counts':{n:len(rs) for n,rs in t.items()},'runtime_ready':False,
            'scope':'Authoring structure only; numeric tuning, atomic effects and Unity import remain pending.'}

def main():
    ap=argparse.ArgumentParser();ap.add_argument('xlsx');ap.add_argument('--report');args=ap.parse_args()
    # Keep legacy callers working after the explicitly versioned authoring migration.
    with zipfile.ZipFile(args.xlsx) as archive:
        if 'xl/tables/table1.xml' in archive.namelist():
            names=[s.attrib['name'] for s in ET.fromstring(archive.read('xl/workbook.xml')).findall('s:sheets/s:sheet',NS)]
            if '무기' in names:
                import data_pipeline
                return data_pipeline.main()
    root=Path(__file__).resolve().parents[2]
    contract=json.loads(Path(__file__).with_name('myth_schema.json').read_text(encoding='utf-8'))
    try:
        data,formula_errors=load(args.xlsx,contract)
        report=validate(data,contract['fields'],root/contract['design_ref'])
        report['formula_errors']=formula_errors;report['error_count']+=len(formula_errors)
    except Exception as e:report={'error_count':1,'errors':[str(e)]}
    encoded=json.dumps(report,ensure_ascii=False,indent=2)
    if args.report:Path(args.report).write_text(encoded,encoding='utf-8')
    print(encoded);return 1 if report['error_count'] else 0
if __name__=='__main__':sys.exit(main())
