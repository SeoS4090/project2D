"""Named Excel Tables -> validated, deterministic data-1.0 JSON/C#.

No Excel engine or third-party Python packages required. Formula input is rejected.
"""
import argparse
import hashlib
import json
import math
import os
from pathlib import Path
import posixpath
import re
import tempfile
import xml.etree.ElementTree as ET
import zipfile
import validate_myth_workbook as legacy

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[1]
NS=legacy.NS
SCHEMA=HERE/'schema_v1.json'
REGISTRY=HERE/'id_registry.json'
DEFAULT_BOOK=ROOT/'Assets/Documentation/RoguelikeDesign/09 게임데이터/MythBuildData.xlsx'

def read(path):return json.loads(Path(path).read_text(encoding='utf-8'))
def encoded(value):return (json.dumps(value,ensure_ascii=False,sort_keys=True,indent=2,allow_nan=False)+'\n').encode('utf-8')
def digest(value):return hashlib.sha256(value).hexdigest()
def blank(v):return v is None or v==''
def cell(a):
    letters,num=re.fullmatch(r'([A-Z]+)(\d+)',a).groups(); col=0
    for c in letters:col=col*26+ord(c)-64
    return int(num),col-1
def rect(s):
    a,b=s.split(':') if ':' in s else (s,s)
    r,c=cell(a);rr,cc=cell(b);return r,c,rr,cc

def load_book(path,schema):
    raw,errors=legacy.read_xlsx(path);errors=[str(e) for e in errors];tables={};locations={}
    if set(raw)!=set(schema['sheets']):errors.append('Workbook must contain exactly the six schema sheets')
    with zipfile.ZipFile(path) as z:
        rels={r.attrib['Id']:r.attrib['Target'] for r in ET.fromstring(z.read('xl/_rels/workbook.xml.rels'))}
        for s in ET.fromstring(z.read('xl/workbook.xml')).findall('s:sheets/s:sheet',NS):
            name=s.attrib['name'];target=rels[s.attrib[legacy.REL]]
            target=target.lstrip('/') if target.startswith('/') else posixpath.normpath('xl/'+target)
            xml=ET.fromstring(z.read(target));sheetrels=posixpath.join(posixpath.dirname(target),'_rels',posixpath.basename(target)+'.rels')
            rel={r.attrib['Id']:r.attrib['Target'] for r in ET.fromstring(z.read(sheetrels))} if sheetrels in z.namelist() else {}
            boxes=[]
            for part in xml.findall('s:tableParts/s:tablePart',NS):
                p=rel[part.attrib[legacy.REL]];p=p.lstrip('/') if p.startswith('/') else posixpath.normpath(posixpath.dirname(target)+'/'+p)
                t=ET.fromstring(z.read(p));tn=t.attrib['name'];bounds=rect(t.attrib['ref']);a,b,c,d=bounds
                if tn in tables:errors.append(f'{name}/{tn}: duplicate Table')
                if any(not(c<x or a>xx or d<y or b>yy) for x,y,xx,yy in boxes):errors.append(f'{name}/{tn}: overlapping Tables')
                boxes.append(bounds)
                spec=schema['tables'].get(tn)
                if not spec:errors.append(f'{name}/{tn}: unknown Table');continue
                if spec['sheet']!=name:errors.append(f'{name}/{tn}: wrong sheet')
                headers=[raw[name].get(a,{}).get(i) for i in range(b,d+1)]
                expected=[f['name'] for f in spec['fields']]
                if headers!=expected:errors.append(f'{name}/{tn}: expected headers {expected}');continue
                rows=[]
                for rr in range(a+1,c+1):
                    values=[raw[name].get(rr,{}).get(cc) for cc in range(b,d+1)]
                    if all(blank(v) for v in values):continue
                    row=dict(zip(headers,values));rows.append(row)
                tables[tn]=rows;locations[tn]={'sheet':name,'range':t.attrib['ref']}
            for c in xml.findall('s:sheetData/s:row/s:c',NS):
                if c.find('s:f',NS) is not None:errors.append(f'{name}/{c.attrib["r"]}: formulas prohibited in authored workbook')
                r,col=cell(c.attrib['r']);value=raw[name].get(r,{}).get(col)
                if r>=5 and not blank(value) and not any(a<=r<=b and x<=col<=y for a,x,b,y in boxes):
                    errors.append(f'{name}/{c.attrib["r"]}: populated cell outside Table; expand its Table range')
    for name in schema['tables']:
        if name not in tables:errors.append('Missing Table '+name)
    return tables,errors,locations

