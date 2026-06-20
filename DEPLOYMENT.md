# Spoken English App — Deployment & Operations Guide

## Admin Credentials

| Field    | Value             |
|----------|-------------------|
| Email    | Admin@gmail.com   |
| Password | Admin@123         |
| Role     | Admin             |
| URL      | /login then /admin |

> **Change this password immediately in production!** Use `/api/auth/reset-password`.

---

## Architecture

```
[React UI] → [.NET 8 API on port 5101] → [PostgreSQL 16 on port 5432]
```

- **Database**: `spokenenglish` (PostgreSQL 16)
- **API**: .NET 8 Web API + Dapper + Npgsql
- **UI**: React + Vite (built to static files)
- **Auth**: JWT Bearer tokens (15 min) + Refresh tokens (7 days)

---

## Local Development

### Prerequisites
- .NET 8 SDK
- Node.js 20+
- PostgreSQL 16

### API
```bash
cd SpokenEnglishAPI
dotnet run
# Runs on http://localhost:5101
```

### UI
```bash
cd ui   # (SpokenEnglishReact)
npm install
npm run dev
# Runs on http://localhost:5175
# Proxies /api → http://localhost:5101
```

### Database Setup (first time)
```bash
psql -U postgres -c "CREATE DATABASE spokenenglish;"
psql -U postgres -d spokenenglish -f schema.sql
psql -U postgres -d spokenenglish -f lesson_data_full.sql
```

---

## Auto Deploy via GitHub Actions

### 1. Push to GitHub
The API repo is already linked: `https://github.com/avemariyasep8-ux/SpokenEnglishApi`

For the UI, initialize git in the React project:
```bash
cd SpokenEnglishReact
git init
git remote add origin https://github.com/avemariyasep8-ux/SpokenEnglishUi.git
git push -u origin main
```

### 2. Set GitHub Secrets
Go to each repo → Settings → Secrets → Actions:

| Secret Name           | Value                                          |
|-----------------------|------------------------------------------------|
| `DB_CONNECTION_STRING`| `Host=your-db-host;Database=spokenenglish;Username=postgres;Password=yourpassword` |
| `SSH_HOST`            | Your server IP (e.g. `157.245.100.50`)        |
| `SSH_USER`            | `ubuntu` or `root`                            |
| `SSH_PRIVATE_KEY`     | Contents of `~/.ssh/id_rsa` (private key)     |
| `VITE_API_URL`        | `https://your-api-domain.com`                 |

### 3. Trigger Deploy
- **Automatic**: Every push to `main` branch triggers full build + test + deploy
- **Manual**: GitHub → Actions → "Build & Deploy" → Run workflow

---

## Production Server Setup (Ubuntu 22.04)

### 1. Install Prerequisites
```bash
# .NET 8
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update && sudo apt install -y dotnet-runtime-8.0

# Nginx
sudo apt install -y nginx

# PostgreSQL 16
sudo apt install -y postgresql-16
```

### 2. API Systemd Service
Create `/etc/systemd/system/spokenenglish-api.service`:
```ini
[Unit]
Description=Spoken English API
After=network.target postgresql.service

[Service]
WorkingDirectory=/opt/spokenenglish/api
ExecStart=/usr/bin/dotnet SpokenEnglishAPI.dll
Restart=always
RestartSec=10
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5101
EnvironmentFile=/opt/spokenenglish/api/.env

[Install]
WantedBy=multi-user.target
```

Create `/opt/spokenenglish/api/.env`:
```
ConnectionStrings__DefaultConnection=Host=localhost;Database=spokenenglish;Username=postgres;Password=CHANGE_ME
JwtSettings__SecretKey=CHANGE_ME_TO_A_LONG_RANDOM_STRING_MIN_32_CHARS
JwtSettings__Issuer=SpokenEnglishApp
JwtSettings__Audience=SpokenEnglishUsers
```

```bash
sudo systemctl enable spokenenglish-api
sudo systemctl start spokenenglish-api
```

### 3. Nginx Config
Create `/etc/nginx/sites-available/spokenenglish`:
```nginx
server {
    listen 80;
    server_name yourdomain.com;

    # React UI (static files)
    root /var/www/spokenenglish;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    # API proxy
    location /api/ {
        proxy_pass http://localhost:5101;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }
}
```

```bash
sudo ln -s /etc/nginx/sites-available/spokenenglish /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx
```

### 4. SSL (HTTPS) — Free with Let's Encrypt
```bash
sudo apt install -y certbot python3-certbot-nginx
sudo certbot --nginx -d yourdomain.com
```

---

## Subscription Plans

| Plan    | Price  | Duration | Access                |
|---------|--------|----------|-----------------------|
| Free    | ₹0     | Forever  | Lessons 1–3 only      |
| Monthly | ₹199   | 1 month  | All 15 lessons        |
| Yearly  | ₹999   | 12 months| All 15 lessons + 17%  |
| 2-Year  | ₹1499  | 24 months| All 15 lessons + 37%  |

---

## Security Checklist

- [x] JWT tokens expire in 15 minutes
- [x] Refresh tokens expire in 7 days
- [x] All admin endpoints require `[Authorize(Roles = "Admin")]`
- [x] Passwords hashed with BCrypt (cost 11)
- [x] CORS restricted to known origins
- [x] CSV import validates all fields before insert
- [ ] **TODO**: Enable HTTPS in production (certbot above)
- [ ] **TODO**: Change default admin password
- [ ] **TODO**: Restrict PostgreSQL to localhost only
- [ ] **TODO**: Set up database backups (daily pg_dump)

---

## Database Backup

```bash
# Daily backup cron
0 2 * * * pg_dump -U postgres spokenenglish | gzip > /backups/spokenenglish-$(date +%Y%m%d).sql.gz

# Restore
gunzip < backup.sql.gz | psql -U postgres spokenenglish
```

---

## Admin Features

| Feature               | URL                | Description                        |
|-----------------------|--------------------|------------------------------------|
| Admin Dashboard       | `/admin`           | Stats, user management, export     |
| Add Full Lesson       | `/admin/add-lesson`| 5-step lesson creator              |
| Bulk Upload           | `/admin/bulk`      | Excel import for MCQ/Arrange/Reading|
| Export Lessons CSV    | `/api/admin/export/lessons`    | Download all lessons      |
| Export Word Content   | `/api/admin/export/wordcontent`| Download definitions      |
| Export MCQ            | `/api/admin/export/mcq`        | Download all questions    |
| Import Word Content   | `/api/admin/import/wordcontent`| Upload CSV to bulk add   |

---

## Run Unit Tests

```bash
cd SpokenEnglishAPI.Tests
dotnet test
# 22/22 tests should pass
```

---

## Support

- GitHub API repo: https://github.com/avemariyasep8-ux/SpokenEnglishApi
- Admin email: Admin@gmail.com
- Tech stack: .NET 8 + React + PostgreSQL 16
