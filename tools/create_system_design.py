"""Fill the retained System Design template without rebuilding its style system."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
from hashlib import sha256
import io, json, sys
import re
from lxml import etree as E
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[1]
VERSION = re.search(r'<CalradiaForgeVersion>([^<]+)</CalradiaForgeVersion>', (root / 'Directory.Build.props').read_text(encoding='utf-8')).group(1)
reference = Path(sys.argv[1])
contract = root / 'artifacts/system-design'
baseline = json.loads((contract / 'reference-parts.json').read_text())
ns = {'w': 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}
replacements = {
8:'Calradia Forge',9:'Developer tools architecture',21:f'Developer preview {VERSION}',24:'Project maintainer',27:'September 22, 2026',
30:'Calradia Forge project',32:'Independent review pending',34:'README.md and docs/VALIDATION.md',36:'Single-player diagnostics, inspection and guarded tests for Bannerlord 1.4.8.',
38:'Calradia Forge combines a Gauntlet panel, a Windows WPF client and a public C# SDK for module inspection and guarded tests. The preview compiles against Bannerlord 1.4.8. Native inventory restoration, troop creation and removal, and loading the tested sandbox save without Forge have been observed.',
39:'Core services need no Harmony, MCM or ButterLib. Game access and shared-library scopes use the game thread. Reports stay outside saves. The optional desktop EXE ships separately. English is primary; the panel follows the game language with English fallback.',
44:'Diagnose module folders without starting the game.',45:'Multiplayer and scene editing.',46:'Capture bounded logs, snapshots and test results.',47:'Capturing every external game failure.',48:'Support versioned C# extensions and local IPC.',49:'DLL hot reload and AI generation.',50:'Require test mode and a copied campaign for mutations.',51:'Deterministic replay of third-party systems.',
54:'Mod authors need concrete evidence when dependencies are missing, object state changes or a test fails. A folder scan can establish structural defects, but cannot establish every runtime incompatibility. The suite therefore labels uncertain findings and keeps static, automated and native validation separate.',
55:'The SDK owns extension contracts and module-owned shared libraries. Providers publish versioned interfaces; consumers require an explicit owner and compatible version. Handles expire on withdrawal, scope disposal or disconnect. Core owns test policy; the game adapter owns engine access.',
59:'Figure 1. Calradia Forge components and execution boundaries.',
90:'The desktop sends a JSON request over the current user’s named pipe.',91:'Protocol version and request ID are checked; the server limits request size and queue length.',92:'Registered extension descriptors determine context and mutation permissions.',93:'One queued request is dispatched per application tick on the game thread.',94:'Mutation permission is session-only. Scenario preparation records initial state for cleanup.',95:'The server requests cancellation after 15 seconds. Clients do not retry mutations automatically.',96:'A response returns results or an error. Bounded logs and reports preserve selected evidence.',
136:'Batch preflight checks all selected IDs and contexts before any scenario starts.',137:'Request IDs correlate replies; they are not durable deduplication keys.',138:'Results capture the selected seed, context, steps, duration and cleanup errors.',139:'Diagnostics describe inspected folders, not the launcher’s selected load order.',
140:'The versioned contracts are maintained in the source tree:',141:'src/CalradiaForge.Sdk and src/CalradiaForge.Core/Models.cs.',
145:'A pipe connection serializes requests. Game operations run on the main thread, while scans and persistence run asynchronously. Repeating a state-changing request may execute it again: clients must inspect the prior result and current state before deciding whether to retry. Extension cleanup is attempted even when preparation or execution fails.',
149:'Repeated test request',150:'Runs again if explicitly requested.',151:'Request IDs only correlate replies.',152:'Log persistence fails',153:'Keep bounded in-memory records.',154:'Expose the persistence error.',155:'Request times out',156:'Request cooperative cancellation.',157:'Do not promise rollback or automatic retry.',158:'Descriptor changes after registration',159:'Use the captured descriptor.',160:'Prevent permission changes by mutation.',
164:'The named pipe grants access to the current Windows user. It is not a boundary against that user’s other processes.',165:'Inspect a scalar-property allowlist. External messages retain their original text as evidence.',166:'No cloud account, API key or remote endpoint is required by the suite.',167:'Testing starts disabled. Campaign mutations additionally require copy confirmation.',168:'Keep 2,000 log entries and 20 session files. Exported reports are retained by the user.',
196:'Developer preview: interaction, compatibility and overhead checks remain. No store upload.',
204:'Mandatory Harmony hooks',205:'Broader interception.',206:'Adds coupling and cannot prove causality.',207:'Remote HTTP service',208:'Browser and remote access.',209:'Unnecessary exposure for a local tool.',210:'Persist suite data in saves',211:'Resume suite state with a campaign.',212:'Adds avoidable save dependencies.',213:'Automatic state-changing retries',214:'Recover from disconnects.',215:'Can repeat mutations after an uncertain result.',
218:'Does complete keyboard traversal and mixed mouse input work across campaign and mission contexts?',219:'Are all language catalogs visually and linguistically reviewed beyond the sampled languages?',220:'What overhead appears during active campaign, combat and prolonged capture?',221:'Which license and store ownership details will the maintainer select before publication?',
224:'Continue controlled integration checks before public release. Native campaign and battle examples passed; the tested saved copy loaded without Forge. These observations support this developer preview, not universal save compatibility. Keep remaining interaction and performance limits explicit in store descriptions.',
229:'Build and automated regressions',230:'Release builds cleanly; targeted tests pass.',232:'Native panel and protocol',233:'Observed render and correlated responses.',235:'Campaign and custom battle',236:'Restore state; save, reload and remove safely.',238:'Store release preparation',239:'Native gates pass; packages and listing verified.'
}

def fill(start, rows):
    for i, value in enumerate(rows): replacements[start+i] = value

fill(67,['SDK','Extensions and versioned shared libraries.','Module scopes.','Reject stale handles and duplicate IDs.',
         'Core','Validation, test policy and reports.','Bounded memory.','Return findings or structured errors.',
         'Game adapter','Gauntlet view and main-thread engine access.','Live game state.','Log exceptions and close a failed panel.',
         'Desktop','Offline scans, report browsing and pipe client.','Current report.','Keep evidence on disconnect.',
         'Session storage','Persist logs, settings and exports.','Local user data.','Expose write errors; retain memory.'])
fill(105,['Version','Integer','Yes','Protocol 1 in each envelope.',
          'Id','String','Yes','Correlates a request and response.',
          'Action','String','Request','Names an available operation.',
          'Argument','String','Optional','Action-specific query or test ID.',
          'Seed','Integer','Tests','Controls suite random values.',
          'Success','Boolean','Response','Whether the handler completed successfully.',
          'Data or Error','String','Response','Payload or actionable failure message.'])
fill(176,['Automated checks','All targeted regressions pass.','Maintainer','Required',
          'Operation timing','600 callback samples; active benchmarks pending.','Maintainer','Required',
          'Disconnect recovery','Reconnect without replaying mutations.','Maintainer','Required',
          'Package integrity','Built and packaged DLL hashes match.','Maintainer','Required',
          'Save and removal','Tested copy loads without Forge.','Maintainer','Observed'])

with ZipFile(reference) as source:
    for name, entry in baseline.items():
        assert sha256(source.read(name)).hexdigest() == entry['sha256'], name
    body = E.fromstring(source.read('word/document.xml'))
    paragraphs = body.xpath('//w:body/w:p | //w:body/w:tbl//w:p', namespaces=ns)
    for index, value in replacements.items():
        nodes = paragraphs[index].xpath('.//w:t', namespaces=ns)
        assert nodes, index
        nodes[0].text = value
        for node in nodes[1:]: node.text = ''
    # Preserve the reference's page pattern after shorter real text replaces prompts.
    for index in (89,223):
        properties=paragraphs[index].find('w:pPr',ns)
        if properties is None:
            properties=E.Element('{'+ns['w']+'}pPr');paragraphs[index].insert(0,properties)
        if properties.find('w:pageBreakBefore',ns) is None:E.SubElement(properties,'{'+ns['w']+'}pageBreakBefore')
    # Preserve each original part except documented content slots.
    parts = {name: source.read(name) for name in source.namelist()}
    parts['word/document.xml'] = E.tostring(body, xml_declaration=True, encoding='UTF-8', standalone=True)
    for part, value in [('word/footer1.xml','Calradia Forge | System Design'),('word/footnotes.xml','Native validation refers to execution inside Bannerlord, separately from compiled or simulated tests.')]:
        tree=E.fromstring(parts[part]);nodes=tree.xpath('//w:t',namespaces=ns)
        if nodes:
            nodes[0].text=value
            for node in nodes[1:]:node.text=''
        parts[part]=E.tostring(tree,xml_declaration=True,encoding='UTF-8',standalone=True)

    original=Image.open(io.BytesIO(parts['word/media/image1.png']))
    w,h=original.size
    image=Image.new('RGB',(w,h),'#F7FAFD');draw=ImageDraw.Draw(image)
    font_path='C:/Windows/Fonts/arial.ttf'
    font=ImageFont.truetype(font_path,max(14,int(w/65)))
    heading=ImageFont.truetype(font_path,max(18,int(w/48)))
    draw.rectangle((0,0,w,h*.15),fill='#0C2D49')
    draw.text((w*.04,h*.045),'Calradia Forge architecture',font=heading,fill='white')
    boxes=[(.035,.26,.23,.48,'WPF desktop\nOffline reports'),(.28,.26,.475,.48,'Named pipe\nUser access'),(.525,.26,.72,.48,'Game thread\nRuntime adapter'),(.77,.26,.965,.48,'Gauntlet panel\nNative UI'),(.28,.64,.475,.85,'Core and SDK\nTest policy'),(.525,.64,.72,.85,'Game objects\nScalar snapshots'),(.77,.64,.965,.85,'User data\nLogs and exports')]
    for x1,y1,x2,y2,label in boxes:
        box=(int(x1*w),int(y1*h),int(x2*w),int(y2*h));draw.rectangle(box,fill='white',outline='#426A8A',width=2)
        draw.multiline_text((box[0]+12,box[1]+16),label,font=font,fill='#0C2D49',spacing=6)
    for x1,y1,x2,y2 in [(.23,.37,.28,.37),(.475,.37,.525,.37),(.72,.37,.77,.37),(.377,.48,.377,.64),(.62,.48,.62,.64),(.86,.48,.86,.64)]:
        a=(int(x1*w),int(y1*h));b=(int(x2*w),int(y2*h));draw.line((a,b),fill='#3178A8',width=3)
        if y1==y2:draw.polygon([(b[0],b[1]),(b[0]-10,b[1]-6),(b[0]-10,b[1]+6)],fill='#3178A8')
        else:draw.polygon([(b[0],b[1]),(b[0]-6,b[1]-10),(b[0]+6,b[1]-10)],fill='#3178A8')
    draw.text((w*.04,h*.93),'Game mutations require context checks and explicit test permission.',font=font,fill='#426A8A')
    data=io.BytesIO();image.save(data,format='PNG');parts['word/media/image1.png']=data.getvalue()
    editable={'word/document.xml','word/footer1.xml','word/footnotes.xml','word/media/image1.png'}
    for name in parts:
        if name not in editable:assert sha256(parts[name]).hexdigest()==baseline[name]['sha256'],name
    output=root/'docs/CalradiaForge-SystemDesign.docx'
    with ZipFile(output,'w',ZIP_DEFLATED) as target:
        for name,data in parts.items():target.writestr(name,data)
    # Catch forgotten template prompts without depending only on rendering.
    text=' '.join(E.fromstring(parts['word/document.xml']).xpath('//w:t/text()',namespaces=ns))
    assert '[' not in text, 'Unfilled template placeholder'
    print(output)


