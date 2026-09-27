from pathlib import Path
import re, json, hashlib
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

ROOT = Path(__file__).resolve().parents[1]
DOCS = ROOT / 'docs'
ES = DOCS / 'CHANGELOG.es.md'
def next_revision():
    revisions = [int(m.group(1)) for p in DOCS.glob('CalradiaForge-Registro-Mejoras-Rev*.docx')
                 if (m := re.fullmatch(r'CalradiaForge-Registro-Mejoras-Rev(\d{3})\.docx', p.name))]
    return max(revisions, default=0) + 1

REV = next_revision()
OUT = DOCS / f'CalradiaForge-Registro-Mejoras-Rev{REV:03}.docx'
PROTECTED = DOCS / f'.CalradiaForge-Registro-Mejoras-Rev{REV:03}.protected.docx'
LEDGER = DOCS / 'CalradiaForge-Registro-Mejoras.integrity.jsonl'

# These sections are present in the canonical English log but not in its Spanish counterpart.
SUPPLEMENTS = {
 '0.1.0': ('0.1.0 — Vista previa inicial para desarrolladores', [
  'Panel Gauntlet inicial dentro del juego y aplicación cliente WPF para inspección y operación.',
  'Comunicación local por named pipes para intercambiar solicitudes y resultados entre el cliente y el módulo.',
  'Diagnósticos, registros limitados, inspección de objetos, ejemplos de pruebas y de tropas/inventario, y exportación JSON/HTML.']),
 '0.2.0': ('0.2.0 — Vista previa de desarrollo y seguridad operativa', [
  'Se etiqueta y delimita el campo nativo de búsqueda/argumentos; se comprueban su enfoque, sustitución con Ctrl+A y navegación por registros dentro del juego.',
  'Se añaden Ctrl+F para enfocar búsqueda y Ctrl+Enter para actualizar. El estado vacío explica cómo limpiar el filtro; el texto inglés/español y la limpieza se comprueban en el juego.',
  'Se crean servicios compartidos versionados con propietario, contratos, comprobaciones de hilo y handles con ciclo de vida. El SDK inicia en el primer update para no capturar el hilo de carga.',
  'Las escrituras de registros se serializan. Si falla un reemplazo se conserva el archivo anterior y se informa operación, ruta y HRESULT; se prueba la recuperación ante un bloqueo controlado.',
  'Se añaden Ctrl+1–8 y Ctrl+Izquierda/Derecha para recorrer secciones, más un pie localizado; solo navegan información mientras el panel está abierto.',
  'El idioma nativo sigue los recursos de traducción de Bannerlord, sin interruptor ni lista cerrada; otros paquetes pueden aportar los mismos IDs.',
  'Se añade una suite de regresión de PipeClient sobre named pipes locales, con reinicio del interlocutor y rechazo del protocolo.',
  'Se muestra orden de dependencias, módulos bloqueados y advertencias de versión; la sugerencia no escribe ajustes del launcher.',
  'Se habilitan lotes de hasta 50 pruebas con validación previa y detención al primer fallo.',
  'Se añade consulta y eliminación de snapshots en ambas interfaces.',
  'Los informes importados se exploran sin conexión y se exportan sin reemplazarlos con una sesión conectada.',
  'Se añade captura explícita en vivo y secciones estructuradas en HTML.',
  'Se rediseña Desktop con navegación persistente, acciones contextuales, entradas etiquetadas, estado ocupado y escala de texto.',
  'Los metadatos de extensiones se congelan al registrarse; los resultados sobreviven a errores del registro y se corrigen la propiedad de cancelación y las carreras de reconexión.',
  'La revisión XML de activos del motor se distingue de un defecto genérico confirmado.',
  'El inglés se mantiene como idioma fuente y el español como traducción secundaria.',
  'La batalla personalizada nativa usa BasicBattleAgentOrigin en vez de un origen exclusivo de campaña; se verifica la ejecución y la eliminación posterior de la colección de misión.',
  'Se corrige la interceptación de clics por etiquetas de botones Gauntlet; se verifican Tests, idioma, exportación y Cerrar en el juego instalado.',
  'Se evita acceder a recursos de pantalla/capas ya finalizados al cerrar con el panel abierto. Una batalla con RBM/Harmony reprodujo el fallo; la salida corregida pasa la misma comprobación.',
  'Se verifica la restauración de inventario en una campaña de prueba copiada y se carga esa copia sin Forge; la evidencia aplica solo a ese guardado.',
  'Los análisis sin conexión permanecen en un espacio de informes propio, con la carpeta analizada como origen.',
  'VALIDATION.md separa compilación, regresión, verificaciones nativas y pendientes; esta vista previa no se certifica para distribución pública.']),
 '4.1.0': ('4.1.0 — Registro de errores, memoria de agentes y API local', [
  'Los errores de callbacks ForgeCampaignEvents dejan de descartarse: se registran mediante ForgeApi.Logger.LogError y aparecen en los registros de Forge.',
  'ForgeAgentMemory.Semantic.Upsert acepta TTL opcional; las entradas vencidas se retiran en la siguiente consulta Get/GetKeys.',
  'Semantic.GetKeys(agentId) enumera claves activas, excluyendo hechos vencidos.',
  'Episodic.Count y Episodic.TotalCount cuentan episodios de un tipo o de todas las categorías.',
  'Procedural.GetTaskNames(agentId) enumera tareas o habilidades guardadas.',
  'ForgeLocalApi añade /info con versión/SDK/estado y /agents con estadísticas JSON sin revelar los datos de memoria.',
  'Las respuestas locales incorporan CORS para paneles web locales.',
  'KeepAlive=false y la atención de cada petición en ThreadPool evitan conexiones retenidas y liberan el bucle de aceptación.',
  'ObjectDisposedException queda controlada en llamadas Start repetidas o posteriores a Dispose.',
  'ForgeApi.Version pasa a 5 y SuiteInfo 4.1.0 se sincroniza en Directory.Build.props y cuatro manifiestos.',
  'Se añaden seis pruebas de TTL, memoria, endpoints/CORS, registro de errores y versión; el changelog reporta al menos 137 pruebas.']),
 '11.0.0': ('11.0.0 — Ciclo de vida seguro y constructores de contenido', [
  'ForgeCampaignEvents ofrece Unsubscribe y ClearSubscribers para separar delegados estáticos al cerrar campañas o cambiar de escena.',
  'CampaignVariableInspector limpia variables observadas y oyentes de snapshots al terminar o descargar una sesión.',
  'ForgeData añade RemoveForgeData y HasForgeData para no retener entidades eliminadas, agentes muertos o grupos disueltos.',
  'ForgeTroopBuilder valida edad entera, ranuras de equipo, equipamiento civil y prefijos de objetivos al producir NPCCharacters.xml.',
  'ForgeItemBuilder crea armas, armaduras y monturas con tipos de daño y componentes admitidos; enlaza plantillas de herrería.',
  'ForgeAudioBuilder valida categorías y formatos OGG/WAV de module_sounds.xml.',
  'ForgeQuestBuilder prepara QuestBase y diálogos con las dos llamadas SetDialogs requeridas e IDs SaveableTypeDefiner desde 2.500.000.',
  'Desktop incorpora generador de tropas/NPC con presets y comprobación de reglas XML.',
  'Desktop añade generador de objetos y herrería para armas, armaduras, monturas y plantillas modulares.',
  'Tasks 260–264 cubren constructores y guardas de memoria; el changelog registra el 100% de aprobación.']),
 '12.0.0': ('12.0.0 — Arquitectura, seguridad de guardados y organización', [
  'ForgeSaveChunker divide cadenas/JSON por debajo del límite aproximado de 31 KB del serializador y recompone los bloques al cargar.',
  'ForgeMissionLogicBuilder aplaza cambios de malla, esqueleto y física desde OnInit a OnMissionTick y organiza hooks de interacción/fin de misión.',
  'ForgeDetour registra bytes originales y permite Unpatch, UnpatchAll e IsPatched para restaurar instrucciones al retirar un desvío.',
  'ForgeUI.Clear purga registros XML, fábricas de widgets y suscriptores de paneles.',
  'OnSubModuleUnloaded limpia memoria de agentes, UI y detours entre sesiones.',
  'Gauntlet muestra versión dinámica, estado de sesión/hook y oculta contenedores contextuales vacíos; se completa paridad del comando project-wizard.',
  'Desktop agrupa 46 herramientas en cinco categorías funcionales y compacta el command deck.',
  'La búsqueda recorre niveles y etiquetas; los filtros usan identidad de objeto en vez de cadenas traducidas.',
  'Tasks 265–268 prueban fragmentación, scaffolding, restauración de detours y limpieza del registro UI; se reportan 160 pruebas.',
  'Se publican ZIP históricos 12.0.0 para paquete general, Modules y Desktop.',
  'Estas APIs describen aquella versión y no implican que cada helper siga siendo la ruta estable actual.']),
 '12.1.0': ('12.1.0 — Panel de operaciones y ajustes Gauntlet', [
  'Se incorpora un dashboard central para evitar espacios vacíos y ofrecer acceso directo a los flujos principales.',
  'Cuatro tarjetas lanzan el validador SubModule.xml, consola/log en vivo, generador C# y estudio XML Gauntlet.',
  'El filtro DASH ofrece acceso inmediato; títulos, tarjetas y atajos se localizan en cuatro idiomas.',
  'La insignia de memoria muestra heap aproximado, contexto y salud de GC.',
  'Gauntlet añade separadores, estado vacío del terminal y un botón para limpiar salida con ayuda contextual y acceso por teclado.',
  'Task 269 valida telemetría, estado vacío, limpieza y prefab; el historial reporta 161 pruebas.',
  'Se compila Release y se publican archivos 12.1.0.']),
 '12.2.0': ('12.2.0 — Navegación de escritorio y flujo sin scroll global', [
  'El marco de trabajo desactiva scroll vertical de página para mantener su disposición estable.',
  'El rail se convierte en acordeón: al expandir una categoría se contraen las hermanas; un control permite alternarlo.',
  'Un selector instantáneo ofrece dieciséis ámbitos de diagnóstico, inspección, generación, simulación, despliegue y SDK.',
  'Las migas permiten volver al Command Deck o a la categoría activa.',
  'Un historial MRU ofrece chips para las seis herramientas recientes.',
  'Zen Mode y F10 ocultan el rail para ampliar el área de edición/inspección, con un botón de restauración.',
  'Se compacta el dashboard para eliminar la barra vertical en tamaños de pantalla probados.',
  'El generador SDK puede emitir SaveableTypeDefiner y guardas para inicialización OnInit.',
  'Se localizan controles, migas, ayudas e indicadores en cuatro idiomas.',
  'Task 271 valida filtrado, acordeón, migas, recientes y XAML; el historial reporta 163 pruebas.',
  'Se generan archivos de distribución 12.2.0.']),
 '12.4.0': ('12.4.0 — Simulación, auditoría de reglas y diagnósticos del juego', [
  'Gauntlet pasa a seis áreas: Overview, Inspect, Toolkit, Weave, Simulate y Audit. La disposición de tres filas separa navegación y contenido.',
  'ForgeSimDiplomacy calcula puntuaciones de guerra, tributos de paz y estabilidad; puede inspeccionar reinos activos o comparar facciones fuera de campaña.',
  'ForgeSimSettlements evalúa lealtad, seguridad, proporción guarnición/milicia y riesgo de rebelión.',
  'ForgeSimEconomy calcula elasticidad de precios, rendimiento de talleres y márgenes de callejones.',
  'ForgeSimTactics estima cargas de caballería, resistencia de picas, impacto en moral y brecha de murallas.',
  'ForgeSimProgression calcula curvas de aprendizaje, hitos de renombre y adecuación de roles/perks.',
  'ForgeRuleAuditor aplica 36 reglas a módulos: namespaces Campaign, IDs SaveableTypeDefiner, SetDialogs, operaciones de malla en OnInit, categorías de audio, edades de tropas y scripts de distribución.',
  'ForgeModelAudit revisa GameModels y decoradores; ForgeDumpDiagnostics informa salud de memoria y sistema.',
  'Los botones quick-run siguen el área activa y mantienen paridad de sugerencias, ayuda, nombre, navegación y atajos.',
  'Runtime añade IPC rule-auditor para devolver resultados JSON al companion Desktop.',
  'Task 285 valida simulaciones, auditorías, XML, botones y propiedades; el changelog reporta 203 pruebas, 177 Core y 26 Desktop.',
  'Core/SDK añade simulación para campaign mechanics, combate, economía, política y UI: misiones, clima, tiempo, lealtad, heridas/plagas, caza, torneos, formaciones, daño de asedio, armadura, moral, comercio, sucesión y HUD.',
  'ModRuleAuditor también inspecciona directorios; ForgeApi agrupa Diplomacy, Settlements, Underworld, Progression, Combat, Trade, Parties y Hud.',
  'Se incorporan comandos de consola para guerra, tributos, rebelión, callejones, aprendizaje, moral, asedio, precios, transferencias y reglas.',
  'La entrada fuente bajo 12.4 contiene además una subsección con pruebas y empaquetado de 12.3.0. Se conserva como inconsistencia histórica, sin crear una versión no documentada.',
  'El mismo historial reporta pruebas y ZIP 12.4.0, además de la referencia separada a 12.3.0; son afirmaciones históricas, no una verificación actual.' ])
}

