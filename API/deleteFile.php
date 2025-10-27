<?php
// delete_file.php

// فقط درخواست POST مجاز است
if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405); // Method Not Allowed
    echo json_encode(["error" => "Method Not Allowed"]);
    exit;
}

// بررسی پارامتر 'file'
if (!isset($_POST['file']) || empty($_POST['file'])) {
    http_response_code(400); // Bad Request
    echo json_encode(["error" => "Missing 'file' parameter"]);
    exit;
}

// مسیر فایل روی سرور
$file = $_POST['file'];

// جلوگیری از دسترسی خارج از پوشه
$baseDir = __DIR__ . "/../Content/Images/"; // مسیر واقعی فایل‌ها
$filePath = realpath($baseDir . basename($file));

if (!$filePath || !file_exists($filePath)) {
    http_response_code(404);
    echo json_encode(["error" => "File not found"]);
    exit;
}

// حذف فایل
if (unlink($filePath)) {
    echo json_encode(["success" => true, "file" => basename($filePath)]);
} else {
    http_response_code(500);
    echo json_encode(["error" => "Failed to delete file"]);
}
?>