def validate(data,schema,registry):
    errors=[]
    expected=[dict(enum_name=e,member_name=k,number=v) for e,vs in schema['enums'].items() for k,v in vs.items()]
    if sorted(data.get('EnumGuide',[]),key=lambda r:(r['enum_name'],r['member_name']))!=sorted(expected,key=lambda r:(r['enum_name'],r['member_name'])):errors.append('EnumGuide: generated guide differs from schema')
    def fail(t,row,field,reason):errors.append(f'{schema["tables"][t]["sheet"]}/{t}/{tuple(row.get(k) for k in schema["tables"][t]["key"])}/{field}: {reason}')
    for t,spec in schema['tables'].items():
        seen=set();codes=set()
        for row in data.get(t,[]):
            key=tuple(row.get(k) for k in spec['key'])
            if key in seen:fail(t,row,'PK','duplicate')
            seen.add(key)
            for f in spec['fields']:
                v=row.get(f['name']);typ=f['type']
                if blank(v):
                    if f['required']:fail(t,row,f['name'],'required value missing')
                    continue
                valid=(isinstance(v,str) if typ=='string' else type(v)==int if typ in ('int','id') else type(v) in (int,float) and math.isfinite(v) if typ=='float' else v in schema['enums'][typ])
                if not valid:fail(t,row,f['name'],'invalid '+typ);continue
                if typ=='id' and not 0<v<=2147483647:fail(t,row,f['name'],'positive Int32 required')
                if typ in ('int','float'):
                    if 'min' in f and v<f['min'] or 'max' in f and v>f['max'] or v==0 and f.get('allow_zero') is False:fail(t,row,f['name'],'out of range')
                    if typ=='int' and not -2147483648<=v<=2147483647:fail(t,row,f['name'],'Int32 overflow')
                if 'ref' in f:
                    target,_,col=f['ref'].partition('.');col=col or 'id'
                    candidates=[r for r in data.get(target,[]) if r[col]==v and (target!='Text' or r['locale']=='ko')]
                    if len(candidates)!=1:fail(t,row,f['name'],'missing/ambiguous FK '+f['ref'])
            if 'code_name' in row:
                code=row['code_name']
                if not isinstance(code,str) or not re.fullmatch('[a-z][a-z0-9_]*',code):fail(t,row,'code_name','lower_snake_case required')
                if code in codes:fail(t,row,'code_name','duplicate')
                codes.add(code)
                if registry['tables'].get(t,{}).get(code)!=row['id']:fail(t,row,'id','not registered; register new content or rename explicitly')
                if row['id'] in registry.get('retired',{}).get(t,[]):fail(t,row,'id','retired ID cannot be reused')
            if not spec['runtime'] and row.get('row_state') not in (None,'Draft','Retired'):fail(t,row,'row_state','authoring design has no executable handler')
    # Registry uniqueness is checked even for currently absent content.
    for name,values in registry['tables'].items():
        if len(set(values.values()))!=len(values):errors.append(name+': registry duplicate IDs')
        if any(type(v)!=int or not 0<v<=2147483647 for v in values.values()):errors.append(name+': registry invalid ID')
    if errors:return errors
    stages={r['id']:r for r in data['WeaponStage']};actions={r['id']:r for r in data['ActionDesign']}
    for row in stages.values():
        seen=set();current=row
        while current:
            if current['id'] in seen:fail('WeaponStage',row,'parent_id','growth cycle');break
            seen.add(current['id'])
            if current['family_id']!=row['family_id']:fail('WeaponStage',row,'parent_id','family mismatch')
            current=stages.get(current['parent_id'])
        if actions[row['action_design_id']]['family_id']!=row['family_id']:fail('WeaponStage',row,'action_design_id','family mismatch')
    effects={r['id']:r for r in data['EffectDesign']}
    for row in data['WeaponEffect']:
        resonance=effects[row['effect_id']]['layer']=='Resonance'
        if resonance!=(not blank(row['divinity_id']) and not blank(row['min_divinity_stage'])):fail('WeaponEffect',row,'divinity_id','resonance requires divinity and stage; base requires neither')
        if not resonance and (not blank(row['divinity_id']) or not blank(row['min_divinity_stage'])):fail('WeaponEffect',row,'divinity_id','base must be independent of divinity')
    for row in data['CardCandidate']:
        if (row['category']=='General')!=(row['access_policy']=='None'):fail('CardCandidate',row,'access_policy','category/access mismatch')
    for table,owner,order in [('AttackStep','combo_id','step_index'),('WeaponEffect','stage_id','order_index')]:
        for oid in {r[owner] for r in data[table]}:
            indices=sorted(r[order] for r in data[table] if r[owner]==oid)
            if indices!=list(range(1,len(indices)+1)):errors.append(f'{table}/{oid}: order must be contiguous from 1')
    for row in data['ChargeProfile']:
        if row['damage_min']>row['damage_max'] or row['move_ratio']>1:fail('ChargeProfile',row,'damage_min/move_ratio','invalid charge bounds')
    for row in data['ComboProfile']:
        if not any(s['combo_id']==row['id'] for s in data['AttackStep']):fail('ComboProfile',row,'id','empty combo')
    reviews={}
    for row in data['ValueReview']:
        table=row['table_name']; field=row['field_name']; oid=row['owner_id']; step=None
        if table=='AttackStep':
            try:step,field=field.split('.');step=int(step)
            except ValueError:fail('ValueReview',row,'field_name','expected step_index.field');continue
        spec=schema['tables'].get(table)
        if not spec:fail('ValueReview',row,'table_name','unknown table');continue
        candidates=[r for r in data[table] if r.get('id',r.get('owner_id',r.get('combo_id')))==oid and (step is None or r.get('step_index')==step)]
        fs=next((f for f in spec['fields'] if f['name']==field and f['type'] in ('int','float')),None)
        if len(candidates)!=1 or not fs:fail('ValueReview',row,'field_name','unknown numeric field/owner');continue
        v=candidates[0][field]
        if (row['value_status']=='Pending')!=blank(v):fail('ValueReview',row,'value_status','Pending must be blank; other statuses require a number')
        reviews[(table,oid,row['field_name'])]=row
    for t,spec in schema['tables'].items():
        if t in ('ValueReview','StageLabel','WeaponEffect','EnumGuide'):continue
        for row in data[t]:
            for f in spec['fields']:
                if f['type'] not in ('int','float') or f['name']=='step_index':continue
                key=(t,row.get('id',row.get('owner_id',row.get('combo_id'))),(str(row['step_index'])+'.' if t=='AttackStep' else '')+f['name'])
                # Blank optional facets without a review mean absent, never implicit zero.
                if not blank(row[f['name']]) and key not in reviews:fail(t,row,f['name'],'numeric value lacks review provenance')
    return errors