SUMMARIES = {
 '0.1.0':'Primera vista previa con panel del juego, cliente de escritorio y canal local para diagnósticos y reportes. Establece la base de inspección, pruebas y exportación.',
 '0.2.0':'Amplía el uso diario con navegación nativa, idioma automático, reconexión y reportes offline. Registra correcciones de ciclo de vida y distingue comprobaciones pendientes de resultados verificados.',
 '0.3.0':'Consolida la identidad visual, navegación y estructura de distribución; formaliza validaciones de recursos y preserva la evidencia técnica sin traducir.',
 '0.4.0':'Mantiene la independencia del módulo y agrega diagnóstico de compatibilidad opcional de solo lectura, con tolerancia a errores aislados.',
 '0.5.0':'Introduce ForgeWeave, un sistema original de eventos cooperativos con orden explícito, datos copiados, límites y compuertas de escritura.',
 '0.6.0':'Replay Lab repite una secuencia retenida con opt-in por handler, coincidencia de contexto y sin inyección de datos arbitrarios.',
 '0.6.5':'La decoración se fija en una composición alineada y acotada para no cruzar logotipos, texto ni controles.',
 '0.7.0':'ForgeWeave agrega filtros declarativos sobre datos escalares y presupuestos de tiempo por handler para ejecución y replay.',
 '0.7.1':'Desktop incorpora catálogos para los trece idiomas instalados de Bannerlord y fallback explícito al inglés.',
 '0.7.2':'La marca heráldica y la navegación se reconstruyen con una composición táctica coherente que conserva escala y foco.',
 '0.7.3':'Escudo, retícula y disco de mando comparten una cuadrícula vectorial; la línea de despliegue se acota a su sección.',
 '0.7.4':'La navegación y la marca mantienen proporciones al 200%; los adornos permanecen dentro de su columna.',
 '0.7.5':'Ajuste óptico pequeño del disco de mando para compensar el redondeo a alta densidad.',
 '0.7.6':'La retícula se fija en la intersección geométrica y crece la suite corta de regresión Desktop.',
 '4.1.0':'Mejora observabilidad, memoria de agentes y robustez de la API local con estado y estadísticas sin exponer datos privados.',
 '11.0.0':'Los servicios ganan operaciones explícitas de limpieza y constructores que validan esquemas antes de producir contenido XML.',
 '12.0.0':'Organiza el ciclo de vida, protege guardados grandes y clasifica herramientas. Las APIs se describen como registro histórico, no como promesa de estabilidad actual.',
 '12.1.0':'El escritorio recibe un dashboard operativo y Gauntlet incorpora estados vacíos, telemetría y limpieza de salida.',
 '12.2.0':'Reduce navegación global por scroll y acelera el acceso a categorías, recientes y espacio de trabajo amplio.',
 '12.4.0':'Añade simulación multi-dominio y auditorías estructurales. Las estimaciones no equivalen a modificar o validar una campaña real; se conserva la discrepancia 12.3/12.4 del registro.',
 '14.0.0':'Unifica versiones y convierte acciones de auditoría en análisis con evidencia, procedencia y límites visibles.',
 '14.1.0':'Introduce MVVM interno y comandos cancelables manteniendo límites y compuertas de ForgeWeave.',
 '14.3.0':'Enruta las herramientas por definiciones declaradas y páginas bajo demanda, separando servicios de la ventana WPF.',
 '14.4.0':'Dinamiza recursos y transporte cancelable; separa interfaz traducida de evidencia técnica.',
 '14.4.1':'Corrige el arranque WPF asociado al binding de resultado y refuerza la vista de evidencia y las comprobaciones de rutas.',
 '17.0.0':'Rediseña Desktop como mesa MVVM con temas, comandos contextuales, navegación por teclado y listas acotadas.',
 '18.0.0':'Agrupa el rail por áreas plegables, búsqueda y filtros sin recorrer el árbol visual.',
 '19.0.0':'Registra una integración de toolkits limitada a Desktop; los módulos permanecen independientes y la versión posterior elimina paquetes no necesarios.',
 '20.0.0':'Usa geometrías vectoriales, metadatos declarativos, inspección estática de ensamblados y generación local DocFX.',
 '21.0.0':'Expone páginas Gauntlet declarativas, inspección acotada de ensamblados y ayuda offline derivada del SDK.',
 '21.0.1':'Distingue señales estructurales de validación de payload y ofrece preflight TPAC de solo lectura.',
 '22.0.0':'Valida referencias de sprites, dimensiones y límites estructurales TPAC sin afirmar que decodifica o carga el contenido.',
 '22.0.0-follow-up':'Agrupa cambios posteriores al ZIP: planificador, comparación de inventarios y análisis FBX ASCII; el código fuente no se confunde con lo distribuido.'}

