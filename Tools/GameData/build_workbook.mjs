// Author the six-sheet workbook using the supported artifact-tool API.
// NODE_PATH must point at the installed @oai/artifact-tool dependencies.
import fs from 'node:fs/promises';
import path from 'node:path';
import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';
const require=createRequire(import.meta.url);
const {Workbook,SpreadsheetFile}=await import(pathToFileURL(require.resolve('@oai/artifact-tool')).href);
const schema=JSON.parse(await fs.readFile('Tools/GameData/schema_v1.json','utf8'));
const data=JSON.parse(await fs.readFile(process.argv[2]??'Library/GameDataMigration/authoring.json','utf8'));
const output=process.argv[3]??'Library/GameDataMigration/MythBuildData.xlsx';
const wb=Workbook.create();
const col=n=>{let s='';for(n++;n;n=Math.floor((n-1)/26))s=String.fromCharCode(65+(n-1)%26)+s;return s;};
const positions={};
for(const name of schema.sheets){
  const sheet=wb.worksheets.add(name); sheet.showGridLines=false; sheet.freezePanes.freezeRows(5);
  let offset=0; const locations=[], rowHeights=new Map();
  for(const [table,spec] of Object.entries(schema.tables).filter(([,s])=>s.sheet===name)){
    const rows=data[table], fields=spec.fields, end=offset+fields.length-1;
    const range=`${col(offset)}5:${col(end)}${Math.max(6,5+rows.length)}`;
    positions[table]={sheet:name,range}; locations.push(`${table}: ${col(offset)}5`);
    sheet.getRange(`${col(offset)}1:${col(end)}2`).format.fill='#243E55';
    const titleColumn=col(offset+1);
    sheet.getRange(`${titleColumn}1`).values=[[table]];
    sheet.getRange(`${titleColumn}1`).format.font={name:'Arial',size:12,bold:true,color:'#FFFFFF'};
    sheet.getRange(`${titleColumn}2`).values=[[spec.runtime?'Test / 훈련소 임시값':'작성 데이터 / 빈칸은 미정']];
    sheet.getRange(`${col(offset)}2:${col(end)}2`).format.font={name:'Arial',size:10,color:'#FFFFFF'};
    sheet.getRange(range).values=[fields.map(f=>f.name),...rows.map(r=>fields.map(f=>r[f.name]??null)),...(rows.length?[]:[fields.map(()=>null)])];
    const t=sheet.tables.add(range,true,table); t.style='TableStyleMedium2';
    sheet.getRange(range).format.font={name:'Arial',size:10,color:'#233849'};
    sheet.getRange(range).format.rowHeight=30;
    sheet.getRange(range).format.verticalAlignment='center';
    sheet.getRange(range).format.wrapText=true;
    sheet.getRange(`${col(offset)}5:${col(end)}5`).format={fill:'#243E55',font:{name:'Arial',size:10,bold:true,color:'#FFFFFF'},rowHeight:42,wrapText:true};
    for(let i=0;i<fields.length;i++){
      const f=fields[i], letter=col(offset+i), input=sheet.getRange(`${letter}6:${letter}${Math.max(6,5+rows.length)}`);
      const prose=/description|failure_policy|theme$|myth_basis|adaptation|design_effect|remaining|completion|value_basis|text$/.test(f.name);
      const width=prose?46:['code_name','name_key','table_name','field_name'].includes(f.name)?32:Math.max(15,Math.min(27,f.name.length+2));
      sheet.getRange(`${letter}1:${letter}${Math.max(6,5+rows.length)}`).format.columnWidth=width;
      if(['int','id'].includes(f.type))input.setNumberFormat('0');
      if(f.type==='float')input.setNumberFormat('0.###');
      if(schema.enums[f.type])input.dataValidation={rule:{type:'list',values:Object.keys(schema.enums[f.type])}};
      if(f.type==='id')input.dataValidation={rule:{type:'whole',operator:'between',formula1:1,formula2:2147483647}};
      for(let r=0;r<rows.length;r++){
        const text=String(rows[r][f.name]??'');
        const units=[...text].reduce((n,c)=>n+(c.charCodeAt(0)>255?2:1),0);
        rowHeights.set(r+6,Math.max(rowHeights.get(r+6)??30,Math.ceil(units/Math.max(10,width-3))*16+12));
      }
      if(['float','int'].includes(f.type)){
        for(let r=0;r<rows.length;r++)if(rows[r][f.name]==null)sheet.getRange(`${letter}${r+6}`).format.fill='#FFF0C2';
      }
    }
    offset=end+3;
  }
  sheet.getRange('A3').values=[[`${name} | data-1.0 | 표 이름으로 이동(Ctrl+G) · 표 내부에 행 추가 · 노란 빈칸=검토 필요`]];
  sheet.getRange('A3').format.font={name:'Arial',size:10,color:'#6A4B13'};
  // Navigation stays outside data tables and is never ingested as game data.
  for(let i=0;i<locations.length;i++)sheet.getRange(`${col(i*3)}4`).values=[[locations[i]]];
  for(const [row,height] of rowHeights)sheet.getRange(`A${row}:${col(offset-3)}${row}`).format.rowHeight=height;
  sheet.getRange('A1:A2').format.rowHeight=24;
}
wb.recalculate();
await fs.mkdir(path.dirname(output),{recursive:true});
await (await SpreadsheetFile.exportXlsx(wb)).save(output);
await fs.writeFile('Library/GameDataMigration/layout.json',JSON.stringify(positions,null,2));
for(const name of schema.sheets){
  const preview=await wb.render({sheetName:name,range:'A1:F11',scale:1,format:'png'});
  await fs.writeFile(`Library/GameDataMigration/${name}.png`,new Uint8Array(await preview.arrayBuffer()));
}
console.log(JSON.stringify({output,sheets:schema.sheets,tables:Object.keys(positions).length}));
