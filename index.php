<?php
declare(strict_types=1);

// Some hosts ưu tiên index.php hơn index.html. File này đảm bảo luôn hiển thị frontend.
readfile(__DIR__ . '/index.html');