ORDER = ['0.1.0','0.2.0','0.3.0','0.4.0','0.5.0','0.6.0','0.6.5','0.7.0','0.7.1','0.7.2','0.7.3','0.7.4','0.7.5','0.7.6','4.1.0','11.0.0','12.0.0','12.1.0','12.2.0','12.4.0','14.0.0','14.1.0','14.3.0','14.4.0','14.4.1','17.0.0','18.0.0','19.0.0','20.0.0','21.0.0','21.0.1','22.0.0','22.0.0-follow-up']

def parse_sources():
    records={}
    blocks=re.split(r'(?m)^##\s+', ES.read_text(encoding='utf-8-sig'))
    for block in blocks[1:]:
        lines=block.splitlines(); heading=lines[0].strip(); body='\n'.join(lines[1:]).strip()
        match=re.search(r'(?<!\d)(\d+\.\d+\.\d+)',heading)
        if 'Seguimiento de fuentes' in heading:
            key='22.0.0-follow-up'
        elif match:
            key=match.group(1)
        else:
            raise ValueError('Encabezado de changelog sin versión reconocible: '+heading)
        if key in records: raise ValueError('Versión duplicada en el changelog: '+key)
        records[key]={'heading':heading,'body':body}
    for key,(heading,items) in SUPPLEMENTS.items():
        if key in records: raise ValueError('Se rehúsa sobrescribir el registro existente '+key)
        records[key]={'heading':heading,'body':'\n'.join('- '+x for x in items)}
    missing=[k for k in ORDER if k not in records]
    extra=[k for k in records if k not in ORDER]
    if missing or extra: raise ValueError(f'Desajuste entre changelog y catálogo. Faltan={missing}; no catalogadas={extra}')
    return records

