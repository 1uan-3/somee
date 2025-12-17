<?php
declare(strict_types=1);

require __DIR__ . '/db.php';

header('Content-Type: application/json; charset=utf-8');

function json_response(array $payload, int $status = 200): void
{
    http_response_code($status);
    echo json_encode($payload, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
    exit;
}

function bad_request(string $message, array $extra = []): void
{
    json_response(['ok' => false, 'error' => $message] + $extra, 400);
}

function method_not_allowed(string $message = 'Method not allowed'): void
{
    json_response(['ok' => false, 'error' => $message], 405);
}

function require_post(): void
{
    $method = $_SERVER['REQUEST_METHOD'] ?? 'GET';
    if (strtoupper($method) !== 'POST') {
        method_not_allowed('Chỉ hỗ trợ POST cho thao tác này.');
    }
}

function int_param(array $source, string $key): int
{
    if (!isset($source[$key])) {
        bad_request("Thiếu tham số: {$key}");
    }
    if (!is_numeric($source[$key])) {
        bad_request("Tham số không hợp lệ: {$key}");
    }
    return (int)$source[$key];
}

function str_param(array $source, string $key): string
{
    if (!isset($source[$key])) {
        bad_request("Thiếu tham số: {$key}");
    }
    $value = trim((string)$source[$key]);
    if ($value === '') {
        bad_request("Tham số rỗng: {$key}");
    }
    return $value;
}

try {
    $pdo = app_db();
    $action = $_GET['action'] ?? '';

    switch ($action) {
        case 'list': {
            $stmt = $pdo->query('SELECT id, title, is_done, created_at FROM tasks ORDER BY id DESC');
            $tasks = $stmt->fetchAll();
            json_response(['ok' => true, 'tasks' => $tasks]);
        }

        case 'add': {
            require_post();
            $title = str_param($_POST, 'title');
            $len = function_exists('mb_strlen') ? mb_strlen($title) : strlen($title);
            if ($len > 200) {
                bad_request('Tiêu đề quá dài (tối đa 200 ký tự).');
            }

            $stmt = $pdo->prepare('INSERT INTO tasks (title, is_done) VALUES (:title, 0)');
            $stmt->execute([':title' => $title]);

            $id = (int)$pdo->lastInsertId();
            $taskStmt = $pdo->prepare('SELECT id, title, is_done, created_at FROM tasks WHERE id = :id');
            $taskStmt->execute([':id' => $id]);
            $task = $taskStmt->fetch();

            json_response(['ok' => true, 'task' => $task], 201);
        }

        case 'toggle': {
            require_post();
            $id = int_param($_POST, 'id');
            $isDone = int_param($_POST, 'is_done');
            $isDone = $isDone ? 1 : 0;

            $stmt = $pdo->prepare('UPDATE tasks SET is_done = :is_done WHERE id = :id');
            $stmt->execute([':is_done' => $isDone, ':id' => $id]);
            if ($stmt->rowCount() === 0) {
                bad_request('Không tìm thấy task để cập nhật.');
            }

            json_response(['ok' => true]);
        }

        case 'delete': {
            require_post();
            $id = int_param($_POST, 'id');

            $stmt = $pdo->prepare('DELETE FROM tasks WHERE id = :id');
            $stmt->execute([':id' => $id]);
            if ($stmt->rowCount() === 0) {
                bad_request('Không tìm thấy task để xoá.');
            }

            json_response(['ok' => true]);
        }

        default:
            bad_request('Action không hợp lệ. Dùng: list, add, toggle, delete.');
    }
} catch (Throwable $e) {
    $payload = ['ok' => false, 'error' => 'Lỗi server.'];
    if (getenv('APP_DEBUG')) {
        $payload['detail'] = $e->getMessage();
    }
    json_response($payload, 500);
}
