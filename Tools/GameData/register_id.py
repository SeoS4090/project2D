"""Explicit stable-ID allocation/rename/retirement; never tied to Excel row order."""
import argparse
import re
from data_pipeline import REGISTRY,SCHEMA,read,encoded

def allocate(registry,table,code):
    if not re.fullmatch('[a-z][a-z0-9_]*',code):raise ValueError('code_name must be lower_snake_case')
    ids=registry['tables'].setdefault(table,{})
    if code in ids:raise ValueError('code_name already registered')
    value=max([0,*ids.values(),*registry['retired'].get(table,[])])+1
    if value>2147483647:raise ValueError('Int32 exhausted')
    ids[code]=value;return value

def main():
    p=argparse.ArgumentParser();p.add_argument('table');p.add_argument('code_name');p.add_argument('--rename-to');p.add_argument('--retire',action='store_true');a=p.parse_args()
    r=read(REGISTRY);s=read(SCHEMA)
    if a.table not in s['tables'] or 'code_name' not in [f['name'] for f in s['tables'][a.table]['fields']]:raise ValueError('Not a registered content table')
    if a.rename_to and a.retire:raise ValueError('Choose rename or retire')
    if a.rename_to:
        if not re.fullmatch('[a-z][a-z0-9_]*',a.rename_to) or a.rename_to in r['tables'][a.table]:raise ValueError('Invalid/new name already exists')
        rid=r['tables'][a.table].pop(a.code_name);r['tables'][a.table][a.rename_to]=rid
    elif a.retire:
        rid=r['tables'][a.table].pop(a.code_name);r['retired'].setdefault(a.table,[]).append(rid)
    else:rid=allocate(r,a.table,a.code_name)
    staged=REGISTRY.with_suffix('.json.tmp');staged.write_bytes(encoded(r));staged.replace(REGISTRY)
    print(f'{a.table}: {rid}. Update the Excel row in the same change; never change its existing ID.')
if __name__=='__main__':main()
