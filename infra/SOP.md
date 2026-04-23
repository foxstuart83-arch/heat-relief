# Heat Relief — Step-by-Step Operating Procedures

---

## SECTION 1 — First-Time Setup
> Do this once before anything else. Never needs to be repeated.

---

### STEP 1 — Install the required tools on your computer

1. Open **Terminal** on your Mac
2. Install the Google Cloud SDK by pasting this and pressing Enter:
   ```
   curl https://sdk.cloud.google.com | bash
   ```
3. Close Terminal and reopen it
4. Confirm it installed by typing:
   ```
   gcloud --version
   ```
   You should see version numbers printed. If you get "command not found", restart Terminal and try again.

5. Install the Firebase CLI by typing:
   ```
   npm install -g firebase-tools
   ```
   If you get "command not found" for npm, install Node.js first from https://nodejs.org (download the LTS version, run the installer, then restart Terminal).

---

### STEP 2 — Log in to Google Cloud

1. In Terminal, type:
   ```
   gcloud auth login
   ```
2. A browser window will open — sign in with your Google account
3. Click **Allow** when prompted
4. Return to Terminal — you should see "You are now logged in as [your email]"

5. Also log in to Firebase:
   ```
   firebase login
   ```
6. Follow the same browser login steps

---

### STEP 3 — Create the GCP Project

1. In Terminal, type the following exactly (replace `YOUR-BILLING-ID` with your billing account ID):
   ```
   gcloud projects create heat-relief-game --name="Heat Relief"
   ```
