<?php
header("Content-Type: application/json; charset=UTF-8");

$dir = __DIR__ . "/Content/Images";  // مسیر پوشه عکس‌ها روی هاست
$baseUrl = "https://" . $_SERVER['HTTP_HOST'] . "/Content/Images"; // آدرس اینترنتی عکس‌ها

$files = array();
if (is_dir($dir)) {
    if ($dh = opendir($dir)) {
        while (($file = readdir($dh)) !== false) {
            if ($file != "." && $file != ".." && !is_dir($dir . "/" . $file)) {
                $files[] = array(
                    "fileName" => $file,
                    "url" => $baseUrl . "/" . $file
                );
            }
        }
        closedir($dh);
    }
}

echo json_encode(array(
    "success" => true,
    "images" => $files
));