def migration_check(data,registry):
    """Explicit migration audit only, not a permanent ban on subsequent data edits."""
    old,_=legacy.load(HERE/'Legacy/MythBuildData.v0.xlsx',read(HERE/'myth_schema.json'))
    errors=[]
    for m in registry['legacy_mapping']:
        if len([r for r in data[m['table']] if r['id']==m['id']])!=1:errors.append('Lost content '+str(m))
    originals={(r['owner_table'],r['owner_id'],r['parameter_id']):r for r in old['NumericValue']}
    for m in registry['numeric_mapping']:
        previous=originals[(m['old_table'],m['old_id'],m['parameter'])]
        rows=[r for r in data[m['table']] if r['owner_id']==m['id']]
        reviews=[r for r in data['ValueReview'] if (r['table_name'],r['owner_id'],r['field_name'])==(m['table'],m['id'],m['field'])]
        if len(rows)!=1 or rows[0][m['field']]!=previous['number_value'] or len(reviews)!=1 or reviews[0]['value_status'].lower()!=previous['value_status'] or reviews[0]['value_basis']!=previous['value_basis']:errors.append('Numeric migration mismatch '+str(m))
    stages={r['id']:r for r in data['WeaponStage']};idx={(m['old_table'],m['old_id']):m['id'] for m in registry['legacy_mapping']}
    for r in old['ProgressionEdge']:
        if stages[idx['Upgrade',r['to_upgrade_id']]]['parent_id']!=idx[r['from_type'],r['from_id']]:errors.append('Lost growth edge '+r['edge_id'])
    for t in ['Text']:
        if sorted(data[t],key=lambda r:tuple(r.values()))!=sorted(old[t],key=lambda r:tuple(r.values())):errors.append('Lost '+t)
    for r in old['Effect']:
        n=next(n for n in data['EffectDesign'] if n['id']==idx['Effect',r['effect_id']])
        if any(n[k]!=r[k] for k in ('description','failure_policy','profile_key')):errors.append('Lost effect description '+r['effect_id'])
    for r in old['Divinity']:
        n=next(n for n in data['Divinity'] if n['id']==idx['Divinity',r['divinity_id']])
        if any(n[k]!=r[k] for k in ('theme','myth_basis','adaptation','source_url')):errors.append('Lost divinity provenance')
    if len(data['WeaponEffect'])!=46 or len(data['CardCandidate'])!=14 or len(data['WeaponStage'])!=35:errors.append('Migration count mismatch')
    return errors

