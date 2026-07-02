import { useEffect, useMemo, useState } from "react";

// Dados de exemplo — mostrados quando o Notion ainda não está conectado,
// pra dar pra ver o painel "vivo". O uso real puxa da SUA database do Notion.
const SAMPLE = [
  { id: "s1", name: "Cálculo I", category: "Faculdade", status: "Estudando", progress: 62 },
  { id: "s2", name: "Banco de Dados", category: "Faculdade", status: "Estudando", progress: 78 },
  { id: "s3", name: "POO em C#", category: "Faculdade", status: "Concluída", progress: 100 },
  { id: "s4", name: "Estrutura de Dados", category: "Faculdade", status: "A revisar", progress: 40 },
  { id: "s5", name: "AWS Cloud Practitioner", category: "Certificações", status: "Estudando", progress: 55 },
  { id: "s6", name: "Inglês — Advanced", category: "Pessoal", status: "Concluída", progress: 100 },
  { id: "s7", name: "Design Patterns", category: "Pessoal", status: "A revisar", progress: 30 },
];

export default function App() {
  const [state, setState] = useState({ loading: true });

  useEffect(() => {
    fetch("/api/subjects")
      .then((r) => r.json())
      .then((d) => setState({ loading: false, data: d }))
      .catch((e) => setState({ loading: false, error: e.message }));
  }, []);

  const view = useMemo(() => {
    if (state.loading) return { kind: "loading" };
    if (state.error) return { kind: "error", text: state.error };
    const d = state.data;
    if (d.error) return { kind: "notion-error", text: d.error };
    if (d.configured === false) return { kind: "demo", subjects: SAMPLE };
    const subjects = d.subjects ?? [];
    return subjects.length ? { kind: "live", subjects } : { kind: "empty" };
  }, [state]);

  return (
    <div className="page">
      <div className="sheet">
        <Header />
        <Body view={view} />
        <footer className="foot">
          <span>C#/.NET&nbsp;+&nbsp;React&nbsp;+&nbsp;Notion</span>
          <a href="https://github.com/davievan12/estudos-dash">github.com/davievan12/estudos-dash</a>
        </footer>
      </div>
    </div>
  );
}

function Header() {
  return (
    <header className="head">
      <div className="kicker">caderno de estudos</div>
      <h1 className="title">
        Estudos<mark className="hl hl-y">Dash</mark>
      </h1>
      <p className="lede">
        Suas matérias, status e progresso — puxados do seu Notion, num lugar só.
      </p>
    </header>
  );
}

function Body({ view }) {
  if (view.kind === "loading") return <Note>Abrindo o caderno…</Note>;
  if (view.kind === "error") return <Note tone="warn">A API não respondeu: {view.text}</Note>;
  if (view.kind === "notion-error")
    return <Note tone="warn">O Notion recusou a consulta: <code>{view.text}</code>. Confira o token e o acesso à database.</Note>;
  if (view.kind === "empty") return <Note>Sua database está vazia — cadastre uma matéria no Notion pra ela aparecer aqui.</Note>;

  const subjects = view.subjects;
  const demo = view.kind === "demo";
  return (
    <>
      <Snapshot subjects={subjects} />
      {demo && (
        <div className="demo-tag" role="status">
          dados de exemplo — <a href="#config" onClick={(e) => e.preventDefault()}>conecte seu Notion</a> pra ver os seus
        </div>
      )}
      <Subjects subjects={subjects} />
      {demo && <Setup />}
    </>
  );
}

function Snapshot({ subjects }) {
  const total = subjects.length;
  const done = subjects.filter((s) => /conclu|feito|done|pronto/i.test(s.status || "")).length;
  const withP = subjects.filter((s) => s.progress != null);
  const avg = withP.length ? Math.round(withP.reduce((a, s) => a + s.progress, 0) / withP.length) : null;

  return (
    <p className="snapshot">
      <b>{total}</b> matérias&nbsp;&nbsp;·&nbsp;&nbsp;
      <b>{done}</b> concluídas&nbsp;&nbsp;·&nbsp;&nbsp;
      <b>{avg == null ? "—" : avg + "%"}</b> de progresso médio
    </p>
  );
}

function Subjects({ subjects }) {
  const groups = useMemo(() => {
    const g = {};
    for (const s of subjects) (g[s.category || "Avulsas"] ??= []).push(s);
    return Object.entries(g);
  }, [subjects]);

  return (
    <div className="matters">
      {groups.map(([cat, items]) => (
        <section className="matter" key={cat}>
          <div className="tab">
            <span className="tab-name">{cat}</span>
            <span className="tab-count">{items.length}</span>
          </div>
          <ul className="entries">
            {items.map((s) => <Entry key={s.id} s={s} />)}
          </ul>
        </section>
      ))}
    </div>
  );
}

const STATUS_TONE = (status = "") => {
  const t = status.toLowerCase();
  if (/conclu|feito|done|pronto/.test(t)) return "g"; // verde
  if (/revis|atras|pend|refazer/.test(t)) return "p"; // rosa
  return "y"; // amarelo (estudando/default)
};

function Entry({ s }) {
  const [mounted, setMounted] = useState(false);
  useEffect(() => {
    const id = requestAnimationFrame(() => setMounted(true));
    return () => cancelAnimationFrame(id);
  }, []);
  const pct = s.progress != null ? Math.max(0, Math.min(100, s.progress)) : null;

  return (
    <li className="entry">
      <div className="entry-top">
        <span className="entry-name">
          {s.url ? <a href={s.url} target="_blank" rel="noreferrer">{s.name}</a> : s.name}
        </span>
        {s.status && <span className={`tag hl hl-${STATUS_TONE(s.status)}`}>{s.status}</span>}
      </div>
      {pct != null && (
        <div className="prog">
          <div className={`stroke stroke-${STATUS_TONE(s.status)}`} style={{ width: mounted ? pct + "%" : 0 }} />
          <span className="pct">{Math.round(pct)}%</span>
        </div>
      )}
    </li>
  );
}

function Setup() {
  return (
    <div className="setup" id="config">
      <div className="setup-h">como conectar o seu Notion</div>
      <ol>
        <li>Crie uma integração em <code>notion.so/my-integrations</code> e compartilhe sua database de estudos com ela.</li>
        <li>Defina <code>Notion__Token</code> e <code>Notion__DatabaseId</code> (ou preencha <code>appsettings.Local.json</code>).</li>
        <li>Reinicie o app — o painel passa a mostrar as suas matérias.</li>
      </ol>
    </div>
  );
}

function Note({ children, tone }) {
  return <div className={`note ${tone === "warn" ? "note-warn" : ""}`}>{children}</div>;
}
