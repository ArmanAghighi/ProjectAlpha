This AR project uses WebAR plus an onServer personalized API compiler to create a .mind file to be used and run as PWA webGL unity project.
It has two side:

1.Admin Core which handles server side file control (Add , Edit and Delete) files and targets.

2.AR Core which handles the core project it self.

there must be some initilization on the server to make the project runs correctly :
1.Server root file directory MUST be the same as below:
-public_html
----API
--------deleteFile.php
--------ReadItems.php
--------uploadFile.php
--------uploadJson.php
----MindAR
--------controller-i-djYoaY.js
--------controller-sLTuLJIQ.js
--------index.html
--------mindar-face-aframe.prod.js
--------mindar-face-three.prod.js
--------mindar-face.prod.js
--------mindar-image-aframe.prod.js
--------mindar-image-three.prod.js
--------mindar-image.prod.js
--------ui-2_N98-vS.js
----Content
--------Admin
----------------Admin-Info.json
--------Assets
----------------Pages(must be named automaticly exm. Page_00)
--------Audios(audio files directory)
--------Images(target files)
--------JSON
----------------AssetJson.json
----------------TargetsJson.json
--------Mind
----------------targets.mind
--------PDF
--------Videos

p.n:
Chaging to some other directory with no changes on scriptable object in the project would make the app NOT run as it should.
