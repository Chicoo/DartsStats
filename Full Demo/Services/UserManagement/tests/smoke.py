"""Local Aspire smoke test. Creates and deletes only its own uniquely named test user."""
import html.parser
import http.cookiejar
import json
import os
from pathlib import Path
import urllib.error
import urllib.parse
import urllib.request
import uuid

class FormParser(html.parser.HTMLParser):
    def __init__(self):
        super().__init__()
        self.action = None
        self.values = {}
    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        if tag == 'form': self.action = attrs.get('action')
        if tag == 'meta' and attrs.get('http-equiv', '').lower() == 'refresh':
            self.action = attrs.get('content', '').split('url=', 1)[-1]
        if tag == 'input' and attrs.get('name'):
            self.values[attrs['name']] = attrs.get('value', '')

origin = 'https://localhost:5178'
secrets = json.loads((Path(os.environ['APPDATA']) / 'Microsoft/UserSecrets/c4771afa-d91a-475d-b8a7-052bb239a509/secrets.json').read_text(encoding='utf-8-sig'))
opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))

def login(username, password):
    response = opener.open(origin + '/api/auth/login')
    form = FormParser()
    form.feed(response.read().decode())
    form.values.update(username=username, password=password, credentialId='')
    response = opener.open(form.action, urllib.parse.urlencode(form.values).encode())
    content = response.read().decode()
    callback = FormParser()
    callback.feed(content)
    if 'code' in callback.values:
        opener.open(callback.action, urllib.parse.urlencode(callback.values).encode()).read()
    elif callback.action and callback.action.startswith(origin):
        response = opener.open(callback.action)
        callback = FormParser()
        callback.feed(response.read().decode())
        if 'code' in callback.values:
            opener.open(callback.action, urllib.parse.urlencode(callback.values).encode()).read()
    elif 'password-new' in callback.values:
        return callback
    else:
        raise RuntimeError('Login did not return an authorization response: ' + str(list(callback.values)))

def api(path, method='GET', body=None, expected=200, token=None):
    headers = {'Content-Type': 'application/json'}
    if token: headers['X-CSRF-TOKEN'] = token
    request = urllib.request.Request(origin + path, method=method, headers=headers,
        data=None if body is None else json.dumps(body).encode())
    try:
        response = opener.open(request)
    except urllib.error.HTTPError as error:
        response = error
    assert response.status == expected, (path, response.status, response.read().decode())
    content = response.read()
    return json.loads(content) if content else None

api('/api/users', expected=401)
login('demo-admin', secrets['Parameters:user-management-demo-admin-password'])
session = api('/api/session')
csrf = session['csrfToken']
username = 'smoke-' + uuid.uuid4().hex[:12]
user_id = None
try:
    user = api('/api/users', 'POST', dict(username=username, firstName='Smoke', lastName='Test',
        email=username + '@example.test', enabled=True, temporaryPassword='Temporary123!'), 201, csrf)
    user_id = user['id']
    assert user['roles'] == ['user']
    api('/api/users', 'POST', dict(username=username, enabled=True, temporaryPassword='Temporary123!'), 409, csrf)
    found = api('/api/users?search=' + username)
    assert found['total'] == 1 and found['items'][0]['id'] == user_id
    api('/api/users/' + user_id, 'PUT', dict(firstName='Updated', lastName='Test', email=username+'@example.test'), 204, csrf)
    assert api('/api/users/' + user_id)['firstName'] == 'Updated'
    api('/api/users/' + user_id + '/enabled', 'PUT', dict(enabled=False), 204, csrf)
    assert not api('/api/users/' + user_id)['enabled']
    api('/api/users/' + user_id + '/enabled', 'PUT', dict(enabled=True), 204, csrf)
    api('/api/users/' + user_id + '/roles', 'PUT', dict(roles=['admin', 'user']), 204, csrf)
    assert set(api('/api/users/' + user_id)['roles']) == {'admin', 'user'}
    api('/api/users/' + user_id + '/roles', 'PUT', dict(roles=['user']), 204, csrf)
    api('/api/users/' + user_id + '/password-reset', 'POST', dict(temporaryPassword='Temporary456!'), 204, csrf)
    admin_opener = opener
    test_opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
    opener = test_opener
    required_action = login(username, 'Temporary456!')
    assert required_action and 'password-new' in required_action.values, 'Temporary password must require a password change'
    required_action.values.update({'password-new': 'ChangedTemporary789!', 'password-confirm': 'ChangedTemporary789!'})
    response = opener.open(required_action.action, urllib.parse.urlencode(required_action.values).encode())
    callback = FormParser()
    callback.feed(response.read().decode())
    if 'code' in callback.values:
        opener.open(callback.action, urllib.parse.urlencode(callback.values).encode()).read()
    elif callback.action and callback.action.startswith(origin):
        response = opener.open(callback.action)
        callback = FormParser()
        callback.feed(response.read().decode())
        if 'code' in callback.values:
            opener.open(callback.action, urllib.parse.urlencode(callback.values).encode()).read()
    api('/api/session', expected=401)
    opener = admin_opener
    api('/api/users/' + user_id + '/roles', 'PUT', dict(roles=['admin', 'user']), 204, csrf)
    opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
    login(username, 'ChangedTemporary789!')
    test_session = api('/api/session')
    test_admin_opener = opener
    opener = admin_opener
    api('/api/users/' + user_id + '/roles', 'PUT', dict(roles=['user']), 204, csrf)
    opener = test_admin_opener
    api('/api/users', expected=401)
    opener = admin_opener
    api('/api/users/' + user_id, 'DELETE', expected=400)
    api('/api/users/' + session['userId'], 'DELETE', expected=400, token=csrf)
finally:
    if 'admin_opener' in globals(): opener = admin_opener
    if user_id: api('/api/users/' + user_id, 'DELETE', expected=204, token=csrf)
api('/api/users/' + user_id, expected=404)
api('/api/auth/logout', 'POST', expected=204, token=csrf)
api('/api/session', expected=401)
print('PASS: real OIDC login, session, search, CRUD, roles, temporary reset, CSRF, self-protection, logout and cleanup')
