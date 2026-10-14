"""One-time, lossless data-0.1 -> data-1.0 authoring payload. Never reassign published IDs."""
import json
import shutil
import hashlib
from pathlib import Path
import validate_myth_workbook as legacy

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
BOOK = ROOT / 'Assets/Documentation/RoguelikeDesign/09 게임데이터/MythBuildData.xlsx'
OUT = ROOT / 'Library/GameDataMigration'

def dump(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

def main():
    archive = HERE / 'Legacy/MythBuildData.v0.xlsx'
    if not archive.exists():
        archive.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(BOOK, archive)
    old, errors = legacy.load(archive, json.loads((HERE/'myth_schema.json').read_text()))
    if errors:
        raise ValueError(errors)
    schema = {'schema_version':'data-1.0', 'generator_version':1,
              'sheets':['무기','전투','효과','빌드','공통','검증'], 'enums':{}, 'tables':{}}
    data, registry, mappings = {}, {}, []
    def enum(name, names):
        schema['enums'][name] = {v:i+1 for i,v in enumerate(names)}
    enum('RowState',['Draft','Test','Release','Retired'])
    enum('ValueStatus',['Pending','Provisional','Fixed'])
    enum('ImplementationStatus',['Pending','Implemented'])
    enum('EffectLayer',['Base','Resonance'])
    enum('AttackMode',['Combo','Magazine'])
    enum('SpecialAction',['ChargeAttack','Parry','Guard','Reload'])
    enum('ApplyPolicy',['ReplaceParentBase'])
    enum('CardCategory',['General','Divine'])
    enum('AccessPolicy',['None','OwnDivinity'])
    def f(name, typ='string', required=True, ref=None, minimum=None, maximum=None):
        d={'name':name,'type':typ,'required':required}
        if ref: d['ref']=ref
        if minimum is not None:d['min']=minimum
        if maximum is not None:d['max']=maximum
        return d
    def table(name, sheet, fields, rows, key=None, runtime=False):
        schema['tables'][name]={'sheet':sheet,'fields':fields,'key':key or ['id'],'runtime':runtime}
        data[name]=rows
    def ids(oldname,newname,key):
        rows=old[oldname]
        dest=registry.setdefault(newname,{})
        for r in rows:
            code=r[key]; dest[code]=len(dest)+1
            mappings.append({'old_table':oldname,'old_id':code,'table':newname,'id':dest[code]})
        return dest
    for a,b,k in [('Culture','Culture','culture_id'),('Divinity','Divinity','divinity_id'),
                  ('WeaponFamily','WeaponFamily','family_id'),('AttackPattern','ActionDesign','pattern_id'),
                  ('Weapon','WeaponStage','weapon_id'),('Upgrade','WeaponStage','upgrade_id'),
                  ('Effect','EffectDesign','effect_id'),('CardCandidate','CardCandidate','card_id')]:ids(a,b,k)
    def lookup(table,code):return registry[table][code] if code else None
    ident=[f('id','id'),f('code_name')]
    state=f('row_state','RowState')
    table('Culture','공통',ident+[f('name_ko')],
          [dict(id=lookup('Culture',r['culture_id']),code_name=r['culture_id'],name_ko=r['name_ko']) for r in old['Culture']])
    table('WeaponFamily','공통',ident+[f('name_key',ref='Text.name_key')],
          [dict(id=lookup('WeaponFamily',r['family_id']),code_name=r['family_id'],name_key=r['name_key']) for r in old['WeaponFamily']])
    for name,sheet,oldid,refcols,enumcols,statuscols in [
        ('Divinity','빌드','divinity_id',{'culture_id':'Culture'},{},['condition_status']),
        ('CardCandidate','빌드','card_id',{'theme_divinity_id':'Divinity'},
         {'category':('CardCategory',{'general':'General','divinity':'Divine'}),
          'access_policy':('AccessPolicy',{'none':'None','selected_divinity':'OwnDivinity'})},['effect_status'])]:
        rows=[]
        for r in old[name]:
            n={'id':lookup(name,r[oldid]),'code_name':r[oldid]}
            for k,v in r.items():
                if k in (oldid,'enabled'):continue
                n[k]=lookup(refcols[k],v) if k in refcols else enumcols[k][1][v] if k in enumcols else 'Pending' if k in statuscols else v
            n['row_state']='Draft'; rows.append(n)
        fields=ident+[f(k, 'id' if k in refcols else enumcols[k][0] if k in enumcols else 'ImplementationStatus' if k in statuscols else 'string',ref=refcols.get(k) or ('Text.name_key' if k=='name_key' else None)) for k in rows[0] if k not in ('id','code_name','row_state')]+[state]
        table(name,sheet,fields,rows)
    table('ActionDesign','전투',ident+[f('family_id','id',ref='WeaponFamily'),f('attack_mode','AttackMode'),f('special_action','SpecialAction'),f('action_description'),f('implementation_status','ImplementationStatus'),state],
          [dict(id=lookup('ActionDesign',r['pattern_id']),code_name=r['pattern_id'],family_id=lookup('WeaponFamily',r['family_id']),attack_mode={'combo':'Combo','magazine':'Magazine'}[r['attack_mode']],special_action={'charge_attack':'ChargeAttack','parry':'Parry','guard':'Guard','reload':'Reload'}[r['special_action']],action_description=r['action_description'],implementation_status='Pending',row_state='Draft') for r in old['AttackPattern']])
    parents={r['to_upgrade_id']:r['from_id'] for r in old['ProgressionEdge']}
    family_action={r['family_id']:lookup('ActionDesign',r['attack_pattern_id']) for r in old['Weapon']}
    stages=[]
    for r in old['Weapon']+old['Upgrade']:
        code=r.get('weapon_id',r.get('upgrade_id')); fam=r.get('family_id',r.get('weapon_family_id'))
        stages.append(dict(id=lookup('WeaponStage',code),code_name=code,name_key=r['name_key'],parent_id=lookup('WeaponStage',parents.get(code)),family_id=lookup('WeaponFamily',fam),action_design_id=family_action[fam],design_key=r['design_key'],apply_policy='ReplaceParentBase',row_state='Draft'))
    table('WeaponStage','무기',ident+[f('name_key',ref='Text.name_key'),f('parent_id','id',False,'WeaponStage'),f('family_id','id',ref='WeaponFamily'),f('action_design_id','id',ref='ActionDesign'),f('design_key'),f('apply_policy','ApplyPolicy'),state],stages)
    table('EffectDesign','효과',ident+[f('layer','EffectLayer'),f('profile_key'),f('description'),f('failure_policy'),f('implementation_status','ImplementationStatus'),state],
          [dict(id=lookup('EffectDesign',r['effect_id']),code_name=r['effect_id'],layer=r['layer'].title(),profile_key=r['profile_key'],description=r['description'],failure_policy=r['failure_policy'],implementation_status='Pending',row_state='Draft') for r in old['Effect']])
    table('WeaponEffect','무기',[f('stage_id','id',ref='WeaponStage'),f('order_index','int',minimum=1),f('effect_id','id',ref='EffectDesign'),f('divinity_id','id',False,'Divinity'),f('min_divinity_stage','int',False,minimum=1)],
          [dict(stage_id=lookup('WeaponStage',r['upgrade_id']),order_index=r['order_index'],effect_id=lookup('EffectDesign',r['effect_id']),divinity_id=lookup('Divinity',r['divinity_id']),min_divinity_stage=r['min_divinity_stage']) for r in old['UpgradeEffect']],['stage_id','order_index'])
    # Typed draft facets preserve unresolved narrative intent, without inventing executable effects.
    facets={'EffectDamage':['damage_coefficient_ratio'], 'EffectTiming':['duration_sec','cooldown_sec','delay_sec','timing_window_sec'],
            'EffectSpace':['radius_units','range_units','move_distance_units'],
            'EffectProjectile':['projectile_speed_units_per_sec','return_timeout_sec','max_hits_per_leg_count'],
            'EffectLimit':['max_stack_count','max_target_count','max_area_count'], 'EffectSlow':['slow_ratio'],
            'WeaponBalance':['base_attack','mana_cost','attack_move_speed_ratio'],
            'ComboBalance':['combo_end_gap_sec'],'MagazineBalance':['magazine_capacity','reload_sec'],'UpgradeCost':['cost_amount']}
    params={r['parameter_id']:r for r in old['ParameterDefinition']}
    reviews=[]; numeric_map=[]
    for name,columns in facets.items():
        effect=name.startswith('Effect'); owner='EffectDesign' if effect else 'WeaponStage'
        rows={}
        for r in old['NumericValue']:
            if (r['owner_table']=='Effect')!=effect or r['parameter_id'] not in columns:continue
            rid=lookup(owner,r['owner_id']); row=rows.setdefault(rid,dict(owner_id=rid,**{k:None for k in columns}))
            row[r['parameter_id']]=r['number_value']
            reviews.append(dict(table_name=name,owner_id=rid,field_name=r['parameter_id'],value_status=r['value_status'].title(),value_basis=r['value_basis']))
            numeric_map.append(dict(old_table=r['owner_table'],old_id=r['owner_id'],parameter=r['parameter_id'],table=name,id=rid,field=r['parameter_id']))
        fields=[f('owner_id','id',ref=owner)]
        for k in columns:
            p=params[k]; field=f(k,'int' if p['value_type']=='integer' else 'float',False,minimum=p['min_value'],maximum=p['max_value']); field.update(unit=p['unit'],meaning=p['meaning'],allow_zero=p['allow_zero']);fields.append(field)
        table(name,'효과' if effect else '무기',fields,list(rows.values()),['owner_id'])
    table('ValueReview','검증',[f('table_name'),f('owner_id','id'),f('field_name'),f('value_status','ValueStatus'),f('value_basis')],reviews,['table_name','owner_id','field_name'])
    table('Text','공통',[f('name_key'),f('locale'),f('text')],old['Text'],['name_key','locale'])
    table('PendingWork','검증',[f(k) for k in old['Pending'][0]],old['Pending'],['work_id'])
    table('StageLabel','공통',[f('stage_domain'),f('stage_index','int',minimum=1),f('name_ko')],old['StageDefinition'],['stage_domain','stage_index'])
    # Existing training controller tuning only; no myth value promoted to a balance decision.
    runtime={
        'MovementProfile':dict(move_speed=5.,dash_speed=13.,dash_duration_sec=.16,dash_recharge_sec=1.1,max_dash_charges=2),
        'ManaProfile':dict(max_mana=100.,regen_per_sec=8.),
        'ComboProfile':dict(cooldown_sec=.42,link_window_sec=.22,end_gap_sec=.28,break_gap_sec=.08),
        'ChargeProfile':dict(max_charge_sec=1.4,move_ratio=.45,windup_sec=.08,active_sec=.48,recovery_sec=.2,damage_min=35.,damage_max=75.,range_units=1.65,mana_cost=20.)}
    for name,values in runtime.items():
        registry[name]={'training_'+name.lower():1}
        table(name,'전투',ident+[f(k,'int' if isinstance(v,int) else 'float',minimum=0 if k=='regen_per_sec' else .000001) for k,v in values.items()]+[state],
              [dict(id=1,code_name='training_'+name.lower(),**values,row_state='Test')],runtime=True)
    registry['TrainingLoadout']={'training_greatsword':1}
    table('TrainingLoadout','전투',ident+[f('movement_id','id',ref='MovementProfile'),f('mana_id','id',ref='ManaProfile'),f('combo_id','id',ref='ComboProfile'),f('charge_id','id',ref='ChargeProfile'),state],
          [dict(id=1,code_name='training_greatsword',movement_id=1,mana_id=1,combo_id=1,charge_id=1,row_state='Test')],runtime=True)
    table('AttackStep','전투',[f('combo_id','id',ref='ComboProfile'),f('step_index','int',minimum=1),f('clip'),f('windup_sec','float',minimum=.000001),f('active_sec','float',minimum=.000001),f('recovery_sec','float',minimum=.000001),f('damage','float',minimum=0),f('range_units','float',minimum=.000001),f('half_angle_deg','float',minimum=0,maximum=180)],
          [dict(combo_id=1,step_index=i+1,clip=c,windup_sec=.08,active_sec=.12,recovery_sec=.14,damage=25.,range_units=2.3,half_angle_deg=58.) for i,c in enumerate(['attack','attack_reverse','attack_heavy'])],['combo_id','step_index'],True)
    for name,spec in schema['tables'].items():
        if not spec['runtime']:continue
        for row in data[name]:
            for field in spec['fields']:
                if field['type'] not in ('int','float') or field['name']=='step_index':continue
                reviews.append(dict(table_name=name,owner_id=row.get('id',row.get('combo_id')),field_name=(str(row['step_index'])+'.' if name=='AttackStep' else '')+field['name'],value_status='Provisional',value_basis='TrainingGround scene + TrainingPlayerController existing provisional tuning; 2026-10-12'))
    table('EnumGuide','공통',[f('enum_name'),f('member_name'),f('number','int',minimum=1)],
          [dict(enum_name=e,member_name=k,number=v) for e,vs in schema['enums'].items() for k,v in vs.items()],['enum_name','member_name'])
    reg={'schema_version':'data-1.0','tables':registry,'retired':{},'legacy_mapping':mappings,'numeric_mapping':numeric_map,
         'legacy_sha256':hashlib.sha256(archive.read_bytes()).hexdigest()}
    regpath=HERE/'id_registry.json'
    if regpath.exists() and json.loads(regpath.read_text(encoding='utf-8'))!=reg:
        raise ValueError('Existing registry differs. Migration must not reallocate published IDs.')
    dump(regpath,reg);dump(HERE/'schema_v1.json',schema);dump(OUT/'authoring.json',data)
    print('Prepared',len(stages),'stages;',len(numeric_map),'legacy numeric reviews;',len(schema['tables']),'named tables')

if __name__=='__main__': main()
