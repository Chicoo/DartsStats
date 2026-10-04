import { StrictMode, useEffect, useState, type FormEvent } from 'react';
import { createRoot } from 'react-dom/client';
import './style.css';

type User = { id: string; username: string; firstName: string | null; lastName: string | null; email: string | null; enabled: boolean; roles: string[] };
type Page = { items: User[]; total: number; page: number; pageSize: number };
type Session = { username: string; userId: string; csrfToken: string; dartsUrl: string };
type Editor = { kind: 'create' | 'edit' | 'password' | 'roles'; user?: User };

function App() {
  const [session, setSession] = useState<Session | null>(null);
  const [checking, setChecking] = useState(true);
  const [data, setData] = useState<Page>({ items: [], total: 0, page: 1, pageSize: 20 });
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState(new URLSearchParams(location.search).has('error') ? 'Sign-in failed. An enabled administrator account is required.' : '');
  const [notice, setNotice] = useState('');
  const [editor, setEditor] = useState<Editor | null>(null);
  const [revision, setRevision] = useState(0);

  async function api<T>(path: string, method = 'GET', body?: unknown): Promise<T> {
    const response = await fetch(path, {
      method, credentials: 'same-origin',
      headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': session?.csrfToken || '' },
      body: body === undefined ? undefined : JSON.stringify(body)
    });
    if (response.status === 401 || response.status === 403) { setSession(null); throw new Error('Your administrator session has ended. Please sign in again.'); }
    if (!response.ok) {
      const problem = await response.json().catch(() => ({}));
      throw new Error(problem.errors ? Object.values(problem.errors).flat().join(' ') : problem.title || 'The request failed. Please try again.');
    }
    return response.status === 204 ? undefined as T : response.json();
  }

  useEffect(() => {
    fetch('/api/session').then(async response => {
      if (response.ok) setSession(await response.json());
      else if (response.status !== 401 && response.status !== 403) setError('User management is unavailable. Please try again.');
    }).catch(() => setError('Cannot connect to user management.')).finally(() => setChecking(false));
  }, []);

  useEffect(() => {
    if (!session) return;
    let active = true;
    setLoading(true);
    api<Page>(`/api/users?page=${page}&pageSize=20&search=${encodeURIComponent(query)}`)
      .then(result => { if (active) setData(result); })
      .catch(reason => { if (active) setError(reason.message); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [session, page, query, revision]);

  async function mutate(action: () => Promise<unknown>, message: string) {
    setBusy(true); setError(''); setNotice('');
    try { await action(); setEditor(null); setNotice(message); setRevision(n => n + 1); }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'The change failed.'); }
    finally { setBusy(false); }
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!editor) return;
    const form = new FormData(event.currentTarget);
    const value = (name: string) => String(form.get(name) || '').trim();
    const profile = { firstName: value('firstName') || null, lastName: value('lastName') || null, email: value('email') || null };
    const id = editor.user?.id;
    if (editor.kind === 'create') await mutate(() => api('/api/users', 'POST', {
      ...profile, username: value('username'), enabled: form.has('enabled'), temporaryPassword: String(form.get('password'))
    }), 'User created. They must change their temporary password at next login.');
    if (editor.kind === 'edit') await mutate(() => api(`/api/users/${id}`, 'PUT', profile), 'User updated.');
    if (editor.kind === 'password') await mutate(() => api(`/api/users/${id}/password-reset`, 'POST', {
      temporaryPassword: String(form.get('password'))
    }), 'Temporary password set. Existing Keycloak sessions have been revoked.');
    if (editor.kind === 'roles') await mutate(() => api(`/api/users/${id}/roles`, 'PUT', {
      roles: form.getAll('roles')
    }), 'Roles updated.');
  }

  return <div className="shell">
    <header><a className="brand" href={session?.dartsUrl || import.meta.env.VITE_DARTS_URL || '/'}>🎯 DartsStats</a><span>User management</span>
      {session && <div className="account"><span>{session.username}</span><button onClick={() => mutate(async () => { await api('/api/auth/logout', 'POST'); setSession(null); }, 'Signed out.')} disabled={busy}>Sign out</button></div>}
    </header>
    <main>
      {error && <div role="alert" className="alert error">{error}<button aria-label="Dismiss error" onClick={() => setError('')}>×</button></div>}
      {notice && <div role="status" className="alert success">{notice}</div>}
      {checking ? <p role="status">Checking session…</p> : !session ? <section className="login"><div className="eyebrow">ADMINISTRATION</div><h1>Manage your users</h1><p>Create accounts, manage access, and reset passwords from one place.</p><a className="primary button" href="/api/auth/login">Sign in as administrator</a></section> : <>
        <div className="heading"><div><div className="eyebrow">ADMINISTRATION</div><h1>Users</h1><p>Manage accounts and their access to DartsStats.</p></div><button className="primary" disabled={busy} onClick={() => { setError(''); setEditor({ kind: 'create' }); }}>＋ Create user</button></div>
        <form className="search" onSubmit={e => { e.preventDefault(); setPage(1); setQuery(search); }}><label htmlFor="search">Search users</label><input id="search" placeholder="Username, name, or email" value={search} maxLength={200} onChange={e => setSearch(e.target.value)} /><button>Search</button><button type="button" onClick={() => { setSearch(''); setQuery(''); setPage(1); }}>Clear</button></form>
        <div className="table-wrap" aria-busy={loading}><table><thead><tr><th>User</th><th>Email</th><th>Status</th><th>Roles</th><th>Actions</th></tr></thead><tbody>
          {data.items.map(user => <tr key={user.id}><td><strong>{user.username}</strong><small>{[user.firstName, user.lastName].filter(Boolean).join(' ')}</small></td><td>{user.email || '—'}</td><td><span className={`badge ${user.enabled ? 'enabled' : ''}`}>{user.enabled ? 'Enabled' : 'Disabled'}</span></td><td>{user.roles.join(', ') || '—'}</td><td className="actions">
            <button disabled={busy} onClick={() => { setError(''); setEditor({ kind: 'edit', user }); }}>Edit</button>
            <button disabled={busy} onClick={() => setEditor({ kind: 'roles', user })}>Roles</button>
            <button disabled={busy} onClick={() => setEditor({ kind: 'password', user })}>Reset password</button>
            <button disabled={busy || user.id === session.userId} onClick={() => { if (confirm(`${user.enabled ? 'Disable' : 'Enable'} ${user.username}?`)) void mutate(() => api(`/api/users/${user.id}/enabled`, 'PUT', { enabled: !user.enabled }), 'Account status updated.'); }}>{user.enabled ? 'Disable' : 'Enable'}</button>
            <button className="danger" disabled={busy || user.id === session.userId} onClick={() => { if (confirm(`Permanently delete ${user.username}? This cannot be undone.`)) void mutate(() => api(`/api/users/${user.id}`, 'DELETE'), 'User deleted.'); }}>Delete</button>
          </td></tr>)}
          {!loading && !data.items.length && <tr><td colSpan={5} className="empty">No users found.</td></tr>}
        </tbody></table></div>
        <footer><span>{loading ? 'Loading…' : `${data.total} users · Page ${page}`}</span><div><button disabled={page === 1 || loading} onClick={() => setPage(p => p - 1)}>Previous</button><button disabled={page * 20 >= data.total || loading} onClick={() => setPage(p => p + 1)}>Next</button></div></footer>
      </>}
      {editor && <div className="overlay"><section className="dialog" role="dialog" aria-modal="true" aria-labelledby="dialog-title"><h2 id="dialog-title">{editor.kind === 'create' ? 'Create user' : `${editor.kind === 'edit' ? 'Edit' : editor.kind === 'roles' ? 'Roles for' : 'Reset password for'} ${editor.user?.username}`}</h2>
        {error && <p role="alert" className="error alert">{error}</p>}
        <form onSubmit={save}>
          {editor.kind === 'create' && <label>Username<input name="username" required maxLength={100} autoFocus autoComplete="off" /></label>}
          {(editor.kind === 'create' || editor.kind === 'edit') && <><label>First name<input name="firstName" maxLength={100} defaultValue={editor.user?.firstName || ''} /></label><label>Last name<input name="lastName" maxLength={100} defaultValue={editor.user?.lastName || ''} /></label><label>Email<input name="email" type="email" maxLength={254} defaultValue={editor.user?.email || ''} /></label></>}
          {editor.kind === 'create' && <label className="check"><input type="checkbox" name="enabled" defaultChecked />Enabled account</label>}
          {(editor.kind === 'create' || editor.kind === 'password') && <><label>Temporary password<input name="password" type="password" required minLength={8} maxLength={128} autoComplete="new-password" /></label><p className="hint">Share this password securely. The user must change it at their next login.</p></>}
          {editor.kind === 'roles' && ['user', 'admin'].map(role => <label className="check" key={role}><input type="checkbox" name="roles" value={role} defaultChecked={editor.user?.roles.includes(role)} disabled={role === 'admin' && editor.user?.id === session?.userId} />{role}{role === 'admin' && editor.user?.id === session?.userId && <input type="hidden" name="roles" value="admin" />}</label>)}
          <div className="dialog-actions"><button type="button" disabled={busy} onClick={() => { setEditor(null); setError(''); }}>Cancel</button><button className="primary" disabled={busy}>{busy ? 'Saving…' : 'Save'}</button></div>
        </form></section></div>}
    </main>
  </div>;
}

createRoot(document.getElementById('root')!).render(<StrictMode><App /></StrictMode>);
