# COM3D2.Scriplay.Plugin

## --- How to Instal ---

### -- plugin isntall

- Sybaris\UnityInjector

### -- csv isntall

- Sybaris\UnityInjector\Config\Scriplay\csv

### -- scripts isntall

- Sybaris\UnityInjector\Config\Scriplay\scripts

## --- 기능 사항 ---

### -- @wait 명령어에 랜덤 지연시간 추가

- @wait 0.0 2.5
- @wait '기본 지연 시간' '추가 랜덤 지연 시간'

### -- @motion 명령어 사용시

- COM3D2\PhotoModeData\MyPose 폴더의 anm 파일을 참조하도록 변경
- 게임내의 anm 파일은 참조 안함
- 파일명 형식 아래같이 둘다 사용 가능. anm 확장자가 자동으로 붇어서 처리됨. (원래 있던 기능) <br>
@motion name=test_00017500<br>
@motion name=test_00017500.anm<br>
