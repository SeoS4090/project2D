import copy
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import zipfile
import xml.etree.ElementTree as ET
import data_pipeline as p
from register_id import allocate

class PipelineTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.s=p.read(p.SCHEMA);cls.r=p.read(p.REGISTRY)
        cls.book=p.DEFAULT_BOOK
        if '무기' not in legacy_sheet_names(cls.book):cls.book=p.ROOT/'Library/GameDataMigration/MythBuildData.xlsx'
        cls.data,errors,_=p.load_book(cls.book,cls.s)
        assert not errors,errors
    def setUp(self):self.d=copy.deepcopy(self.data);self.reg=copy.deepcopy(self.r)
    def errors(self):return p.validate(self.d,self.s,self.reg)
    def test_baseline(self):self.assertEqual([],self.errors());self.assertEqual([],p.migration_check(self.d,self.r))
    def test_reordering_deterministic(self):
        for rows in self.d.values():rows.reverse()
        self.assertEqual(p.outputs(self.data,self.s,self.r,self.book,'test',1),p.outputs(self.d,self.s,self.r,self.book,'test',1))
    def test_duplicate_id(self):self.d['WeaponStage'].append(self.d['WeaponStage'][0].copy());self.assertTrue(self.errors())
    def test_int_bounds_and_types(self):
        for bad in (-1,0,2147483648,1.5,True,'1'):
            with self.subTest(bad=bad):self.d['WeaponStage'][0]['id']=bad;self.assertTrue(self.errors())
    def test_wrong_fk(self):self.d['WeaponStage'][0]['family_id']=999;self.assertTrue(self.errors())
    def test_cycle(self):self.d['WeaponStage'][0]['parent_id']=self.d['WeaponStage'][0]['id'];self.assertTrue(self.errors())
    def test_cross_family(self):self.d['WeaponStage'][0]['parent_id']=self.d['WeaponStage'][1]['id'];self.assertTrue(self.errors())
    def test_enum_invalid(self):self.d['WeaponStage'][0]['row_state']=2;self.assertTrue(self.errors())
    def test_pending_not_zero(self):self.d['EffectDamage'][0]['damage_coefficient_ratio']=0;self.assertTrue(self.errors())
    def test_review_required(self):self.d['ValueReview']=[r for r in self.d['ValueReview'] if r['table_name']!='MovementProfile'];self.assertTrue(self.errors())
    def test_release_rejects_test(self):
        with self.assertRaises(ValueError):p.closure(self.d,self.s,'release',1)
    def test_draft_dependency_rejected(self):
        self.d['ChargeProfile'][0]['row_state']='Draft'
        with self.assertRaises(ValueError):p.closure(self.d,self.s,'test',1)
    def test_unsupported_myth_activation(self):self.d['EffectDesign'][0]['row_state']='Test';self.assertTrue(self.errors())
    def test_order_gap(self):self.d['AttackStep'][1]['step_index']=4;self.assertTrue(self.errors())
    def test_empty_combo(self):self.d['AttackStep']=[];self.assertTrue(self.errors())
    def test_no_id_reuse(self):
        self.reg['retired']['WeaponStage']=[1000]
        self.assertEqual(1001,allocate(self.reg,'WeaponStage','new_weapon'))
    def test_rename_keeps_id(self):
        r=self.d['WeaponStage'][0];old=r['code_name'];r['code_name']='renamed_weapon'
        self.reg['tables']['WeaponStage'][r['code_name']]=self.reg['tables']['WeaponStage'].pop(old)
        self.assertEqual([],self.errors())
    def test_retired_id_rejected(self):self.reg['retired']['WeaponStage']=[self.d['WeaponStage'][0]['id']];self.assertTrue(self.errors())
    def test_share_and_clone(self):
        clone=self.d['TrainingLoadout'][0].copy();clone['id']=allocate(self.reg,'TrainingLoadout','another_training_weapon');clone['code_name']='another_training_weapon';self.d['TrainingLoadout'].append(clone)
        self.assertEqual([],self.errors());self.assertEqual(1,len(p.closure(self.d,self.s,'test',clone['id'])['ComboProfile']))
    def test_failed_publication_rolls_back(self):
        with tempfile.TemporaryDirectory() as d:
            dest=Path(d);(dest/'a').write_bytes(b'old');replace=p.os.replace
            def fail(src,target):
                if str(target).endswith('b'):raise OSError('simulated storage failure')
                replace(src,target)
            with patch.object(p.os,'replace',side_effect=fail):
                with self.assertRaises(OSError):p.publish({'a':b'new','b':b'x'},dest)
            self.assertEqual(b'old',(dest/'a').read_bytes());self.assertFalse((dest/'b').exists())
    def mutated_book(self,edit):
        temp=tempfile.TemporaryDirectory();self.addCleanup(temp.cleanup);dest=Path(temp.name)/'test.xlsx'
        with zipfile.ZipFile(self.book) as src,zipfile.ZipFile(dest,'w') as out:
            items={n:src.read(n) for n in src.namelist()};edit(items)
            for n,b in items.items():out.writestr(n,b)
        return dest
    def test_formula_rejected(self):
        def edit(items):
            root=ET.fromstring(items['xl/worksheets/sheet1.xml']);c=root.find('s:sheetData/s:row/s:c',p.NS);ET.SubElement(c,'{'+p.NS['s']+'}f').text='1+1';items['xl/worksheets/sheet1.xml']=ET.tostring(root)
        _,errors,_=p.load_book(self.mutated_book(edit),self.s);self.assertTrue(any('formulas prohibited' in e for e in errors))
    def test_outside_table_rejected(self):
        def edit(items):
            root=ET.fromstring(items['xl/worksheets/sheet1.xml']);sd=root.find('s:sheetData',p.NS);r=ET.SubElement(sd,'{'+p.NS['s']+'}row',r='1201');c=ET.SubElement(r,'{'+p.NS['s']+'}c',r='A1201',t='n');ET.SubElement(c,'{'+p.NS['s']+'}v').text='123';items['xl/worksheets/sheet1.xml']=ET.tostring(root)
        _,errors,_=p.load_book(self.mutated_book(edit),self.s);self.assertTrue(any('outside Table' in e for e in errors))
    def test_past_1001_and_blank_rows(self):
        def edit(items):
            root=ET.fromstring(items['xl/worksheets/sheet1.xml']);sd=root.find('s:sheetData',p.NS);r=ET.SubElement(sd,'{'+p.NS['s']+'}row',r='1201');c=ET.SubElement(r,'{'+p.NS['s']+'}c',r='A1201',t='n');ET.SubElement(c,'{'+p.NS['s']+'}v').text='123';items['xl/worksheets/sheet1.xml']=ET.tostring(root)
            for n in items:
                if n.startswith('xl/tables/'):
                    t=ET.fromstring(items[n])
                    if t.attrib.get('name')=='WeaponStage':t.set('ref',t.attrib['ref'].split(':')[0]+':I1201');items[n]=ET.tostring(t)
        data,errors,_=p.load_book(self.mutated_book(edit),self.s);self.assertEqual([],errors);self.assertEqual(36,len(data['WeaponStage']));self.assertEqual(123,data['WeaponStage'][-1]['id'])

def legacy_sheet_names(book):
    with zipfile.ZipFile(book) as z:return [s.attrib['name'] for s in ET.fromstring(z.read('xl/workbook.xml')).findall('s:sheets/s:sheet',p.NS)]

if __name__=='__main__':unittest.main(verbosity=2)