2. Wait for it to finish (you'll see "Create in progress... done")
3. Link your billing account:
   ```
   gcloud billing projects link heat-relief-game --billing-account=YOUR-BILLING-ID
   ```
   > To find your billing account ID: go to https://console.cloud.google.com/billing, click your billing account, and copy the ID shown at the top (looks like `01ABCD-EF1234-567890`)

---

### STEP 4 — Navigate to the project folder

1. In Terminal, type:
   ```
   cd ~/Documents/Heat\ Relief\ App
   ```
2. Confirm you're in the right place:
   ```
   ls
   ```
   You should see files like `index.html`, `game.js`, `cloudbuild.yaml`, and a folder called `infra`

---

### STEP 5 — Run the infrastructure setup script

1. In Terminal (still in the project folder), type:
   ```
   bash infra/setup.sh heat-relief-game
   ```
2. This will run for 1–3 minutes. You will see lines printed as each piece is created.
3. When it finishes, you will see a summary block starting with:
   ```
   ================================================================
    Infrastructure setup complete for project: heat-relief-game
   ================================================================
   ```
4. Read through the summary — it lists the service accounts, databases, and BigQuery dataset that were created.

---

### STEP 6 — Deploy the Firestore security rules

1. In Terminal, type:
   ```
   firebase use heat-relief-game
   ```
2. Then type:
   ```
   firebase deploy --only firestore:rules --project heat-relief-game
   ```
3. Wait until you see `Deploy complete!`
4. This locks the audit database so nothing can tamper with it from outside.

---

### STEP 7 — Connect Cloud Build to your GitHub repository

1. Open your browser and go to:
   ```
   https://console.cloud.google.com/cloud-build/triggers?project=heat-relief-game
   ```
2. Click **Connect Repository**
3. Select **GitHub (Cloud Build GitHub App)** and click **Continue**
4. Authenticate with GitHub when prompted
5. Find and select the repository `foxstuart83-arch/heat-relief` and click **Connect**
6. Click **Create a Trigger**
7. Fill in:
   - Name: `deploy-on-push`
   - Event: **Push to a branch**
   - Branch: `^main$`
   - Configuration: **Cloud Build configuration file**
   - File location: `cloudbuild.yaml`
8. Click **Save**

---

### STEP 8 — Do the first deployment

1. In Terminal, type:
   ```
   gcloud builds submit . --config=cloudbuild.yaml --project=heat-relief-game
   ```
2. This takes 3–5 minutes. You will see build steps printing as they run.
3. When it says `BUILD SUCCEEDED`, both services are live.

---

### STEP 9 — Enable Cloud IAP (Access Control)

1. Open your browser and go to:
   ```
   https://console.cloud.google.com/security/iap?project=heat-relief-game
   ```
2. If prompted, click **Enable API** and wait
3. You will see a table with your Cloud Run services listed
4. Find **heat-relief** in the table
5. Toggle the switch in the **IAP** column to turn it ON
6. A popup will appear — click **Turn On**
7. IAP is now protecting the app. No one can access it until you grant them access (Section 3 below).

---

## SECTION 2 — Deploying an Update

---

### Automatic deployment (recommended)

1. Make your code changes
2. Commit them in Terminal:
   ```
   git add .
   git commit -m "Description of what you changed"
   git push origin main
   ```
3. Cloud Build will automatically detect the push and start deploying
4. To watch the progress, go to:
   ```
   https://console.cloud.google.com/cloud-build/builds?project=heat-relief-game
   ```

---

### Manual deployment

1. Open Terminal
2. Navigate to the project folder:
   ```
   cd ~/Documents/Heat\ Relief\ App
   ```
3. Type:
   ```
   gcloud builds submit . --config=cloudbuild.yaml --project=heat-relief-game
   ```
4. Wait for `BUILD SUCCEEDED`

---

## SECTION 3 — Granting and Revoking Access to the Game

---

### Give a person access

1. Open Terminal
2. Type the following, replacing `person@example.com` with their actual email:
   ```
   gcloud iap web add-iam-policy-binding \
     --resource-type=cloud-run \
     --service=heat-relief \
     --region=us-central1 \
     --member="user:person@example.com" \
     --role="roles/iap.httpsResourceAccessor" \
     --project=heat-relief-game
   ```
3. Press Enter. You will see a policy block printed confirming it worked.
4. The person can now log in at the game URL using their Google account.

---

### Give an entire Google Group access

1. Open Terminal
2. Type the following, replacing `team@example.com` with the group email:
   ```
   gcloud iap web add-iam-policy-binding \
     --resource-type=cloud-run \
     --service=heat-relief \
     --region=us-central1 \
     --member="group:team@example.com" \
     --role="roles/iap.httpsResourceAccessor" \
     --project=heat-relief-game
   ```
3. Press Enter and confirm the output shows success.

---

### Remove a person's access

1. Open Terminal
2. Type the following, replacing `person@example.com` with their email:
   ```
   gcloud iap web remove-iam-policy-binding \
     --resource-type=cloud-run \
     --service=heat-relief \
     --region=us-central1 \
     --member="user:person@example.com" \
     --role="roles/iap.httpsResourceAccessor" \
     --project=heat-relief-game
   ```
3. Press Enter. Their access is immediately revoked.

---

### See who currently has access

1. Open Terminal
2. Type:
   ```
   gcloud iap web get-iam-policy \
     --resource-type=cloud-run \
     --service=heat-relief \
     --region=us-central1 \
     --project=heat-relief-game
   ```
3. A list of all users and groups with access will be printed.

---

## SECTION 4 — Reading the Audit Log

The audit log records who accessed the app, from where, and when.
There are two ways to read it.

---

### METHOD A — Quick look in the browser (Cloud Logging)

1. Open your browser and go to:
   ```
   https://console.cloud.google.com/logs/query?project=heat-relief-game
   ```
2. In the query box at the top, paste this:
   ```
   resource.type="cloud_run_revision"
   resource.labels.service_name="heat-relief"
   ```
3. Click **Run Query**
4. Each line is one access event. Click any line to expand it.
5. Inside each entry you will see:
   - `iap_user` — the Google account that accessed the app
   - `ip` — the IP address they came from
   - `timestamp` — exact date and time
   - `method` — GET, POST, etc.
   - `uri` — which page/file they accessed
   - `status` — 200 means success, 401 means blocked

---

### METHOD B — SQL queries in BigQuery (full history)

1. Open your browser and go to:
   ```
   https://console.cloud.google.com/bigquery?project=heat-relief-game
   ```
2. In the left sidebar, expand **heat-relief-game** → **heat_relief_audit_logs**
3. Click the **+** icon next to the dataset name to open a new query tab
4. Paste any of the queries below and click **Run**

#### See the 100 most recent accesses
```sql
SELECT
  timestamp,
  json_payload.iap_user   AS user,
  json_payload.ip         AS ip_address,
  json_payload.method     AS method,
  json_payload.uri        AS page,
  json_payload.status     AS result
FROM `heat-relief-game.heat_relief_audit_logs.requests_*`
ORDER BY timestamp DESC
LIMIT 100
```

#### Who accessed in the last 24 hours?
```sql
SELECT
  json_payload.iap_user AS user,
  COUNT(*)              AS times_accessed,
  MIN(timestamp)        AS first_access,
  MAX(timestamp)        AS last_access
FROM `heat-relief-game.heat_relief_audit_logs.requests_*`
WHERE timestamp > TIMESTAMP_SUB(CURRENT_TIMESTAMP(), INTERVAL 24 HOUR)
GROUP BY user
ORDER BY times_accessed DESC
```

#### Find access from a specific IP address
```sql
SELECT
  timestamp,
  json_payload.iap_user AS user,
  json_payload.uri      AS page,
  json_payload.status   AS result
FROM `heat-relief-game.heat_relief_audit_logs.requests_*`
WHERE json_payload.ip = "1.2.3.4"
ORDER BY timestamp DESC
```
> Replace `1.2.3.4` with the IP address you want to investigate.

#### Find all access by a specific user
```sql
SELECT
  timestamp,
  json_payload.ip       AS ip_address,
  json_payload.uri      AS page,
  json_payload.status   AS result
FROM `heat-relief-game.heat_relief_audit_logs.requests_*`
WHERE json_payload.iap_user = "accounts.google.com:person@example.com"
ORDER BY timestamp DESC
```
> Replace `person@example.com` with the email you are looking up.

#### Find blocked access attempts (401 / 403)
```sql
SELECT
  timestamp,
  json_payload.iap_user AS user,
  json_payload.ip       AS ip_address,
  json_payload.status   AS result
FROM `heat-relief-game.heat_relief_audit_logs.requests_*`
WHERE CAST(json_payload.status AS INT64) IN (401, 403)
ORDER BY timestamp DESC
```

---

## SECTION 5 — Checking Everything is Running

---

### Check both services are live

1. Open Terminal
2. Type:
   ```
   gcloud run services list --region=us-central1 --project=heat-relief-game
   ```
3. You should see two rows:
   - `heat-relief` with status `READY`
   - `heat-relief-audit` with status `READY`

---

### Get the game URL

1. In Terminal, type:
   ```
   gcloud run services describe heat-relief \
     --region=us-central1 \
     --project=heat-relief-game \
     --format="value(status.url)"
   ```
2. The URL printed is where the game lives. Share this with users who have been granted access.

---

## SECTION 6 — Rolling Back to a Previous Version

> Use this if a deployment breaks something.

### Step 1 — Find the last working version

1. Open Terminal
2. Type:
   ```
   gcloud run revisions list \
     --service=heat-relief \
     --region=us-central1 \
     --project=heat-relief-game
   ```
3. A list of revisions will print, newest first. Each has a name like `heat-relief-00005-abc`.
4. Identify the revision from before the problem.

### Step 2 — Roll back to it

1. Type the following, replacing `REVISION-NAME` with the name from Step 1:
   ```
   gcloud run services update-traffic heat-relief \
     --to-revisions=REVISION-NAME=100 \
     --region=us-central1 \
     --project=heat-relief-game
   ```
2. 100% of traffic is now sent to the old version. The rollback is immediate.

### Step 3 — Confirm it worked

1. Type:
   ```
   gcloud run services describe heat-relief \
     --region=us-central1 \
     --project=heat-relief-game \
     --format="value(status.traffic)"
   ```
2. Confirm the revision name and 100% traffic shown matches what you rolled back to.

---

## SECTION 7 — Quick Reference

| What you want to do | Where to do it |
|---|---|
| See who has access | Terminal → Section 3 "See who has access" |
| Add a user | Terminal → Section 3 "Give a person access" |
| Remove a user | Terminal → Section 3 "Remove a person's access" |
| View live access logs | Browser → Cloud Logging (Section 4, Method A) |
| Run audit queries | Browser → BigQuery (Section 4, Method B) |
| Deploy an update | Terminal → Section 2 |
| Roll back a bad deploy | Terminal → Section 6 |
| Check services are running | Terminal → Section 5 |
| Get the game URL | Terminal → Section 5 "Get the game URL" |
