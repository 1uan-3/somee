using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

var dataDir = Path.Combine(app.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDir);

var dbPath = Path.Combine(dataDir, "app.sqlite");
var connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();

EnsureDatabase(connectionString);

app.MapGet("/api/health", () => Results.Json(new { ok = true }));

app.MapGet("/api/tasks", () =>
{
    var tasks = new List<TaskItem>();
    using var conn = OpenConnection(connectionString);
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT id, title, is_done, created_at FROM tasks ORDER BY id DESC;";

    using var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
        tasks.Add(new TaskItem(
            id: reader.GetInt64(0),
            title: reader.GetString(1),
            is_done: reader.GetInt64(2) == 1,
            created_at: reader.GetString(3)
        ));
    }

    return Results.Json(new { ok = true, tasks });
});

app.MapPost("/api/tasks", async (HttpRequest request) =>
{
    AddTaskRequest? body;
    try
    {
        body = await request.ReadFromJsonAsync<AddTaskRequest>();
    }
    catch
    {
        return Results.BadRequest(new { ok = false, error = "Body JSON không hợp lệ." });
    }

    var title = (body?.title ?? string.Empty).Trim();
    if (string.IsNullOrWhiteSpace(title))
    {
        return Results.BadRequest(new { ok = false, error = "Thiếu title." });
    }

    if (title.Length > 200)
    {
        return Results.BadRequest(new { ok = false, error = "Tiêu đề quá dài (tối đa 200 ký tự)." });
    }

    using var conn = OpenConnection(connectionString);

    long id;
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "INSERT INTO tasks (title, is_done) VALUES ($title, 0); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$title", title);
        id = (long)(cmd.ExecuteScalar() ?? 0L);
    }

    TaskItem? task;
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "SELECT id, title, is_done, created_at FROM tasks WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return Results.Problem("Không thể đọc task vừa tạo.", statusCode: 500);
        }

        task = new TaskItem(
            id: reader.GetInt64(0),
            title: reader.GetString(1),
            is_done: reader.GetInt64(2) == 1,
            created_at: reader.GetString(3)
        );
    }

    return Results.Json(new { ok = true, task }, statusCode: 201);
});

app.MapPost("/api/tasks/{id:long}/toggle", async (long id, HttpRequest request) =>
{
    ToggleTaskRequest? body;
    try
    {
        body = await request.ReadFromJsonAsync<ToggleTaskRequest>();
    }
    catch
    {
        return Results.BadRequest(new { ok = false, error = "Body JSON không hợp lệ." });
    }

    var isDone = body?.is_done ?? false;

    using var conn = OpenConnection(connectionString);
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "UPDATE tasks SET is_done = $is_done WHERE id = $id;";
    cmd.Parameters.AddWithValue("$is_done", isDone ? 1 : 0);
    cmd.Parameters.AddWithValue("$id", id);
    var rows = cmd.ExecuteNonQuery();
    if (rows == 0)
    {
        return Results.NotFound(new { ok = false, error = "Không tìm thấy task để cập nhật." });
    }

    return Results.Json(new { ok = true });
});

app.MapDelete("/api/tasks/{id:long}", (long id) =>
{
    using var conn = OpenConnection(connectionString);
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "DELETE FROM tasks WHERE id = $id;";
    cmd.Parameters.AddWithValue("$id", id);
    var rows = cmd.ExecuteNonQuery();
    if (rows == 0)
    {
        return Results.NotFound(new { ok = false, error = "Không tìm thấy task để xoá." });
    }

    return Results.Json(new { ok = true });
});

app.MapFallbackToFile("index.html");

app.Run();

static SqliteConnection OpenConnection(string connectionString)
{
    var conn = new SqliteConnection(connectionString);
    conn.Open();
    return conn;
}

static void EnsureDatabase(string connectionString)
{
    using var conn = OpenConnection(connectionString);
    using var cmd = conn.CreateCommand();
    cmd.CommandText = @"CREATE TABLE IF NOT EXISTS tasks (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            title TEXT NOT NULL,
            is_done INTEGER NOT NULL DEFAULT 0,
            created_at TEXT NOT NULL DEFAULT (datetime('now'))
        );";
    cmd.ExecuteNonQuery();
}

record TaskItem(long id, string title, bool is_done, string created_at);

record AddTaskRequest(string? title);

record ToggleTaskRequest(bool is_done);
