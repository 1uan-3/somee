# Todo App (HTML + PHP + SQLite) + CI/CD

Repo này là một ví dụ đơn giản để phục vụ bài test Git/GitHub + CI/CD: khi push lên nhánh `main/master` thì GitHub Actions sẽ chạy lint/test và (nếu pass) tự động deploy qua FTP lên hosting.

## Chức năng
- Giao diện HTML/JS (`index.html`) gọi API PHP (`api.php`)
- Lưu dữ liệu vào SQLite (`data/app.sqlite`) bằng PDO
- CRUD cơ bản: xem danh sách, thêm task, tick hoàn thành, xoá

## Chạy local
Yêu cầu máy có PHP 8.x.

```bash
php -S 127.0.0.1:8000
```

Mở `http://127.0.0.1:8000/`.

## API endpoints
- `GET api.php?action=list`
- `POST api.php?action=add` với form field `title`
- `POST api.php?action=toggle` với `id`, `is_done` (0/1)
- `POST api.php?action=delete` với `id`

## CI/CD GitHub Actions
Workflow: `.github/workflows/deploy.yml`

### 1) Thêm GitHub Secrets
Vào **Repo → Settings → Secrets and variables → Actions → New repository secret** và tạo:
- `FTP_SERVER` (vd: `luan13.somee.com`)
- `FTP_USERNAME`
- `FTP_PASSWORD`
- `FTP_SERVER_DIR` (vd: `/www.luan13.somee.com/`)

### 2) Luồng chạy
- Push lên `main/master` → job `ci` chạy `php -l` + `php tests/smoke.php`
- Nếu CI pass → job `deploy` tạo `build-info.json` và deploy qua FTP

## Lưu ý hosting
- SQLite file được tạo ở `data/app.sqlite` → thư mục `data/` cần quyền ghi.
- Repo có sẵn `data/web.config` và `data/.htaccess` để hạn chế tải trực tiếp file database.

