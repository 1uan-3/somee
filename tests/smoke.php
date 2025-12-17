<?php
declare(strict_types=1);

putenv('APP_DB_DSN=sqlite::memory:');

require __DIR__ . '/../db.php';

function fail(string $message): void
{
    fwrite(STDERR, $message . PHP_EOL);
    exit(1);
}

try {
    $pdo = app_db();

    $stmt = $pdo->prepare('INSERT INTO tasks (title, is_done) VALUES (:title, :is_done)');
    $stmt->execute([':title' => 'Smoke test', ':is_done' => 0]);

    $row = $pdo->query('SELECT title, is_done FROM tasks LIMIT 1')->fetch();
    if (!is_array($row)) {
        fail('No row returned.');
    }
    if (($row['title'] ?? null) !== 'Smoke test') {
        fail('Unexpected title.');
    }
    if ((int)($row['is_done'] ?? -1) !== 0) {
        fail('Unexpected is_done.');
    }

    echo "OK\n";
} catch (Throwable $e) {
    fail('Exception: ' . $e->getMessage());
}

