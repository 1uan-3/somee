# Todo App (HTML + ASP.NET + SQLite) + CI/CD

Repo này là một ví dụ đơn giản để phục vụ bài test Git/GitHub + CI/CD trên hosting Somee (IIS): khi push lên nhánh `main/master` thì GitHub Actions sẽ chạy build/smoke-test và (nếu pass) tự động publish + deploy qua FTP lên hosting.

## Chức năng
- Giao diện HTML/JS ở `wwwroot/index.html` gọi API ASP.NET (`/api/*`)
- Lưu dữ liệu vào SQLite (`App_Data/app.sqlite`)
- CRUD cơ bản: xem danh sách, thêm task, tick hoàn thành, xoá

## Chạy local
Yêu cầu máy có .NET SDK 6.x+.

```bash
dotnet restore
dotnet run
```

Mở URL hiển thị trong console (thường là `http://localhost:5000/` hoặc `https://localhost:5001/`).

## API endpoints
- `GET /api/tasks`
- `POST /api/tasks` (JSON) `{ "title": "..." }`
- `POST /api/tasks/{id}/toggle` (JSON) `{ "is_done": true }`
- `DELETE /api/tasks/{id}`

## CI/CD GitHub Actions
Workflow: `.github/workflows/deploy.yml`

### 1) Thêm GitHub Secrets
Vào **Repo → Settings → Secrets and variables → Actions → New repository secret** và tạo:
- `FTP_SERVER` (vd: `luan13.somee.com`)
- `FTP_USERNAME`
- `FTP_PASSWORD`
- `FTP_SERVER_DIR` (vd: `/www.luan13.somee.com/`)

### 2) Luồng chạy
- Push lên `main/master` → job `ci` chạy `dotnet build` + smoke test `/api/health`
- Nếu CI pass → job `deploy` chạy `dotnet publish` → tạo `publish/build-info.json` → deploy qua FTP

## Lưu ý hosting
- Somee không chạy PHP, nên backend dùng ASP.NET để hoạt động trên IIS.
- SQLite file được tạo ở `App_Data/app.sqlite` → thư mục `App_Data/` cần quyền ghi (thường OK trên hosting).
