<?php
// upload.php
// این فایل روی سرور توی پوشه Content/JSON قرار می‌گیره
// مثال: https://armanitproject.ir/Content/JSON/upload.php

// فقط درخواست POST اجازه داده شده
if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405); // Method Not Allowed
    echo json_encode(['error' => 'Only POST requests are allowed']);
    exit;
}

// دریافت محتوای خام JSON
$jsonContent = file_get_contents('php://input');

if (empty($jsonContent)) {
    http_response_code(400);
    echo json_encode(['error' => 'Empty JSON content']);
    exit;
}

// مسیر فایل هدف روی سرور
$targetFile = $_SERVER['DOCUMENT_ROOT'] . '/Content/JSON/TargetsJson.json';

try {
    file_put_contents($targetFile, $jsonContent);
    echo json_encode(['success' => true, 'message' => 'JSON uploaded successfully']);
} catch (Exception $e) {
    http_response_code(500);
    echo json_encode(['error' => 'Failed to write file', 'details' => $e->getMessage()]);
}