def closure(data,schema,target,root_id):
    selected={t:[] for t,s in schema['tables'].items() if s['runtime']};visited=set()
    def visit(table,row):
        key=(table,tuple(row[k] for k in schema['tables'][table]['key']))
        if key in visited:return
        visited.add(key)
        if 'row_state' in row and row['row_state'] not in (['Release'] if target=='release' else ['Test','Release']):raise ValueError(f'{table}/{key}: {target} dependency not ready')
        selected[table].append(row)
        for f in schema['tables'][table]['fields']:
            if f.get('ref') and not blank(row[f['name']]):
                visit(f['ref'],next(r for r in data[f['ref']] if r['id']==row[f['name']]))
        if table=='ComboProfile':
            for step in data['AttackStep']:
                if step['combo_id']==row['id']:visit('AttackStep',step)
    root=next((r for r in data['TrainingLoadout'] if r['id']==root_id),None)
    if root is None:raise ValueError('TrainingLoadout root missing')
    visit('TrainingLoadout',root)
    for review in data['ValueReview']:
        t=review['table_name']
        if t not in selected or not any(r.get('id',r.get('combo_id'))==review['owner_id'] for r in selected[t]):continue
        if review['value_status']=='Pending' or target=='release' and review['value_status']!='Fixed':raise ValueError('Unreviewed dependency '+str(review))
    result={}
    for t,rows in selected.items():
        result[t]=[]
        for row in sorted(rows,key=lambda r:tuple(r[k] for k in schema['tables'][t]['key'])):
            result[t].append({f['name']:schema['enums'][f['type']][row[f['name']]] if f['type'] in schema['enums'] else row[f['name']] for f in schema['tables'][t]['fields']})
    return result

def generate_cs(schema,registry):
    lines=['// Generated by Tools/GameData/data_pipeline.py. Do not edit.','using System;','using System.Collections.Generic;','using Newtonsoft.Json;','namespace ProjectAr.GameData','{']
    for name,values in schema['enums'].items():lines.append('    public enum '+name+' { '+', '.join(k+' = '+str(v) for k,v in values.items())+' }')
    lines+=['    public sealed class TrainingDataDocument','    {','        [JsonProperty] public string schema_version { get; private set; }','        [JsonProperty] public TrainingTables tables { get; private set; }','    }','    public sealed class TrainingTables','    {']
    runtime={t:s for t,s in schema['tables'].items() if s['runtime']}
    for t in runtime:lines.append(f'        [JsonProperty] public {t}Row[] {t} {{ get; private set; }}')
    lines+=['    }']
    for t,s in runtime.items():
        lines.extend(['    public sealed class '+t+'Row','    {'])
        for f in s['fields']:
            typ={'id':'int','int':'int','float':'float','string':'string'}.get(f['type'],f['type'])
            lines.append(f'        [JsonProperty] public {typ} {f["name"]} {{ get; private set; }}')
        lines.append('        public void Validate()')
        lines.append('        {')
        for f in s['fields']:
            n=f['name'];typ=f['type'];checks=[]
            if typ=='id':checks.append(f'{n} <= 0')
            elif typ=='string':checks.append(f'string.IsNullOrWhiteSpace({n})')
            elif typ in ('int','float'):
                if typ=='float':checks.extend([f'float.IsNaN({n})',f'float.IsInfinity({n})'])
                if 'min' in f:checks.append(f'{n} < {f["min"]}f')
                if 'max' in f:checks.append(f'{n} > {f["max"]}f')
            elif typ in schema['enums']:checks.append(f'!Enum.IsDefined(typeof({typ}), {n})')
            if n=='row_state':checks.append(f'({n} != RowState.Test && {n} != RowState.Release)')
            if checks:lines.append(f'            if ({" || ".join(checks)}) throw new InvalidOperationException("{t}.{n}: invalid value");')
        lines+=['        }','    }']
        if 'id' not in s['key']:continue
        lines.extend([f'    public readonly struct {t}Id : IEquatable<{t}Id>','    {','        public int Value { get; }',f'        public {t}Id(int value) {{ if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); Value = value; }}',f'        public bool Equals({t}Id other) => Value == other.Value;',f'        public override bool Equals(object obj) => obj is {t}Id other && Equals(other);','        public override int GetHashCode() => Value;','    }',f'    public static class {t}Ids','    {'])
        for code,rid in registry['tables'][t].items():lines.append(f'        public static readonly {t}Id {"".join(w.title() for w in code.split("_"))} = new {t}Id({rid});')
        lines.append('    }')
    lines+=['}'];return ('\n'.join(lines)+'\n').replace('[JsonProperty]', '[JsonProperty(Required = Required.Always)]').encode()