def markdown_runs(p,text):
    pat=re.compile(r'(\*\*.*?\*\*|`[^`]+`|(?<!\*)\*[^*]+\*)'); pos=0
    for m in pat.finditer(text):
        if m.start()>pos: p.add_run(text[pos:m.start()])
        token=m.group(0)
        if token.startswith('**'):
            r=p.add_run(token[2:-2]); r.bold=True
        elif token.startswith('`'):
            r=p.add_run(token[1:-1]); r.font.name='Consolas'; r.font.size=Pt(9)
        else:
            r=p.add_run(token[1:-1]); r.italic=True
        pos=m.end()
    if pos<len(text): p.add_run(text[pos:])
    for r in p.runs:
        if not r.font.name:r.font.name='Arial'
        if not r.font.size:r.font.size=Pt(10)

def add_body(doc,body):
    for line in body.splitlines():
        s=line.strip()
        if not s or s in ('---','***'): continue
        if s.startswith('### '): doc.add_heading(s[4:],level=2); continue
        if s.startswith('#### '): doc.add_heading(s[5:],level=3); continue
        depth=len(line)-len(line.lstrip(' '))
        if s.startswith('- '):
            p=doc.add_paragraph(style='List Bullet 2' if depth>=4 else 'List Bullet')
            item=s[2:].strip()
            if 'Verifica la infografía/PDF de recursos actualizada 1.2' in item:
                item=('Verificada la infografía/PDF 1.2 con guías oficiales y comunitarias: Core/BAT respetan cancelación y señalan Camera/Light; '
                      'no reescriben FBX ni automatizan la importación. Los ZIP existentes no se reconstruyeron.')
            markdown_runs(p,item)
        elif re.match(r'^\d+[.)]\s+',s):
            # Keep source numbering as literal text so each release starts at 1.
            p=doc.add_paragraph();p.paragraph_format.left_indent=Inches(.24);p.paragraph_format.first_line_indent=Inches(-.24)
            marker=re.match(r'^(\d+[.)])\s+',s).group(1)
            r=p.add_run(marker+' ');r.bold=True;markdown_runs(p,re.sub(r'^\d+[.)]\s+','',s))
        else:
            p=doc.add_paragraph(); p.paragraph_format.space_after=Pt(5); markdown_runs(p,s)

