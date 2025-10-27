<?php

if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405);
    echo json_encode(['error' => 'Only POST allowed']);
    exit;
}

if (!isset($_FILES['file'])) {
    http_response_code(400);
    echo json_encode(['error' => 'No file sent']);
    exit;
}

$uploadDir = $_SERVER['DOCUMENT_ROOT'] . "/Content/Images/";
$targetFile = $uploadDir . basename($_FILES['file']['name']);

if (move_uploaded_file($_FILES['file']['tmp_name'], $targetFile)) {
    echo json_encode(['success' => true, 'path' => $targetFile]);
} else {
    http_response_code(500);
    echo json_encode(['error' => 'Upload failed']);
}