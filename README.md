# Pipeline — Job Search Command Center

> The modern, high-performance command center for engineering job seekers. Manage applications, contacts, interview prep, resumes, tasks, offers, analytics, and offline PWA resilience in one unified workspace.

[![CI](https://github.com/pipeline/pipeline/actions/workflows/ci.yml/badge.svg)](https://github.com/pipeline/pipeline/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![React 18](https://img.shields.io/badge/React-18.3-cyan.svg)](https://react.dev/)

---

## 1. Quick Start (3 Commands)

Get Pipeline running locally with PostgreSQL, MinIO S3 object storage, Mailpit SMTP testing, .NET API backend, and Vite frontend:

```bash
# 1. Clone repository & configure environment
git clone https://github.com/pipeline/pipeline.git && cd Pipeline
cp .env.example .env

# 2. Start full-stack Docker environment
docker compose up -d

# 3. Open Command Center in your browser
# Web App:  http://localhost:5173
# API Docs: http://localhost:5000/swagger
# Mailpit:  http://localhost:8025
# MinIO:    http://localhost:9001 (minio / minioadmin)
```

---

## 2. Key Architecture & Features

- **Application Pipeline Kanban & Table:** Drag-and-drop status stages (`Wishlist`, `Applied`, `Screening`, `Interview`, `Offer`, `Accepted`, `Rejected`, `Ghosted`), custom stage tracking, stale application inactivity alerts, and RFC 4180 CSV import/export.
- **Networking & Warmth Intelligence:** Contact relationship scoring (`1-5` warmth rating), touchpoint timeline logging, and automated outreach reminders.
- **Interview Command Center & Question Bank:** Technical, behavioral, and system design interview scheduling, real-time prep checklists, STAR framework question debriefs, and ICS calendar feeds.
- **Document Management & CV Tailoring:** S3 object storage with presigned URLs, document versioning, and conversion analytics tracking which resume version drove the highest interview yield.
- **Next-Action Automation Engine & Email Templates:** Automated deliverable scheduling (post-interview thank you within 24h, stale follow-up at 7 days, offer decision deadlines) with variable-substituted email templates.
- **References & Consent Enforcement:** Professional references directory with consent status (`NotAsked`, `Asked`, `Agreed`, `Declined`) and automated warning banners preventing premature submission.
- **Offer Comparison Matrix:** Side-by-side total compensation evaluation (base salary, equity/options, sign-on bonus, benefits stipends, commuting score, and offer deadlines).
- **Analytics & Calendar Engine:** Funnel conversion rate calculations, weekly application velocity charts, stage duration analysis, calendar views (month, week, agenda), and live ICS calendar subscription token.
- **Data Portability & GDPR Compliance:** Article 20 JSON data archive export, cascading account deletion cascade, and one-click demo data seeding.
- **Command Palette & PWA:** Quick-jump modal (`Ctrl+K`/`Cmd+K`), installable Progressive Web App, and offline resilience banner.
- **Job Discovery Extension Point:** Pluggable `IJobSourceConnector` interface for aggregating external job opportunities into your pipeline.

---

## 3. Environment Variables Reference

| Variable | Default Value | Description |
|---|---|---|
| `DOMAIN` | `localhost` | Production domain used for TLS certificates (Caddy) |
| `POSTGRES_USER` | `pipeline` | Database superuser username |
| `POSTGRES_PASSWORD` | `change-me` | Database password |
| `POSTGRES_DB` | `pipeline` | PostgreSQL database name |
| `ConnectionStrings__Default` | `Host=db;Database=pipeline;...` | ADO.NET connection string (supports Postgres & SQLite) |
| `Jwt__Secret` | `change-me-32+chars...` | Symmetric HMAC-SHA256 signing key (min 32 characters) |
| `Jwt__Issuer` | `pipeline` | JWT issuer token claim |
| `Jwt__Audience` | `pipeline-web` | JWT audience token claim |
| `Frontend__Origin` | `http://localhost:5173` | Allowed CORS origin for frontend single page app |
| `Storage__Provider` | `S3` | File storage provider (`S3` or `Local`) |
| `Storage__Endpoint` | `http://minio:9000` | S3-compatible service URL (MinIO, AWS S3, Cloudflare R2) |
| `Storage__Bucket` | `pipeline` | S3 target bucket name |
| `Storage__AccessKey` | `minio` | S3 access key ID |
| `Storage__SecretKey` | `change-me` | S3 secret access key |
| `Smtp__Host` | `mailpit` | SMTP server hostname |
| `Smtp__Port` | `1025` | SMTP port (1025 for Mailpit, 587 for TLS) |
| `AUTO_MIGRATE` | `true` | Apply database migrations automatically on startup |
| `SEED_DEMO_DATA` | `false` | Pre-populate demo user with complete portfolio on startup |

---

## 4. Production VPS Deployment Guide

Deploy Pipeline to an Ubuntu VPS (Hetzner, DigitalOcean, Linode) with automated HTTPS via Caddy:

### Step 1: Server Setup
```bash
# Update and install Docker
sudo apt update && sudo apt upgrade -y
sudo apt install -y curl git docker.io docker-compose-plugin
sudo systemctl enable --now docker
```

### Step 2: Clone & Configure
```bash
git clone https://github.com/pipeline/pipeline.git /opt/pipeline
cd /opt/pipeline

# Copy production env file and fill secrets
cp .env.example .env
nano .env
# Set DOMAIN=pipeline.yourdomain.com
# Set secure POSTGRES_PASSWORD, Jwt__Secret, and Storage credentials
```

### Step 3: Launch Production Stack
```bash
docker compose -f docker-compose.prod.yml up -d --build
```
Caddy will automatically provision and renew a free Let's Encrypt TLS certificate for your domain.

---

## 5. Managed Cloud Deployment

### Azure Container Apps / AWS ECS
1. Build and push containers to GitHub Container Registry (GHCR):
   - `ghcr.io/pipeline/backend:latest`
   - `ghcr.io/pipeline/frontend:latest`
2. Provision managed PostgreSQL (e.g. Azure Flexible Server or AWS RDS).
3. Provision managed Object Storage (AWS S3 or Azure Blob Storage).
4. Deploy API container with `ConnectionStrings__Default` and `Storage__Endpoint` environment variables.

---

## 6. Database Backup & Restore Runbook

### Automated Daily Backup via `pg_dump`
Add a daily cron job on the host machine:
```bash
# Edit crontab
crontab -e

# Run daily backup at 03:00 AM and keep 14 days of retention
0 3 * * * docker exec pipeline-db pg_dump -U pipeline pipeline | gzip > /opt/backups/pipeline_$(date +\%Y\%m\%d).sql.gz && find /opt/backups -name "pipeline_*.sql.gz" -mtime +14 -delete
```

### Disaster Recovery Restore
```bash
# Decompress and restore into database
gunzip < /opt/backups/pipeline_20260930.sql.gz | docker exec -i pipeline-db psql -U pipeline -d pipeline
```

---

## 7. Extending Job Discovery

To add an external aggregator, RSS feed, or ATS scraper (Greenhouse, Lever, LinkedIn, Indeed):
1. Implement `IJobSourceConnector` in `backend/src/Pipeline.Infrastructure/Services/`:
   ```csharp
   public class MyJobConnector : IJobSourceConnector {
       public string SourceKey => "my-connector";
       public async Task<IReadOnlyList<RawJob>> FetchAsync(JobSource source, CancellationToken ct) { ... }
   }
   ```
2. Register in Dependency Injection: `builder.Services.AddScoped<IJobSourceConnector, MyJobConnector>();`.
3. Detailed architectural spec and code walkthrough in [docs/discovery.md](docs/discovery.md).

---

## 8. Development & Testing

```bash
# Backend unit & integration tests
dotnet test backend/Pipeline.sln

# Frontend tests & production build check
cd frontend
npm test
npm run build
```

---

## 9. License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