def page_field(p):
    r=p.add_run(); b=OxmlElement('w:fldChar');b.set(qn('w:fldCharType'),'begin')
    i=OxmlElement('w:instrText');i.set(qn('xml:space'),'preserve');i.text=' PAGE '
    s=OxmlElement('w:fldChar');s.set(qn('w:fldCharType'),'separate');t=OxmlElement('w:t');t.text='1'
    e=OxmlElement('w:fldChar');e.set(qn('w:fldCharType'),'end');r._r.extend([b,i,s,t,e])
    r.font.name='Arial';r.font.size=Pt(8);r.font.color.rgb=RGBColor(88,100,94)

def main():
    existing_revisions=[p.name for p in DOCS.glob('CalradiaForge-Registro-Mejoras-Rev*.docx')
                        if re.fullmatch(r'CalradiaForge-Registro-Mejoras-Rev\d{3}\.docx',p.name)]
    if existing_revisions or LEDGER.exists():
        raise RuntimeError('Este generador crea solo el documento base. Para conservar revisiones anteriores, usa tools/append_detailed_changelog_revision.py.')
    records=parse_sources(); doc=Document(); sec=doc.sections[0]
    sec.top_margin=Inches(.62);sec.bottom_margin=Inches(.58);sec.left_margin=Inches(.82);sec.right_margin=Inches(.82)
    normal=doc.styles['Normal'];normal.font.name='Arial';normal.font.size=Pt(10);normal.font.color.rgb=RGBColor(38,45,41);normal.paragraph_format.space_after=Pt(5);normal.paragraph_format.line_spacing=1.08
    for name in ('List Bullet','List Bullet 2'):
        doc.styles[name].paragraph_format.space_after=Pt(3)
    for name,size,color in [('Title',25,(15,23,20)),('Heading 1',15,(27,63,49)),('Heading 2',11.5,(120,91,31)),('Heading 3',10.5,(41,70,58))]:
        st=doc.styles[name];st.font.name='Arial';st.font.size=Pt(size);st.font.bold=True;st.font.color.rgb=RGBColor(*color);st.paragraph_format.keep_with_next=True
    # Word's built-in Title style can carry a blue paragraph border; the document
    # design requires a clean title with no rule directly underneath.
    title_ppr=doc.styles['Title']._element.get_or_add_pPr()
    for border in title_ppr.findall(qn('w:pBdr')): title_ppr.remove(border)
    doc.add_paragraph('Registro detallado de mejoras\nCalradia Forge',style='Title')
    p=doc.add_paragraph('Historial documentado desde 0.1.0 hasta 22.0.0');p.paragraph_format.space_after=Pt(8)
    p.runs[0].font.size=Pt(12);p.runs[0].font.color.rgb=RGBColor(85,96,89)
    p=doc.add_paragraph(f'Edición {REV}  |  Idioma: español');p.runs[0].font.size=Pt(9);p.runs[0].font.color.rgb=RGBColor(120,91,31)
    p=doc.add_paragraph();p.paragraph_format.space_after=Pt(8)
    r=p.add_run('Edición base: ');r.bold=True;r.font.color.rgb=RGBColor(120,91,31)
    p.add_run('este generador solo sirve para iniciar un registro vacío. Las actualizaciones se agregan como anexos mediante tools/append_detailed_changelog_revision.py; no se vuelve a construir esta base.')
    p=doc.add_paragraph();markdown_runs(p,'Este documento reúne y amplía las mejoras asentadas en los changelogs del proyecto. Describe cada versión documentada, explica su efecto para autores de mods y usuarios de Calradia Forge, y conserva las limitaciones y estados de validación de la fuente. La última sección distingue el ZIP publicado de los cambios posteriores que existen solo en el árbol de fuentes.')
    doc.add_heading('Estado del registro',level=1)
    p=doc.add_paragraph();r=p.add_run('Última distribución registrada: ');r.bold=True;p.add_run('22.0.0. Seguimiento de fuentes del 23-09-2026: expresamente no incluido en los ZIP existentes.')
    doc.add_heading('Cómo leer este historial',level=1)
    for text in ['Los nombres técnicos, rutas, IDs, comandos y APIs se conservan para contrastarlos con el código y el SDK.','Los saltos de versión no se rellenan con versiones supuestas. Una API que figura en una versión histórica no se presenta por ello como una API estable actual.','Las pruebas y sesiones citadas son las declaradas por el changelog original; aquí no se convierten en una certificación nueva ni en una medición de rendimiento.']:
        p=doc.add_paragraph(style='List Bullet');markdown_runs(p,text)
    p=doc.add_paragraph();r=p.add_run('Fuentes. ');r.bold=True;markdown_runs(p,'docs/CHANGELOG.es.md y docs/CHANGELOG.md. Las secciones históricas que solo estaban en inglés se tradujeron para esta edición; sus funciones, límites y resultados se mantienen atribuidos al registro original.')
    doc.add_heading('Regla de anexado e integridad',level=1)
    p=doc.add_paragraph();markdown_runs(p,'Las futuras mejoras se agregan al final de una nueva revisión; las entradas anteriores no se reescriben ni se eliminan. Este Word queda protegido contra edición accidental y su huella SHA-256 se asienta en el JSONL de integridad. Para actualizarlo se crea la siguiente revisión numerada, se conservan las anteriores y se añade una nueva línea de huella enlazada al registro previo.')
    p=doc.add_paragraph();markdown_runs(p,'Límite técnico: Word y el atributo de solo lectura no impiden que el propietario o un administrador cambie o borre archivos. La huella detecta una modificación si se conserva por separado; impedir el borrado requiere almacenamiento con retención inmutable o control de acceso administrado.')
    doc.add_heading('Índice cronológico',level=1)
    for i in range(0,len(ORDER),4):
        p=doc.add_paragraph();p.paragraph_format.space_after=Pt(2);p.add_run('   •   '.join(records[k]['heading'].split('—')[0].strip() for k in ORDER[i:i+4])).font.size=Pt(9)
    for key in ORDER:
        rec=records[key];doc.add_heading(rec['heading'],level=1)
        p=doc.add_paragraph();r=p.add_run('Descripción ampliada. ');r.bold=True;r.font.color.rgb=RGBColor(120,91,31);markdown_runs(p,SUMMARIES[key])
        if key=='12.4.0':
            p=doc.add_paragraph();r=p.add_run('Nota de consistencia. ');r.bold=True;r.font.color.rgb=RGBColor(160,75,45);markdown_runs(p,'El bloque fuente bajo 12.4.0 menciona en una subsección pruebas y ZIP 12.3.0. Se conserva como aparece, sin corregirlo ni atribuirlo a una versión nueva sin evidencia adicional.')
        add_body(doc,rec['body'])
    footer=sec.footer.paragraphs[0];footer.alignment=WD_ALIGN_PARAGRAPH.CENTER
    r=footer.add_run('Calradia Forge | Registro append-only | Página ');r.font.size=Pt(8);r.font.color.rgb=RGBColor(88,100,94);page_field(footer)
    doc.core_properties.title='Registro detallado de mejoras Calradia Forge';doc.core_properties.subject='Changelog ampliado con control append-only';doc.core_properties.author='Calradia Forge';doc.core_properties.keywords='Calradia Forge, changelog, historial, mejoras'
    if OUT.exists() or PROTECTED.exists(): raise FileExistsError('No se sobrescribe una revisión anterior: '+str(OUT))
    doc.save(OUT)
    print(json.dumps({'draft':str(OUT),'sections':len(ORDER),'supplemented':len(SUPPLEMENTS)}))

if __name__=='__main__': main()
