'use strict';

const express = require('express');
const { Firestore, FieldValue } = require('@google-cloud/firestore');
const { OAuth2Client } = require('google-auth-library');
const crypto = require('crypto');

const app = express();
app.use(express.json({ limit: '16kb' }));

// ─── Firestore: SEPARATE database, never the default app database ─────────────
const db = new Firestore({ databaseId: 'audit-logs' });
const COLLECTION = 'events';

// ─── Config ───────────────────────────────────────────────────────────────────
const PORT = parseInt(process.env.PORT || '8080', 10);
// Comma-separated list of service account emails allowed to call this service
const ALLOWED_CALLERS = new Set(
  (process.env.ALLOWED_SERVICE_ACCOUNTS || '').split(',').map(s => s.trim()).filter(Boolean)
);

// ─── Auth: validate Google ID token (service-to-service) ─────────────────────
const authClient = new OAuth2Client();

async function requireAuthorizedCaller(req) {
  const header = req.headers['authorization'] || '';
  if (!header.startsWith('Bearer ')) throw Object.assign(new Error('Missing Bearer token'), { status: 401 });

  const token = header.slice(7);
  let payload;
  try {
    const ticket = await authClient.verifyIdToken({ idToken: token });
    payload = ticket.getPayload();
  } catch {
    throw Object.assign(new Error('Invalid token'), { status: 401 });
  }

  if (!ALLOWED_CALLERS.has(payload.email)) {
    throw Object.assign(new Error(`Caller not authorized: ${payload.email}`), { status: 403 });
  }

  return payload.email;
}

function sanitize(val, maxLen) {
  return String(val ?? '').slice(0, maxLen);
}

// ─── POST /events — append-only write, never updates an existing record ───────
app.post('/events', async (req, res) => {
  try {
    const caller = await requireAuthorizedCaller(req);
    const { event_type, user_id, ip_address, user_agent, resource, method, status_code, metadata } = req.body;

    if (!event_type || !resource) {
      return res.status(400).json({ error: 'event_type and resource are required' });
    }

    const event = {
      event_id:       crypto.randomUUID(),
      // FieldValue.serverTimestamp() is set by Firestore server — client cannot forge it
      timestamp:      FieldValue.serverTimestamp(),
      event_type:     sanitize(event_type, 64),
      user_id:        sanitize(user_id || 'anonymous', 256),
      ip_address:     sanitize(ip_address, 45),
      user_agent:     sanitize(user_agent, 512),
      resource:       sanitize(resource, 2048),
      method:         sanitize(method || 'GET', 10),
      status_code:    Number.isFinite(Number(status_code)) ? Number(status_code) : 0,
      caller_service: caller,
      metadata:       (metadata && typeof metadata === 'object' && !Array.isArray(metadata))
                        ? metadata
                        : {},
    };

    // add() always creates a NEW document — update/set/delete are never called here
    const ref = await db.collection(COLLECTION).add(event);

    res.status(201).json({ event_id: event.event_id, doc_id: ref.id });
  } catch (err) {
    console.error(JSON.stringify({ severity: 'ERROR', message: err.message, endpoint: 'POST /events' }));
    res.status(err.status || 500).json({ error: err.message });
  }
});

// ─── GET /events — queryable audit log (authorized callers only) ──────────────
app.get('/events', async (req, res) => {
  try {
    await requireAuthorizedCaller(req);

    const { user_id, event_type, ip_address, from, to, limit = '200', page_token } = req.query;
    const pageSize = Math.min(Math.max(parseInt(limit, 10) || 200, 1), 1000);

    let q = db.collection(COLLECTION).orderBy('timestamp', 'desc');

    if (user_id)     q = q.where('user_id',     '==', sanitize(user_id, 256));
    if (event_type)  q = q.where('event_type',  '==', sanitize(event_type, 64));
    if (ip_address)  q = q.where('ip_address',  '==', sanitize(ip_address, 45));
    if (from)        q = q.where('timestamp',   '>=', new Date(from));
    if (to)          q = q.where('timestamp',   '<=', new Date(to));

    if (page_token) {
      const cursorDoc = await db.collection(COLLECTION).doc(page_token).get();
      if (cursorDoc.exists) q = q.startAfter(cursorDoc);
    }

    q = q.limit(pageSize);

    const snap = await q.get();
    const events = snap.docs.map(doc => {
      const data = doc.data();
      return {
        doc_id:         doc.id,
        event_id:       data.event_id,
        timestamp:      data.timestamp?.toDate?.()?.toISOString?.() ?? null,
        event_type:     data.event_type,
        user_id:        data.user_id,
        ip_address:     data.ip_address,
        user_agent:     data.user_agent,
        resource:       data.resource,
        method:         data.method,
        status_code:    data.status_code,
        caller_service: data.caller_service,
        metadata:       data.metadata,
      };
    });

    const next_page_token = snap.docs.length === pageSize
      ? snap.docs[snap.docs.length - 1].id
      : null;

    res.json({ count: events.length, next_page_token, events });
  } catch (err) {
    console.error(JSON.stringify({ severity: 'ERROR', message: err.message, endpoint: 'GET /events' }));
    res.status(err.status || 500).json({ error: err.message });
  }
});

// ─── Health check ─────────────────────────────────────────────────────────────
app.get('/health', (_req, res) => res.json({ status: 'ok' }));

// ─── Reject everything else ───────────────────────────────────────────────────
app.use((_req, res) => res.status(404).json({ error: 'not found' }));

app.listen(PORT, () => {
  console.log(JSON.stringify({ severity: 'INFO', message: `Audit service listening on :${PORT}` }));
});
