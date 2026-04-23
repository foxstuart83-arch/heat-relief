# Heat Relief — Standard Operating Procedure
# Access, Operations & Audit

---

## 1. First-Time Setup (Run Once)

> Do this before the first Cloud Build deploy.

### Prerequisites
- `gcloud` CLI installed and authenticated: `gcloud auth login`
- `bq` CLI (comes with Google Cloud SDK)
- `firebase` CLI: `npm install -g firebase-tools && firebase login`
- Billing account ready in GCP Console

### Step 1 — Create the GCP project
```bash
gcloud projects create heat-relief-game --name="Heat Relief"
gcloud billing projects link heat-relief-game --billing-account=<YOUR_BILLING_ACCOUNT_ID>
```

### Step 2 — Run the infrastructure setup script
```bash
bash infra/setup.sh heat-relief-game
```
This creates service accounts, the isolated audit Firestore database, the BigQuery
dataset, and the Cloud Logging sink. It prints next steps when done.

### Step 3 — Deploy Firestore security rules
```bash
firebase use heat-relief-game
firebase deploy --only firestore:rules --project heat-relief-game
```
Uses `infra/firestore.rules`. Locks the audit database so no client can read or write it.

### Step 4 — Enable Cloud IAP
1. Go to: https://console.cloud.google.com/security/iap?project=heat-relief-game
2. Click **Enable API** if prompted
3. Configure the **OAuth consent screen** (Internal or External depending on your org)
4. After the first Cloud Build deploy, come back here and enable IAP for **heat-relief**

### Step 5 — Connect Cloud Build to this repository
1. Go to: https://console.cloud.google.com/cloud-build/triggers?project=heat-relief-game
2. Click **Connect Repository** → select GitHub → select `foxstuart83-arch/heat-relief`
3. Create a trigger pointing to `cloudbuild.yaml` on branch `main`

---

## 2. Deploying the App

### Automatic (recommended)
Push to `main`. Cloud Build triggers automatically and deploys both services.

### Manual
```bash
gcloud builds submit . \
  --config=cloudbuild.yaml \
  --project=heat-relief-game
```

### What gets deployed
| Service | URL pattern | Access |
|---|---|---|
| `heat-relief` | `https://heat-relief-<hash>-uc.a.run.app` | Cloud IAP (users must be granted access) |
| `heat-relief-audit` | `https://heat-relief-audit-<hash>-uc.a.run.app` | Internal only — no public URL |

---

## 3. Granting Access to the Game (Cloud IAP)

Only users explicitly granted access can open the game.

### Add a user
```bash
gcloud iap web add-iam-policy-binding \
  --resource-type=cloud-run \
  --service=heat-relief \
  --region=us-central1 \
  --member="user:someone@example.com" \
  --role="roles/iap.httpsResourceAccessor" \
  --project=heat-relief-game
```

### Add a Google Group
```bash
gcloud iap web add-iam-policy-binding \
  --resource-type=cloud-run \
  --service=heat-relief \
  --region=us-central1 \
  --member="group:team@example.com" \
  --role="roles/iap.httpsResourceAccessor" \
  --project=heat-relief-game
```

### Remove a user
```bash
gcloud iap web remove-iam-policy-binding \
  --resource-type=cloud-run \
  --service=heat-relief \
  --region=us-central1 \
  --member="user:someone@example.com" \
  --role="roles/iap.httpsResourceAccessor" \
  --project=heat-relief-game
```

### List who currently has access
```bash
gcloud iap web get-iam-policy \
  --resource-type=cloud-run \
  --service=heat-relief \
  --region=us-central1 \
  --project=heat-relief-game
```

---

## 4. Accessing the Audit Log

The audit log has two layers: the Firestore live store (recent events, queryable via API)
and BigQuery (full history, SQL queryable, immutable).

### 4a. Query via the Audit Service API

The audit service is internal-only. To call it, get an ID token as the app service account:

```bash
# Get an identity token for the audit service URL
AUDIT_URL=$(gcloud run services describe heat-relief-audit \
  --region=us-central1 \
  --project=heat-relief-game \
  --format="value(status.url)")

TOKEN=$(gcloud auth print-identity-token \
  --impersonate-service-account=heat-relief-app@heat-relief-game.iam.gserviceaccount.com)
```

#### List recent access events (last 200)
```bash
curl -s -H "Authorization: Bearer $TOKEN" \
  "${AUDIT_URL}/events" | jq .
```

#### Filter by user
```bash
curl -s -H "Authorization: Bearer $TOKEN" \
  "${AUDIT_URL}/events?user_id=accounts.google.com:someone@example.com" | jq .
```

#### Filter by IP address
```bash
curl -s -H "Authorization: Bearer $TOKEN" \
  "${AUDIT_URL}/events?ip_address=1.2.3.4" | jq .
```

#### Filter by event type
```bash
curl -s -H "Authorization: Bearer $TOKEN" \
  "${AUDIT_URL}/events?event_type=ACCESS" | jq .
```

