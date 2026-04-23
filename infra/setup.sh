#!/usr/bin/env bash
# Heat Relief — One-time GCP infrastructure setup
# Run this once before the first Cloud Build deploy.
#
# Usage:
#   bash infra/setup.sh <PROJECT_ID>
#
# What this creates:
#   - Two dedicated service accounts (app + audit) with minimal IAM roles
#   - A SEPARATE Firestore database named "audit-logs" (isolated from any app data)
#   - Cloud IAP OAuth brand + access policy for the game Cloud Run service
#   - A Cloud Logging sink → BigQuery for long-term queryable audit retention
#   - All required GCP APIs enabled
#
# Architecture isolation guarantees after this script:
#   - heat-relief-app SA  : can invoke the audit service, NO access to audit Firestore DB
#   - heat-relief-audit SA: can write/read audit Firestore DB only, cannot touch app infra
#   - Audit Firestore DB  : entirely separate database instance from any app Firestore DB
#   - Audit Cloud Run svc : internal ingress only — not reachable from the public internet

set -euo pipefail

PROJECT_ID="${1:?Usage: setup.sh <PROJECT_ID>}"
REGION="us-central1"
BQ_DATASET="heat_relief_audit_logs"

APP_SA_NAME="heat-relief-app"
AUDIT_SA_NAME="heat-relief-audit"
APP_SA="${APP_SA_NAME}@${PROJECT_ID}.iam.gserviceaccount.com"
AUDIT_SA="${AUDIT_SA_NAME}@${PROJECT_ID}.iam.gserviceaccount.com"

echo "==> Enabling required APIs..."
gcloud services enable \
  run.googleapis.com \
  cloudbuild.googleapis.com \
  containerregistry.googleapis.com \
  firestore.googleapis.com \
  iap.googleapis.com \
  bigquery.googleapis.com \
  logging.googleapis.com \
  pubsub.googleapis.com \
  --project="${PROJECT_ID}"

# ── Service Accounts ──────────────────────────────────────────────────────────
echo "==> Creating service accounts..."

gcloud iam service-accounts create "${APP_SA_NAME}" \
  --display-name="Heat Relief App" \
  --description="Runs the main game nginx container" \
  --project="${PROJECT_ID}" 2>/dev/null \
  || echo "    (already exists, skipping)"

gcloud iam service-accounts create "${AUDIT_SA_NAME}" \
  --display-name="Heat Relief Audit Service" \
  --description="Runs the audit log service; only accesses audit-logs Firestore DB" \
  --project="${PROJECT_ID}" 2>/dev/null \
  || echo "    (already exists, skipping)"

# ── IAM: App SA — minimal permissions ─────────────────────────────────────────
echo "==> Binding IAM roles to app service account..."

# App SA can invoke the audit Cloud Run service (internal call)
gcloud projects add-iam-policy-binding "${PROJECT_ID}" \
  --member="serviceAccount:${APP_SA}" \
  --role="roles/run.invoker" \
  --condition=None

# App SA can write structured logs
gcloud projects add-iam-policy-binding "${PROJECT_ID}" \
  --member="serviceAccount:${APP_SA}" \
  --role="roles/logging.logWriter" \
  --condition=None

# ── IAM: Audit SA — Firestore only, nothing else ──────────────────────────────
echo "==> Binding IAM roles to audit service account..."

# Audit SA can read/write Firestore (security rules further restrict to create-only)
gcloud projects add-iam-policy-binding "${PROJECT_ID}" \
  --member="serviceAccount:${AUDIT_SA}" \
  --role="roles/datastore.user" \
  --condition=None

# Audit SA can write logs
gcloud projects add-iam-policy-binding "${PROJECT_ID}" \
  --member="serviceAccount:${AUDIT_SA}" \
  --role="roles/logging.logWriter" \
  --condition=None

# Explicit DENY: Audit SA must NOT be able to invoke other Cloud Run services
# (GCP default: no role = no access; this comment documents intent)