def outputs(data,schema,registry,book,target,root_id):
    payload={'schema_version':schema['schema_version'],'tables':closure(data,schema,target,root_id)}
    runtime=encoded(payload);code=generate_cs(schema,registry)
    manifest={'schema_version':schema['schema_version'],'generator_version':schema['generator_version'],'target':target,'root_id':root_id,
              'data_revision':digest(runtime),'source_sha256':digest(Path(book).read_bytes()),'schema_sha256':digest(encoded(schema)),
              'registry_sha256':digest(encoded(registry)),'outputs':{'TrainingData.json':digest(runtime),'GameData.Generated.cs':digest(code)}}
    return {'TrainingData.json':runtime,'GameData.Generated.cs':code,'manifest.json':encoded(manifest)}

def publish(files,out):
    # Validate and generate everything before touching the destination. Consumers verify hashes.
    out=Path(out);out.mkdir(parents=True,exist_ok=True)
    previous={name:(out/name).read_bytes() if (out/name).exists() else None for name in files}
    with tempfile.TemporaryDirectory(dir=out,prefix='.staging-') as tmp:
        for name,contents in files.items():
            staged=Path(tmp)/name;staged.parent.mkdir(parents=True,exist_ok=True);staged.write_bytes(contents)
        committed=[]
        try:
            for name in sorted(files,key=lambda n:n=='manifest.json'):
                (out/name).parent.mkdir(parents=True,exist_ok=True)
                os.replace(Path(tmp)/name,out/name);committed.append(name)
        except OSError:
            for name in committed:
                if previous[name] is None:(out/name).unlink(missing_ok=True)
                else:(out/name).write_bytes(previous[name])
            raise

def main():
    p=argparse.ArgumentParser();p.add_argument('workbook',nargs='?',default=str(DEFAULT_BOOK));p.add_argument('--target',choices=['test','release'],default='test');p.add_argument('--root-id',type=int,default=1);p.add_argument('--out');p.add_argument('--report');p.add_argument('--migration-check',action='store_true');p.add_argument('--install',action='store_true');p.add_argument('--check',action='store_true');args=p.parse_args()
    schema,registry=read(SCHEMA),read(REGISTRY)
    data,errors,locations=load_book(args.workbook,schema)
    if not errors:errors+=validate(data,schema,registry)
    if not errors and args.migration_check:errors+=migration_check(data,registry)
    files=None
    if not errors:
        try:files=outputs(data,schema,registry,args.workbook,args.target,args.root_id)
        except (ValueError,StopIteration) as e:errors.append(str(e))
    report={'schema_version':schema['schema_version'],'errors':errors,'counts':{k:len(v) for k,v in data.items()},'pending':sum(r['value_status']=='Pending' for r in data.get('ValueReview',[])),'target':args.target,'myth_runtime_ready':False,'locations':locations}
    if args.report:Path(args.report).write_bytes(encoded(report))
    print(json.dumps({k:v for k,v in report.items() if k!='locations'},ensure_ascii=False,indent=2))
    if errors:return 1
    if args.out:publish(files,args.out)
    destinations={('Assets/Scripts/FrameWork/Generated/' if name.endswith('.cs') else 'Assets/Resources/GameData/Training/')+name:value for name,value in files.items()}
    if args.check:
        stale=[name for name,value in destinations.items() if not (ROOT/name).exists() or (ROOT/name).read_bytes()!=value]
        if stale:print('Generated files out of date: '+', '.join(stale));return 1
    if args.install:publish(destinations,ROOT)
    return 0

if __name__=='__main__':raise SystemExit(main())
