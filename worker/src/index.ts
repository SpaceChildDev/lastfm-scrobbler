import { Md5 } from 'ts-md5';

interface Env {
  LASTFM_API_KEY: string;
  LASTFM_API_SECRET: string;
}

const R2_BASE = 'https://pub-8a5464b225534730b481b262ffe4748b.r2.dev';

function md5hex(input: string): string {
  return Md5.hashStr(input);
}

function buildSignature(params: Record<string, string>, secret: string): string {
  const keys = Object.keys(params)
    .filter(k => k !== 'format' && k !== 'callback')
    .sort();

  let str = '';
  for (const key of keys) str += key + params[key];
  str += secret;

  return md5hex(str);
}

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);

    // GET /lastfm-scrobbler/* → proxy to R2. Some networks (corporate
    // firewalls, SSL-inspecting AVs) block *.r2.dev directly, but reach
    // this worker fine because it's served from a custom domain.
    if (request.method === 'GET' && url.pathname.startsWith('/lastfm-scrobbler/')) {
      const upstream = await fetch(`${R2_BASE}${url.pathname}${url.search}`);
      const headers  = new Headers(upstream.headers);
      headers.set('Access-Control-Allow-Origin', '*');
      if (url.pathname.endsWith('.json')) {
        headers.set('Cache-Control', 'no-cache, no-store, max-age=0');
      }
      return new Response(upstream.body, { status: upstream.status, headers });
    }

    if (request.method === 'OPTIONS') {
      return new Response(null, {
        headers: {
          'Access-Control-Allow-Origin': '*',
          'Access-Control-Allow-Methods': 'GET, POST',
          'Access-Control-Allow-Headers': 'Content-Type',
        },
      });
    }

    if (request.method !== 'POST') {
      return new Response('Method not allowed', { status: 405 });
    }

    let formData: FormData;
    try {
      formData = await request.formData();
    } catch {
      return new Response('Bad request: expected form data', { status: 400 });
    }

    const params: Record<string, string> = {};
    let sign = false;

    for (const [key, value] of formData.entries()) {
      if (key === '_sign') {
        sign = value === '1';
        continue;
      }
      params[key] = value.toString();
    }

    params['api_key'] = env.LASTFM_API_KEY;

    if (sign) {
      params['api_sig'] = buildSignature(params, env.LASTFM_API_SECRET);
    }

    params['format'] = 'json';

    const body = new URLSearchParams(params);
    const upstream = await fetch('https://ws.audioscrobbler.com/2.0/', {
      method: 'POST',
      body,
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    });

    const json = await upstream.json() as Record<string, unknown>;

    // Inject the full auth URL so the C# client never needs to know the api_key
    if (params['method'] === 'auth.getToken' && typeof json['token'] === 'string') {
      json['_authUrl'] = `https://www.last.fm/api/auth/?api_key=${env.LASTFM_API_KEY}&token=${json['token']}`;
    }

    return Response.json(json, {
      headers: { 'Access-Control-Allow-Origin': '*' },
    });
  },
};