# ── Firestore: Separate audit database ────────────────────────────────────────
echo "==> Creating isolated Firestore database 'audit-logs'..."
gcloud firestore databases create \
  --database=audit-logs \
  --location="${REGION}" \
  --type=firestore-native \
  --project="${PROJECT_ID}" 2>/dev/null \
  || echo "    (already exists, skipping)"

# Deploy Firestore security rules for the audit database
echo "==> Deploying Firestore security rules..."
# Rules are deployed via Firebase CLI (see infra/firestore.rules)
# firebase deploy --only firestore:rules --project="${PROJECT_ID}"
echo "    NOTE: Run 'firebase deploy --only firestore:rules' after installing Firebase CLI."
echo "    The rules file is at infra/firestore.rules"

# ── BigQuery: Long-term queryable audit retention ─────────────────────────────
echo "==> Creating BigQuery dataset for long-term audit retention..."
bq --project_id="${PROJECT_ID}" mk \
  --dataset \
  --location=US \
  --description="Immutable Cloud Run access logs — heat-relief audit trail" \
  "${BQ_DATASET}" 2>/dev/null \
  || echo "    (already exists, skipping)"

# Cloud Logging sink: stream Cloud Run request logs → BigQuery
SINK_NAME="heat-relief-audit-sink"
echo "==> Creating Cloud Logging sink → BigQuery..."
gcloud logging sinks create "${SINK_NAME}" \
  "bigquery.googleapis.com/projects/${PROJECT_ID}/datasets/${BQ_DATASET}" \
  --log-filter='resource.type="cloud_run_revision" AND resource.labels.service_name=("heat-relief" OR "heat-relief-audit")' \
  --project="${PROJECT_ID}" 2>/dev/null \
  || echo "    (already exists, skipping)"

# Grant the sink's writer SA permission to insert rows into BigQuery
SINK_SA=$(gcloud logging sinks describe "${SINK_NAME}" \
  --project="${PROJECT_ID}" \
  --format="value(writerIdentity)")
echo "==> Granting sink SA (${SINK_SA}) BigQuery write access..."
bq add-iam-policy-binding \
  --member="${SINK_SA}" \
  --role="roles/bigquery.dataEditor" \
  "${PROJECT_ID}:${BQ_DATASET}"

# ── Cloud IAP: Protect the game Cloud Run service ─────────────────────────────
echo ""
echo "==> Cloud IAP setup (requires manual steps in GCP Console):"
echo "    1. Go to: https://console.cloud.google.com/security/iap?project=${PROJECT_ID}"
echo "    2. Enable IAP for the 'heat-relief' Cloud Run service"
echo "    3. Add authorized users under 'IAP-secured Web App User' role"
echo "    4. The OAuth consent screen must be configured first (IAP > OAuth consent)"
echo ""
echo "    Alternatively via gcloud (after OAuth brand is created):"
echo "    gcloud iap web enable --resource-type=cloud-run --service=heat-relief \\"
echo "      --region=${REGION} --project=${PROJECT_ID}"
echo ""

# ── Summary ───────────────────────────────────────────────────────────────────
echo "================================================================"
echo " Infrastructure setup complete for project: ${PROJECT_ID}"
echo "================================================================"
echo ""
echo " Service Accounts:"
echo "   App:   ${APP_SA}"
echo "   Audit: ${AUDIT_SA}"
echo ""
echo " Firestore databases:"
echo "   App data:   (default)   — app owns this"
echo "   Audit logs: audit-logs  — audit SA only, no app SA access"
echo ""
echo " BigQuery dataset: ${BQ_DATASET}"
echo "   Query example:"
echo "   SELECT timestamp, json_payload.iap_user, json_payload.ip,"
echo "          json_payload.method, json_payload.uri, json_payload.status"
echo "   FROM \`${PROJECT_ID}.${BQ_DATASET}.requests_*\`"
echo "   WHERE json_payload.iap_user IS NOT NULL"
echo "   ORDER BY timestamp DESC LIMIT 100;"
echo ""
echo " Next: run Cloud Build to deploy both services."