#### Filter by time range
```bash
curl -s -H "Authorization: Bearer $TOKEN" \
  "${AUDIT_URL}/events?from=2026-04-01T00:00:00Z&to=2026-04-23T23:59:59Z" | jq .
```

#### Paginate (use next_page_token from previous response)
```bash
curl -s -H "Authorization: Bearer $TOKEN" \
  "${AUDIT_URL}/events?page_token=<doc_id_from_previous_response>" | jq .
```

### 4b. Query via BigQuery (Full History, SQL)

BigQuery receives all Cloud Run request logs via the log sink. This is the
long-term immutable record.

```bash
bq query --use_legacy_sql=false --project_id=heat-relief-game '
SELECT
  timestamp,
  json_payload.iap_user   AS user,
  json_payload.ip         AS ip_address,
  json_payload.method     AS method,
  json_payload.uri        AS path,
  json_payload.status     AS http_status,
  json_payload.user_agent AS user_agent
FROM `heat-relief-game.heat_relief_audit_logs.requests_*`
ORDER BY timestamp DESC
LIMIT 100
'
```

#### Who accessed in the last 24 hours?
```bash
bq query --use_legacy_sql=false --project_id=heat-relief-game '
SELECT
  json_payload.iap_user AS user,
  COUNT(*) AS request_count,
  MIN(timestamp) AS first_seen,
  MAX(timestamp) AS last_seen
FROM `heat-relief-game.heat_relief_audit_logs.requests_*`
WHERE timestamp > TIMESTAMP_SUB(CURRENT_TIMESTAMP(), INTERVAL 24 HOUR)
GROUP BY user
ORDER BY request_count DESC
'
```

#### Access from a specific IP?
```bash
bq query --use_legacy_sql=false --project_id=heat-relief-game '
SELECT timestamp, json_payload.iap_user, json_payload.uri, json_payload.status
FROM `heat-relief-game.heat_relief_audit_logs.requests_*`
WHERE json_payload.ip = "1.2.3.4"
ORDER BY timestamp DESC
'
```

### 4c. Cloud Logging Console (real-time)
1. Go to: https://console.cloud.google.com/logs/query?project=heat-relief-game
2. Paste this filter:
```
resource.type="cloud_run_revision"
resource.labels.service_name=("heat-relief" OR "heat-relief-audit")
```
3. All structured JSON fields (iap_user, ip, method, uri, status) are searchable.

---

## 5. Checking Service Health

```bash
# Check both Cloud Run services are running
gcloud run services list --region=us-central1 --project=heat-relief-game

# Get the game URL
gcloud run services describe heat-relief \
  --region=us-central1 \
  --project=heat-relief-game \
  --format="value(status.url)"

# Audit service health (via internal call)
curl -s -H "Authorization: Bearer $TOKEN" "${AUDIT_URL}/health"
```

---

## 6. Viewing Deployment History

```bash
# Cloud Build history
gcloud builds list --project=heat-relief-game --limit=10

# Cloud Run revision history
gcloud run revisions list \
  --service=heat-relief \
  --region=us-central1 \
  --project=heat-relief-game
```

---

## 7. Rolling Back a Deployment

```bash
# List revisions to find a good one
gcloud run revisions list \
  --service=heat-relief \
  --region=us-central1 \
  --project=heat-relief-game

# Roll back to a specific revision
gcloud run services update-traffic heat-relief \
  --to-revisions=heat-relief-00005-abc=100 \
  --region=us-central1 \
  --project=heat-relief-game
```

---

## 8. Architecture Reference

```
Internet
   │
   ▼
Cloud IAP  ── blocks unauthenticated requests
   │            ── logs auth events to Cloud Logging
   ▼
Cloud Run: heat-relief  (SA: heat-relief-app@...)
   │  JSON structured logs ──────────────────────► Cloud Logging
   │                                                    │
   │  internal HTTPS + ID token                         ▼ Log Sink
   ▼                                              BigQuery: heat_relief_audit_logs
Cloud Run: heat-relief-audit  (SA: heat-relief-audit@...)
   │  --ingress=internal  (no public access)
   │  --no-allow-unauthenticated
   ▼
Firestore DB: "audit-logs"  (separate from any app database)
   └── /events/{id}  append-only, no update/delete
```

| Component | Isolation mechanism |
|---|---|
| Audit Firestore DB | Separate named database instance; app SA has no IAM binding to it |
| Audit Cloud Run service | `--ingress=internal`; only callable by app SA via ID token |
| Audit SA | No `roles/run.invoker` on any service except its own; no app DB access |
| BigQuery logs | Written by log sink SA only; read-only for analysis |
| Firestore security rules | Deny all client-side read/write/update/delete |

---

## 9. Key Contacts / Ownership

| Resource | Owner SA | Access path |
|---|---|---|
| Game (heat-relief) | `heat-relief-app@...` | Cloud IAP → IAP user grant |
| Audit service | `heat-relief-audit@...` | Internal only; impersonate app SA |
| Audit Firestore DB | `heat-relief-audit@...` | Admin SDK only (no console client access) |
| BigQuery audit logs | Log sink SA | `bq query` or BigQuery Console |
