%
(GojoCAD G-code import sample: square and arc corners)
(Units: millimeters. Coordinates are absolute.)
G21
G90
F900

(Stroke 1: outer path with square left corners and rounded right corners)
T1 M6
G0 Z5
G0 X0 Y0
G1 Z-1
G1 X70 Y0
G3 X82 Y12 I0 J12
G1 X82 Y48
G3 X70 Y60 I-12 J0
G1 X0 Y60
G1 X0 Y0
G0 Z5

(Stroke 2: inner square with hard line-line corners)
T2 M6
G0 X20 Y18
G1 Z-1
G1 X50 Y18
G1 X50 Y42
G1 X20 Y42
G1 X20 Y18
G0 Z5

M30
%
